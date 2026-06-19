using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Doctor;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediSphere.Application.Services;

/// <summary>
/// Rule-based Clinical Matching Engine (deterministic MCDA).
/// Filters doctors to symptom-matching candidates first, then scores and ranks only that set.
/// </summary>
public class SmartRecommendationService : ISmartRecommendationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SmartRecommendationService> _logger;

    private static readonly Dictionary<string, string[]> SymptomMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Cardiology", new[] { "chest pain", "heart", "palpitations", "arrhythmia", "cardiac", "shortness of breath", "bp", "blood pressure", "hypertension" } },
        { "Dermatology", new[] { "skin", "rash", "acne", "itching", "eczema", "mole", "allergy", "hair loss", "nail" } },
        { "Pediatrics", new[] { "child", "baby", "infant", "pediatric", "immunization", "growth", "autism", "kid", "kids", "child health" } },
        { "Orthopedics", new[] { "bone", "fracture", "joint", "back pain", "knee", "spine", "muscle pain", "arthritis", "sprain" } },
        { "Gynecology", new[] { "pregnancy", "gynecology", "menstrual", "period", "uterus", "contraception", "maternity", "women" } },
        { "Neurology", new[] { "brain", "neurology", "headache", "migraine", "seizure", "stroke", "paralysis", "nerve", "tremor" } },
        { "General Medicine", new[] { "fever", "cough", "cold", "flu", "stomach ache", "diarrhea", "vomiting", "weakness" } }
    };

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "from", "have", "has", "been", "that", "this", "your", "my", "day", "days"
    };

    public SmartRecommendationService(IUnitOfWork unitOfWork, ILogger<SmartRecommendationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<DoctorDto>> RecommendDoctorsAsync(string symptoms)
    {
        _logger.LogInformation("Processing smart recommendation request for symptoms: {Symptoms}", symptoms);

        if (string.IsNullOrWhiteSpace(symptoms))
        {
            return Enumerable.Empty<DoctorDto>();
        }

        symptoms = symptoms.Trim();

        var matchedSpecialties = ResolveMatchedSpecialties(symptoms);
        _logger.LogInformation("Matched specialties: {Specialties}", string.Join(", ", matchedSpecialties));

        var allDoctors = await _unitOfWork.Repository<Doctor>().Query()
            .Include(d => d.Department)
            .Where(d => d.IsActive && d.IsApproved)
            .ToListAsync();

        var eligibleDoctors = FilterEligibleDoctors(allDoctors, symptoms, matchedSpecialties);

        if (eligibleDoctors.Count == 0)
        {
            _logger.LogInformation("No doctors matched the supplied symptom criteria.");
            return Enumerable.Empty<DoctorDto>();
        }

        var scoredDoctors = eligibleDoctors.Select(doctor =>
        {
            double score = 0;

            var isSpecialtyMatch = matchedSpecialties.Count == 0 || matchedSpecialties.Any(s =>
                DoctorMatchesSpecialty(doctor, s));

            if (isSpecialtyMatch)
            {
                score += 100;
            }

            score += Math.Min(doctor.ExperienceYears * 2.0, 30.0);
            score += (double)doctor.AverageRating * 10.0;

            if (doctor.IsAvailable)
            {
                score += 20;
            }

            if (doctor.RatingCount > 0)
            {
                score += Math.Min(Math.Log(doctor.RatingCount + 1) * 2.5, 10.0);
            }

            return new { Doctor = doctor, Score = score };
        });

        return scoredDoctors
            .OrderByDescending(x => x.Score)
            .Select(x => MapToDto(x.Doctor));
    }

    private static HashSet<string> ResolveMatchedSpecialties(string symptoms)
    {
        var matchedSpecialties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in SymptomMap)
        {
            foreach (var keyword in mapping.Value)
            {
                if (symptoms.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    matchedSpecialties.Add(mapping.Key);
                }
            }
        }

        return matchedSpecialties;
    }

    private static List<Doctor> FilterEligibleDoctors(
        IReadOnlyList<Doctor> doctors,
        string symptoms,
        HashSet<string> matchedSpecialties)
    {
        if (matchedSpecialties.Count > 0)
        {
            return doctors
                .Where(d => matchedSpecialties.Any(s => DoctorMatchesSpecialty(d, s)))
                .ToList();
        }

        var tokens = ExtractSearchTokens(symptoms);
        if (tokens.Count == 0)
        {
            return new List<Doctor>();
        }

        return doctors
            .Where(d => tokens.Any(token => DoctorMatchesToken(d, token)))
            .ToList();
    }

    private static bool DoctorMatchesSpecialty(Doctor doctor, string specialty)
    {
        return doctor.Specialty.Contains(specialty, StringComparison.OrdinalIgnoreCase)
            || (doctor.Department != null && doctor.Department.Name.Contains(specialty, StringComparison.OrdinalIgnoreCase));
    }

    private static bool DoctorMatchesToken(Doctor doctor, string token)
    {
        return doctor.Specialty.Contains(token, StringComparison.OrdinalIgnoreCase)
            || (doctor.Department?.Name.Contains(token, StringComparison.OrdinalIgnoreCase) ?? false)
            || doctor.FirstName.Contains(token, StringComparison.OrdinalIgnoreCase)
            || doctor.LastName.Contains(token, StringComparison.OrdinalIgnoreCase)
            || doctor.Bio.Contains(token, StringComparison.OrdinalIgnoreCase)
            || doctor.Qualification.Contains(token, StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> ExtractSearchTokens(string symptoms)
    {
        return symptoms
            .Split(new[] { ',', '.', ';', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => t.Length >= 3 && !StopWords.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static DoctorDto MapToDto(Doctor doctor) => new()
    {
        Id = doctor.Id,
        FirstName = doctor.FirstName,
        LastName = doctor.LastName,
        Email = doctor.Email,
        PhoneNumber = doctor.PhoneNumber,
        Specialty = doctor.Specialty,
        Qualification = doctor.Qualification,
        ExperienceYears = doctor.ExperienceYears,
        ConsultationFee = doctor.ConsultationFee,
        ProfileImageUrl = doctor.ProfileImageUrl,
        Bio = doctor.Bio,
        IsAvailable = doctor.IsAvailable,
        IsApproved = doctor.IsApproved,
        Gender = doctor.Gender,
        Location = doctor.Location,
        LanguagesSpoken = doctor.LanguagesSpoken,
        AverageRating = doctor.AverageRating,
        RatingCount = doctor.RatingCount,
        DepartmentId = doctor.DepartmentId,
        DepartmentName = doctor.Department?.Name ?? string.Empty
    };
}
