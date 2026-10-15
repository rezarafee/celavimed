using Celavimed.Data;
using Celavimed.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Areas.Admin.Controllers;

[Route("admin/stages")]
[Authorize(Roles = Roles.Managers)]
public class StagesController : AdminController
{
    public static readonly string[] IconChoices =
    {
        "calendar-plus", "phone-call", "messages-square", "wallet", "syringe", "door-open",
        "stethoscope", "sparkles", "heart-pulse", "clipboard-list", "notebook-pen", "badge-check", "clock", "user",
    };

    private readonly AppDbContext _db;

    public StagesController(AppDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index(int? edit)
    {
        Nav("stages", "مراحل پذیرش");
        ViewBag.List = await _db.Stages.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToListAsync();
        ViewBag.Usage = await _db.Appointments.Where(a => a.Status == AppointmentStatus.Active)
            .GroupBy(a => a.StageId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        var form = edit.HasValue ? await _db.Stages.FindAsync(edit.Value) : null;
        return View(form ?? new WorkflowStage { SortOrder = await _db.Stages.CountAsync() + 1, Icon = "clipboard-list" });
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save(WorkflowStage form)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(form.Color ?? "", "^#[0-9A-Fa-f]{6}$")) form.Color = "#7B4A82";
        if (!IconChoices.Contains(form.Icon)) form.Icon = "clipboard-list";
        if (!ModelState.IsValid)
        {
            Flash("نام مرحله را وارد کنید.", true);
            return Redirect("/admin/stages");
        }
        if (form.Id == 0)
        {
            form.Name = form.Name.Trim();
            _db.Stages.Add(form);
        }
        else
        {
            var s = await _db.Stages.FindAsync(form.Id);
            if (s == null) return NotFound();
            if (!form.IsActive && s.IsActive && await _db.Stages.CountAsync(x => x.IsActive) <= 2)
            {
                Flash("حداقل دو مرحله‌ی فعال لازم است.", true);
                return Redirect("/admin/stages");
            }
            s.Name = form.Name.Trim();
            s.Description = form.Description?.Trim();
            s.Color = form.Color!;
            s.Icon = form.Icon;
            s.SortOrder = form.SortOrder;
            s.IsActive = form.IsActive;
        }
        await _db.SaveChangesAsync();
        Flash("مراحل به‌روز شد.");
        return Redirect("/admin/stages");
    }

    [HttpPost("{id:int}/move")]
    public async Task<IActionResult> Move(int id, int dir)
    {
        var list = await _db.Stages.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToListAsync();
        var i = list.FindIndex(s => s.Id == id);
        var j = i + Math.Sign(dir);
        if (i >= 0 && j >= 0 && j < list.Count)
        {
            (list[i], list[j]) = (list[j], list[i]);
            for (var k = 0; k < list.Count; k++) list[k].SortOrder = k + 1;
            await _db.SaveChangesAsync();
        }
        return Redirect("/admin/stages");
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _db.Stages.FindAsync(id);
        if (s == null) return NotFound();
        var used = await _db.Appointments.AnyAsync(a => a.StageId == id) || await _db.StageLogs.AnyAsync(l => l.StageId == id);
        if (await _db.Stages.CountAsync(x => x.IsActive) <= 2 && s.IsActive)
            Flash("حداقل دو مرحله‌ی فعال لازم است.", true);
        else if (used)
        {
            s.IsActive = false;
            await _db.SaveChangesAsync();
            Flash($"«{s.Name}» در سابقه‌ی بیماران استفاده شده، برای همین غیرفعال شد. نوبت‌هایی که در این مرحله بودند در اولین مرحله نشان داده می‌شوند.");
        }
        else
        {
            _db.Stages.Remove(s);
            await _db.SaveChangesAsync();
            Flash($"«{s.Name}» حذف شد.");
        }
        return Redirect("/admin/stages");
    }
}
