using Celavimed.Areas.Admin.Models;
using Celavimed.Models;
using Celavimed.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Areas.Admin.Controllers;

[Route("admin/board")]
public class BoardController : AdminController
{
    private readonly AccessScope _scope;
    private readonly WorkflowService _flow;

    public BoardController(AccessScope scope, WorkflowService flow)
    {
        _scope = scope;
        _flow = flow;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(DateOnly? date, int? doctorId)
    {
        Nav("board", "مراحل بیماران");
        var day = date ?? Clock.Today;
        var q = (await _scope.AppointmentsAsync())
            .Where(a => a.Date == day && a.Status == AppointmentStatus.Active);
        if (doctorId.HasValue) q = q.Where(a => a.DoctorId == doctorId);

        var vm = new BoardVm
        {
            Date = day,
            DoctorId = doctorId,
            Doctors = await (await _scope.DoctorsAsync()).OrderBy(d => d.SortOrder).ToListAsync(),
            Stages = await _flow.ActiveStagesAsync(),
            Items = await q.Include(a => a.Patient).Include(a => a.Doctor).Include(a => a.Service)
                .OrderBy(a => a.StartTime).ToListAsync(),
            CanEdit = _scope.CanEditFlow,
        };
        // نوبت‌هایی که در مرحله‌ی غیرفعال مانده‌اند در اولین ستون نشان داده می‌شوند
        var activeIds = vm.Stages.Select(s => s.Id).ToHashSet();
        foreach (var a in vm.Items.Where(a => !activeIds.Contains(a.StageId))) a.StageId = vm.Stages[0].Id;
        return View(vm);
    }
}
