using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MediSphere.Application.DTOs.Patient;
using MediSphere.Application.DTOs.Common;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediSphere.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RewardsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<RewardsController> _logger;

    public RewardsController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<RewardsController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet("my-statement")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ApiResponse<PatientRewardStatementDto>>> GetMyRewardStatement()
    {
        try
        {
            var refId = int.Parse(User.FindFirst("referenceId")?.Value ?? "0");
            
            var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(refId);
            if (patient == null)
            {
                return NotFound(ApiResponse<PatientRewardStatementDto>.Fail("Patient not found."));
            }

            var logs = await _unitOfWork.Repository<PatientRewardLog>().Query()
                .Where(l => l.PatientId == refId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var logsDto = _mapper.Map<IEnumerable<PatientRewardLogDto>>(logs);

            var statement = new PatientRewardStatementDto
            {
                CurrentPoints = patient.RewardPoints,
                ReferralCode = patient.ReferralCode,
                TransactionHistory = logsDto
            };

            return Ok(ApiResponse<PatientRewardStatementDto>.Ok(statement));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch reward statement.");
            return StatusCode(500, ApiResponse<PatientRewardStatementDto>.Fail($"Rewards statement error: {ex.Message}"));
        }
    }

    [HttpGet("patient/{patientId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PatientRewardStatementDto>>> GetPatientRewardStatement(int patientId)
    {
        try
        {
            var patient = await _unitOfWork.Repository<Patient>().GetByIdAsync(patientId);
            if (patient == null)
            {
                return NotFound(ApiResponse<PatientRewardStatementDto>.Fail("Patient not found."));
            }

            var logs = await _unitOfWork.Repository<PatientRewardLog>().Query()
                .Where(l => l.PatientId == patientId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var logsDto = _mapper.Map<IEnumerable<PatientRewardLogDto>>(logs);

            var statement = new PatientRewardStatementDto
            {
                CurrentPoints = patient.RewardPoints,
                ReferralCode = patient.ReferralCode,
                TransactionHistory = logsDto
            };

            return Ok(ApiResponse<PatientRewardStatementDto>.Ok(statement));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch reward statement.");
            return StatusCode(500, ApiResponse<PatientRewardStatementDto>.Fail($"Rewards statement error: {ex.Message}"));
        }
    }

    [HttpGet("rules")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<IDictionary<string, object>>> GetRewardRules()
    {
        var rules = new Dictionary<string, object>
        {
            { "PointsEarnedPerBooking", 10 },
            { "PointsEarnedPerReferral", 100 },
            { "WelcomeBonusPoints", 50 },
            { "PointValueInINR", 1 }, // 1 Point = 1 INR
            { "MaxBookingDiscountPercentage", 50 } // Max discount is 50% of Doctor fee
        };

        return Ok(ApiResponse<IDictionary<string, object>>.Ok(rules));
    }
}

public class PatientRewardStatementDto
{
    public int CurrentPoints { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public IEnumerable<PatientRewardLogDto> TransactionHistory { get; set; } = Enumerable.Empty<PatientRewardLogDto>();
}
