using Celavimed.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Celavimed.Controllers;

[Route("account")]
[AutoValidateAntiforgeryToken]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IConfiguration _config;
    private readonly ILogger<AccountController> _log;

    public AccountController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users, IConfiguration config, ILogger<AccountController> log)
    {
        _signIn = signIn;
        _users = users;
        _config = config;
        _log = log;
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true) return Redirect("/admin");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(string userName, string password, bool remember, string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.UserName = userName;
        const string wrong = "نام کاربری یا رمز عبور درست نیست.";
        var user = string.IsNullOrWhiteSpace(userName) ? null : await _users.FindByNameAsync(userName.Trim());
        if (user == null)
        {
            ViewBag.Error = wrong;
            return View();
        }

        // اگر هنوز رمز پیش‌فرض نصب روی حساب است، قبل از ورود علامت «تغییر اجباری رمز» بخورد
        var seedPassword = _config["Seed:SuperAdminPassword"] ?? "Celavi@1405";
        if (!user.MustChangePassword && password == seedPassword && await _users.CheckPasswordAsync(user, password))
        {
            user.MustChangePassword = true;
            await _users.UpdateAsync(user);
        }

        var res = await _signIn.PasswordSignInAsync(user, password ?? "", remember, lockoutOnFailure: true);
        if (res.Succeeded)
        {
            _log.LogInformation("Login {User} from {Ip}", user.UserName, HttpContext.Connection.RemoteIpAddress);
            if (user.MustChangePassword) return Redirect("/account/password");
            return LocalRedirect(Url.IsLocalUrl(returnUrl) && returnUrl!.StartsWith("/admin") ? returnUrl : "/admin");
        }
        _log.LogWarning("Failed login {User} from {Ip} (locked: {Locked})", user.UserName, HttpContext.Connection.RemoteIpAddress, res.IsLockedOut);
        ViewBag.Error = res.IsLockedOut ? "به خاطر تلاش‌های ناموفق، حساب چند دقیقه قفل شد."
            : res.IsNotAllowed ? "این حساب غیرفعال شده است. با مدیر تماس بگیرید."
            : wrong;
        return View();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return Redirect("/account/login");
    }

    [HttpGet("denied")]
    public IActionResult Denied() => View();

    [HttpGet("password")]
    [Authorize]
    public IActionResult Password()
    {
        ViewBag.Forced = User.HasClaim(Services.AppClaims.MustChangePassword, "1");
        return View();
    }

    [HttpPost("password")]
    [Authorize]
    public async Task<IActionResult> Password(string current, string next, string confirm)
    {
        ViewBag.Forced = User.HasClaim(Services.AppClaims.MustChangePassword, "1");
        if (next != confirm)
        {
            ViewBag.Error = "تکرار رمز جدید با خودش یکی نیست.";
            return View();
        }
        if (!string.IsNullOrEmpty(next) && next == current)
        {
            ViewBag.Error = "رمز جدید باید با رمز فعلی فرق داشته باشد.";
            return View();
        }
        if (next == (_config["Seed:SuperAdminPassword"] ?? "Celavi@1405"))
        {
            ViewBag.Error = "این رمز پیش‌فرض نصب است و همه آن را می‌دانند؛ رمز دیگری انتخاب کنید.";
            return View();
        }
        var user = await _users.GetUserAsync(User);
        if (user == null) return Challenge();
        var res = await _users.ChangePasswordAsync(user, current ?? "", next ?? "");
        if (!res.Succeeded)
        {
            ViewBag.Error = string.Join(" ", res.Errors.Select(e => e.Description));
            return View();
        }
        if (user.MustChangePassword)
        {
            user.MustChangePassword = false;
            await _users.UpdateAsync(user);
        }
        await _signIn.RefreshSignInAsync(user);
        TempData["Flash"] = "رمز عبور عوض شد.";
        return Redirect("/admin");
    }
}
