using Celavimed.Data;
using Celavimed.Models;
using Celavimed.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Areas.Admin.Controllers;

/// <summary>جستجوی سریع بیمار با نام، موبایل یا کد پیگیری (کادر داشبورد و نوار بالا)</summary>
[Route("admin/search")]
[Authorize(Roles = Roles.Staff)]
public class SearchController : AdminController
{
    private readonly AppDbContext _db;
    private readonly AccessScope _scope;

    public SearchController(AppDbContext db, AccessScope scope)
    {
        _db = db;
        _scope = scope;
    }

    [HttpGet("patients")]
    public async Task<IActionResult> Patients(string? q)
    {
        var term = Jalali.NormalizeDigits(q ?? "").Trim();
        if (term.Length < 2 || term.Length > 60) return Ok(Array.Empty<object>());

        // شماره را بدون فاصله و خط تیره و با ۰ به‌جای ‎+98 مقایسه می‌کنیم
        var digits = new string(term.Where(char.IsDigit).ToArray());
        var isNumber = digits.Length >= 3 && term.All(c => char.IsDigit(c) || c is ' ' or '-' or '+');
        if (isNumber && digits.StartsWith("98") && term.StartsWith('+')) digits = "0" + digits[2..];
        var code = term.ToUpperInvariant();

        var ids = await _scope.DoctorIdsAsync();
        var query = _db.Patients.AsQueryable();
        if (ids != null) query = query.Where(p => p.Appointments.Any(a => ids.Contains(a.DoctorId)));
        query = isNumber
            ? query.Where(p => p.Mobile.Contains(digits))
            : query.Where(p => p.FullName.Contains(term) || p.Appointments.Any(a => a.TrackingCode == code));

        var today = Clock.Today;
        var list = await query
            .OrderBy(p => isNumber ? (p.Mobile.StartsWith(digits) ? 0 : 1) : (p.FullName.StartsWith(term) ? 0 : 1))
            .ThenByDescending(p => p.Appointments.Max(a => (DateOnly?)a.Date))
            .Take(8)
            .Select(p => new
            {
                p.Id,
                p.FullName,
                p.Mobile,
                Visits = p.Appointments.Count(a => ids == null || ids.Contains(a.DoctorId)),
                Next = p.Appointments
                    .Where(a => a.Status == AppointmentStatus.Active && a.Date >= today && (ids == null || ids.Contains(a.DoctorId)))
                    .OrderBy(a => a.Date).ThenBy(a => a.StartTime)
                    .Select(a => new { a.Date, a.StartTime, Doctor = a.Doctor.FullName }).FirstOrDefault(),
                Last = p.Appointments.Where(a => a.Date < today && (ids == null || ids.Contains(a.DoctorId)))
                    .Max(a => (DateOnly?)a.Date),
            })
            .ToListAsync();

        return Ok(list.Select(p => new
        {
            id = p.Id,
            name = p.FullName,
            mobile = Ui.Mobile(p.Mobile),
            visits = p.Visits,
            next = p.Next == null ? null : $"{Jalali.DayMonth(p.Next.Date)}، ساعت {Jalali.Time(p.Next.StartTime)} · {p.Next.Doctor}",
            last = p.Last.HasValue ? Jalali.Short(p.Last.Value) : null,
            url = $"/admin/patients/{p.Id}",
        }));
    }
}
