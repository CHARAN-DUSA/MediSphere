using System;

namespace MediSphere.Application.DTOs.Review;

public class ReviewDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DoctorResponse { get; set; } = string.Empty;
    public DateTime? ResponseCreatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewDto
{
    public int DoctorId { get; set; }
    public int AppointmentId { get; set; }
    public int Rating { get; set; } // 1 to 5
    public string Comment { get; set; } = string.Empty;
}
