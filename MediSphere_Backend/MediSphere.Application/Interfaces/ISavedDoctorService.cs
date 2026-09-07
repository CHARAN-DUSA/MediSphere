using System.Collections.Generic;
using System.Threading.Tasks;
using MediSphere.Application.DTOs.Doctor;

namespace MediSphere.Application.Interfaces;

public interface ISavedDoctorService
{
    Task SaveDoctorAsync(int patientId, int doctorId);
    Task RemoveSavedDoctorAsync(int patientId, int doctorId);
    Task<IEnumerable<DoctorDto>> GetSavedDoctorsAsync(int patientId);
    Task<IEnumerable<DoctorDto>> SearchSavedDoctorsAsync(int patientId, string search);
}
