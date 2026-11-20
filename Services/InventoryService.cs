using System.Security.Claims;
using Celavimed.Data;
using Celavimed.Models;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Services;

/// <summary>منطق ورود و خروج انبار. هر تغییر موجودی یک ردیف در دفتر گردش (InventoryMovement) می‌سازد.</summary>
public class InventoryService
{
    private readonly AppDbContext _db;
    public int ExpiryWarnDays { get; }

    public InventoryService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        ExpiryWarnDays = Math.Clamp(config.GetValue("Inventory:ExpiryWarnDays", 60), 1, 365);
    }

    public static string Actor(ClaimsPrincipal user) =>
        user.FindFirst(AppClaims.FullName)?.Value ?? user.Identity?.Name ?? "";

    private async Task<decimal> StockAsync(int itemId) =>
        await _db.InventoryBatches.Where(b => b.ItemId == itemId).SumAsync(b => (decimal?)b.Quantity) ?? 0;

    /// <summary>ورود کالا: بچ با همان شماره و انقضا اگر باشد پر می‌شود، وگرنه بچ تازه ساخته می‌شود.</summary>
    public async Task<string?> ReceiveAsync(InventoryItem item, decimal qty, string? batchNo, DateOnly? expiry, long? unitCost,
        string? reason, string? note, ClaimsPrincipal user)
    {
        if (qty <= 0) return "مقدار ورودی باید بیشتر از صفر باشد.";
        if (qty > 1_000_000) return "مقدار واردشده خیلی زیاد است.";
        batchNo = string.IsNullOrWhiteSpace(batchNo) ? null : Trim(batchNo, 60);
        var cost = unitCost ?? item.PurchasePrice;

        var batch = await _db.InventoryBatches.FirstOrDefaultAsync(b =>
            b.ItemId == item.Id && b.BatchNo == batchNo && b.ExpiryDate == expiry && b.UnitCost == cost);
        if (batch == null)
        {
            batch = new InventoryBatch { Item = item, BatchNo = batchNo, ExpiryDate = expiry, UnitCost = cost };
            _db.InventoryBatches.Add(batch);
        }
        batch.Quantity += qty;
        var balance = await StockAsync(item.Id) + qty;
        _db.InventoryMovements.Add(new InventoryMovement
        {
            Item = item, Batch = batch, Type = MovementType.In, Quantity = qty, BalanceAfter = balance,
            UnitPrice = cost, Reason = Clean(reason, InventoryReasons.In), Note = Trim(note, 300), CreatedBy = Actor(user),
        });
        if (unitCost.HasValue) item.PurchasePrice = unitCost;
        return null;
    }

    /// <summary>خروج کالا از یک بچ مشخص؛ اگر بچ انتخاب نشود، از بچی که زودتر منقضی می‌شود برداشته می‌شود.</summary>
    public async Task<string?> IssueAsync(InventoryItem item, decimal qty, int? batchId, string? reason, string? note, ClaimsPrincipal user)
    {
        if (qty <= 0) return "مقدار خروجی باید بیشتر از صفر باشد.";
        var batches = await _db.InventoryBatches.Where(b => b.ItemId == item.Id && b.Quantity > 0).ToListAsync();
        var total = batches.Sum(b => b.Quantity);
        if (qty > total) return $"موجودی کافی نیست؛ فقط {Num(total)} {item.Unit} در انبار هست.";

        List<InventoryBatch> order;
        if (batchId.HasValue)
        {
            var chosen = batches.FirstOrDefault(b => b.Id == batchId);
            if (chosen == null) return "این بچ موجودی ندارد.";
            if (qty > chosen.Quantity) return $"این بچ فقط {Num(chosen.Quantity)} {item.Unit} موجودی دارد.";
            order = new() { chosen };
        }
        else
        {
            order = batches.OrderBy(b => b.ExpiryDate ?? DateOnly.MaxValue).ThenBy(b => b.ReceivedAt).ToList();
        }

        var balance = total;
        var left = qty;
        var actor = Actor(user);
        var cleanReason = Clean(reason, InventoryReasons.Out);
        foreach (var b in order)
        {
            if (left <= 0) break;
            var take = Math.Min(left, b.Quantity);
            b.Quantity -= take;
            left -= take;
            balance -= take;
            _db.InventoryMovements.Add(new InventoryMovement
            {
                Item = item, Batch = b, Type = MovementType.Out, Quantity = -take, BalanceAfter = balance,
                UnitPrice = item.SalePrice, Reason = cleanReason, Note = Trim(note, 300), CreatedBy = actor,
            });
        }
        return null;
    }

    /// <summary>اصلاح موجودی یک بچ بعد از شمارش (انبارگردانی)</summary>
    public async Task<string?> AdjustAsync(InventoryBatch batch, decimal counted, string? note, ClaimsPrincipal user)
    {
        if (counted < 0) return "موجودی شمارش‌شده نمی‌تواند منفی باشد.";
        var delta = counted - batch.Quantity;
        if (delta == 0) return "موجودی شمارش‌شده با موجودی ثبت‌شده یکی است.";
        var balance = await StockAsync(batch.ItemId) + delta;
        batch.Quantity = counted;
        _db.InventoryMovements.Add(new InventoryMovement
        {
            ItemId = batch.ItemId, Batch = batch, Type = MovementType.Adjust, Quantity = delta, BalanceAfter = balance,
            Reason = "انبارگردانی", Note = Trim(note, 300), CreatedBy = Actor(user),
        });
        return null;
    }

    private static string? Clean(string? reason, string[] allowed) =>
        reason != null && allowed.Contains(reason) ? reason : allowed[^1];

    private static string? Trim(string? s, int max)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        return s.Length > max ? s[..max] : s;
    }

    public static string Num(decimal d) => d == decimal.Truncate(d) ? d.ToString("#,0") : d.ToString("#,0.##");
}
