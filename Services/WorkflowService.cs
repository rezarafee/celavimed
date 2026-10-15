using System.Security.Claims;
using System.Security.Cryptography;
using Celavimed.Data;
using Celavimed.Models;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Services;

public class WorkflowService
{
    private readonly AppDbContext _db;

    public WorkflowService(AppDbContext db) => _db = db;

    public Task<List<WorkflowStage>> ActiveStagesAsync() =>
        _db.Stages.Where(s => s.IsActive).OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToListAsync();

    public async Task<WorkflowStage> InitialStageAsync() =>
        (await ActiveStagesAsync()).FirstOrDefault()
        ?? throw new InvalidOperationException("هیچ مرحله‌ی فعالی تعریف نشده است.");

    /// <summary>مرحله‌ی بعد از «رزرو» که منشی با تایید تلفنی بیمار را به آن می‌برد.</summary>
    public async Task<WorkflowStage?> ConfirmStageAsync()
    {
        var stages = await ActiveStagesAsync();
        return stages.Count > 1 ? stages[1] : null;
    }

    public async Task MoveAsync(Appointment appt, int stageId, ClaimsPrincipal by, string? note = null)
    {
        if (appt.StageId == stageId && string.IsNullOrWhiteSpace(note)) return;
        var stages = await ActiveStagesAsync();
        var first = stages.FirstOrDefault();
        appt.StageId = stageId;
        if (first != null && stageId != first.Id && appt.ConfirmedAt == null)
            appt.ConfirmedAt = DateTime.UtcNow;
        _db.StageLogs.Add(new AppointmentStageLog
        {
            Appointment = appt,
            StageId = stageId,
            ChangedBy = DisplayName(by),
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        });
    }

    public static string? DisplayName(ClaimsPrincipal? user) =>
        user?.FindFirst("FullName")?.Value is { Length: > 0 } n ? n : user?.Identity?.Name;

    public async Task<string> NewTrackingCodeAsync()
    {
        const string alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        while (true)
        {
            var code = new string(Enumerable.Range(0, 6).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray());
            if (!await _db.Appointments.AnyAsync(a => a.TrackingCode == code)) return code;
        }
    }
}
