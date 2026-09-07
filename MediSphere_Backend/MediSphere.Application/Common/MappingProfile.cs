
using AutoMapper;
using MediSphere.Application.DTOs.Appointment;
using MediSphere.Application.DTOs.Patient;
using MediSphere.Domain.Entities;

namespace MediSphere.Application.Common;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Patient, PatientProfileDto>();

CreateMap<PatientProfileDto, Patient>();
        CreateMap<Appointment, AppointmentDto>()
            .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => $"{src.Patient.FirstName} {src.Patient.LastName}"))
            .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => $"Dr. {src.Doctor.FirstName} {src.Doctor.LastName}"))
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Doctor.Department != null ? src.Doctor.Department.Name : string.Empty))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<PatientRewardLog, PatientRewardLogDto>();
        CreateMap<PaymentTransaction, PaymentTransactionDto>();
    }
}
