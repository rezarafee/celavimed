using System.Net;
using System.Threading.RateLimiting;
using Celavimed.Data;
using Celavimed.Models;
using Celavimed.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);
var isDev = builder.Environment.IsDevelopment();

var provider = builder.Configuration.GetValue("Database:Provider", "SqlServer")!;
var conn = builder.Configuration.GetConnectionString("Default")
           ?? throw new InvalidOperationException("ConnectionStrings:Default تنظیم نشده است.");

builder.Services.AddDbContext<AppDbContext>(o =>
{
    if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase)) o.UseSqlite(conn);
    else o.UseSqlServer(conn);
});

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(o =>
    {
        o.Password.RequiredLength = 8;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireLowercase = false;
        o.Password.RequireDigit = true;
        o.User.RequireUniqueEmail = false;
        o.Lockout.MaxFailedAccessAttempts = 6;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        o.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager<AppSignInManager>()
    .AddErrorDescriber<PersianIdentityErrors>()
    .AddClaimsPrincipalFactory<AppClaimsFactory>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/account/login";
    o.AccessDeniedPath = "/account/denied";
    o.Cookie.Name = "celavimed.auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = isDev ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromHours(12);
    o.SlidingExpiration = true;
});

// کاربری که غیرفعال یا رمزش عوض شود، حداکثر بعد از ۲ دقیقه از پنل بیرون می‌افتد (پیش‌فرض ۳۰ دقیقه است)
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(2));

builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "RequestVerificationToken";
    o.Cookie.Name = "celavimed.csrf";
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = isDev ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});

// کلیدهای رمزنگاری کوکی‌ها؛ اگر مسیر تنظیم شود، با ری‌استارت سرور کاربران از پنل بیرون نمی‌افتند
if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath)).SetApplicationName("Celavimed");

// اگر سایت پشت nginx یا IIS به‌عنوان reverse proxy اجرا شود، IP واقعی بازدیدکننده از هدر X-Forwarded-For خوانده می‌شود
var behindProxy = builder.Configuration.GetValue("ReverseProxy:Enabled", false);
if (behindProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        foreach (var ip in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
            if (IPAddress.TryParse(ip, out var addr)) o.KnownProxies.Add(addr);
    });
}
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SlotService>();
builder.Services.AddScoped<WorkflowService>();
builder.Services.AddScoped<AccessScope>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddControllersWithViews();

// محدودیت نرخ درخواست برای هر IP
static string Ip(HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString() ?? "anon";
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // ثبت رزرو: جلوگیری از رزروهای اسپم
    o.AddPolicy("booking", ctx => RateLimitPartition.GetFixedWindowLimiter(Ip(ctx),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(10) }));
    // ورود: جلوگیری از حدس زدن رمز روی نام‌های کاربری مختلف از یک IP
    o.AddPolicy("login", ctx => HttpMethods.IsPost(ctx.Request.Method)
        ? RateLimitPartition.GetFixedWindowLimiter(Ip(ctx), _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(5) })
        : RateLimitPartition.GetNoLimiter("get"));
    // خواندن تقویم و ساعت‌های آزاد: جلوگیری از فشار روی دیتابیس
    o.AddPolicy("public-read", ctx => RateLimitPartition.GetSlidingWindowLimiter(Ip(ctx),
        _ => new SlidingWindowRateLimiterOptions { PermitLimit = 90, Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = 6 }));
});

var app = builder.Build();

if (behindProxy) app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    await SeedData.RunAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseSecurityHeaders();
app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseForcedPasswordChange();
app.UseAuthorization();

app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();
