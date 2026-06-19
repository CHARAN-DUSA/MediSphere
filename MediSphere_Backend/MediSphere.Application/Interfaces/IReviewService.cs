using System.Collections.Generic;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Review;

namespace MediSphere.Application.Interfaces;

public interface IReviewService
{
    Task<ReviewDto> CreateReviewAsync(int patientId, CreateReviewDto dto);
    Task<IEnumerable<ReviewDto>> GetDoctorReviewsAsync(int doctorId);
    Task<IEnumerable<ReviewDto>> GetPendingReviewsAsync();
    Task ModerateReviewAsync(int reviewId, string status);
    Task RespondToReviewAsync(int reviewId, int doctorId, string responseText);
}
