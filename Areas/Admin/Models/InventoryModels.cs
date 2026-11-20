using System.ComponentModel.DataAnnotations;
using Celavimed.Models;

namespace Celavimed.Areas.Admin.Models;

/// <summary>یک ردیف کالا در فهرست انبار با موجودی و هشدارها</summary>
public class StockRow
{
    public InventoryItem Item { get; set; } = null!;
    public decimal Stock { get; set; }
    public long Value { get; set; }
    public DateOnly? NearestExpiry { get; set; }
    public bool IsLow => Item.MinStock > 0 && Stock <= Item.MinStock;
    public bool IsOut => Stock <= 0;
    public bool HasExpired { get; set; }
    public bool ExpiresSoon { get; set; }
    public Dictionary<int, string> Values { get; set; } = new();
}

public class InventoryIndexVm
{
    public List<InventoryGroup> Groups { get; set; } = new();
    public List<StockRow> Rows { get; set; } = new();
    public int? GroupId { get; set; }
    public string? Q { get; set; }
    public string? Flag { get; set; }
    public int ItemCount { get; set; }
    public long TotalValue { get; set; }
    public int LowCount { get; set; }
    public int ExpiringCount { get; set; }
    public int ExpiredCount { get; set; }
    public int WarnDays { get; set; }
}

public class GroupFieldInput
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class GroupForm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام گروه را وارد کنید."), MaxLength(100)]
    public string Name { get; set; } = "";

    [MaxLength(300)]
    public string? Description { get; set; }

    public string Color { get; set; } = "#7B4A82";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<GroupFieldInput> Fields { get; set; } = new();
}

public class ItemForm
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "گروه را انتخاب کنید.")]
    public int GroupId { get; set; }

    [Required(ErrorMessage = "نام کالا را وارد کنید."), MaxLength(150, ErrorMessage = "نام کالا خیلی طولانی است.")]
    public string Name { get; set; } = "";

    [MaxLength(40)] public string? Code { get; set; }
    [MaxLength(120)] public string? Manufacturer { get; set; }
    [MaxLength(60)] public string? Country { get; set; }
    [MaxLength(120)] public string? Supplier { get; set; }

    [Required(ErrorMessage = "واحد شمارش را وارد کنید."), MaxLength(30)]
    public string Unit { get; set; } = "عدد";

    /// <summary>قیمت‌ها به‌صورت متن گرفته می‌شوند تا ارقام فارسی و جداکننده هم قبول شود.</summary>
    public string? PurchasePrice { get; set; }
    public string? SalePrice { get; set; }
    public string? MinStock { get; set; }

    [MaxLength(120)] public string? Storage { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public Dictionary<int, string?> Values { get; set; } = new();

    // موجودی اولیه (فقط هنگام ساخت کالا)
    public string? OpeningQty { get; set; }
    public string? OpeningBatch { get; set; }
    public DateOnly? OpeningExpiry { get; set; }

    public List<InventoryGroup> AllGroups { get; set; } = new();
}

public class ItemDetailsVm
{
    public InventoryItem Item { get; set; } = null!;
    public decimal Stock { get; set; }
    public long Value { get; set; }
    public List<InventoryBatch> Batches { get; set; } = new();
    public List<InventoryMovement> Movements { get; set; } = new();
    public int WarnDays { get; set; }
}

public class MovementsVm
{
    public List<InventoryMovement> Movements { get; set; } = new();
    public List<InventoryGroup> Groups { get; set; } = new();
    public string? Type { get; set; }
    public int? GroupId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public decimal InQty { get; set; }
    public decimal OutQty { get; set; }
    public long OutValue { get; set; }
}
