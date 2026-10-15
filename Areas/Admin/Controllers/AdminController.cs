using Celavimed.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Celavimed.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Everyone)]
[AutoValidateAntiforgeryToken]
public abstract class AdminController : Controller
{
    protected void Flash(string message, bool error = false)
    {
        TempData["Flash"] = message;
        if (error) TempData["FlashError"] = "1";
    }

    protected void Nav(string key, string title)
    {
        ViewData["Nav"] = key;
        ViewData["Title"] = title;
    }
}
