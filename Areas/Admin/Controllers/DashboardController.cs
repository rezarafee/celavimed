using Celavimed.Areas.Admin.Models;
using Celavimed.Models;
using Celavimed.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Areas.Admin.Controllers;

[Route("admin")]
public class DashboardController : AdminController
{
    private readonly AccessScope _scope;
    private readonly WorkflowService _flow;

    public DashboardController(AccessScope scope, WorkflowService flow)
    {
        _scope = scope;
        _flow = flow;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        Nav("dashboard", "داشبورد");
        var today = Clock.Today;
        var stages = await _flow.ActiveStagesAsync();
        var first = stages.First();
        var last = stages.Last();
        var q = (await _scope.AppointmentsAsync()).Where(a => a.Status == AppointmentStatus.Active);

        var todayList = await q.Where(a => a.Date == today)
            .Include(a => a.Patient).Include(a => a.Doctor).Include(a => a.Service).Include(a => a.Stage)
            .OrderBy(a => a.StartTime).ToListAsync();

        var since = DateTime.UtcNow.AddHours(-24);
        var vm = new DashboardVm
        {
            TodayTotal = todayList.Count,
            TodayDone = todayList.Count(a => a.StageId == last.Id),
            PendingConfirm = await q.CountAsync(a => a.StageId == first.Id && a.Date >= today),
            UpcomingWeek = await q.CountAsync(a => a.Date > today && a.Date <= today.AddDays(7)),
            NewOnline24h = await q.CountAsync(a => a.CreatedAt >= since),
            Today = todayList,
            StageCounts = stages.Select(s => (s, todayList.Count(a => a.StageId == s.Id))).ToList(),
            ToConfirm = await q.Where(a => a.StageId == first.Id && a.Date >= today)
                .Include(a => a.Patient).Include(a => a.Doctor).Include(a => a.Service)
                .OrderBy(a => a.Date).ThenBy(a => a.StartTime).Take(6).ToListAsync(),
        };
        return View(vm);
    }
}
