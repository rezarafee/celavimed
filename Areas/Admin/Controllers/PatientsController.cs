using Celavimed.Areas.Admin.Models;
using Celavimed.Data;
using Celavimed.Models;
using Celavimed.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Areas.Admin.Controllers;

[Route("admin/patients")]
[Authorize(Roles = Roles.Staff)]
public class PatientsController : AdminController
{
    private readonly AppDbContext _db;
    private readonly AccessScope _scope;

    public PatientsController(AppDbContext db, AccessScope scope)
    {
        _db = db;
        _scope = scope;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q)
    {
        Nav("patients", "بیماران");
        var ids = await _scope.DoctorIdsAsync();
        var query = _db.Patients.AsQueryable();
        if (ids != null) query = query.Where(p => p.Appointments.Any(a => ids.Contains(a.DoctorId)));
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = Jalali.NormalizeDigits(q.Trim());
            query = query.Where(p => p.FullName.Contains(term) || p.Mobile.Contains(term));
        }
        ViewBag.Q = q;
        var list = await query
            .OrderByDescending(p => p.Appointments.Max(a => (DateOnly?)a.Date))
            .Take(200)
            .Select(p => new PatientRow(p.Id, p.FullName, p.Mobile, p.Appointments.Count,
                p.Appointments.OrderByDescending(a => a.Date).Select(a => (DateOnly?)a.Date).FirstOrDefault(),
                p.Appointments.OrderByDescending(a => a.Date).Select(a => a.Id).FirstOrDefault()))
            .ToListAsync();
        return View(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient == null) return NotFound();
        var ids = await _scope.DoctorIdsAsync();
        var q = _db.Appointments.Where(a => a.PatientId == id);
        if (ids != null)
        {
            if (!await q.AnyAsync(a => ids.Contains(a.DoctorId))) return Forbid();
            q = q.Where(a => ids.Contains(a.DoctorId));
        }
        Nav("patients", "خلاصه‌ی وضعیت بیمار");
        var stages = await _db.Stages.Where(s => s.IsActive).OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToListAsync();
        var vm = new PatientSummaryVm
        {
            Patient = patient,
            Appointments = await q.Include(a => a.Doctor).Include(a => a.Service).Include(a => a.Stage)
                .Include(a => a.StageLogs).ThenInclude(l => l.Stage)
                .OrderByDescending(a => a.Date).ThenByDescending(a => a.StartTime).ToListAsync(),
            FinalStageId = stages.LastOrDefault()?.Id,
            Stages = stages,
            CanEdit = _scope.CanEditFlow,
        };
        return View(vm);
    }

    [HttpPost("{id:int}")]
    public async Task<IActionResult> Save(int id, string fullName, string? notes)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient == null) return NotFound();
        var ids = await _scope.DoctorIdsAsync();
        if (ids != null && !await _db.Appointments.AnyAsync(a => a.PatientId == id && ids.Contains(a.DoctorId))) return Forbid();
        if (!string.IsNullOrWhiteSpace(fullName)) patient.FullName = fullName.Trim();
        patient.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        await _db.SaveChangesAsync();
        Flash("مشخصات بیمار ذخیره شد.");
        return Redirect($"/admin/patients/{id}");
    }

    public record PatientRow(int Id, string FullName, string Mobile, int Visits, DateOnly? LastVisit, int LastAppointmentId);
}
