using System.ComponentModel.DataAnnotations;

namespace Celavimed.Models;

/// <summary>گروه مواد اولیه، مثلا «بوتاکس» یا «فیلر»</summary>
public class InventoryGroup
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام گروه را وارد کنید."), MaxLength(100)]
    public string Name { get; set; } = "";

    [MaxLength(300)]
    public string? Description { get; set; }

    [MaxLength(7)]
    public string Color { get; set; } = "#7B4A82";

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<InventoryField> Fields { get; set; } = new();
    public List<InventoryItem> Items { get; set; } = new();
}

/// <summary>مشخصه‌ی اختصاصی یک گروه، مثلا «واحد در هر ویال» برای بوتاکس</summary>
public class InventoryField
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public InventoryGroup Group { get; set; } = null!;

    [Required, MaxLength(60)]
    public string Name { get; set; } = "";

    public int SortOrder { get; set; }
}

/// <summary>یک قلم کالا (زیرمجموعه‌ی گروه)، مثلا «دیسپورت ۵۰۰ واحد»</summary>
public class InventoryItem
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public InventoryGroup Group { get; set; } = null!;

    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    /// <summary>کد کالا یا بارکد</summary>
    [MaxLength(40)]
    public string? Code { get; set; }

    [MaxLength(120)]
    public string? Manufacturer { get; set; }

    [MaxLength(60)]
    public string? Country { get; set; }

    [MaxLength(120)]
    public string? Supplier { get; set; }

    /// <summary>واحد شمارش: ویال، سرنگ، عدد، جعبه، سی‌سی</summary>
    [MaxLength(30)]
    public string Unit { get; set; } = "عدد";

    /// <summary>قیمت خرید هر واحد (تومان)</summary>
    public long? PurchasePrice { get; set; }

    /// <summary>قیمت فروش یا مصرف هر واحد (تومان)</summary>
    public long? SalePrice { get; set; }

    /// <summary>اگر موجودی به این عدد یا کمتر برسد هشدار کمبود داده می‌شود.</summary>
    public decimal MinStock { get; set; }

    /// <summary>شرایط نگهداری، مثلا «یخچال ۲ تا ۸ درجه»</summary>
    [MaxLength(120)]
    public string? Storage { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<InventoryItemValue> Values { get; set; } = new();
    public List<InventoryBatch> Batches { get; set; } = new();
    public List<InventoryMovement> Movements { get; set; } = new();
}

/// <summary>مقدار یک مشخصه‌ی اختصاصی گروه برای یک کالا</summary>
public class InventoryItemValue
{
    public int ItemId { get; set; }
    public InventoryItem Item { get; set; } = null!;
    public int FieldId { get; set; }
    public InventoryField Field { get; set; } = null!;

    [MaxLength(300)]
    public string Value { get; set; } = "";
}

/// <summary>یک سری ساخت (بچ) از کالا با تاریخ انقضا و موجودی باقی‌مانده</summary>
public class InventoryBatch
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public InventoryItem Item { get; set; } = null!;

    [MaxLength(60)]
    public string? BatchNo { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    /// <summary>موجودی باقی‌مانده‌ی این بچ</summary>
    public decimal Quantity { get; set; }

    /// <summary>قیمت خرید هر واحد در این بچ (تومان)</summary>
    public long? UnitCost { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}

public enum MovementType
{
    In = 0,
    Out = 1,
    Adjust = 2,
}

/// <summary>ورود، خروج یا اصلاح موجودی؛ دفتر کامل گردش انبار</summary>
public class InventoryMovement
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public InventoryItem Item { get; set; } = null!;
    public int? BatchId { get; set; }
    public InventoryBatch? Batch { get; set; }

    public MovementType Type { get; set; }

    /// <summary>تغییر موجودی: مثبت برای ورود، منفی برای خروج</summary>
    public decimal Quantity { get; set; }

    /// <summary>موجودی کل کالا بعد از این گردش</summary>
    public decimal BalanceAfter { get; set; }

    public long? UnitPrice { get; set; }

    [MaxLength(40)]
    public string? Reason { get; set; }

    [MaxLength(300)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(120)]
    public string CreatedBy { get; set; } = "";
}

public static class InventoryReasons
{
    public static readonly string[] In = { "خرید", "مرجوعی از بخش", "هدیه یا نمونه", "سایر" };
    public static readonly string[] Out = { "مصرف برای بیمار", "مصرف داخلی", "تاریخ گذشته", "آسیب‌دیده یا ضایعات", "مرجوعی به تأمین‌کننده", "سایر" };
    public static readonly string[] Units = { "عدد", "ویال", "سرنگ", "آمپول", "جعبه", "بسته", "سی‌سی", "میلی‌لیتر", "گرم", "واحد" };
}
