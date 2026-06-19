using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Review;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Enums;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediSphere.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public ReviewService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<ReviewDto> CreateReviewAsync(int patientId, CreateReviewDto dto)
    {
        // Verify appointment exists, belongs to patient, and is completed
        var appointment = await _unitOfWork.Repository<Appointment>().GetByIdAsync(dto.AppointmentId)
            ?? throw new KeyNotFoundException($"Appointment {dto.AppointmentId} not found.");

        if (appointment.PatientId != patientId)
            throw new UnauthorizedAccessException("You cannot review an appointment that is not yours.");

        if (appointment.Status != AppointmentStatus.Completed)
            throw new InvalidOperationException("You can only review completed appointments.");

        // Check for duplicate reviews
        var existing = await _unitOfWork.Repository<DoctorReview>().Query()
            .AnyAsync(r => r.AppointmentId == dto.AppointmentId);
        if (existing)
            throw new InvalidOperationException("You have already submitted a review for this appointment.");

        var review = new DoctorReview
        {
            PatientId = patientId,
            DoctorId = dto.DoctorId,
            AppointmentId = dto.AppointmentId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            Status = "Pending" // Starts as pending moderation
        };

        var created = await _unitOfWork.Repository<DoctorReview>().AddAsync(review);
        await _unitOfWork.SaveChangesAsync();

        // Get patient and doctor names for the DTO
        var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(patientId);
        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(dto.DoctorId);
        var patientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "A patient";

        await _notificationService.SendToRoleReferenceAsync(
            UserRole.Doctor,
            dto.DoctorId,
            "New Review Submitted",
            $"You received a new review from {patientName}.",
            "Review");

        return new ReviewDto
        {
            Id = created.Id,
            PatientId = patientId,
            PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : string.Empty,
            DoctorId = dto.DoctorId,
            DoctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}" : string.Empty,
            AppointmentId = dto.AppointmentId,
            Rating = created.Rating,
            Comment = created.Comment,
            Status = created.Status,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<IEnumerable<ReviewDto>> GetDoctorReviewsAsync(int doctorId)
    {
        var reviews = await _unitOfWork.Repository<DoctorReview>().Query()
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .Where(r => r.DoctorId == doctorId && r.Status == "Approved")
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            PatientId = r.PatientId,
            PatientName = $"{r.Patient.FirstName} {r.Patient.LastName}",
            DoctorId = r.DoctorId,
            DoctorName = $"{r.Doctor.FirstName} {r.Doctor.LastName}",
            AppointmentId = r.AppointmentId,
            Rating = r.Rating,
            Comment = r.Comment,
            Status = r.Status,
            DoctorResponse = r.DoctorResponse,
            ResponseCreatedAt = r.ResponseCreatedAt,
            CreatedAt = r.CreatedAt
        });
    }

    public async Task<IEnumerable<ReviewDto>> GetPendingReviewsAsync()
    {
        var reviews = await _unitOfWork.Repository<DoctorReview>().Query()
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .Where(r => r.Status == "Pending")
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            PatientId = r.PatientId,
            PatientName = $"{r.Patient.FirstName} {r.Patient.LastName}",
            DoctorId = r.DoctorId,
            DoctorName = $"{r.Doctor.FirstName} {r.Doctor.LastName}",
            AppointmentId = r.AppointmentId,
            Rating = r.Rating,
            Comment = r.Comment,
            Status = r.Status,
            DoctorResponse = r.DoctorResponse,
            ResponseCreatedAt = r.ResponseCreatedAt,
            CreatedAt = r.CreatedAt
        });
    }

    public async Task ModerateReviewAsync(int reviewId, string status)
    {
        if (status != "Approved" && status != "Rejected")
            throw new ArgumentException("Invalid moderation status. Must be 'Approved' or 'Rejected'.");

        var review = await _unitOfWork.Repository<DoctorReview>().GetByIdAsync(reviewId)
            ?? throw new KeyNotFoundException($"Review {reviewId} not found.");

        review.Status = status;
        review.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<DoctorReview>().UpdateAsync(review);
        await _unitOfWork.SaveChangesAsync();

        // Recalculate doctor ratings
        await RecalculateDoctorRatingAsync(review.DoctorId);
    }

    public async Task RespondToReviewAsync(int reviewId, int doctorId, string responseText)
    {
        var review = await _unitOfWork.Repository<DoctorReview>().GetByIdAsync(reviewId)
            ?? throw new KeyNotFoundException($"Review {reviewId} not found.");

        if (review.DoctorId != doctorId)
            throw new UnauthorizedAccessException("You can only respond to reviews left for you.");

        review.DoctorResponse = responseText;
        review.ResponseCreatedAt = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<DoctorReview>().UpdateAsync(review);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task RecalculateDoctorRatingAsync(int doctorId)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().GetByIdAsync(doctorId);
        if (doctor == null) return;

        var approvedReviews = await _unitOfWork.Repository<DoctorReview>().Query()
            .Where(r => r.DoctorId == doctorId && r.Status == "Approved")
            .ToListAsync();

        if (approvedReviews.Any())
        {
            doctor.AverageRating = (decimal)approvedReviews.Average(r => r.Rating);
            doctor.RatingCount = approvedReviews.Count;
        }
        else
        {
            doctor.AverageRating = 0;
            doctor.RatingCount = 0;
        }

        await _unitOfWork.Repository<Doctor>().UpdateAsync(doctor);
        await _unitOfWork.SaveChangesAsync();
    }
}
