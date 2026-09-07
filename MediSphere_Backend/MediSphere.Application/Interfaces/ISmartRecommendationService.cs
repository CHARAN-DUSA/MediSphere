using System.Collections.Generic;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Doctor;

namespace MediSphere.Application.Interfaces;

/// <summary>
/// Interface for doctor recommendation engine.
/// Note: This is currently a rule-based clinical matching engine (symptom matching + multi-criteria ranking),
/// but is abstractly decoupled to support future machine learning integrations (e.g. LLMs, PyTorch or ML.NET NLP models).
/// </summary>
public interface ISmartRecommendationService
{
    Task<IEnumerable<DoctorDto>> RecommendDoctorsAsync(string symptoms);
}
