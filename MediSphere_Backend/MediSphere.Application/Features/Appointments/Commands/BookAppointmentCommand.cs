using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using MediSphere.Application.DTOs.Appointment;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Enums;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MediSphere.Application.Features.Appointments.Commands;

public class BookAppointmentCommand : IRequest<AppointmentDto>
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsFollowUp { get; set; }
    public int? PreviousAppointmentId { get; set; }
    public bool UseRewardPoints { get; set; }
    public string? ReferralCode { get; set; }
}

public class BookAppointmentCommandHandler : IRequestHandler<BookAppointmentCommand, AppointmentDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly IPaymentService _paymentService;
    private readonly IEmailSmsService _emailSms;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<BookAppointmentCommandHandler> _logger;

    public BookAppointmentCommandHandler(
        IUnitOfWork unitOfWork,
        ICacheService cache,
        IPaymentService paymentService,
        IEmailSmsService emailSms,
        IBackgroundTaskQueue backgroundTaskQueue,
        IServiceScopeFactory scopeFactory,
        IMapper mapper,
        ILogger<BookAppointmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _paymentService = paymentService;
        _emailSms = emailSms;
        _backgroundTaskQueue = backgroundTaskQueue;
        _scopeFactory = scopeFactory;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppointmentDto> Handle(BookAppointmentCommand request, CancellationToken cancellationToken)
    {
        var lockKey = $"appointment:lock:{request.DoctorId}:{request.AppointmentDate:yyyyMMdd}:{request.StartTime}";

        // Distributed booking lock
        try
        {
            if (await _cache.ExistsAsync(lockKey))
            {
                throw new InvalidOperationException("This slot is temporarily locked by another patient. Please try again.");
            }
            await _cache.SetAsync(lockKey, true, TimeSpan.FromMinutes(2));
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogWarning(ex, "Redis distributed lock check failed, proceeding without lock.");
        }

        try
        {
            // Verify doctor & check conflicts
            var doctor = await _unitOfWork.Repository<Doctor>().Query()
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.Id == request.DoctorId, cancellationToken)
                ?? throw new KeyNotFoundException("Doctor not found.");

            var conflict = await _unitOfWork.Repository<Appointment>().Query()
                .AnyAsync(a => a.DoctorId == request.DoctorId &&
                               a.AppointmentDate.Date == request.AppointmentDate.Date &&
                               a.StartTime == request.StartTime &&
                               a.Status != AppointmentStatus.Cancelled, cancellationToken);

            if (conflict)
            {
                throw new InvalidOperationException("The requested appointment slot is already booked.");
            }

            var patient = await _unitOfWork.Repository<Patient>()
                .GetByIdAsync(request.PatientId)
                ?? throw new KeyNotFoundException("Patient not found.");

            // Calculate fees & rewards logic
            decimal fee = doctor.ConsultationFee;
            decimal discount = 0;

            if (request.UseRewardPoints && patient.RewardPoints > 0)
            {
                // 1 point = 1 INR, capped at 50% of the consultation fee
                decimal maxDiscount = fee * 0.5m;
                int pointsToRedeem = (int)Math.Min(patient.RewardPoints, maxDiscount);

                if (pointsToRedeem > 0)
                {
                    discount = pointsToRedeem;
                    patient.RewardPoints -= pointsToRedeem;

                    var rewardLog = new PatientRewardLog
                    {
                        PatientId = patient.Id,
                        Points = -pointsToRedeem,
                        Action = "Redemption",
                        Description = $"Redeemed {pointsToRedeem} points for 50% appointment discount.",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Repository<PatientRewardLog>().AddAsync(rewardLog);
                }
            }

            decimal finalFee = fee - discount;

            // Generate referral code for patient if none exists
            if (string.IsNullOrWhiteSpace(patient.ReferralCode))
            {
                string initials = patient.FirstName.Length >= 4 
                    ? patient.FirstName[..4].ToUpper() 
                    : patient.FirstName.ToUpper();
                patient.ReferralCode = $"{initials}{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
            }

            // If finalFee == 0, we can credit booking/loyalty and referral points immediately
            if (finalFee == 0)
            {
                // Earn booking loyalty points (10 points per booking)
                patient.RewardPoints += 10;
                var bookingRewardLog = new PatientRewardLog
                {
                    PatientId = patient.Id,
                    Points = 10,
                    Action = "Loyalty Booking",
                    Description = "Earned 10 points for booking appointment.",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<PatientRewardLog>().AddAsync(bookingRewardLog);

                // Handle referral code (first appointment welcome bonuses)
                var existingBookingsCount = await _unitOfWork.Repository<Appointment>().Query()
                    .CountAsync(a => a.PatientId == patient.Id, cancellationToken);

                if (existingBookingsCount == 0 && !string.IsNullOrWhiteSpace(patient.ReferredByCode))
                {
                    var referrer = await _unitOfWork.Repository<Patient>().Query()
                        .FirstOrDefaultAsync(p => p.ReferralCode == patient.ReferredByCode, cancellationToken);

                    if (referrer != null)
                    {
                        // Credit referrer 100 points
                        referrer.RewardPoints += 100;
                        var referrerRewardLog = new PatientRewardLog
                        {
                            PatientId = referrer.Id,
                            Points = 100,
                            Action = "Referral Bonus",
                            Description = $"Earned 100 points for referring {patient.FirstName} {patient.LastName}.",
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.Repository<PatientRewardLog>().AddAsync(referrerRewardLog);
                        await _unitOfWork.Repository<Patient>().UpdateAsync(referrer);

                        // Credit referee patient 50 welcome points
                        patient.RewardPoints += 50;
                        var refereeRewardLog = new PatientRewardLog
                        {
                            PatientId = patient.Id,
                            Points = 50,
                            Action = "Welcome Bonus",
                            Description = "Earned 50 points welcome bonus for using referral code.",
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.Repository<PatientRewardLog>().AddAsync(refereeRewardLog);
                    }
                }
            }

            await _unitOfWork.Repository<Patient>().UpdateAsync(patient);

            // Allocate Queue Token number
            var departmentDailyBookingsCount = await _unitOfWork.Repository<Appointment>().Query()
                .CountAsync(a => a.DoctorId == request.DoctorId && 
                                 a.AppointmentDate.Date == request.AppointmentDate.Date && 
                                 a.Status != AppointmentStatus.Cancelled, cancellationToken);
            int nextQueueToken = departmentDailyBookingsCount + 1;

            // Setup WebRTC DTLS-SRTP P2P Telemedicine room parameters
            string teleMeetingId = Guid.NewGuid().ToString("N");
            string meetingUrl = $"https://meet.jit.si/medisphere-{teleMeetingId}";

            // Generate Razorpay checkout order if fee is required
            string paymentStatus = finalFee > 0 ? "PendingPayment" : "Paid";
            AppointmentStatus appointmentStatus = finalFee > 0 ? AppointmentStatus.PendingPayment : AppointmentStatus.Confirmed;

            // Create Appointment
            var appointment = new Appointment
            {
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                AppointmentDate = request.AppointmentDate,
                StartTime = request.StartTime,
                EndTime = request.StartTime.Add(TimeSpan.FromMinutes(30)),
                Reason = request.Reason,
                IsFollowUp = request.IsFollowUp,
                PreviousAppointmentId = request.PreviousAppointmentId,
                Fee = finalFee,
                Status = appointmentStatus,
                TelemedicineMeetingId = teleMeetingId,
                MeetingUrl = meetingUrl,
                QueueToken = nextQueueToken,
                QueueStatus = "Waiting",
                PaymentStatus = paymentStatus,
                RazorpayOrderId = string.Empty
            };

            await _unitOfWork.Repository<Appointment>().AddAsync(appointment);
            await _unitOfWork.SaveChangesAsync();

            if (finalFee > 0)
            {
                try
                {
                    string razorpayOrderId = await _paymentService.CreateOrderAsync(appointment.Id, finalFee);
                    appointment.RazorpayOrderId = razorpayOrderId;
                    await _unitOfWork.Repository<Appointment>().UpdateAsync(appointment);
                    await _unitOfWork.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Razorpay checkout order generation failed for Appointment {AptId}. Booking will continue as payment pending.", appointment.Id);
                }
            }

            // Fire transactional confirmations via Brevo Notification Services only if finalFee == 0
            if (finalFee == 0)
            {
                string patientName = $"{patient.FirstName} {patient.LastName}";
                string doctorName = $"{doctor.FirstName} {doctor.LastName}";
                string specialty = doctor.Specialty;
                string dateStr = request.AppointmentDate.ToString("yyyy-MM-dd");
                string timeStr = request.StartTime.ToString(@"hh\:mm");

                string mailBody = MediSphere.Application.Common.EmailTemplates.BuildAppointmentConfirmationEmail(
                    patientName, doctorName, specialty, dateStr, timeStr, nextQueueToken, finalFee, meetingUrl);

                await _emailSms.SendEmailAsync(patient.Email, "Appointment Booking Confirmation", mailBody);
                await _emailSms.SendSmsAsync(patient.PhoneNumber, $"MediSphere: Booking Confirmed! Token #{nextQueueToken} on {request.AppointmentDate:yyyy-MM-dd} at {request.StartTime}. Telemedicine Room: {meetingUrl}");

                var patientId = patient.Id;
                var doctorId = doctor.Id;
                var departmentId = doctor.DepartmentId;
                var doctorFirstName = doctor.FirstName;

                await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async ct =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    var signalR = scope.ServiceProvider.GetRequiredService<ISignalRNotificationService>();

                    string patientFullName = $"{patient.FirstName} {patient.LastName}";

                    await notifications.SendToRoleReferenceAsync(
                        UserRole.Patient,
                        patientId,
                        "Appointment Booked",
                        $"Your booking with Dr. {doctorFirstName} (Token #{nextQueueToken}) has been scheduled.",
                        "Booking");

                    await notifications.SendToRoleReferenceAsync(
                        UserRole.Doctor,
                        doctorId,
                        "Appointment Booked",
                        $"New appointment booked by {patientFullName}.",
                        "Booking");

                    await signalR.NotifyQueueUpdateAsync(doctorId, departmentId);
                    await signalR.NotifyQueuePositionChangedAsync(patientId, nextQueueToken, "Waiting");
                });
            }

            // Fetch and map to return dto
            var createdAppointment = await _unitOfWork.Repository<Appointment>().Query()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Department)
                .FirstOrDefaultAsync(a => a.Id == appointment.Id, cancellationToken);

            return _mapper.Map<AppointmentDto>(createdAppointment);
        }
        finally
        {
            try
            {
                await _cache.RemoveAsync(lockKey);
            }
            catch
            {
                // Ignore redis cleanup errors
            }
        }
    }
}
