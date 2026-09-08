using MediSphere.Application.DTOs.MedicalRecord;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediSphere.Application.Services;

public class MedicalRecordService : IMedicalRecordService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;

    public MedicalRecordService(IUnitOfWork unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
    }

    public async Task<IEnumerable<MedicalRecordDto>> GetPatientRecordsAsync(int patientId, int requestingUserId, string role)
    {
        // A patient may only list their own records.
        if (string.Equals(role, "Patient", StringComparison.OrdinalIgnoreCase))
        {
            if (requestingUserId != patientId)
                throw new UnauthorizedAccessException("You are not authorized to view these records.");
        }
        // A doctor may list a patient's records only if they have a legitimate
        // appointment relationship with that patient (any appointment, not just
        // a currently-open one).
        else if (string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase))
        {
            var hasRelationship = await _unitOfWork.Repository<Appointment>()
                .Query()
                .AnyAsync(a => a.DoctorId == requestingUserId && a.PatientId == patientId);

            if (!hasRelationship)
                throw new UnauthorizedAccessException("You are not authorized to view this patient's records.");
        }
        else if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Access denied.");
        }

        var records = await _unitOfWork.Repository<MedicalRecord>().Query()
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.UploadedAt)
            .ToListAsync();
        return records.Select(MapToDto);
    }

    public async Task<MedicalRecordDto> UploadRecordAsync(int patientId, int? appointmentId, Stream fileStream, string fileName, string description)
    {
        var allowedTypes = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
        var ext = Path.GetExtension(fileName).ToLower();
        if (!allowedTypes.Contains(ext))
            throw new ArgumentException("File type not allowed.");

        var url = await _fileStorage.UploadAsync(fileStream, fileName, "medical-records");
        var record = new MedicalRecord
        {
            PatientId = patientId,
            AppointmentId = appointmentId,
            FileName = fileName,
            FileUrl = url,
            FileType = ext,
            FileSizeBytes = fileStream.Length,
            Description = description
        };
        await _unitOfWork.Repository<MedicalRecord>().AddAsync(record);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(record);
    }

    public async Task DeleteRecordAsync(int id, int patientId)
    {
        var record = await _unitOfWork.Repository<MedicalRecord>().GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Record not found.");
        if (record.PatientId != patientId)
            throw new UnauthorizedAccessException("Access denied.");
        await _fileStorage.DeleteAsync(record.FileUrl);
        await _unitOfWork.Repository<MedicalRecord>().DeleteAsync(record);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<(Stream Stream, string ContentType, string FileName)?> GetRecordFileAsync(
    int id,
    int userId,
    string role,
    int? appointmentId = null)
    {
        var record = await _unitOfWork.Repository<MedicalRecord>()
            .Query()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record == null)
        {
            return null;
        }

        // Patient can access their own records.
        if (string.Equals(role, "Patient", StringComparison.OrdinalIgnoreCase))
        {
            if (record.PatientId != userId)
            {
                throw new UnauthorizedAccessException("Access denied.");
            }
        }

        // Doctor can access ANY of the patient's records as long as the doctor
        // has a legitimate appointment relationship with that patient — the
        // record does NOT need to belong to the doctor's current appointment.
        // (appointmentId is accepted for backward compatibility but is no
        // longer required or checked here.)
        else if (string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase))
        {
            var hasRelationship = await _unitOfWork.Repository<Appointment>()
                .Query()
                .AnyAsync(a =>
                    a.DoctorId == userId &&
                    a.PatientId == record.PatientId);

            if (!hasRelationship)
            {
                throw new UnauthorizedAccessException(
                    "You are not authorized to access this patient's medical records.");
            }
        }

        // Admin remains allowed.
        else if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Access denied.");
        }

        var fileResult = await _fileStorage.GetFileAsync(record.FileUrl);

        if (fileResult == null)
        {
            return null;
        }

        return (
            fileResult.Value.Stream,
            fileResult.Value.ContentType,
            record.FileName
        );
    }
    private static MedicalRecordDto MapToDto(MedicalRecord r) => new()
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
    };
    
}