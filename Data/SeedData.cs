using Celavimed.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Data;

public static class SeedData
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var config = sp.GetRequiredService<IConfiguration>();
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");

        if (config.GetValue("Database:Provider", "SqlServer")!.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var r in Roles.All)
            if (!await roles.RoleExistsAsync(r))
                await roles.CreateAsync(new IdentityRole(r));

        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        if (!(await users.GetUsersInRoleAsync(Roles.SuperAdmin)).Any())
        {
            var userName = config["Seed:SuperAdminUserName"] ?? "superadmin";
            var password = config["Seed:SuperAdminPassword"] ?? "Celavi@1405";
            var admin = new ApplicationUser { UserName = userName, FullName = "مدیر ارشد", EmailConfirmed = true };
            var res = await users.CreateAsync(admin, password);
            if (res.Succeeded)
            {
                await users.AddToRoleAsync(admin, Roles.SuperAdmin);
                log.LogWarning("Super admin '{User}' created. Change its password after first login.", userName);
            }
            else
            {
                log.LogError("Could not create super admin: {Errors}", string.Join("; ", res.Errors.Select(e => e.Description)));
            }
        }

        if (!await db.Stages.AnyAsync())
        {
            db.Stages.AddRange(
                new WorkflowStage { Name = "رزرو", Description = "نوبت آنلاین ثبت شده و منتظر تماس منشی است.", Color = "#7A5C8E", Icon = "calendar-plus", SortOrder = 1 },
                new WorkflowStage { Name = "تایید منشی", Description = "منشی تماس گرفته و حضور بیمار قطعی شده است.", Color = "#2F7F78", Icon = "phone-call", SortOrder = 2 },
                new WorkflowStage { Name = "مشاوره", Description = "بیمار در مرکز حاضر است و مشاوره می‌گیرد.", Color = "#B07A2E", Icon = "messages-square", SortOrder = 3 },
                new WorkflowStage { Name = "مالی", Description = "پرداخت هزینه در واحد مالی.", Color = "#3F5F8F", Icon = "wallet", SortOrder = 4 },
                new WorkflowStage { Name = "تزریق / پزشک", Description = "انجام خدمت توسط پزشک.", Color = "#A8466B", Icon = "syringe", SortOrder = 5 },
                new WorkflowStage { Name = "خروج", Description = "کار بیمار تمام شده و از مرکز خارج شده است.", Color = "#5B6660", Icon = "door-open", SortOrder = 6 });
            await db.SaveChangesAsync();
        }

        if (config.GetValue("Seed:DemoData", false) && !await db.Doctors.AnyAsync())
            await SeedDemoAsync(db, users);
    }

    private static async Task SeedDemoAsync(AppDbContext db, UserManager<ApplicationUser> users)
    {
        var services = new[]
        {
            new ClinicService { Name = "بوتاکس", Description = "رفع چین و چروک پیشانی و دور چشم", SortOrder = 1 },
            new ClinicService { Name = "فیلر", Description = "حجم‌دهی لب، گونه و خط خنده", SortOrder = 2 },
            new ClinicService { Name = "لیزر موهای زائد", Description = "لیزر الکساندرایت و دایود", SortOrder = 3 },
            new ClinicService { Name = "مزوتراپی", Description = "جوان‌سازی و درمان ریزش مو", SortOrder = 4 },
            new ClinicService { Name = "پاکسازی پوست", Description = "هیدرافیشیال و پاکسازی عمقی", SortOrder = 5 },
            new ClinicService { Name = "مشاوره پوست", Description = "معاینه و برنامه‌ی درمانی", SortOrder = 6 },
        };
        db.Services.AddRange(services);

        var doctors = new[]
        {
            new Doctor { FullName = "دکتر سارا مقدم", Headline = "متخصص پوست، مو و زیبایی", MedicalCode = "۱۲۳۴۵۶", SortOrder = 1,
                Bio = "بیش از دوازده سال تجربه در تزریقات زیبایی و درمان‌های غیرجراحی پوست." },
            new Doctor { FullName = "دکتر نیما کاظمی", Headline = "پزشک زیبایی و لیزر", MedicalCode = "۲۳۴۵۶۷", SortOrder = 2,
                Bio = "متمرکز بر لیزرهای پوستی و جوان‌سازی با روش‌های کم‌تهاجمی." },
            new Doctor { FullName = "دکتر مهسا رضوی", Headline = "متخصص پوست و مو", MedicalCode = "۳۴۵۶۷۸", SortOrder = 3,
                Bio = "درمان ریزش مو، مزوتراپی و مراقبت‌های تخصصی پوست حساس." },
        };
        db.Doctors.AddRange(doctors);
        await db.SaveChangesAsync();

        void Link(Doctor d, params int[] idx)
        {
            foreach (var i in idx) db.DoctorServices.Add(new DoctorService { DoctorId = d.Id, ServiceId = services[i].Id });
        }
        Link(doctors[0], 0, 1, 5);
        Link(doctors[1], 2, 4, 0);
        Link(doctors[2], 3, 4, 5);

        void Week(Doctor d, TimeOnly from, TimeOnly to, params DayOfWeek[] days)
        {
            foreach (var day in days)
                db.WeeklySchedules.Add(new WeeklySchedule { DoctorId = d.Id, DayOfWeek = day, StartTime = from, EndTime = to, SlotMinutes = 15 });
        }
        Week(doctors[0], new(10, 0), new(13, 0), DayOfWeek.Saturday, DayOfWeek.Monday, DayOfWeek.Wednesday);
        Week(doctors[0], new(16, 0), new(19, 0), DayOfWeek.Saturday, DayOfWeek.Monday);
        Week(doctors[1], new(9, 0), new(14, 0), DayOfWeek.Sunday, DayOfWeek.Tuesday, DayOfWeek.Thursday);
        Week(doctors[2], new(15, 0), new(20, 0), DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Tuesday, DayOfWeek.Wednesday);

        var secretary = new ApplicationUser { UserName = "monshi", FullName = "مریم احمدی", EmailConfirmed = true };
        if ((await users.CreateAsync(secretary, "Monshi@1405")).Succeeded)
        {
            await users.AddToRoleAsync(secretary, Roles.Secretary);
            foreach (var d in doctors) db.DoctorSecretaries.Add(new DoctorSecretary { DoctorId = d.Id, UserId = secretary.Id });
        }
        await db.SaveChangesAsync();
    }
}
