using System.Threading.Tasks;

namespace MediSphere.Application.Interfaces;

public interface IAccountDeletionService
{
    Task DeletePatientAccountAsync(int patientId, string reason = "User requested account deletion");
    Task DeleteDoctorAccountAsync(int doctorId, string reason = "User requested account deletion");
}
