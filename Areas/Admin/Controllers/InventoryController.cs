using System.Globalization;
using System.Text.RegularExpressions;
using Celavimed.Areas.Admin.Models;
using Celavimed.Data;
using Celavimed.Models;
using Celavimed.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Celavimed.Areas.Admin.Controllers;

/// <summary>انبار مواد اولیه: گروه‌ها، کالاها، بچ و تاریخ انقضا، ورود و خروج</summary>
[Route("admin/inventory")]
[Authorize(Roles = Roles.Inventory)]
public class InventoryController : AdminController
{
    private readonly AppDbContext _db;
    private readonly InventoryService _inv;

    public InventoryController(AppDbContext db, InventoryService inv)
    {
        _db = db;
        _inv = inv;
    }

    // ---------------------------------------------------------------- فهرست

    [HttpGet("")]
    public async Task<IActionResult> Index(int? group, string? q, string? flag)
    {
        Nav("inventory", "انبار مواد اولیه");
        var today = Clock.Today;
        var warn = today.AddDays(_inv.ExpiryWarnDays);

        var groups = await _db.InventoryGroups.Include(g => g.Fields.OrderBy(f => f.SortOrder).ThenBy(f => f.Id))
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Name).ToListAsync();
        var items = await _db.InventoryItems.Include(i => i.Batches).Include(i => i.Values)
            .OrderBy(i => i.Name).ToListAsync();

        var all = items.Select(i => ToRow(i, today, warn)).ToList();
        var vm = new InventoryIndexVm
        {
            Groups = groups, GroupId = group, Q = q, Flag = flag, WarnDays = _inv.ExpiryWarnDays,
            ItemCount = all.Count(r => r.Item.IsActive),
            TotalValue = all.Sum(r => r.Value),
            LowCount = all.Count(r => r.Item.IsActive && r.IsLow),
            ExpiringCount = all.Count(r => r.ExpiresSoon),
            ExpiredCount = all.Count(r => r.HasExpired),
        };

