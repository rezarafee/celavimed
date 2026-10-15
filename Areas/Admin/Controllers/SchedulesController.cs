using Celavimed.Areas.Admin.Models;
using Celavimed.Data;
using Celavimed.Models;
using Celavimed.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Areas.Admin.Controllers;

[Route("admin/schedules")]
[Authorize(Roles = Roles.FrontDesk)]
public class SchedulesController : AdminController
{
    private readonly AppDbContext _db;
    private readonly AccessScope _scope;

    public SchedulesController(AppDbContext db, AccessScope scope)
    {
        _db = db;
        _scope = scope;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var first = await (await _scope.DoctorsAsync()).OrderBy(d => d.SortOrder).ThenBy(d => d.FullName).FirstOrDefaultAsync();
        if (first == null)
        {
            Nav("schedules", "برنامه‌ی کاری");
            return View("NoDoctor");
        }
        return Redirect($"/admin/schedules/{first.Id}");
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Doctor(int id)
    {
        if (!await _scope.CanAccessDoctorAsync(id)) return Forbid();
        var doctor = await _db.Doctors.FindAsync(id);
        if (doctor == null) return NotFound();
        Nav("schedules", "برنامه‌ی کاری");
        var vm = new ScheduleVm
        {
            Doctor = doctor,
            Doctors = await (await _scope.DoctorsAsync()).OrderBy(d => d.SortOrder).ThenBy(d => d.FullName).ToListAsync(),
            Rules = await _db.WeeklySchedules.Where(r => r.DoctorId == id).OrderBy(r => r.StartTime).ToListAsync(),
            TimeOffs = await _db.TimeOffs.Where(t => t.DoctorId == id && t.Date >= Clock.Today).OrderBy(t => t.Date).ToListAsync(),
        };
        return View(vm);
    }

    [HttpPost("{id:int}")]
    public async Task<IActionResult> Save(int id, List<RangeInput> ranges)
    {
        if (!await _scope.CanAccessDoctorAsync(id)) return Forbid();
        if (!await _db.Doctors.AnyAsync(d => d.Id == id)) return NotFound();

        var errors = new List<string>();
        var clean = new List<RangeInput>();
        foreach (var g in ranges.GroupBy(r => r.DayOfWeek))
        {
            var sorted = g.OrderBy(r => r.StartTime).ToList();
            for (var i = 0; i < sorted.Count; i++)
            {
                var r = sorted[i];
                r.SlotMinutes = Math.Clamp(r.SlotMinutes, 5, 240);
                if (r.EndTime <= r.StartTime || (r.EndTime - r.StartTime).TotalMinutes < r.SlotMinutes)
                {
                    errors.Add($"{Jalali.DayName(r.DayOfWeek)}: بازه‌ی {Jalali.Time(r.StartTime)} تا {Jalali.Time(r.EndTime)} حتی یک نوبت جا نمی‌شود.");
                    continue;
                }
                if (i > 0 && r.StartTime < sorted[i - 1].EndTime)
                {
                    errors.Add($"{Jalali.DayName(r.DayOfWeek)}: بازه‌ها با هم هم‌پوشانی دارند.");
                    continue;
                }
                clean.Add(r);
            }
        }
        if (errors.Count > 0)
        {
            Flash(string.Join(" ", errors.Distinct()), error: true);
            return Redirect($"/admin/schedules/{id}");
        }

        _db.WeeklySchedules.RemoveRange(_db.WeeklySchedules.Where(r => r.DoctorId == id));
        _db.WeeklySchedules.AddRange(clean.Select(r => new WeeklySchedule
        {
            DoctorId = id, DayOfWeek = r.DayOfWeek, StartTime = r.StartTime, EndTime = r.EndTime, SlotMinutes = r.SlotMinutes,
        }));
        await _db.SaveChangesAsync();
        var total = clean.Sum(r => (int)((r.EndTime - r.StartTime).TotalMinutes / r.SlotMinutes));
        Flash($"برنامه ذخیره شد: {total} نوبت در هفته. نوبت‌های قبلاً رزروشده تغییری نمی‌کنند.");
        return Redirect($"/admin/schedules/{id}");
    }

    [HttpPost("{id:int}/off")]
    public async Task<IActionResult> AddOff(int id, DateOnly? date, string? reason)
    {
        if (!await _scope.CanAccessDoctorAsync(id)) return Forbid();
        if (date == null)
        {
            Flash("تاریخ را انتخاب کنید.", true);
            return Redirect($"/admin/schedules/{id}");
        }
        if (!await _db.TimeOffs.AnyAsync(t => t.DoctorId == id && t.Date == date))
        {
            _db.TimeOffs.Add(new DoctorTimeOff { DoctorId = id, Date = date.Value, Reason = reason?.Trim() });
            await _db.SaveChangesAsync();
        }
        var booked = await _db.Appointments.CountAsync(a => a.DoctorId == id && a.Date == date && a.Status == AppointmentStatus.Active);
        Flash(booked > 0
            ? $"{Jalali.Long(date.Value)} تعطیل شد. توجه: {booked} نوبت از قبل در این روز ثبت شده که باید با بیماران هماهنگ کنید."
            : $"{Jalali.Long(date.Value)} برای این پزشک تعطیل شد.", booked > 0);
        return Redirect($"/admin/schedules/{id}");
    }

    [HttpPost("{id:int}/off/{offId:int}/delete")]
    public async Task<IActionResult> DeleteOff(int id, int offId)
    {
        if (!await _scope.CanAccessDoctorAsync(id)) return Forbid();
        var off = await _db.TimeOffs.FirstOrDefaultAsync(t => t.Id == offId && t.DoctorId == id);
        if (off != null)
        {
            _db.TimeOffs.Remove(off);
            await _db.SaveChangesAsync();
            Flash("روز تعطیل حذف شد.");
        }
        return Redirect($"/admin/schedules/{id}");
    }
}
