using System.Security.Claims;
using Celavimed.Data;
using Celavimed.Models;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Services;

/// <summary>
/// منشی فقط پزشکانی را می‌بیند که به او سپرده شده‌اند؛ بقیه‌ی نقش‌ها همه‌ی پزشکان را می‌بینند.
/// </summary>
public class AccessScope
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private HashSet<int>? _cache;
    private bool _loaded;

    public AccessScope(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    private ClaimsPrincipal User => _http.HttpContext!.User;

    public bool IsManager => User.IsInRole(Roles.SuperAdmin) || User.IsInRole(Roles.Admin);
    public bool CanEditFlow => IsManager || User.IsInRole(Roles.Secretary) || User.IsInRole(Roles.Finance);
    public bool CanManageSchedules => IsManager || User.IsInRole(Roles.Secretary);

    /// <summary>null یعنی دسترسی به همه‌ی پزشکان</summary>
    public async Task<HashSet<int>?> DoctorIdsAsync()
    {
        if (_loaded) return _cache;
        _loaded = true;
        var restricted = User.IsInRole(Roles.Secretary)
                         && !IsManager && !User.IsInRole(Roles.Finance);
        if (!restricted) return _cache = null;
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _cache = (await _db.DoctorSecretaries.Where(x => x.UserId == uid).Select(x => x.DoctorId).ToListAsync()).ToHashSet();
        return _cache;
    }

    public async Task<bool> CanAccessDoctorAsync(int doctorId)
    {
        var ids = await DoctorIdsAsync();
        return ids == null || ids.Contains(doctorId);
    }

    public async Task<IQueryable<Appointment>> AppointmentsAsync()
    {
        var ids = await DoctorIdsAsync();
        var q = _db.Appointments.AsQueryable();
        if (ids != null) q = q.Where(a => ids.Contains(a.DoctorId));
        return q;
    }

    public async Task<IQueryable<Doctor>> DoctorsAsync()
    {
        var ids = await DoctorIdsAsync();
        var q = _db.Doctors.AsQueryable();
        if (ids != null) q = q.Where(d => ids.Contains(d.Id));
        return q;
    }
}
