using System;

namespace MediSphere.Application.DTOs.Doctor;

public class DoctorEarningsDto
{
    public decimal TotalGrossEarnings { get; set; }
    public decimal TotalNetEarnings { get; set; }
    public decimal TotalPlatformFeesPaid { get; set; }
    public decimal TotalTaxesPaid { get; set; }
    public decimal TotalAdminCommissionPaid { get; set; }
    public int PaidAppointmentsCount { get; set; }
}
