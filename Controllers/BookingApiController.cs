using System.Text.RegularExpressions;
using Celavimed.Data;
using Celavimed.Models;
using Celavimed.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Controllers;

[ApiController]
[Route("api/booking")]
public class BookingApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SlotService _slots;
    private readonly WorkflowService _flow;

    public BookingApiController(AppDbContext db, SlotService slots, WorkflowService flow)
    {
        _db = db;
        _slots = slots;
        _flow = flow;
    }

    private Task<bool> DoctorIsPublic(int id) => _db.Doctors.AnyAsync(d => d.Id == id && d.IsActive);

    [HttpGet("availability")]
    public async Task<IActionResult> Availability(int doctorId, DateOnly from, DateOnly to)
    {
        if (to < from || to.DayNumber - from.DayNumber > 62) return BadRequest(new { error = "بازه‌ی تاریخ نامعتبر است." });
        if (!await DoctorIsPublic(doctorId)) return NotFound(new { error = "پزشک پیدا نشد." });
        var days = await _slots.GetRangeAsync(doctorId, from, to);
        return Ok(new
        {
            today = Clock.Today.ToString("yyyy-MM-dd"),
            last = Clock.Today.AddDays(_slots.BookingWindowDays).ToString("yyyy-MM-dd"),
            days = days.Select(d => new { date = d.Date.ToString("yyyy-MM-dd"), free = d.FreeCount, total = d.TotalCount, off = d.IsOff }),
        });
    }

    [HttpGet("slots")]
    public async Task<IActionResult> Slots(int doctorId, DateOnly date)
    {
        if (!await DoctorIsPublic(doctorId)) return NotFound(new { error = "پزشک پیدا نشد." });
        if (date < Clock.Today || date > Clock.Today.AddDays(_slots.BookingWindowDays)) return Ok(Array.Empty<object>());
        var slots = await _slots.GetSlotsAsync(doctorId, date, forPublic: true);
        return Ok(slots.Select(s => new { start = Jalali.Time(s.Start), end = Jalali.Time(s.End), free = s.IsFree }));
    }

    public class BookRequest
    {
        public int DoctorId { get; set; }
        public DateOnly Date { get; set; }
        public string Start { get; set; } = "";
        public int? ServiceId { get; set; }
        public string Name { get; set; } = "";
        public string Mobile { get; set; } = "";
        /// <summary>فیلد مخفی برای ربات‌ها؛ باید خالی بماند.</summary>
        public string? Website { get; set; }
    }

    [HttpPost("book")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("booking")]
    public async Task<IActionResult> Book([FromBody] BookRequest req)
    {
        if (!string.IsNullOrEmpty(req.Website)) return BadRequest(new { error = "درخواست نامعتبر است." });

        var name = Regex.Replace(req.Name ?? "", @"\s+", " ").Trim();
        var mobile = Jalali.NormalizeDigits(req.Mobile).Replace(" ", "").Replace("-", "").Trim();
        if (mobile.StartsWith("+98")) mobile = "0" + mobile[3..];
        if (name.Length < 3) return BadRequest(new { error = "نام و نام خانوادگی را کامل بنویسید.", field = "name" });
        if (!Regex.IsMatch(mobile, @"^09\d{9}$")) return BadRequest(new { error = "شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود.", field = "mobile" });
        if (!TimeOnly.TryParseExact(req.Start, "HH:mm", out var start)) return BadRequest(new { error = "ساعت نامعتبر است." });

        var doctor = await _db.Doctors.Include(d => d.Services).FirstOrDefaultAsync(d => d.Id == req.DoctorId && d.IsActive);
        if (doctor == null) return NotFound(new { error = "پزشک پیدا نشد." });
        if (doctor.Services.Count > 0 && (req.ServiceId == null || doctor.Services.All(s => s.ServiceId != req.ServiceId)))
            return BadRequest(new { error = "نوع خدمت را انتخاب کنید.", field = "service" });

        var slots = await _slots.GetSlotsAsync(doctor.Id, req.Date, forPublic: true);
        var slot = slots.FirstOrDefault(s => s.Start == start && s.IsFree);
        if (slot == null || req.Date > Clock.Today.AddDays(_slots.BookingWindowDays))
            return Conflict(new { error = "این ساعت همین الان پر شد. لطفا ساعت دیگری انتخاب کنید.", field = "slot" });

        // جلوگیری از رزرو تکراری یک شماره برای یک پزشک در یک روز
        if (await _db.Appointments.AnyAsync(a => a.DoctorId == doctor.Id && a.Date == req.Date && a.Status == AppointmentStatus.Active && a.Patient.Mobile == mobile))
            return Conflict(new { error = "با این شماره برای همین روز و همین پزشک قبلا نوبت گرفته‌اید." });

        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Mobile == mobile);
        if (patient == null)
        {
            patient = new Patient { FullName = name, Mobile = mobile };
            _db.Patients.Add(patient);
        }

        var stage = await _flow.InitialStageAsync();
        var appt = new Appointment
        {
            Doctor = doctor,
            Patient = patient,
            ServiceId = req.ServiceId,
            Date = req.Date,
            StartTime = slot.Start,
            EndTime = slot.End,
            StageId = stage.Id,
            TrackingCode = await _flow.NewTrackingCodeAsync(),
            Notes = patient.FullName != name ? $"نام واردشده در رزرو آنلاین: {name}" : null,
        };
        _db.Appointments.Add(appt);
        _db.StageLogs.Add(new AppointmentStageLog { Appointment = appt, StageId = stage.Id, ChangedBy = "رزرو آنلاین", Note = "بیمار از سایت نوبت گرفت." });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "این ساعت همین الان پر شد. لطفا ساعت دیگری انتخاب کنید.", field = "slot" });
        }

        var service = req.ServiceId.HasValue ? await _db.Services.Where(s => s.Id == req.ServiceId).Select(s => s.Name).FirstOrDefaultAsync() : null;
        return Ok(new
        {
            trackingCode = appt.TrackingCode,
            date = Jalali.Long(appt.Date),
            time = Jalali.Time(appt.StartTime),
            doctor = doctor.FullName,
            service,
        });
    }
}
