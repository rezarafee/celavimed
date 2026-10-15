using System.Text.RegularExpressions;
using Celavimed.Areas.Admin.Models;
using Celavimed.Data;
using Celavimed.Models;
using Celavimed.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Areas.Admin.Controllers;

[Route("admin/appointments")]
public class AppointmentsController : AdminController
{
    private readonly AppDbContext _db;
    private readonly AccessScope _scope;
    private readonly WorkflowService _flow;
    private readonly SlotService _slots;

    public AppointmentsController(AppDbContext db, AccessScope scope, WorkflowService flow, SlotService slots)
    {
        _db = db;
        _scope = scope;
        _flow = flow;
        _slots = slots;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] AppointmentFilter f)
    {
        Nav("appointments", "رزروها و تایید");
        var stages = await _flow.ActiveStagesAsync();
        var initial = stages.First();
        var q = (await _scope.AppointmentsAsync())
            .Include(a => a.Patient).Include(a => a.Doctor).Include(a => a.Service).Include(a => a.Stage)
            .AsQueryable();

        var today = Clock.Today;
        switch (f.View)
        {
            case "pending":
                q = q.Where(a => a.Status == AppointmentStatus.Active && a.StageId == initial.Id && a.Date >= today);
                break;
            case "cancelled":
                q = q.Where(a => a.Status != AppointmentStatus.Active);
                break;
            case "all":
                break;
            default:
                f.View = "upcoming";
                q = q.Where(a => a.Status == AppointmentStatus.Active && a.Date >= today);
                break;
        }
        if (f.Date.HasValue) q = q.Where(a => a.Date == f.Date);
        if (f.DoctorId.HasValue) q = q.Where(a => a.DoctorId == f.DoctorId);
        if (f.StageId.HasValue) q = q.Where(a => a.StageId == f.StageId);
        if (!string.IsNullOrWhiteSpace(f.Q))
        {
            var term = Jalali.NormalizeDigits(f.Q.Trim());
            q = q.Where(a => a.Patient.FullName.Contains(term) || a.Patient.Mobile.Contains(term) || a.TrackingCode == term.ToUpper());
        }

        var ordered = f.View is "all" or "cancelled"
            ? q.OrderByDescending(a => a.Date).ThenByDescending(a => a.StartTime)
            : q.OrderBy(a => a.Date).ThenBy(a => a.StartTime);

        var vm = new AppointmentListVm
        {
            Filter = f,
            Total = await q.CountAsync(),
            Items = await ordered.Take(300).ToListAsync(),
            Doctors = await (await _scope.DoctorsAsync()).OrderBy(d => d.SortOrder).ToListAsync(),
            Stages = stages,
            InitialStageId = initial.Id,
            ConfirmStageId = stages.Count > 1 ? stages[1].Id : null,
        };
        return View(vm);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var a = await (await _scope.AppointmentsAsync())
            .Include(x => x.Patient).Include(x => x.Doctor).Include(x => x.Service).Include(x => x.Stage)
            .Include(x => x.StageLogs).ThenInclude(l => l.Stage)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        Nav("appointments", "پرونده‌ی نوبت");
        var vm = new AppointmentDetailVm
        {
            Appointment = a,
            Stages = await _flow.ActiveStagesAsync(),
            Doctors = await (await _scope.DoctorsAsync()).OrderBy(d => d.SortOrder).ToListAsync(),
            Services = await _db.Services.Where(s => s.IsActive || s.Id == a.ServiceId).OrderBy(s => s.SortOrder).ToListAsync(),
            PatientHistory = await _db.Appointments.Where(x => x.PatientId == a.PatientId && x.Id != a.Id)
                .Include(x => x.Doctor).Include(x => x.Service).Include(x => x.Stage)
                .OrderByDescending(x => x.Date).Take(10).ToListAsync(),
            CanEdit = _scope.CanEditFlow,
        };
        // اگر مرحله‌ی فعلی غیرفعال شده باشد، باز هم در استپر دیده شود
        if (vm.Stages.All(s => s.Id != a.StageId)) vm.Stages.Add(a.Stage);
        return View(vm);
    }

    public record MoveRequest(int Id, int StageId, string? Note);

    [HttpPost("move")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> Move([FromBody] MoveRequest req)
    {
        var a = await (await _scope.AppointmentsAsync()).FirstOrDefaultAsync(x => x.Id == req.Id);
        if (a == null) return NotFound(new { error = "نوبت پیدا نشد." });
        if (a.Status != AppointmentStatus.Active) return BadRequest(new { error = "این نوبت لغو شده است." });
        if (!await _db.Stages.AnyAsync(s => s.Id == req.StageId && s.IsActive)) return BadRequest(new { error = "مرحله نامعتبر است." });
        await _flow.MoveAsync(a, req.StageId, User, req.Note);
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    [HttpPost("{id:int}/stage")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> SetStage(int id, int stageId, string? note, string? back)
    {
        var a = await (await _scope.AppointmentsAsync()).FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        if (a.Status != AppointmentStatus.Active)
        {
            Flash("نوبت لغو شده را نمی‌توان جابه‌جا کرد؛ اول آن را برگردانید.", true);
        }
        else if (await _db.Stages.AnyAsync(s => s.Id == stageId))
        {
            await _flow.MoveAsync(a, stageId, User, note);
            await _db.SaveChangesAsync();
            var name = await _db.Stages.Where(s => s.Id == stageId).Select(s => s.Name).FirstAsync();
            Flash($"وضعیت به «{name}» تغییر کرد.");
        }
        return LocalRedirect(SafeBack(back, $"/admin/appointments/{id}"));
    }

    /// <summary>تایید تلفنی: از «رزرو» به مرحله‌ی دوم</summary>
    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> Confirm(int id, string? back)
    {
        var a = await (await _scope.AppointmentsAsync()).Include(x => x.Patient).FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        var next = await _flow.ConfirmStageAsync();
        if (next != null && a.Status == AppointmentStatus.Active)
        {
            await _flow.MoveAsync(a, next.Id, User, "حضور بیمار تلفنی تایید شد.");
            await _db.SaveChangesAsync();
            Flash($"نوبت {a.Patient.FullName} تایید شد.");
        }
        return LocalRedirect(SafeBack(back, "/admin/appointments?view=pending"));
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> Cancel(int id, bool noShow, string? note, string? back)
    {
        var a = await (await _scope.AppointmentsAsync()).Include(x => x.Patient).FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        a.Status = noShow ? AppointmentStatus.NoShow : AppointmentStatus.Cancelled;
        _db.StageLogs.Add(new AppointmentStageLog
        {
            AppointmentId = a.Id, StageId = a.StageId, ChangedBy = WorkflowService.DisplayName(User),
            Note = (noShow ? "بیمار نیامد." : "نوبت لغو شد.") + (string.IsNullOrWhiteSpace(note) ? "" : " " + note.Trim()),
        });
        await _db.SaveChangesAsync();
        Flash(noShow ? $"{a.Patient.FullName} «نیامد» ثبت شد." : $"نوبت {a.Patient.FullName} لغو شد و این ساعت دوباره آزاد است.");
        return LocalRedirect(SafeBack(back, $"/admin/appointments/{id}"));
    }

    [HttpPost("{id:int}/restore")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> Restore(int id)
    {
        var a = await (await _scope.AppointmentsAsync()).FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        var taken = await _db.Appointments.AnyAsync(x => x.Id != id && x.DoctorId == a.DoctorId && x.Date == a.Date
                                                          && x.Status == AppointmentStatus.Active
                                                          && x.StartTime < a.EndTime && a.StartTime < x.EndTime);
        if (taken)
        {
            Flash("این ساعت در این فاصله به بیمار دیگری داده شده؛ اول زمان نوبت را تغییر دهید.", true);
            return Redirect($"/admin/appointments/{id}");
        }
        a.Status = AppointmentStatus.Active;
        _db.StageLogs.Add(new AppointmentStageLog { AppointmentId = a.Id, StageId = a.StageId, ChangedBy = WorkflowService.DisplayName(User), Note = "نوبت دوباره فعال شد." });
        await _db.SaveChangesAsync();
        Flash("نوبت دوباره فعال شد.");
        return Redirect($"/admin/appointments/{id}");
    }

    [HttpPost("{id:int}/edit")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> Edit(AppointmentEditForm f)
    {
        var a = await (await _scope.AppointmentsAsync()).Include(x => x.Patient).FirstOrDefaultAsync(x => x.Id == f.Id);
        if (a == null) return NotFound();
        if (!await _scope.CanAccessDoctorAsync(f.DoctorId)) return Forbid();

        var mobile = Jalali.NormalizeDigits(f.PatientMobile).Trim();
        if (!Regex.IsMatch(mobile, @"^09\d{9}$"))
        {
            Flash("شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود.", true);
            return Redirect($"/admin/appointments/{f.Id}");
        }

        var duration = Math.Clamp(f.DurationMinutes, 5, 480);
        var end = f.StartTime.AddMinutes(duration);
        if (end <= f.StartTime)
        {
            Flash("زمان پایان نوبت از نیمه‌شب گذشته است.", true);
            return Redirect($"/admin/appointments/{f.Id}");
        }
        var timeChanged = a.DoctorId != f.DoctorId || a.Date != f.Date || a.StartTime != f.StartTime || a.EndTime != end;
        if (timeChanged && a.Status == AppointmentStatus.Active)
        {
            var clash = await _db.Appointments.Include(x => x.Patient).FirstOrDefaultAsync(x => x.Id != a.Id && x.DoctorId == f.DoctorId && x.Date == f.Date
                && x.Status == AppointmentStatus.Active && x.StartTime < end && f.StartTime < x.EndTime);
            if (clash != null)
            {
                Flash($"این زمان با نوبت {clash.Patient.FullName} ({Jalali.Time(clash.StartTime)}) تداخل دارد.", true);
                return Redirect($"/admin/appointments/{f.Id}");
            }
        }

        // اگر شماره عوض شد و متعلق به بیمار دیگری است، نوبت به همان پرونده منتقل می‌شود
        if (mobile != a.Patient.Mobile)
        {
            var other = await _db.Patients.FirstOrDefaultAsync(p => p.Mobile == mobile);
            if (other != null) a.Patient = other;
            else a.Patient.Mobile = mobile;
        }
        if (!string.IsNullOrWhiteSpace(f.PatientName)) a.Patient.FullName = f.PatientName.Trim();

        var changes = new List<string>();
        if (timeChanged) changes.Add($"زمان به {Jalali.Short(f.Date)} ساعت {Jalali.Time(f.StartTime)} تغییر کرد");
        a.DoctorId = f.DoctorId;
        a.ServiceId = f.ServiceId;
        a.Date = f.Date;
        a.StartTime = f.StartTime;
        a.EndTime = end;
        a.Notes = f.Notes?.Trim();
        var amountText = Jalali.NormalizeDigits(f.Amount).Replace(",", "").Replace("٬", "").Trim();
        a.Amount = long.TryParse(amountText, out var amt) ? amt : null;
        if (a.IsPaid != f.IsPaid) changes.Add(f.IsPaid ? "پرداخت ثبت شد" : "پرداخت برداشته شد");
        a.IsPaid = f.IsPaid;
        if (changes.Count > 0)
            _db.StageLogs.Add(new AppointmentStageLog { AppointmentId = a.Id, StageId = a.StageId, ChangedBy = WorkflowService.DisplayName(User), Note = string.Join("، ", changes) + "." });

        try
        {
            await _db.SaveChangesAsync();
            Flash("پرونده ذخیره شد.");
        }
        catch (DbUpdateException)
        {
            Flash("این ساعت همین الان برای بیمار دیگری ثبت شد. زمان دیگری انتخاب کنید.", true);
        }
        return Redirect($"/admin/appointments/{f.Id}");
    }

    [HttpGet("new")]
    [Authorize(Roles = Roles.FrontDesk)]
    public async Task<IActionResult> Create(int? doctorId, DateOnly? date, string? mobile, string? name)
    {
        Nav("appointments", "نوبت تلفنی / حضوری");
        ViewBag.Doctors = await (await _scope.DoctorsAsync()).Where(d => d.IsActive).Include(d => d.Services).ThenInclude(s => s.Service).OrderBy(d => d.SortOrder).ToListAsync();
        return View(new NewAppointmentForm { DoctorId = doctorId, Date = date ?? Clock.Today, PatientMobile = mobile ?? "", PatientName = name ?? "" });
    }

    [HttpPost("new")]
    [Authorize(Roles = Roles.FrontDesk)]
    public async Task<IActionResult> Create(NewAppointmentForm f)
    {
        var mobile = Jalali.NormalizeDigits(f.PatientMobile).Trim();
        if (!Regex.IsMatch(mobile, @"^09\d{9}$")) ModelState.AddModelError(nameof(f.PatientMobile), "شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود.");
        if (f.Date == null || f.StartTime == null) ModelState.AddModelError(nameof(f.StartTime), "روز و ساعت نوبت را انتخاب کنید.");
        if (f.DoctorId.HasValue && !await _scope.CanAccessDoctorAsync(f.DoctorId.Value)) return Forbid();

        Slot? slot = null;
        if (ModelState.IsValid)
        {
            slot = await _slots.FindSlotAsync(f.DoctorId!.Value, f.Date!.Value, f.StartTime!.Value);
            if (slot == null || !slot.IsFree) ModelState.AddModelError(nameof(f.StartTime), "این ساعت آزاد نیست. ساعت دیگری انتخاب کنید.");
        }
        if (!ModelState.IsValid)
        {
            Nav("appointments", "نوبت تلفنی / حضوری");
            ViewBag.Doctors = await (await _scope.DoctorsAsync()).Where(d => d.IsActive).Include(d => d.Services).ThenInclude(s => s.Service).OrderBy(d => d.SortOrder).ToListAsync();
            return View(f);
        }

        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Mobile == mobile);
        if (patient == null)
        {
            patient = new Patient { Mobile = mobile, FullName = f.PatientName.Trim() };
            _db.Patients.Add(patient);
        }
        // منشی تلفنی ثبت کرده، پس نوبت از همان ابتدا تایید شده است
        var stage = await _flow.ConfirmStageAsync() ?? await _flow.InitialStageAsync();
        var appt = new Appointment
        {
            Doctor = (await _db.Doctors.FindAsync(f.DoctorId))!,
            Patient = patient,
            ServiceId = f.ServiceId,
            Date = f.Date!.Value,
            StartTime = slot!.Start,
            EndTime = slot.End,
            StageId = stage.Id,
            Notes = f.Notes?.Trim(),
            TrackingCode = await _flow.NewTrackingCodeAsync(),
            ConfirmedAt = DateTime.UtcNow,
        };
        _db.Appointments.Add(appt);
        _db.StageLogs.Add(new AppointmentStageLog { Appointment = appt, StageId = stage.Id, ChangedBy = WorkflowService.DisplayName(User), Note = "ثبت تلفنی/حضوری توسط پذیرش." });
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            Flash("این ساعت همین الان رزرو شد. ساعت دیگری انتخاب کنید.", true);
            return Redirect($"/admin/appointments/new?doctorId={f.DoctorId}&date={f.Date:yyyy-MM-dd}");
        }
        Flash($"نوبت {patient.FullName} ثبت شد.");
        return Redirect($"/admin/appointments/{appt.Id}");
    }

    /// <summary>ساعت‌های یک روز برای فرم نوبت تلفنی</summary>
    [HttpGet("slots")]
    public async Task<IActionResult> Slots(int doctorId, DateOnly date)
    {
        if (!await _scope.CanAccessDoctorAsync(doctorId)) return Forbid();
        var slots = await _slots.GetSlotsAsync(doctorId, date, forPublic: false);
        return Json(slots.Select(s => new { start = Jalali.Time(s.Start), end = Jalali.Time(s.End), free = s.IsFree }));
    }

    private static string SafeBack(string? back, string fallback) =>
        !string.IsNullOrEmpty(back) && back.StartsWith("/admin") && !back.StartsWith("//") ? back : fallback;
}
