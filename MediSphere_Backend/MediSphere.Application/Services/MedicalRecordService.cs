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

    private static MedicalRecordDto MapToDto(MedicalRecord r) => new()
    {
        Id = r.Id, PatientId = r.PatientId, AppointmentId = r.AppointmentId,
        FileName = r.FileName, FileUrl = r.FileUrl, FileType = r.FileType,
        FileSizeBytes = r.FileSizeBytes, Description = r.Description, UploadedAt = r.UploadedAt
    };
}
