using Celavimed.Areas.Admin.Models;
using Celavimed.Data;
using Celavimed.Models;
using Celavimed.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Areas.Admin.Controllers;

[Route("admin/doctors")]
[Authorize(Roles = Roles.FrontDesk)]
public class DoctorsController : AdminController
{
    private static readonly string[] AllowedExt = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxPhotoBytes = 4 * 1024 * 1024;

    private readonly AppDbContext _db;
    private readonly AccessScope _scope;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IWebHostEnvironment _env;

    public DoctorsController(AppDbContext db, AccessScope scope, UserManager<ApplicationUser> users, IWebHostEnvironment env)
    {
        _db = db;
        _scope = scope;
        _users = users;
        _env = env;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        Nav("doctors", "پزشکان");
        var list = await (await _scope.DoctorsAsync())
            .Include(d => d.Services).ThenInclude(s => s.Service)
            .Include(d => d.Secretaries).ThenInclude(s => s.User)
            .Include(d => d.WeeklySchedules)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.FullName)
            .ToListAsync();
        ViewBag.CanEdit = _scope.IsManager;
        return View(list);
    }

    [HttpGet("new")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> Create()
    {
        Nav("doctors", "پزشک جدید");
        var form = new DoctorForm { SortOrder = await _db.Doctors.CountAsync() + 1 };
        await FillLists(form);
        return View("Edit", form);
    }

    [HttpGet("{id:int}/edit")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> Edit(int id)
    {
        var d = await _db.Doctors.Include(x => x.Services).Include(x => x.Secretaries).FirstOrDefaultAsync(x => x.Id == id);
        if (d == null) return NotFound();
        Nav("doctors", "ویرایش " + d.FullName);
        var form = new DoctorForm
        {
            Id = d.Id, FullName = d.FullName, Headline = d.Headline, Bio = d.Bio, MedicalCode = d.MedicalCode,
            Phone = d.Phone, IsActive = d.IsActive, SortOrder = d.SortOrder, PhotoPath = d.PhotoPath,
            ServiceIds = d.Services.Select(s => s.ServiceId).ToList(),
            SecretaryIds = d.Secretaries.Select(s => s.UserId).ToList(),
        };
        await FillLists(form);
        return View(form);
    }

    [HttpPost("save")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> Save(DoctorForm form)
    {
        Doctor? d = form.Id == 0 ? new Doctor() : await _db.Doctors.Include(x => x.Services).Include(x => x.Secretaries).FirstOrDefaultAsync(x => x.Id == form.Id);
        if (d == null) return NotFound();

        if (form.Photo is { Length: > 0 })
        {
            var ext = Path.GetExtension(form.Photo.FileName).ToLowerInvariant();
            if (!AllowedExt.Contains(ext)) ModelState.AddModelError(nameof(form.Photo), "فقط عکس JPG، PNG یا WEBP قابل قبول است.");
            else if (form.Photo.Length > MaxPhotoBytes) ModelState.AddModelError(nameof(form.Photo), "حجم عکس باید کمتر از ۴ مگابایت باشد.");
        }

        if (!ModelState.IsValid)
        {
            Nav("doctors", form.Id == 0 ? "پزشک جدید" : "ویرایش " + form.FullName);
            form.PhotoPath = d.PhotoPath;
            await FillLists(form);
            return View("Edit", form);
        }

        d.FullName = form.FullName.Trim();
        d.Headline = form.Headline?.Trim();
        d.Bio = form.Bio?.Trim();
        d.MedicalCode = form.MedicalCode?.Trim();
        d.Phone = Jalali.NormalizeDigits(form.Phone).Trim();
        d.IsActive = form.IsActive;
        d.SortOrder = form.SortOrder;

        if (form.RemovePhoto && d.PhotoPath != null)
        {
            DeletePhoto(d.PhotoPath);
            d.PhotoPath = null;
        }
        if (form.Photo is { Length: > 0 })
        {
            if (d.PhotoPath != null) DeletePhoto(d.PhotoPath);
            var ext = Path.GetExtension(form.Photo.FileName).ToLowerInvariant();
            var name = $"{Guid.NewGuid():N}{ext}";
            var dir = Path.Combine(_env.WebRootPath, "uploads", "doctors");
            Directory.CreateDirectory(dir);
            await using (var fs = System.IO.File.Create(Path.Combine(dir, name)))
                await form.Photo.CopyToAsync(fs);
            d.PhotoPath = $"/uploads/doctors/{name}";
        }

        if (form.Id == 0) _db.Doctors.Add(d);

        d.Services.RemoveAll(s => !form.ServiceIds.Contains(s.ServiceId));
        foreach (var sid in form.ServiceIds.Where(sid => d.Services.All(s => s.ServiceId != sid)))
            d.Services.Add(new DoctorService { ServiceId = sid });

        d.Secretaries.RemoveAll(s => !form.SecretaryIds.Contains(s.UserId));
        foreach (var uid in form.SecretaryIds.Where(uid => d.Secretaries.All(s => s.UserId != uid)))
            d.Secretaries.Add(new DoctorSecretary { UserId = uid });

        await _db.SaveChangesAsync();
        Flash(form.Id == 0 ? "پزشک اضافه شد. حالا برنامه‌ی کاری‌اش را تنظیم کنید." : "تغییرات ذخیره شد.");
        return form.Id == 0 ? Redirect($"/admin/schedules/{d.Id}") : Redirect("/admin/doctors");
    }

    [HttpPost("{id:int}/delete")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> Delete(int id)
    {
        var d = await _db.Doctors.FindAsync(id);
        if (d == null) return NotFound();
        if (await _db.Appointments.AnyAsync(a => a.DoctorId == id))
        {
            d.IsActive = false;
            await _db.SaveChangesAsync();
            Flash("این پزشک نوبت ثبت‌شده دارد، برای همین به‌جای حذف غیرفعال شد.");
        }
        else
        {
            if (d.PhotoPath != null) DeletePhoto(d.PhotoPath);
            _db.Doctors.Remove(d);
            await _db.SaveChangesAsync();
            Flash("پزشک حذف شد.");
        }
        return Redirect("/admin/doctors");
    }

    private async Task FillLists(DoctorForm form)
    {
        form.AllServices = await _db.Services.Where(s => s.IsActive || form.ServiceIds.Contains(s.Id)).OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync();
        form.AllSecretaries = (await _users.GetUsersInRoleAsync(Roles.Secretary)).OrderBy(u => u.FullName).ToList();
    }

    private void DeletePhoto(string path)
    {
        var full = Path.Combine(_env.WebRootPath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (full.StartsWith(Path.Combine(_env.WebRootPath, "uploads")) && System.IO.File.Exists(full))
            System.IO.File.Delete(full);
    }
}
