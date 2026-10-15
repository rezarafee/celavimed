using Celavimed.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Celavimed.Controllers;

[Route("account")]
[AutoValidateAntiforgeryToken]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;

    public AccountController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
    {
        _signIn = signIn;
        _users = users;
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true) return Redirect("/admin");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(string userName, string password, bool remember, string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.UserName = userName;
        var user = string.IsNullOrWhiteSpace(userName) ? null : await _users.FindByNameAsync(userName.Trim());
        if (user == null || !user.IsActive)
        {
            ViewBag.Error = user is { IsActive: false } ? "این حساب غیرفعال شده است. با مدیر تماس بگیرید." : "نام کاربری یا رمز عبور درست نیست.";
            return View();
        }
        var res = await _signIn.PasswordSignInAsync(user, password ?? "", remember, lockoutOnFailure: true);
        if (res.Succeeded)
            return LocalRedirect(Url.IsLocalUrl(returnUrl) && returnUrl!.StartsWith("/admin") ? returnUrl : "/admin");
        ViewBag.Error = res.IsLockedOut ? "به خاطر تلاش‌های ناموفق، حساب چند دقیقه قفل شد." : "نام کاربری یا رمز عبور درست نیست.";
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
    public IActionResult Password() => View();

    [HttpPost("password")]
    [Authorize]
    public async Task<IActionResult> Password(string current, string next, string confirm)
    {
        if (next != confirm)
        {
            ViewBag.Error = "تکرار رمز جدید با خودش یکی نیست.";
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
        await _signIn.RefreshSignInAsync(user);
        TempData["Flash"] = "رمز عبور عوض شد.";
        return Redirect("/admin");
    }
}
