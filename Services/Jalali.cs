using System.Globalization;
using System.Text;

namespace Celavimed.Services;

/// <summary>تبدیل و نمایش تاریخ شمسی</summary>
public static class Jalali
{
    private static readonly PersianCalendar Pc = new();

    public static readonly string[] MonthNames =
    {
        "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند",
    };

    // شنبه اول هفته
    public static readonly DayOfWeek[] WeekOrder =
    {
        DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday,
        DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday,
    };

    public static string DayName(DayOfWeek d) => d switch
    {
        DayOfWeek.Saturday => "شنبه",
        DayOfWeek.Sunday => "یکشنبه",
        DayOfWeek.Monday => "دوشنبه",
        DayOfWeek.Tuesday => "سه‌شنبه",
        DayOfWeek.Wednesday => "چهارشنبه",
        DayOfWeek.Thursday => "پنجشنبه",
        _ => "جمعه",
    };

    /// <summary>ایندکس روز در هفته‌ی ایرانی (شنبه = ۰)</summary>
    public static int PersianWeekIndex(DayOfWeek d) => ((int)d + 1) % 7;

    public static (int Year, int Month, int Day) Parts(DateOnly date)
    {
        var dt = date.ToDateTime(TimeOnly.MinValue);
        return (Pc.GetYear(dt), Pc.GetMonth(dt), Pc.GetDayOfMonth(dt));
    }

    public static DateOnly ToGregorian(int year, int month, int day) =>
        DateOnly.FromDateTime(Pc.ToDateTime(year, month, day, 0, 0, 0, 0));

    public static int DaysInMonth(int year, int month) => Pc.GetDaysInMonth(year, month);

    public static string Short(DateOnly date)
    {
        var (y, m, d) = Parts(date);
        return $"{y:0000}/{m:00}/{d:00}";
    }

    public static string Long(DateOnly date)
    {
        var (y, m, d) = Parts(date);
        return $"{DayName(date.DayOfWeek)} {d} {MonthNames[m - 1]} {y}";
    }

    public static string DayMonth(DateOnly date)
    {
        var (_, m, d) = Parts(date);
        return $"{d} {MonthNames[m - 1]}";
    }

    public static string Time(TimeOnly t) => t.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static string DateTimeShort(DateTime utc)
    {
        var local = Clock.ToIran(utc);
        return $"{Short(DateOnly.FromDateTime(local))} · {local:HH:mm}";
    }

    /// <summary>ارقام فارسی و عربی را به لاتین تبدیل می‌کند (برای ورودی موبایل و ...)</summary>
    public static string NormalizeDigits(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (ch >= '۰' && ch <= '۹') sb.Append((char)('0' + (ch - '۰')));
            else if (ch >= '٠' && ch <= '٩') sb.Append((char)('0' + (ch - '٠')));
            else sb.Append(ch);
        }
        return sb.ToString();
    }
}
