namespace Celavimed.Models;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Secretary = "Secretary";
    public const string Finance = "Finance";
    public const string User = "User";

    public static readonly string[] All = { SuperAdmin, Admin, Secretary, Finance, User };

    // ترکیب‌های پرکاربرد برای [Authorize(Roles = ...)]
    public const string Managers = SuperAdmin + "," + Admin;
    public const string FrontDesk = SuperAdmin + "," + Admin + "," + Secretary;
    public const string Staff = SuperAdmin + "," + Admin + "," + Secretary + "," + Finance;
    public const string Everyone = SuperAdmin + "," + Admin + "," + Secretary + "," + Finance + "," + User;

    public static string Title(string role) => role switch
    {
        SuperAdmin => "مدیر ارشد",
        Admin => "مدیر",
        Secretary => "منشی",
        Finance => "مالی",
        User => "کاربر عادی",
        _ => role,
    };
}
