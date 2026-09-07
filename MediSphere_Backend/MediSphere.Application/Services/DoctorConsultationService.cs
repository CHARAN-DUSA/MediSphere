using MediSphere.Application.DTOs.Appointment;
using MediSphere.Application.DTOs.Consultation;
using MediSphere.Application.DTOs.MedicalRecord;
using MediSphere.Application.DTOs.Prescription;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediSphere.Application.Services;

public class DoctorConsultationService : IDoctorConsultationService
{
    private readonly IUnitOfWork _unitOfWork;

    public DoctorConsultationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DoctorConsultationDto> GetConsultationAsync(
        int doctorId,
        int appointmentId)
    {
        /*
         * IMPORTANT SECURITY CHECK
         *
         * We do NOT trust the patient ID supplied by the frontend.
         *
         * The appointment must belong to the logged-in doctor.
         */
        var appointment = await _unitOfWork
            .Repository<Appointment>()
            .Query()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
                .ThenInclude(d => d.Department)
            .FirstOrDefaultAsync(a =>
                a.Id == appointmentId &&
                a.DoctorId == doctorId);

        if (appointment == null)
        {
            throw new UnauthorizedAccessException(
                "You are not authorized to access this consultation.");
        }

        var patientId = appointment.PatientId;

        /*
         * ---------------------------------------------------------
         * PATIENT SUMMARY
         * ---------------------------------------------------------
         */

        var patient = appointment.Patient;

        var patientDto = new DoctorPatientSummaryDto
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            Email = patient.Email,
            PhoneNumber = patient.PhoneNumber,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Address = patient.Address,
            BloodGroup = patient.BloodGroup,
            MedicalHistory = patient.MedicalHistory
        };

        /*
         * ---------------------------------------------------------
         * CURRENT APPOINTMENT
         * ---------------------------------------------------------
         */

        var appointmentDto = new AppointmentDto
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName =
                $"{patient.FirstName} {patient.LastName}".Trim(),

            DoctorId = appointment.DoctorId,
            DoctorName =
                $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}"
                    .Trim(),

            DepartmentName =
                appointment.Doctor.Department?.Name ?? string.Empty,

            AppointmentDate = appointment.AppointmentDate,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            Status = appointment.Status.ToString(),
            Reason = appointment.Reason,
            Notes = appointment.Notes,
            IsFollowUp = appointment.IsFollowUp,
            Fee = appointment.Fee,
            TelemedicineMeetingId =
                appointment.TelemedicineMeetingId,
            MeetingUrl = appointment.MeetingUrl,
            QueueToken = appointment.QueueToken,
            QueueStatus = appointment.QueueStatus,
            PaymentStatus = appointment.PaymentStatus,
            RazorpayOrderId = appointment.RazorpayOrderId,
            CreatedAt = appointment.CreatedAt
        };

        /*
         * ---------------------------------------------------------
         * MEDICAL RECORDS
         *
         * These are intentionally loaded here only after we have
         * verified that the current doctor owns the appointment.
         * ---------------------------------------------------------
         */

        var records = await _unitOfWork
            .Repository<MedicalRecord>()
            .Query()
            .Where(r =>
                r.PatientId == patientId &&
                r.AppointmentId == appointmentId)
            .OrderByDescending(r => r.UploadedAt)
            .ToListAsync();

        var medicalRecords = records.Select(r => new MedicalRecordDto
        {
            Id = r.Id,
            PatientId = r.PatientId,
            AppointmentId = r.AppointmentId,
            FileName = r.FileName,
            FileUrl = r.FileUrl,
            FileType = r.FileType,
            FileSizeBytes = r.FileSizeBytes,
            Description = r.Description,
            UploadedAt = r.UploadedAt
        }).ToList();

        /*
         * ---------------------------------------------------------
         * PREVIOUS CONSULTATIONS
         *
         * Only appointments between this doctor and this patient.
         * ---------------------------------------------------------
         */

        var previousAppointments = await _unitOfWork
            .Repository<Appointment>()
            .Query()
            .Include(a => a.Doctor)
                .ThenInclude(d => d.Department)
            .Where(a =>
                a.PatientId == patientId &&
                a.DoctorId == doctorId &&
                a.Id != appointmentId)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.StartTime)
            .ToListAsync();

        var previousConsultations =
            previousAppointments.Select(a => new AppointmentDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName =
                    $"{patient.FirstName} {patient.LastName}".Trim(),

                DoctorId = a.DoctorId,
                DoctorName =
                    $"{a.Doctor.FirstName} {a.Doctor.LastName}".Trim(),

                DepartmentName =
                    a.Doctor.Department?.Name ?? string.Empty,

                AppointmentDate = a.AppointmentDate,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Status = a.Status.ToString(),
                Reason = a.Reason,
                Notes = a.Notes,
                IsFollowUp = a.IsFollowUp,
                Fee = a.Fee,
                TelemedicineMeetingId =
                    a.TelemedicineMeetingId,
                MeetingUrl = a.MeetingUrl,
                QueueToken = a.QueueToken,
                QueueStatus = a.QueueStatus,
                PaymentStatus = a.PaymentStatus,
                RazorpayOrderId = a.RazorpayOrderId,
                CreatedAt = a.CreatedAt
            }).ToList();

        /*
         * ---------------------------------------------------------
         * PRESCRIPTION HISTORY
         *
         * Doctor can see previous prescriptions whenever there is
         * an appointment relationship with this patient.
         * ---------------------------------------------------------
         */

        var prescriptions = await _unitOfWork
            .Repository<Prescription>()
            .Query()
            .Include(p => p.Doctor)
            .Include(p => p.Appointment)
            .Include(p => p.Medicines)
            .Where(p =>
                p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var previousPrescriptions =
            prescriptions.Select(p => new PrescriptionDto
            {
                Id = p.Id,
                PatientId = p.PatientId,
                PatientName =
                    $"{patient.FirstName} {patient.LastName}".Trim(),

                DoctorId = p.DoctorId,
                DoctorName =
                    $"{p.Doctor.FirstName} {p.Doctor.LastName}".Trim(),

                AppointmentId = p.AppointmentId,
                AppointmentDate = p.Appointment.AppointmentDate,

                Diagnosis = p.Diagnosis,
                ClinicalNotes = p.ClinicalNotes,
                Instructions = p.Instructions,
                FollowUpDate = p.FollowUpDate,
                CreatedAt = p.CreatedAt,

                Medicines = p.Medicines
                    .Select(m => new PrescriptionMedicineDto
                    {
                        Id = m.Id,
                        MedicineName = m.MedicineName,
                        Dosage = m.Dosage,
                        Frequency = m.Frequency,
                        Duration = m.Duration,
                        Route = m.Route,
                        Instructions = m.Instructions
                    })
                    .ToList()
            }).ToList();

        return new DoctorConsultationDto
        {
            Appointment = appointmentDto,
            Patient = patientDto,
            MedicalRecords = medicalRecords,
            PreviousConsultations = previousConsultations,
            PreviousPrescriptions = previousPrescriptions
        };
    }
}