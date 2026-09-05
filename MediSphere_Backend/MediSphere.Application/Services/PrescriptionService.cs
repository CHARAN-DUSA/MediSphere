using MediSphere.Application.DTOs.Prescription;
using MediSphere.Application.Interfaces;
using MediSphere.Domain.Entities;
using MediSphere.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using MediSphere.Domain.Enums;

namespace MediSphere.Application.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly IUnitOfWork _unitOfWork;

    public PrescriptionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PrescriptionDto> CreateAsync(
        int doctorId,
        CreatePrescriptionDto dto)
    {
        var appointment = await _unitOfWork
            .Repository<Appointment>()
            .Query()
            .FirstOrDefaultAsync(a =>
                a.Id == dto.AppointmentId &&
                a.DoctorId == doctorId);

        if (appointment == null)
            throw new UnauthorizedAccessException(
                "You are not authorized for this appointment.");

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Cannot create a prescription for a cancelled appointment.");
        }

        var prescription = new Prescription
        {
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            AppointmentId = appointment.Id,
            Diagnosis = dto.Diagnosis?.Trim() ?? string.Empty,
            ClinicalNotes = dto.ClinicalNotes?.Trim() ?? string.Empty,
            Instructions = dto.Instructions?.Trim() ?? string.Empty,
            FollowUpDate = dto.FollowUpDate
        };

        foreach (var medicine in dto.Medicines)
        {
            if (string.IsNullOrWhiteSpace(medicine.MedicineName))
                continue;

            prescription.Medicines.Add(
                new PrescriptionMedicine
                {
                    MedicineName = medicine.MedicineName.Trim(),
                    Dosage = medicine.Dosage?.Trim() ?? string.Empty,
                    Frequency = medicine.Frequency?.Trim() ?? string.Empty,
                    Duration = medicine.Duration?.Trim() ?? string.Empty,
                    Route = medicine.Route?.Trim() ?? string.Empty,
                    Instructions =
                        medicine.Instructions?.Trim() ?? string.Empty
                });
        }

        if (!prescription.Medicines.Any())
        {
            throw new ArgumentException(
                "At least one medicine is required.");
        }

        await _unitOfWork
            .Repository<Prescription>()
            .AddAsync(prescription);

        await _unitOfWork.SaveChangesAsync();

        return await GetEntityDtoAsync(prescription.Id)
            ?? throw new InvalidOperationException(
                "Prescription was created but could not be loaded.");
    }

    public async Task<IEnumerable<PrescriptionDto>>
        GetPatientHistoryAsync(int patientId)
    {
        var prescriptions = await _unitOfWork
            .Repository<Prescription>()
            .Query()
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Appointment)
            .Include(p => p.Medicines)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return prescriptions.Select(MapToDto);
    }

    public async Task<IEnumerable<PrescriptionDto>>
        GetDoctorPatientHistoryAsync(
            int doctorId,
            int patientId)
    {
        var hasRelationship = await _unitOfWork
            .Repository<Appointment>()
            .Query()
            .AnyAsync(a =>
                a.DoctorId == doctorId &&
                a.PatientId == patientId);

        if (!hasRelationship)
        {
            throw new UnauthorizedAccessException(
                "You are not authorized to view this patient's prescriptions.");
        }

        var prescriptions = await _unitOfWork
            .Repository<Prescription>()
            .Query()
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Appointment)
            .Include(p => p.Medicines)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return prescriptions.Select(MapToDto);
    }

   public async Task<PrescriptionDto?> GetByAppointmentAsync(
    int appointmentId,
    int userId,
    string role)
{
    var prescription = await _unitOfWork
        .Repository<Prescription>()
        .Query()
        .Include(p => p.Patient)
        .Include(p => p.Doctor)
        .Include(p => p.Appointment)
        .Include(p => p.Medicines)
        .FirstOrDefaultAsync(
            p => p.AppointmentId == appointmentId);

    if (prescription == null)
        return null;

    if (string.Equals(
        role,
        "Patient",
        StringComparison.OrdinalIgnoreCase))
    {
        if (prescription.PatientId != userId)
            throw new UnauthorizedAccessException(
                "You are not authorized to access this prescription.");
    }
    else if (string.Equals(
        role,
        "Doctor",
        StringComparison.OrdinalIgnoreCase))
    {
        if (prescription.DoctorId != userId)
            throw new UnauthorizedAccessException(
                "You are not authorized to access this prescription.");
    }
    else if (!string.Equals(
        role,
        "Admin",
        StringComparison.OrdinalIgnoreCase))
    {
        throw new UnauthorizedAccessException(
            "Access denied.");
    }

    return MapToDto(prescription);
}

private async Task<PrescriptionDto?> GetEntityDtoAsync(int id)
    {
        var prescription = await _unitOfWork
            .Repository<Prescription>()
            .Query()
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Appointment)
            .Include(p => p.Medicines)
            .FirstOrDefaultAsync(p => p.Id == id);

        return prescription == null
            ? null
            : MapToDto(prescription);
    }

    private static PrescriptionDto MapToDto(
        Prescription p)
    {
        return new PrescriptionDto
        {
            Id = p.Id,
            PatientId = p.PatientId,
            PatientName =
                $"{p.Patient.FirstName} {p.Patient.LastName}".Trim(),

            DoctorId = p.DoctorId,
            DoctorName =
                $"{p.Doctor.FirstName} {p.Doctor.LastName}".Trim(),

            AppointmentId = p.AppointmentId,
            AppointmentDate = p.Appointment.AppointmentDate,

            Diagnosis = p.Diagnosis,
            ClinicalNotes = p.ClinicalNotes,
            Instructions = p.Instructions,
            FollowUpDate = p.FollowUpDate,
            CreatedAt = p.CreatedAt,

            Medicines = p.Medicines
                .OrderBy(m => m.Id)
                .Select(m => new PrescriptionMedicineDto
                {
                    Id = m.Id,
                    MedicineName = m.MedicineName,
                    Dosage = m.Dosage,
                    Frequency = m.Frequency,
                    Duration = m.Duration,
                    Route = m.Route,
                    Instructions = m.Instructions
                })
                .ToList()
        };
    }
}