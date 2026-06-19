using MediSphere.Domain.Entities;
using MediSphere.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MediSphere.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        Microsoft.Extensions.Configuration.IConfiguration config) : base(options)
    {
        _config = config;
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<DoctorReview> DoctorReviews => Set<DoctorReview>();
    public DbSet<FavoriteDoctor> FavoriteDoctors => Set<FavoriteDoctor>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<PatientRewardLog> PatientRewardLogs => Set<PatientRewardLog>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Patient).WithMany(p => p.Appointments).HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Doctor).WithMany(d => d.Appointments).HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Doctor>()
            .HasOne(d => d.Department).WithMany(dep => dep.Doctors).HasForeignKey(d => d.DepartmentId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DoctorReview>()
            .HasOne(r => r.Patient).WithMany().HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DoctorReview>()
            .HasOne(r => r.Doctor).WithMany().HasForeignKey(r => r.DoctorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DoctorReview>()
            .HasOne(r => r.Appointment).WithMany().HasForeignKey(r => r.AppointmentId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FavoriteDoctor>()
            .HasOne(f => f.Patient).WithMany().HasForeignKey(f => f.PatientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FavoriteDoctor>()
            .HasOne(f => f.Doctor).WithMany().HasForeignKey(f => f.DoctorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FavoriteDoctor>()
            .HasIndex(f => new { f.PatientId, f.DoctorId }).IsUnique();

        modelBuilder.Entity<Appointment>()
            .Property(a => a.Fee).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Doctor>()
            .Property(d => d.ConsultationFee).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<Doctor>()
            .Property(d => d.AverageRating).HasColumnType("decimal(3,2)");

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Doctor>()
            .HasIndex(d => d.Email).IsUnique();
        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.Email).IsUnique();

        modelBuilder.Entity<Department>().HasData(
    new Department { Id = 1, Name = "Cardiology", Description = "Heart & cardiovascular care", IconUrl = "favorite", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new Department { Id = 2, Name = "Neurology", Description = "Brain & nervous system care", IconUrl = "psychology", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new Department { Id = 3, Name = "Orthopedics", Description = "Bone & joint care", IconUrl = "accessibility_new", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new Department { Id = 4, Name = "Pediatrics", Description = "Child healthcare", IconUrl = "child_care", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new Department { Id = 5, Name = "Dermatology", Description = "Skin care & treatment", IconUrl = "water_drop", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

    new Department { Id = 6, Name = "Nephrology", Description = "Kidney care & treatment", IconUrl = "water", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new Department { Id = 7, Name = "Endocrinology", Description = "Hormone and metabolic disorders", IconUrl = "science", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new Department { Id = 8, Name = "Gastroenterology", Description = "Digestive system specialists", IconUrl = "restaurant", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new Department { Id = 9, Name = "Pulmonology", Description = "Lung and respiratory care", IconUrl = "air", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
    new Department { Id = 10, Name = "Oncology", Description = "Cancer diagnosis and treatment", IconUrl = "biotech", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
);

        // Pre-computed BCrypt hash for "Password@123" is: $2a$11$0W5iHhK8D84s6Wb968a3sOdH.vF6U2dZg7C44CkWF2pI9fU770jK.
        var defaultPasswordHash = "$2a$11$0W5iHhK8D84s6Wb968a3sOdH.vF6U2dZg7C44CkWF2pI9fU770jK.";

        modelBuilder.Entity<AppUser>().HasData(
            new AppUser { Id = 1, Email = "admin@medisphere.com", PasswordHash = defaultPasswordHash, Role = UserRole.Admin, IsActive = true, RefreshToken = "", RefreshTokenExpiry = DateTime.MinValue, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AppUser { Id = 2, Email = "ramesh@medisphere.com", PasswordHash = defaultPasswordHash, Role = UserRole.Doctor, ReferenceId = 1, IsActive = true, RefreshToken = "", RefreshTokenExpiry = DateTime.MinValue, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AppUser { Id = 3, Email = "anjali@medisphere.com", PasswordHash = defaultPasswordHash, Role = UserRole.Doctor, ReferenceId = 2, IsActive = true, RefreshToken = "", RefreshTokenExpiry = DateTime.MinValue, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new AppUser { Id = 4, Email = "john@medisphere.com", PasswordHash = defaultPasswordHash, Role = UserRole.Doctor, ReferenceId = 3, IsActive = true, RefreshToken = "", RefreshTokenExpiry = DateTime.MinValue, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<Doctor>().HasData(
            new Doctor
            {
                Id = 1,
                FirstName = "Ramesh",
                LastName = "Kumar",
                Email = "ramesh@medisphere.com",
                PasswordHash = defaultPasswordHash,
                PhoneNumber = "+919876543210",
                Specialty = "Cardiology",
                Qualification = "MD, DM (Cardiology)",
                ExperienceYears = 12,
                ConsultationFee = 500.00m,
                ProfileImageUrl = "",
                Bio = "Experienced cardiologist specializing in interventional cardiology and heart health care.",
                IsActive = true,
                IsAvailable = true,
                IsApproved = true,
                Gender = "Male",
                Location = "Delhi",
                LanguagesSpoken = "English, Hindi",
                AverageRating = 4.80m,
                RatingCount = 24,
                DepartmentId = 1,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Doctor
            {
                Id = 2,
                FirstName = "Anjali",
                LastName = "Sharma",
                Email = "anjali@medisphere.com",
                PasswordHash = defaultPasswordHash,
                PhoneNumber = "+919876543211",
                Specialty = "Neurology",
                Qualification = "MD, DM (Neurology)",
                ExperienceYears = 8,
                ConsultationFee = 800.00m,
                ProfileImageUrl = "",
                Bio = "Compassionate neurologist specializing in migraine treatment, sleep disorders, and stroke care.",
                IsActive = true,
                IsAvailable = true,
                IsApproved = true,
                Gender = "Female",
                Location = "Mumbai",
                LanguagesSpoken = "English, Hindi, Marathi",
                AverageRating = 4.90m,
                RatingCount = 15,
                DepartmentId = 2,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Doctor
            {
                Id = 3,
                FirstName = "John",
                LastName = "Doe",
                Email = "john@medisphere.com",
                PasswordHash = defaultPasswordHash,
                PhoneNumber = "+919876543212",
                Specialty = "Orthopedics",
                Qualification = "MS (Orthopedics), M.Ch",
                ExperienceYears = 15,
                ConsultationFee = 600.00m,
                ProfileImageUrl = "",
                Bio = "Expert orthopedic surgeon specializing in joint replacement, sports injuries, and spine surgery.",
                IsActive = true,
                IsAvailable = true,
                IsApproved = true,
                Gender = "Male",
                Location = "Bangalore",
                LanguagesSpoken = "English",
                AverageRating = 4.20m,
                RatingCount = 42,
                DepartmentId = 3,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        modelBuilder.Entity<DoctorSchedule>().HasData(
            new DoctorSchedule { Id = 1, DoctorId = 1, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), SlotDurationMinutes = 30, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DoctorSchedule { Id = 2, DoctorId = 1, DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), SlotDurationMinutes = 30, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DoctorSchedule { Id = 3, DoctorId = 2, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(16, 0, 0), SlotDurationMinutes = 30, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DoctorSchedule { Id = 4, DoctorId = 2, DayOfWeek = DayOfWeek.Thursday, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(16, 0, 0), SlotDurationMinutes = 30, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new DoctorSchedule { Id = 5, DoctorId = 3, DayOfWeek = DayOfWeek.Friday, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(14, 0, 0), SlotDurationMinutes = 30, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<SystemSetting>().HasData(
            new SystemSetting { Id = 1, Key = "CommissionRate", Value = "15", Description = "System commission rate percentage per booking.", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new SystemSetting { Id = 2, Key = "PlatformPolicy", Value = "No cancellations within 2 hours of appointment time.", Description = "Global patient cancellation policy.", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<ContentItem>().HasData(
            new ContentItem { Id = 1, Type = "FAQ", Title = "How do I book an appointment?", Content = "Search for a doctor, choose an available slot, enter the reason, and click Book Now.", Order = 1, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ContentItem { Id = 2, Type = "FAQ", Title = "Can I cancel a booking?", Content = "Yes, you can cancel appointments up to 2 hours before the scheduled time from your Patient Dashboard.", Order = 2, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ContentItem { Id = 3, Type = "HealthArticle", Title = "10 Tips for a Healthy Heart", Content = "Exercise regularly, eat a balanced diet, avoid smoking, manage stress, and schedule annual heart checkups.", ImageUrl = "heart_health.jpg", Order = 1, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        // Transparent AES-256 Symmetric Database Encryption at Rest
        var encryptionHelper = new Services.EncryptionHelper(_config);
        var encryptionConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<string, string>(
            v => encryptionHelper.Encrypt(v),
            v => encryptionHelper.Decrypt(v)
        );

        modelBuilder.Entity<Patient>()
            .Property(p => p.MedicalHistory)
            .HasConversion(encryptionConverter);

        modelBuilder.Entity<Appointment>()
            .Property(a => a.Notes)
            .HasConversion(encryptionConverter);

        // Loyalty Rewards and Transaction Mappings
        modelBuilder.Entity<PatientRewardLog>()
            .HasOne(r => r.Patient)
            .WithMany()
            .HasForeignKey(r => r.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(t => t.Appointment)
            .WithMany()
            .HasForeignKey(t => t.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentTransaction>()
            .Property(t => t.Amount)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<PaymentTransaction>()
            .Property(t => t.GrossAmount)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<PaymentTransaction>()
            .Property(t => t.AdminCommission)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<PaymentTransaction>()
            .Property(t => t.DoctorEarnings)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<PaymentTransaction>()
            .Property(t => t.PlatformFee)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<PaymentTransaction>()
            .Property(t => t.TaxAmount)
            .HasColumnType("decimal(18,2)");
        modelBuilder.Entity<PaymentTransaction>()
            .Property(t => t.NetDoctorAmount)
            .HasColumnType("decimal(18,2)");


        modelBuilder.Entity<FamilyMember>()
    .HasOne(f => f.Patient)
    .WithMany(p => p.FamilyMembers)
    .HasForeignKey(f => f.PatientId)
    .OnDelete(DeleteBehavior.Cascade);
    }
}
