using System.Security.Claims;
using Celavimed.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Celavimed.Services;

/// <summary>
/// کاربر غیرفعال حتی با رمز درست هم وارد نمی‌شود. چون این بررسی بعد از چک رمز انجام می‌شود،
/// کسی که رمز را نمی‌داند نمی‌فهمد یک نام کاربری وجود دارد یا غیرفعال است.
/// </summary>
public class AppSignInManager : SignInManager<ApplicationUser>
{
    public AppSignInManager(UserManager<ApplicationUser> userManager, IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory, IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> logger, IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation) { }

    public override async Task<bool> CanSignInAsync(ApplicationUser user) =>
        user.IsActive && await base.CanSignInAsync(user);
}

/// <summary>هدرهای امنیتی برای همه‌ی پاسخ‌ها</summary>
public static class SecurityHeaders
{
    // فقط فایل‌های خود سایت؛ هیچ اسکریپت درون‌خطی یا CDN مجاز نیست.
    private const string Csp =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: blob:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (ctx, next) =>
        {
            ctx.Response.OnStarting(() =>
            {
                var h = ctx.Response.Headers;
                h["Content-Security-Policy"] = Csp;
                h["X-Content-Type-Options"] = "nosniff";
                h["X-Frame-Options"] = "DENY";
                h["Referrer-Policy"] = "strict-origin-when-cross-origin";
                h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
                h["Cross-Origin-Opener-Policy"] = "same-origin";
                // صفحه‌های پنل و حساب نباید در کش مرورگر یا پراکسی بمانند
                var path = ctx.Request.Path;
                if (path.StartsWithSegments("/admin") || path.StartsWithSegments("/account"))
                {
                    h["Cache-Control"] = "no-store";
                    h["Pragma"] = "no-cache";
                }
                return Task.CompletedTask;
            });
            await next();
        });

    /// <summary>
    /// کاربری که باید رمزش را عوض کند (رمز پیش‌فرض یا رمز موقتی که مدیر داده)
    /// تا وقتی رمز را عوض نکرده به هیچ صفحه‌ی دیگری از پنل دسترسی ندارد.
    /// </summary>
    public static IApplicationBuilder UseForcedPasswordChange(this IApplicationBuilder app) =>
        app.Use(async (ctx, next) =>
        {
            var p = ctx.Request.Path;
            if (ctx.User.Identity?.IsAuthenticated == true
                && ctx.User.HasClaim(AppClaims.MustChangePassword, "1")
                && p.StartsWithSegments("/admin"))
            {
                if (HttpMethods.IsGet(ctx.Request.Method) || HttpMethods.IsHead(ctx.Request.Method))
                    ctx.Response.Redirect("/account/password");
                else
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            await next();
        });
}

public static class AppClaims
{
    public const string FullName = "FullName";
    public const string MustChangePassword = "mcp";
}

/// <summary>بررسی محتوای واقعی فایل تصویر (نه فقط پسوند)</summary>
public static class ImageSniffer
{
    public static async Task<bool> LooksLikeImageAsync(IFormFile file, string ext)
    {
        var buf = new byte[12];
        await using var s = file.OpenReadStream();
        var n = await s.ReadAtLeastAsync(buf, buf.Length, throwOnEndOfStream: false);
        if (n < 12) return false;
        return ext switch
        {
            ".jpg" or ".jpeg" => buf[0] == 0xFF && buf[1] == 0xD8 && buf[2] == 0xFF,
            ".png" => buf[0] == 0x89 && buf[1] == 0x50 && buf[2] == 0x4E && buf[3] == 0x47,
            ".webp" => buf[0] == 'R' && buf[1] == 'I' && buf[2] == 'F' && buf[3] == 'F' && buf[8] == 'W' && buf[9] == 'E' && buf[10] == 'B' && buf[11] == 'P',
            _ => false,
        };
    }
}
