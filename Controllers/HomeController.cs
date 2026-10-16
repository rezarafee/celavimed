using System.Text.Json;
using Celavimed.Data;
using Celavimed.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly SlotService _slots;

    public HomeController(AppDbContext db, SlotService slots)
    {
        _db = db;
        _slots = slots;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var doctors = await _db.Doctors
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.FullName)
            .Select(d => new
            {
                id = d.Id,
                name = d.FullName,
                headline = d.Headline,
                bio = d.Bio,
                photo = d.PhotoPath,
                hasSchedule = d.WeeklySchedules.Any(w => w.IsActive),
                services = d.Services.Where(s => s.Service.IsActive).OrderBy(s => s.Service.SortOrder)
                    .Select(s => new { id = s.Service.Id, name = s.Service.Name, description = s.Service.Description }),
            })
            .ToListAsync();
        ViewBag.DoctorsJson = JsonSerializer.Serialize(doctors);
        ViewBag.DoctorCount = doctors.Count;
        ViewBag.Next = await FindNextFreeAsync(doctors.Where(d => d.hasSchedule).Select(d => (d.id, d.name)));
        return View();
    }

    /// <summary>نزدیک‌ترین ساعت آزاد بین همه‌ی پزشکان، در دو هفته‌ی آینده</summary>
    private async Task<NextFreeSlot?> FindNextFreeAsync(IEnumerable<(int Id, string Name)> doctors)
    {
        var today = Clock.Today;
        var to = today.AddDays(Math.Min(14, _slots.BookingWindowDays));
        NextFreeSlot? best = null;
        foreach (var (id, name) in doctors)
        {
            var day = (await _slots.GetRangeAsync(id, today, to)).FirstOrDefault(d => d.FreeCount > 0);
            if (day == null || (best != null && day.Date > best.Date)) continue;
            var slot = (await _slots.GetSlotsAsync(id, day.Date, forPublic: true)).FirstOrDefault(s => s.IsFree);
            if (slot == null) continue;
            if (best == null || day.Date < best.Date || slot.Start < best.Start)
                best = new NextFreeSlot(id, name, day.Date, slot.Start);
        }
        return best;
    }

    [Route("/error/{code:int?}")]
    public IActionResult Error(int? code)
    {
        Response.StatusCode = code ?? 500;
        ViewBag.Code = code ?? 500;
        return View();
    }
}

public record NextFreeSlot(int DoctorId, string DoctorName, DateOnly Date, TimeOnly Start);
