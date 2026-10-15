using Celavimed.Data;
using Celavimed.Models;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Services;

public record Slot(TimeOnly Start, TimeOnly End, bool IsFree);

public record DayAvailability(DateOnly Date, int FreeCount, int TotalCount, bool IsOff);

/// <summary>ساخت نوبت‌ها از روی برنامه‌ی هفتگی پزشک، منهای رزروها و روزهای تعطیل.</summary>
public class SlotService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public SlotService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>چند روز آینده برای رزرو آنلاین باز است</summary>
    public int BookingWindowDays => _config.GetValue("Booking:WindowDays", 60);

    /// <summary>حداقل فاصله‌ی زمانی رزرو از الان (دقیقه)</summary>
    public int MinLeadMinutes => _config.GetValue("Booking:MinLeadMinutes", 30);

    public async Task<List<Slot>> GetSlotsAsync(int doctorId, DateOnly date, bool forPublic, int? ignoreAppointmentId = null)
    {
        var rules = await _db.WeeklySchedules
            .Where(r => r.DoctorId == doctorId && r.IsActive && r.DayOfWeek == date.DayOfWeek)
            .OrderBy(r => r.StartTime)
            .ToListAsync();
        if (rules.Count == 0) return new();

        if (await _db.TimeOffs.AnyAsync(t => t.DoctorId == doctorId && t.Date == date)) return new();

        var booked = await _db.Appointments
            .Where(a => a.DoctorId == doctorId && a.Date == date && a.Status == AppointmentStatus.Active
                        && (ignoreAppointmentId == null || a.Id != ignoreAppointmentId))
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        var cutoff = forPublic ? PublicCutoff(date) : null;
        var result = new List<Slot>();
        foreach (var (start, end) in Expand(rules))
        {
            if (cutoff.HasValue && start <= cutoff.Value) continue;
            var free = !booked.Any(b => start < b.EndTime && b.StartTime < end);
            result.Add(new Slot(start, end, free));
        }
        return result.OrderBy(s => s.Start).ToList();
    }

    /// <summary>خلاصه‌ی وضعیت روزهای یک بازه، برای رنگ‌آمیزی تقویم</summary>
    public async Task<List<DayAvailability>> GetRangeAsync(int doctorId, DateOnly from, DateOnly to)
    {
        var today = Clock.Today;
        var last = today.AddDays(BookingWindowDays);
        var rules = await _db.WeeklySchedules.Where(r => r.DoctorId == doctorId && r.IsActive).ToListAsync();
        var offs = (await _db.TimeOffs.Where(t => t.DoctorId == doctorId && t.Date >= from && t.Date <= to)
            .Select(t => t.Date).ToListAsync()).ToHashSet();
        var booked = await _db.Appointments
            .Where(a => a.DoctorId == doctorId && a.Date >= from && a.Date <= to && a.Status == AppointmentStatus.Active)
            .Select(a => new { a.Date, a.StartTime, a.EndTime })
            .ToListAsync();

        var list = new List<DayAvailability>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            if (d < today || d > last)
            {
                list.Add(new DayAvailability(d, 0, 0, false));
                continue;
            }
            var dayRules = rules.Where(r => r.DayOfWeek == d.DayOfWeek).ToList();
            if (dayRules.Count == 0 || offs.Contains(d))
            {
                list.Add(new DayAvailability(d, 0, 0, offs.Contains(d)));
                continue;
            }
            var cutoff = PublicCutoff(d);
            var dayBooked = booked.Where(b => b.Date == d).ToList();
            int total = 0, free = 0;
            foreach (var (start, end) in Expand(dayRules))
            {
                if (cutoff.HasValue && start <= cutoff.Value) continue;
                total++;
                if (!dayBooked.Any(b => start < b.EndTime && b.StartTime < end)) free++;
            }
            list.Add(new DayAvailability(d, free, total, false));
        }
        return list;
    }

    public async Task<bool> IsBookableAsync(int doctorId, DateOnly date, TimeOnly start, bool forPublic, int? ignoreAppointmentId = null)
    {
        if (forPublic && (date < Clock.Today || date > Clock.Today.AddDays(BookingWindowDays))) return false;
        var slots = await GetSlotsAsync(doctorId, date, forPublic, ignoreAppointmentId);
        return slots.Any(s => s.Start == start && s.IsFree);
    }

    public async Task<Slot?> FindSlotAsync(int doctorId, DateOnly date, TimeOnly start)
    {
        var slots = await GetSlotsAsync(doctorId, date, false);
        return slots.FirstOrDefault(s => s.Start == start);
    }

    private TimeOnly? PublicCutoff(DateOnly date)
    {
        var now = Clock.NowIran;
        var limit = now.AddMinutes(MinLeadMinutes);
        var day = DateOnly.FromDateTime(limit);
        if (date < day) return TimeOnly.MaxValue;
        if (date > day) return null;
        return TimeOnly.FromDateTime(limit);
    }

    public static IEnumerable<(TimeOnly Start, TimeOnly End)> Expand(IEnumerable<WeeklySchedule> rules)
    {
        var seen = new HashSet<TimeOnly>();
        foreach (var r in rules.OrderBy(r => r.StartTime))
        {
            var len = Math.Clamp(r.SlotMinutes, 5, 240);
            var t = r.StartTime;
            while (true)
            {
                var end = t.AddMinutes(len);
                // عبور از نیمه‌شب یا خروج از بازه
                if (end <= t || end > r.EndTime) break;
                if (seen.Add(t)) yield return (t, end);
                t = end;
            }
        }
    }

    public static int CountSlots(WeeklySchedule r) => Expand(new[] { r }).Count();
}
