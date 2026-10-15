using System.Net;
using Microsoft.AspNetCore.Html;

namespace Celavimed.Services;

public static class Ui
{
    public static IHtmlContent Icon(string name, string? cls = null) =>
        new HtmlString($"<svg class=\"icon {cls}\" aria-hidden=\"true\"><use href=\"/img/icons.svg#i-{WebUtility.HtmlEncode(name)}\"/></svg>");

    public static string Initial(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "؟";
        var clean = name.Replace("دکتر", "").Trim();
        return clean.Length > 0 ? clean[..1] : name[..1];
    }

    public static string Money(long? amount) => amount.HasValue ? amount.Value.ToString("#,0") : "—";

    public static string Mobile(string m) => m.Length == 11 ? $"{m[..4]} {m[4..7]} {m[7..]}" : m;
}
