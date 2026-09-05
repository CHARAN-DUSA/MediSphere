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

    public async Task<IEnumerable<MedicalRecordDto>> GetPatientRecordsAsync(int patientId)
    {
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

        // Doctor can access a record ONLY through their own appointment.
        else if (string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase))
        {
            if (!appointmentId.HasValue)
            {
                throw new UnauthorizedAccessException(
                    "Medical records can only be accessed during a consultation.");
            }

            // Record must belong to the consultation.
            if (record.AppointmentId != appointmentId.Value)
            {
                throw new UnauthorizedAccessException(
                    "This medical record is not attached to this consultation.");
            }

            // Appointment must belong to this doctor and patient.
            var appointment = await _unitOfWork.Repository<Appointment>()
                .Query()
                .FirstOrDefaultAsync(a =>
                    a.Id == appointmentId.Value &&
                    a.DoctorId == userId &&
                    a.PatientId == record.PatientId);

            if (appointment == null)
            {
                throw new UnauthorizedAccessException(
                    "You are not authorized to access this medical record.");
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
