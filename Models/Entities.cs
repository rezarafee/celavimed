using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Celavimed.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(120)]
    public string FullName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<DoctorSecretary> SecretaryOf { get; set; } = new();
}

public class Doctor
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام پزشک را وارد کنید."), MaxLength(120)]
    public string FullName { get; set; } = "";

    /// <summary>مثلا «متخصص پوست و مو»</summary>
    [MaxLength(160)]
    public string? Headline { get; set; }

    [MaxLength(2000)]
    public string? Bio { get; set; }

    [MaxLength(30)]
    public string? MedicalCode { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(260)]
    public string? PhotoPath { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<DoctorService> Services { get; set; } = new();
    public List<DoctorSecretary> Secretaries { get; set; } = new();
    public List<WeeklySchedule> WeeklySchedules { get; set; } = new();
    public List<DoctorTimeOff> TimeOffs { get; set; } = new();
}

/// <summary>خدمت یا تخصص قابل رزرو (بوتاکس، لیزر، فیلر و ...)</summary>
public class ClinicService
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام خدمت را وارد کنید."), MaxLength(100)]
    public string Name { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public List<DoctorService> Doctors { get; set; } = new();
}

public class DoctorService
{
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public int ServiceId { get; set; }
    public ClinicService Service { get; set; } = null!;
}

public class DoctorSecretary
{
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
}

/// <summary>یک بازه‌ی کاری هفتگی؛ هر روز می‌تواند چند بازه داشته باشد.</summary>
public class WeeklySchedule
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// <summary>طول هر نوبت به دقیقه (پیش‌فرض ۱۵)</summary>
    public int SlotMinutes { get; set; } = 15;
    public bool IsActive { get; set; } = true;
}

/// <summary>روز تعطیل یا مرخصی پزشک</summary>
public class DoctorTimeOff
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
    public DateOnly Date { get; set; }

    [MaxLength(200)]
    public string? Reason { get; set; }
}

public class Patient
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string FullName { get; set; } = "";

    [Required, MaxLength(11)]
    public string Mobile { get; set; } = "";

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Appointment> Appointments { get; set; } = new();
}

/// <summary>مرحله‌ی پذیرش بیمار؛ قابل افزودن، حذف و جابه‌جایی توسط مدیر.</summary>
public class WorkflowStage
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام مرحله را وارد کنید."), MaxLength(60)]
    public string Name { get; set; } = "";

    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>رنگ کادر مرحله، مثل #8A4F7D</summary>
    [MaxLength(9)]
    public string Color { get; set; } = "#8A4F7D";

    [MaxLength(40)]
    public string Icon { get; set; } = "circle";

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum AppointmentStatus
{
    Active = 0,
    Cancelled = 1,
    NoShow = 2,
}

public class Appointment
{
    public int Id { get; set; }

    [MaxLength(8)]
    public string TrackingCode { get; set; } = "";

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int? ServiceId { get; set; }
    public ClinicService? Service { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public int StageId { get; set; }
    public WorkflowStage Stage { get; set; } = null!;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Active;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>مبلغ (ریال یا تومان، طبق قرارداد مرکز) برای واحد مالی</summary>
    public long? Amount { get; set; }
    public bool IsPaid { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }

    public List<AppointmentStageLog> StageLogs { get; set; } = new();
}

public class AppointmentStageLog
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;
    public int StageId { get; set; }
    public WorkflowStage Stage { get; set; } = null!;

    [MaxLength(120)]
    public string? ChangedBy { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
