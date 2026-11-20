using Celavimed.Models;
using Celavimed.Services;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Data;

/// <summary>داده‌ی نمونه‌ی انبار؛ فقط وقتی Seed:DemoData روشن است (حالت Development)</summary>
public static class SeedInventory
{
    public static async Task RunAsync(AppDbContext db)
    {
        if (await db.InventoryGroups.AnyAsync()) return;
        var today = Clock.Today;

        var botox = new InventoryGroup { Name = "بوتاکس", Description = "انواع سم بوتولینوم", Color = "#A8466B", SortOrder = 1 };
        var filler = new InventoryGroup { Name = "فیلر", Description = "ژل‌های هیالورونیک اسید", Color = "#7B4A82", SortOrder = 2 };
        var supplies = new InventoryGroup { Name = "مواد مصرفی", Description = "سرنگ، کرم بی‌حسی و لوازم یک‌بارمصرف", Color = "#2F7F78", SortOrder = 3 };
        botox.Fields.AddRange(new[] { new InventoryField { Name = "واحد در هر ویال", SortOrder = 0 }, new InventoryField { Name = "رقیق‌سازی پیشنهادی", SortOrder = 1 } });
        filler.Fields.AddRange(new[] { new InventoryField { Name = "حجم سرنگ", SortOrder = 0 }, new InventoryField { Name = "کاربرد", SortOrder = 1 } });
        db.InventoryGroups.AddRange(botox, filler, supplies);
        await db.SaveChangesAsync();

        InventoryItem Item(InventoryGroup g, string name, string maker, string country, string unit, long buy, long? sell, decimal min, string? storage, params string[] vals)
        {
            var i = new InventoryItem
            {
                Group = g, Name = name, Manufacturer = maker, Country = country, Unit = unit, PurchasePrice = buy, SalePrice = sell,
                MinStock = min, Storage = storage, Supplier = "پخش دارویی نمونه",
            };
            var fields = g.Fields.OrderBy(f => f.SortOrder).ToList();
            for (var k = 0; k < vals.Length && k < fields.Count; k++) i.Values.Add(new InventoryItemValue { FieldId = fields[k].Id, Value = vals[k] });
            db.InventoryItems.Add(i);
            return i;
        }

        void Receive(InventoryItem i, decimal qty, string batch, int expiryDays, int daysAgo, decimal used = 0)
        {
            var at = DateTime.UtcNow.AddDays(-daysAgo);
            var prior = i.Batches.Sum(x => x.Quantity);
            var b = new InventoryBatch { Item = i, BatchNo = batch, ExpiryDate = today.AddDays(expiryDays), Quantity = qty - used, UnitCost = i.PurchasePrice, ReceivedAt = at };
            db.InventoryBatches.Add(b);
            var bal = prior + (qty - used);
            db.InventoryMovements.Add(new InventoryMovement { Item = i, Batch = b, Type = MovementType.In, Quantity = qty, BalanceAfter = bal + used, UnitPrice = i.PurchasePrice, Reason = "خرید", Note = "فاکتور " + (1400 + daysAgo), CreatedAt = at, CreatedBy = "مدیر ارشد" });
            if (used > 0)
                db.InventoryMovements.Add(new InventoryMovement { Item = i, Batch = b, Type = MovementType.Out, Quantity = -used, BalanceAfter = bal, UnitPrice = i.SalePrice, Reason = "مصرف برای بیمار", CreatedAt = at.AddDays(3), CreatedBy = "مریم احمدی" });
        }

        var dysport = Item(botox, "دیسپورت ۵۰۰ واحد", "Ipsen", "فرانسه", "ویال", 4_800_000, 6_500_000, 3, "یخچال ۲ تا ۸ درجه", "۵۰۰ واحد", "۲٫۵ سی‌سی سالین");
        var botoxA = Item(botox, "بوتاکس ۱۰۰ واحد", "Allergan", "ایرلند", "ویال", 5_200_000, 7_000_000, 2, "یخچال ۲ تا ۸ درجه", "۱۰۰ واحد", "۲٫۵ سی‌سی سالین");
        var xeomin = Item(botox, "زئومین ۱۰۰ واحد", "Merz", "آلمان", "ویال", 4_400_000, 6_000_000, 2, "دمای اتاق، زیر ۲۵ درجه", "۱۰۰ واحد", "۲٫۵ سی‌سی سالین");
        var voluma = Item(filler, "جوویدرم ولوما", "Allergan", "فرانسه", "سرنگ", 6_900_000, 9_500_000, 4, "دمای اتاق", "۱ سی‌سی", "گونه و چانه");
        var lips = Item(filler, "رستیلین کیس", "Galderma", "سوئد", "سرنگ", 5_600_000, 8_000_000, 4, "دمای اتاق", "۱ سی‌سی", "لب");
        var syringe = Item(supplies, "سرنگ انسولین ۱ سی‌سی", "Becton Dickinson", "آمریکا", "جعبه", 450_000, null, 5, null);
        var emla = Item(supplies, "کرم بی‌حسی املا ۵٪", "Aspen", "سوئد", "عدد", 320_000, null, 6, "دور از نور");
        await db.SaveChangesAsync();

        Receive(dysport, 10, "D2412A", 210, 40, used: 4);
        Receive(dysport, 5, "D2501C", 320, 6);
        Receive(botoxA, 4, "C7781", 35, 60, used: 2);
        Receive(xeomin, 6, "MZ-0931", 400, 20, used: 1);
        Receive(voluma, 8, "VB40K", 540, 25, used: 5);
        Receive(lips, 6, "RK2210", 18, 90, used: 3);
        Receive(syringe, 20, "BD-7714", 900, 15, used: 6);
        Receive(emla, 10, "EM-552", -12, 120, used: 6);
        await db.SaveChangesAsync();
    }
}
