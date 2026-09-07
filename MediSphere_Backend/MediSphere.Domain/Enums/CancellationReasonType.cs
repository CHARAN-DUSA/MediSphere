namespace MediSphere.Domain.Enums;

public enum CancellationReasonType
{
    PatientRequest = 1,
    DoctorUnavailable = 2,
    ScheduleConflict = 3,
    MedicalEmergency = 4,
    WeatherOrDisaster = 5,
    AdministrativeAction = 6,
    PaymentIssue = 7,
    Other = 99
}