        IEnumerable<StockRow> rows = all;
        if (group.HasValue) rows = rows.Where(r => r.Item.GroupId == group);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = Jalali.NormalizeDigits(q.Trim());
            rows = rows.Where(r => Has(r.Item.Name, term) || Has(r.Item.Code, term) || Has(r.Item.Manufacturer, term)
                                   || Has(r.Item.Supplier, term) || r.Values.Values.Any(v => Has(v, term)));
        }
        rows = flag switch
        {
            "low" => rows.Where(r => r.Item.IsActive && r.IsLow),
            "expiring" => rows.Where(r => r.ExpiresSoon || r.HasExpired),
            "inactive" => rows.Where(r => !r.Item.IsActive),
            _ => rows.Where(r => r.Item.IsActive),
        };
        vm.Rows = rows.ToList();
        return View(vm);
    }

    private static bool Has(string? s, string term) => s != null && s.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static StockRow ToRow(InventoryItem i, DateOnly today, DateOnly warn)
    {
        var live = i.Batches.Where(b => b.Quantity > 0).ToList();
        var nearest = live.Where(b => b.ExpiryDate.HasValue).Select(b => b.ExpiryDate).Min();
        return new StockRow
        {
            Item = i,
            Stock = live.Sum(b => b.Quantity),
            Value = (long)live.Sum(b => b.Quantity * (b.UnitCost ?? i.PurchasePrice ?? 0)),
            NearestExpiry = nearest,
            HasExpired = live.Any(b => b.ExpiryDate < today),
            ExpiresSoon = live.Any(b => b.ExpiryDate >= today && b.ExpiryDate <= warn),
            Values = i.Values.ToDictionary(v => v.FieldId, v => v.Value),
        };
    }

    // ---------------------------------------------------------------- گروه‌ها

    [HttpGet("groups")]
    public async Task<IActionResult> Groups(int? edit)
    {
        Nav("inventory-groups", "گروه‌های انبار");
        ViewBag.List = await _db.InventoryGroups.Include(g => g.Fields).Include(g => g.Items)
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Name).ToListAsync();
        var form = new GroupForm { SortOrder = await _db.InventoryGroups.CountAsync() + 1 };
        if (edit.HasValue)
        {
            var g = await _db.InventoryGroups.Include(x => x.Fields).FirstOrDefaultAsync(x => x.Id == edit);
            if (g == null) return NotFound();
            form = new GroupForm
            {
                Id = g.Id, Name = g.Name, Description = g.Description, Color = g.Color, SortOrder = g.SortOrder, IsActive = g.IsActive,
                Fields = g.Fields.OrderBy(f => f.SortOrder).ThenBy(f => f.Id).Select(f => new GroupFieldInput { Id = f.Id, Name = f.Name }).ToList(),
            };
        }
        return View(form);
    }

    [HttpPost("groups/save")]
    public async Task<IActionResult> SaveGroup(GroupForm form)
    {
        if (!ModelState.IsValid)
        {
            Flash("نام گروه را وارد کنید.", true);
            return Redirect(form.Id == 0 ? "/admin/inventory/groups" : $"/admin/inventory/groups?edit={form.Id}");
        }
        var g = form.Id == 0 ? new InventoryGroup() : await _db.InventoryGroups.Include(x => x.Fields).FirstOrDefaultAsync(x => x.Id == form.Id);
        if (g == null) return NotFound();

        g.Name = form.Name.Trim();
        g.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
        g.Color = Regex.IsMatch(form.Color ?? "", "^#[0-9A-Fa-f]{6}$") ? form.Color! : "#7B4A82";
        g.SortOrder = form.SortOrder;
        g.IsActive = form.IsActive;
        if (form.Id == 0) _db.InventoryGroups.Add(g);

        // مشخصه‌های اختصاصی: ویرایش، افزودن، حذف
        var wanted = form.Fields.Where(f => !string.IsNullOrWhiteSpace(f.Name)).Take(20).ToList();
        var keepIds = wanted.Where(f => f.Id > 0).Select(f => f.Id).ToHashSet();
        var removed = g.Fields.Where(f => !keepIds.Contains(f.Id)).ToList();
        if (removed.Count > 0)
        {
            var ids = removed.Select(f => f.Id).ToList();
            _db.InventoryValues.RemoveRange(await _db.InventoryValues.Where(v => ids.Contains(v.FieldId)).ToListAsync());
            _db.InventoryFields.RemoveRange(removed);
        }
        for (var i = 0; i < wanted.Count; i++)
        {
            var name = wanted[i].Name!.Trim();
            if (name.Length > 60) name = name[..60];
            var existing = wanted[i].Id > 0 ? g.Fields.FirstOrDefault(f => f.Id == wanted[i].Id) : null;
            if (existing != null) { existing.Name = name; existing.SortOrder = i; }
            else g.Fields.Add(new InventoryField { Name = name, SortOrder = i });
        }

        await _db.SaveChangesAsync();
        Flash(form.Id == 0 ? $"گروه «{g.Name}» ساخته شد. حالا کالاهایش را اضافه کنید." : "گروه به‌روز شد.");
        return Redirect("/admin/inventory/groups");
    }

    [HttpPost("groups/{id:int}/delete")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var g = await _db.InventoryGroups.FindAsync(id);
        if (g == null) return NotFound();
        if (await _db.InventoryItems.AnyAsync(i => i.GroupId == id))
        {
            Flash("این گروه کالا دارد؛ اول کالاها را به گروه دیگری ببرید یا حذف کنید.", true);
            return Redirect("/admin/inventory/groups");
        }
        _db.InventoryGroups.Remove(g);
        await _db.SaveChangesAsync();
        Flash("گروه حذف شد.");
        return Redirect("/admin/inventory/groups");
    }

    // ---------------------------------------------------------------- کالا

    [HttpGet("items/new")]
    public async Task<IActionResult> CreateItem(int? group)
    {
        Nav("inventory", "کالای جدید");
        var form = new ItemForm { GroupId = group ?? 0 };
        if (form.GroupId == 0) form.GroupId = await _db.InventoryGroups.Where(g => g.IsActive).OrderBy(g => g.SortOrder).Select(g => g.Id).FirstOrDefaultAsync();
        if (form.GroupId == 0)
        {
            Flash("اول یک گروه بسازید، مثلا «بوتاکس».", true);
            return Redirect("/admin/inventory/groups");
        }
        await FillItemForm(form);
        return View("EditItem", form);
    }

    [HttpGet("items/{id:int}/edit")]
    public async Task<IActionResult> EditItem(int id)
    {
        var i = await _db.InventoryItems.Include(x => x.Values).FirstOrDefaultAsync(x => x.Id == id);
        if (i == null) return NotFound();
        Nav("inventory", "ویرایش " + i.Name);
        var form = new ItemForm
        {
            Id = i.Id, GroupId = i.GroupId, Name = i.Name, Code = i.Code, Manufacturer = i.Manufacturer, Country = i.Country,
            Supplier = i.Supplier, Unit = i.Unit, PurchasePrice = i.PurchasePrice?.ToString(), SalePrice = i.SalePrice?.ToString(),
            MinStock = i.MinStock > 0 ? InventoryService.Num(i.MinStock) : null, Storage = i.Storage, Notes = i.Notes, IsActive = i.IsActive,
            Values = i.Values.ToDictionary(v => v.FieldId, v => (string?)v.Value),
        };
        await FillItemForm(form);
        return View(form);
    }

    [HttpPost("items/save")]
    public async Task<IActionResult> SaveItem(ItemForm form)
    {
        var purchase = ParseMoney(form.PurchasePrice, nameof(form.PurchasePrice));
        var sale = ParseMoney(form.SalePrice, nameof(form.SalePrice));
        var min = ParseQty(form.MinStock, nameof(form.MinStock), allowZero: true) ?? 0;
        var opening = form.Id == 0 ? ParseQty(form.OpeningQty, nameof(form.OpeningQty)) : null;
        var group = await _db.InventoryGroups.Include(g => g.Fields).FirstOrDefaultAsync(g => g.Id == form.GroupId);
        if (group == null) ModelState.AddModelError(nameof(form.GroupId), "گروه را انتخاب کنید.");

        var item = form.Id == 0 ? new InventoryItem() : await _db.InventoryItems.Include(x => x.Values).FirstOrDefaultAsync(x => x.Id == form.Id);
        if (item == null) return NotFound();

        if (!ModelState.IsValid || group == null)
        {
            Nav("inventory", form.Id == 0 ? "کالای جدید" : "ویرایش " + form.Name);
            await FillItemForm(form);
            return View("EditItem", form);
        }

        item.GroupId = group.Id;
        item.Name = form.Name.Trim();
        item.Code = Opt(Jalali.NormalizeDigits(form.Code));
        item.Manufacturer = Opt(form.Manufacturer);
        item.Country = Opt(form.Country);
        item.Supplier = Opt(form.Supplier);
        item.Unit = form.Unit.Trim();
        item.PurchasePrice = purchase;
        item.SalePrice = sale;
        item.MinStock = min;
        item.Storage = Opt(form.Storage);
        item.Notes = Opt(form.Notes);
        item.IsActive = form.IsActive;
        if (form.Id == 0) _db.InventoryItems.Add(item);

        // فقط مشخصه‌های گروه فعلی نگه داشته می‌شوند
        var fieldIds = group.Fields.Select(f => f.Id).ToHashSet();
        item.Values.RemoveAll(v => !fieldIds.Contains(v.FieldId));
        foreach (var fid in fieldIds)
        {
            var val = form.Values.TryGetValue(fid, out var v) ? Opt(v) : null;
            if (val is { Length: > 300 }) val = val[..300];
            var existing = item.Values.FirstOrDefault(x => x.FieldId == fid);
            if (val == null) { if (existing != null) item.Values.Remove(existing); }
            else if (existing != null) existing.Value = val;
            else item.Values.Add(new InventoryItemValue { FieldId = fid, Value = val });
        }

        if (opening is > 0)
            await _inv.ReceiveAsync(item, opening.Value, form.OpeningBatch, form.OpeningExpiry, purchase, "خرید", "موجودی اولیه", User);

        await _db.SaveChangesAsync();
        Flash(form.Id == 0 ? $"«{item.Name}» به انبار اضافه شد." : "مشخصات کالا ذخیره شد.");
        return Redirect($"/admin/inventory/items/{item.Id}");
    }

    [HttpPost("items/{id:int}/delete")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item == null) return NotFound();
        if (await _db.InventoryMovements.AnyAsync(m => m.ItemId == id))
        {
            item.IsActive = false;
            await _db.SaveChangesAsync();
            Flash("این کالا گردش ثبت‌شده دارد، برای همین به‌جای حذف غیرفعال شد تا سابقه‌اش بماند.");
            return Redirect($"/admin/inventory/items/{id}");
        }
        _db.InventoryItems.Remove(item);
        await _db.SaveChangesAsync();
        Flash("کالا حذف شد.");
        return Redirect("/admin/inventory");
    }

    [HttpGet("items/{id:int}")]
    public async Task<IActionResult> Item(int id)
    {
        var item = await _db.InventoryItems
            .Include(i => i.Group).ThenInclude(g => g.Fields)
            .Include(i => i.Values)
            .Include(i => i.Batches)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) return NotFound();
        Nav("inventory", item.Name);
        var live = item.Batches.Where(b => b.Quantity > 0).ToList();
        var vm = new ItemDetailsVm
        {
            Item = item,
            Stock = live.Sum(b => b.Quantity),
            Value = (long)live.Sum(b => b.Quantity * (b.UnitCost ?? item.PurchasePrice ?? 0)),
            Batches = live.OrderBy(b => b.ExpiryDate ?? DateOnly.MaxValue).ThenBy(b => b.ReceivedAt).ToList(),
            Movements = await _db.InventoryMovements.Include(m => m.Batch).Where(m => m.ItemId == id)
                .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id).Take(100).ToListAsync(),
            WarnDays = _inv.ExpiryWarnDays,
        };
        return View(vm);
    }

    // ---------------------------------------------------------------- ورود و خروج

    [HttpPost("items/{id:int}/in")]
    public async Task<IActionResult> StockIn(int id, string? qty, string? batchNo, DateOnly? expiry, string? unitCost, string? reason, string? note)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item == null) return NotFound();
        var q = ParseQty(qty, "qty");
        var cost = ParseMoney(unitCost, "unitCost");
        if (q == null || !ModelState.IsValid) return Back(id, "مقدار یا قیمت را درست وارد کنید.");
        var err = await _inv.ReceiveAsync(item, q.Value, batchNo, expiry, cost, reason, note, User);
        if (err != null) return Back(id, err);
        await _db.SaveChangesAsync();
        Flash($"{InventoryService.Num(q.Value)} {item.Unit} به انبار اضافه شد.");
        return Redirect($"/admin/inventory/items/{id}");
    }

    [HttpPost("items/{id:int}/out")]
    public async Task<IActionResult> StockOut(int id, string? qty, int? batchId, string? reason, string? note)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item == null) return NotFound();
        var q = ParseQty(qty, "qty");
        if (q == null || !ModelState.IsValid) return Back(id, "مقدار خروجی را درست وارد کنید.");
        var err = await _inv.IssueAsync(item, q.Value, batchId, reason, note, User);
        if (err != null) return Back(id, err);
        await _db.SaveChangesAsync();
        Flash($"{InventoryService.Num(q.Value)} {item.Unit} از انبار خارج شد.");
        return Redirect($"/admin/inventory/items/{id}");
    }

    [HttpPost("batches/{id:int}/adjust")]
    public async Task<IActionResult> Adjust(int id, string? counted, string? note)
    {
        var batch = await _db.InventoryBatches.FindAsync(id);
        if (batch == null) return NotFound();
        var c = ParseQty(counted, "counted", allowZero: true);
        if (c == null || !ModelState.IsValid) return Back(batch.ItemId, "عدد شمارش‌شده را درست وارد کنید.");
        var err = await _inv.AdjustAsync(batch, c.Value, note, User);
        if (err != null) return Back(batch.ItemId, err);
        await _db.SaveChangesAsync();
        Flash("موجودی بچ اصلاح شد.");
        return Redirect($"/admin/inventory/items/{batch.ItemId}");
    }

    // ---------------------------------------------------------------- دفتر گردش

    [HttpGet("movements")]
    public async Task<IActionResult> Movements(string? type, int? group, DateOnly? from, DateOnly? to)
    {
        Nav("inventory-moves", "گردش انبار");
        var q = _db.InventoryMovements.Include(m => m.Item).ThenInclude(i => i.Group).Include(m => m.Batch).AsQueryable();
        if (Enum.TryParse<MovementType>(type, true, out var t) && Enum.IsDefined(t)) q = q.Where(m => m.Type == t);
        else type = null;
        if (group.HasValue) q = q.Where(m => m.Item.GroupId == group);
        if (from.HasValue) { var f = Clock.FromIran(from.Value.ToDateTime(TimeOnly.MinValue)); q = q.Where(m => m.CreatedAt >= f); }
        if (to.HasValue) { var e = Clock.FromIran(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue)); q = q.Where(m => m.CreatedAt < e); }
        var list = await q.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id).Take(400).ToListAsync();
        return View(new MovementsVm
        {
            Movements = list, Type = type, GroupId = group, From = from, To = to,
            Groups = await _db.InventoryGroups.OrderBy(g => g.SortOrder).ThenBy(g => g.Name).ToListAsync(),
            InQty = list.Where(m => m.Type == MovementType.In).Sum(m => m.Quantity),
            OutQty = -list.Where(m => m.Type == MovementType.Out).Sum(m => m.Quantity),
            OutValue = (long)list.Where(m => m.Type == MovementType.Out).Sum(m => -m.Quantity * (m.Batch?.UnitCost ?? m.Item.PurchasePrice ?? 0)),
        });
    }

    // ---------------------------------------------------------------- کمکی

    private IActionResult Back(int itemId, string error)
    {
        Flash(error, true);
        return Redirect($"/admin/inventory/items/{itemId}");
    }

    private async Task FillItemForm(ItemForm form)
    {
        form.AllGroups = await _db.InventoryGroups.Include(g => g.Fields)
            .Where(g => g.IsActive || g.Id == form.GroupId)
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Name).ToListAsync();
    }

    private static string? Opt(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string? CleanNumber(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return Jalali.NormalizeDigits(s).Replace(",", "").Replace("٬", "").Replace("،", "").Replace(" ", "").Replace("٫", ".").Replace("/", ".").Trim();
    }

    private long? ParseMoney(string? s, string key)
    {
        var c = CleanNumber(s);
        if (c == null) return null;
        if (long.TryParse(c, NumberStyles.None, CultureInfo.InvariantCulture, out var v) && v <= 100_000_000_000) return v;
        ModelState.AddModelError(key, "مبلغ را فقط با عدد و به تومان بنویسید.");
        return null;
    }

    private decimal? ParseQty(string? s, string key, bool allowZero = false)
    {
        var c = CleanNumber(s);
        if (c == null) return null;
        if (decimal.TryParse(c, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var v)
            && (allowZero ? v >= 0 : v > 0) && v <= 1_000_000)
            return Math.Round(v, 2);
        ModelState.AddModelError(key, "مقدار را با عدد مثبت بنویسید.");
        return null;
    }
}
