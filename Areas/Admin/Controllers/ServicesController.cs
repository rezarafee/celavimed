using Celavimed.Data;
using Celavimed.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Areas.Admin.Controllers;

[Route("admin/services")]
[Authorize(Roles = Roles.FrontDesk)]
public class ServicesController : AdminController
{
    private readonly AppDbContext _db;

    public ServicesController(AppDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index(int? edit)
    {
        Nav("services", "خدمات و تخصص‌ها");
        ViewBag.List = await _db.Services.Include(s => s.Doctors).ThenInclude(d => d.Doctor)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync();
        var form = edit.HasValue ? await _db.Services.FindAsync(edit.Value) : null;
        return View(form ?? new ClinicService { SortOrder = await _db.Services.CountAsync() + 1 });
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save(ClinicService form)
    {
        form.Name = form.Name?.Trim() ?? "";
        if (await _db.Services.AnyAsync(s => s.Name == form.Name && s.Id != form.Id))
            ModelState.AddModelError(nameof(form.Name), "خدمتی با این نام وجود دارد.");
        if (!ModelState.IsValid)
        {
            Nav("services", "خدمات و تخصص‌ها");
            ViewBag.List = await _db.Services.Include(s => s.Doctors).ThenInclude(d => d.Doctor).OrderBy(s => s.SortOrder).ToListAsync();
            return View("Index", form);
        }
        if (form.Id == 0) _db.Services.Add(form);
        else
        {
            var s = await _db.Services.FindAsync(form.Id);
            if (s == null) return NotFound();
            s.Name = form.Name;
            s.Description = form.Description?.Trim();
            s.SortOrder = form.SortOrder;
            s.IsActive = form.IsActive;
        }
        await _db.SaveChangesAsync();
        Flash(form.Id == 0 ? $"«{form.Name}» اضافه شد." : "تغییرات ذخیره شد.");
        return Redirect("/admin/services");
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _db.Services.FindAsync(id);
        if (s == null) return NotFound();
        if (await _db.Appointments.AnyAsync(a => a.ServiceId == id))
        {
            s.IsActive = false;
            Flash("این خدمت در نوبت‌ها استفاده شده، برای همین غیرفعال شد.");
        }
        else
        {
            _db.Services.Remove(s);
            Flash("خدمت حذف شد.");
        }
        await _db.SaveChangesAsync();
        return Redirect("/admin/services");
    }
}
