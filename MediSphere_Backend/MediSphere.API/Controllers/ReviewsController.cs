using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Common;
using MediSphere.Application.DTOs.Review;
using MediSphere.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService) => _reviewService = reviewService;

    [Authorize(Roles = "Patient")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReviewDto>>> CreateReview([FromBody] CreateReviewDto dto)
    {
        var refId = int.Parse(User.FindFirst("referenceId")?.Value ?? "0");
        var result = await _reviewService.CreateReviewAsync(refId, dto);
        return Ok(ApiResponse<ReviewDto>.Ok(result, "Review submitted and is pending moderation."));
    }

    [HttpGet("doctor/{doctorId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ReviewDto>>>> GetDoctorReviews(int doctorId)
    {
        var result = await _reviewService.GetDoctorReviewsAsync(doctorId);
        return Ok(ApiResponse<IEnumerable<ReviewDto>>.Ok(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ReviewDto>>>> GetPendingReviews()
    {
        var result = await _reviewService.GetPendingReviewsAsync();
        return Ok(ApiResponse<IEnumerable<ReviewDto>>.Ok(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/moderate")]
    public async Task<ActionResult<ApiResponse<object>>> ModerateReview(int id, [FromBody] ModerateReviewRequest req)
    {
        await _reviewService.ModerateReviewAsync(id, req.Status);
        return Ok(ApiResponse<object>.Ok(null!, $"Review status updated to {req.Status}."));
    }

    [Authorize(Roles = "Doctor")]
    [HttpPost("{id}/respond")]
    public async Task<ActionResult<ApiResponse<object>>> RespondToReview(int id, [FromBody] RespondToReviewRequest req)
    {
        var docId = int.Parse(User.FindFirst("referenceId")?.Value ?? "0");
        await _reviewService.RespondToReviewAsync(id, docId, req.ResponseText);
        return Ok(ApiResponse<object>.Ok(null!, "Response submitted successfully."));
    }
}

public record ModerateReviewRequest(string Status);
public record RespondToReviewRequest(string ResponseText);
