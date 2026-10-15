namespace Celavimed.Services;

/// <summary>زمان فعلی به وقت ایران؛ نوبت‌ها به وقت محلی تهران ذخیره می‌شوند.</summary>
public static class Clock
{
    private static readonly TimeZoneInfo Tehran = FindTehran();

    private static TimeZoneInfo FindTehran()
    {
        foreach (var id in new[] { "Asia/Tehran", "Iran Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("Tehran", TimeSpan.FromMinutes(210), "Tehran", "Tehran");
    }

    public static DateTime NowIran => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Tehran);
    public static DateOnly Today => DateOnly.FromDateTime(NowIran);
    public static DateTime ToIran(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Tehran);
}
