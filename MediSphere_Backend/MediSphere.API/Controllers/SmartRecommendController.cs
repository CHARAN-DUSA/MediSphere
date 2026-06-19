using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using MediSphere.Application.DTOs.Doctor;
using MediSphere.Application.DTOs.Common;
using MediSphere.Application.Features.Doctors.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MediSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SmartRecommendController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SmartRecommendController> _logger;

    public SmartRecommendController(IMediator mediator, ILogger<SmartRecommendController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<DoctorDto>>>> GetRecommendations([FromQuery] string symptoms)
    {
        _logger.LogInformation("SmartRecommend controller requested for symptoms: {Symptoms}", symptoms);

        try
        {
            var query = new GetSmartRecommendationsQuery(symptoms);
            var result = await _mediator.Send(query);

            return Ok(ApiResponse<IEnumerable<DoctorDto>>.Ok(result, "Recommended doctors retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute recommendations.");
            return StatusCode(500, ApiResponse<IEnumerable<DoctorDto>>.Fail($"Recommendation engine failed: {ex.Message}"));
        }
    }
}
