using System.Text.Json;
using Celavimed.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

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
        return View();
    }

    [Route("/error/{code:int?}")]
    public IActionResult Error(int? code)
    {
        Response.StatusCode = code ?? 500;
        ViewBag.Code = code ?? 500;
        return View();
    }
}
