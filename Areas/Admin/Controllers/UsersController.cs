using Celavimed.Areas.Admin.Models;
using Celavimed.Data;
using Celavimed.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Areas.Admin.Controllers;

[Route("admin/users")]
[Authorize(Roles = Roles.Managers)]
public class UsersController : AdminController
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppDbContext _db;

    public UsersController(UserManager<ApplicationUser> users, AppDbContext db)
    {
        _users = users;
        _db = db;
    }

    /// <summary>فقط مدیر ارشد می‌تواند مدیر ارشد بسازد یا ویرایش کند.</summary>
    private List<string> Assignable() =>
        User.IsInRole(Roles.SuperAdmin) ? Roles.All.ToList() : Roles.All.Where(r => r != Roles.SuperAdmin).ToList();

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        Nav("users", "کاربران و دسترسی");
        var users = await _users.Users.Include(u => u.SecretaryOf).ThenInclude(s => s.Doctor).OrderBy(u => u.FullName).ToListAsync();
        var rows = new List<UserRow>();
        foreach (var u in users)
        {
            var roles = await _users.GetRolesAsync(u);
            rows.Add(new UserRow { User = u, Role = roles.FirstOrDefault() ?? "", Doctors = u.SecretaryOf.Select(s => s.Doctor.FullName).ToList() });
        }
        return View(rows);
    }

    [HttpGet("new")]
    public async Task<IActionResult> Create()
    {
        Nav("users", "کاربر جدید");
        return View("Edit", await Fill(new UserForm()));
    }

    [HttpGet("{id}/edit")]
    public async Task<IActionResult> Edit(string id)
    {
        var u = await _users.Users.Include(x => x.SecretaryOf).FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) return NotFound();
        var role = (await _users.GetRolesAsync(u)).FirstOrDefault() ?? Roles.User;
        if (role == Roles.SuperAdmin && !User.IsInRole(Roles.SuperAdmin)) return Forbid();
        Nav("users", "ویرایش کاربر");
        var form = new UserForm
        {
            Id = u.Id, FullName = u.FullName, UserName = u.UserName ?? "", PhoneNumber = u.PhoneNumber,
            Role = role, IsActive = u.IsActive, DoctorIds = u.SecretaryOf.Select(s => s.DoctorId).ToList(),
        };
        return View(await Fill(form));
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save(UserForm form)
    {
        if (!Assignable().Contains(form.Role)) ModelState.AddModelError(nameof(form.Role), "این نقش را نمی‌توانید بدهید.");
        if (string.IsNullOrEmpty(form.Id) && string.IsNullOrWhiteSpace(form.Password))
            ModelState.AddModelError(nameof(form.Password), "برای کاربر جدید رمز عبور لازم است.");

        ApplicationUser? u = null;
        if (!string.IsNullOrEmpty(form.Id))
        {
            u = await _users.Users.Include(x => x.SecretaryOf).FirstOrDefaultAsync(x => x.Id == form.Id);
            if (u == null) return NotFound();
            var current = (await _users.GetRolesAsync(u)).FirstOrDefault();
            if (current == Roles.SuperAdmin && !User.IsInRole(Roles.SuperAdmin)) return Forbid();
            if (u.UserName == User.Identity?.Name && (!form.IsActive || form.Role != current))
                ModelState.AddModelError(nameof(form.Role), "نقش یا وضعیت حساب خودتان را نمی‌توانید تغییر دهید.");
            if (current == Roles.SuperAdmin && form.Role != Roles.SuperAdmin && (await _users.GetUsersInRoleAsync(Roles.SuperAdmin)).Count <= 1)
                ModelState.AddModelError(nameof(form.Role), "حداقل یک مدیر ارشد باید وجود داشته باشد.");
        }

        if (!ModelState.IsValid)
        {
            Nav("users", form.Id == null ? "کاربر جدید" : "ویرایش کاربر");
            return View("Edit", await Fill(form));
        }

        IdentityResult result;
        if (u == null)
        {
            u = new ApplicationUser { UserName = form.UserName.Trim(), FullName = form.FullName.Trim(), PhoneNumber = form.PhoneNumber, IsActive = form.IsActive, EmailConfirmed = true };
            result = await _users.CreateAsync(u, form.Password!);
        }
        else
        {
            u.UserName = form.UserName.Trim();
            u.FullName = form.FullName.Trim();
            u.PhoneNumber = form.PhoneNumber;
            u.IsActive = form.IsActive;
            result = await _users.UpdateAsync(u);
            if (result.Succeeded && !string.IsNullOrWhiteSpace(form.Password))
            {
                await _users.RemovePasswordAsync(u);
                result = await _users.AddPasswordAsync(u, form.Password);
            }
            if (result.Succeeded) await _users.UpdateSecurityStampAsync(u);
        }
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            Nav("users", "کاربر");
            return View("Edit", await Fill(form));
        }

        var roles = await _users.GetRolesAsync(u);
        if (!(roles.Count == 1 && roles[0] == form.Role))
        {
            await _users.RemoveFromRolesAsync(u, roles);
            await _users.AddToRoleAsync(u, form.Role);
        }

        var links = await _db.DoctorSecretaries.Where(x => x.UserId == u.Id).ToListAsync();
        _db.DoctorSecretaries.RemoveRange(links);
        if (form.Role == Roles.Secretary)
            _db.DoctorSecretaries.AddRange(form.DoctorIds.Distinct().Select(d => new DoctorSecretary { DoctorId = d, UserId = u.Id }));
        await _db.SaveChangesAsync();

        Flash(string.IsNullOrEmpty(form.Id) ? $"کاربر {u.FullName} ساخته شد." : "تغییرات کاربر ذخیره شد.");
        return Redirect("/admin/users");
    }

    private async Task<UserForm> Fill(UserForm f)
    {
        f.AllDoctors = await _db.Doctors.OrderBy(d => d.SortOrder).ToListAsync();
        f.AssignableRoles = Assignable();
        return f;
    }
}
