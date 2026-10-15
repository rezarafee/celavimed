using System.ComponentModel.DataAnnotations;
using Celavimed.Models;

namespace Celavimed.Areas.Admin.Models;

public record JDateField(string Name, DateOnly? Value, string Placeholder = "انتخاب تاریخ", bool AllowEmpty = false, bool SubmitOnChange = false);

public class DashboardVm
{
    public int TodayTotal { get; set; }
    public int TodayDone { get; set; }
    public int PendingConfirm { get; set; }
    public int UpcomingWeek { get; set; }
    public int NewOnline24h { get; set; }
    public List<(WorkflowStage Stage, int Count)> StageCounts { get; set; } = new();
    public List<Appointment> Today { get; set; } = new();
    public List<Appointment> ToConfirm { get; set; } = new();
}

public class DoctorForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام پزشک را وارد کنید."), MaxLength(120)]
    [Display(Name = "نام و نام خانوادگی")]
    public string FullName { get; set; } = "";

    [MaxLength(160)]
    public string? Headline { get; set; }

    [MaxLength(2000)]
    public string? Bio { get; set; }

    [MaxLength(30)]
    public string? MedicalCode { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public IFormFile? Photo { get; set; }
    public bool RemovePhoto { get; set; }
    public string? PhotoPath { get; set; }

    public List<int> ServiceIds { get; set; } = new();
    public List<string> SecretaryIds { get; set; } = new();

    public List<ClinicService> AllServices { get; set; } = new();
    public List<ApplicationUser> AllSecretaries { get; set; } = new();
}

public class RangeInput
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotMinutes { get; set; } = 15;
}

public class ScheduleVm
{
    public Doctor Doctor { get; set; } = null!;
    public List<Doctor> Doctors { get; set; } = new();
    public List<WeeklySchedule> Rules { get; set; } = new();
    public List<DoctorTimeOff> TimeOffs { get; set; } = new();
}

public class AppointmentFilter
{
    public DateOnly? Date { get; set; }
    public int? DoctorId { get; set; }
    public int? StageId { get; set; }
    public string? Q { get; set; }
    public string View { get; set; } = "upcoming"; // upcoming | pending | all | cancelled
}

public class AppointmentListVm
{
    public AppointmentFilter Filter { get; set; } = new();
    public List<Appointment> Items { get; set; } = new();
    public List<Doctor> Doctors { get; set; } = new();
    public List<WorkflowStage> Stages { get; set; } = new();
    public int? ConfirmStageId { get; set; }
    public int InitialStageId { get; set; }
    public int Total { get; set; }
}

public class BoardVm
{
    public DateOnly Date { get; set; }
    public int? DoctorId { get; set; }
    public List<Doctor> Doctors { get; set; } = new();
    public List<WorkflowStage> Stages { get; set; } = new();
    public List<Appointment> Items { get; set; } = new();
    public bool CanEdit { get; set; }
}

public class AppointmentDetailVm
{
    public Appointment Appointment { get; set; } = null!;
    public List<WorkflowStage> Stages { get; set; } = new();
    public List<Doctor> Doctors { get; set; } = new();
    public List<ClinicService> Services { get; set; } = new();
    public List<Appointment> PatientHistory { get; set; } = new();
    public bool CanEdit { get; set; }
}

public class AppointmentEditForm
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public int? ServiceId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public int DurationMinutes { get; set; } = 15;
    public string? Notes { get; set; }
    public string? Amount { get; set; }
    public bool IsPaid { get; set; }
    public string PatientName { get; set; } = "";
    public string PatientMobile { get; set; } = "";
}

public class NewAppointmentForm
{
    [Required(ErrorMessage = "پزشک را انتخاب کنید.")]
    public int? DoctorId { get; set; }
    public int? ServiceId { get; set; }
    public DateOnly? Date { get; set; }
    public TimeOnly? StartTime { get; set; }
    [Required(ErrorMessage = "نام بیمار را وارد کنید.")]
    public string PatientName { get; set; } = "";
    [Required(ErrorMessage = "شماره موبایل را وارد کنید.")]
    public string PatientMobile { get; set; } = "";
    public string? Notes { get; set; }
}

public class UserForm
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "نام کامل را وارد کنید."), MaxLength(120)]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "نام کاربری را وارد کنید."), MaxLength(60)]
    public string UserName { get; set; } = "";

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public string? Password { get; set; }

    [Required(ErrorMessage = "نقش کاربر را انتخاب کنید.")]
    public string Role { get; set; } = Roles.Secretary;

    public bool IsActive { get; set; } = true;
    public List<int> DoctorIds { get; set; } = new();

    public List<Doctor> AllDoctors { get; set; } = new();
    public List<string> AssignableRoles { get; set; } = new();
}

public class UserRow
{
    public ApplicationUser User { get; set; } = null!;
    public string Role { get; set; } = "";
    public List<string> Doctors { get; set; } = new();
}

public class PatientSummaryVm
{
    public Patient Patient { get; set; } = null!;
    public List<Appointment> Appointments { get; set; } = new();
    public List<WorkflowStage> Stages { get; set; } = new();
    public int? FinalStageId { get; set; }
    public bool CanEdit { get; set; }

    public IEnumerable<Appointment> Upcoming => Appointments
        .Where(a => a.Status == AppointmentStatus.Active && a.Date >= Celavimed.Services.Clock.Today && a.StageId != FinalStageId)
        .OrderBy(a => a.Date).ThenBy(a => a.StartTime);
    public IEnumerable<Appointment> Past => Appointments.Except(Upcoming);
    public int Completed => Appointments.Count(a => a.Status == AppointmentStatus.Active && a.StageId == FinalStageId);
    public int Cancelled => Appointments.Count(a => a.Status == AppointmentStatus.Cancelled);
    public int NoShow => Appointments.Count(a => a.Status == AppointmentStatus.NoShow);
    public long TotalPaid => Appointments.Where(a => a.IsPaid && a.Status == AppointmentStatus.Active).Sum(a => a.Amount ?? 0);
    public long TotalUnpaid => Appointments.Where(a => !a.IsPaid && a.Status == AppointmentStatus.Active).Sum(a => a.Amount ?? 0);
    public List<(string Name, int Count)> TopServices => Appointments.Where(a => a.Service != null && a.Status == AppointmentStatus.Active)
        .GroupBy(a => a.Service!.Name).Select(g => (g.Key, g.Count())).OrderByDescending(x => x.Item2).ToList();
    public List<string> Doctors => Appointments.Select(a => a.Doctor.FullName).Distinct().ToList();
}
