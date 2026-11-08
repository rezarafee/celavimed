# Celavimed: سند کامل پروژه (برای ادامه‌ی کار در چت یا هوش مصنوعی دیگر)

> این فایل را کامل به چت جدید بدهید و بگویید: «این پروژه‌ی من است؛ از روی همین سند ادامه بده.»
> کد کامل در مخزن گیت‌هاب است: https://github.com/rezarafee/celavimed (شاخه‌ی `main`).
> آخرین به‌روزرسانی این سند: ۱۸ آبان ۱۴۰۵ (2026-11-08).

---

## ۱. پروژه چیست

**Celavimed** سامانه‌ی نوبت‌دهی آنلاین و پنل پذیرش یک **مرکز زیبایی (پوست، مو، تزریق، لیزر)** در ایران است.

- بیمار در صفحه‌ی عمومی پزشک را انتخاب می‌کند، در **تقویم شمسی** روزهای آزاد را می‌بیند، ساعت را انتخاب می‌کند و با نام، موبایل و نوع خدمت نوبت می‌گیرد.
- منشی در پنل رزروها را می‌بیند، با بیمار **تماس می‌گیرد و تایید می‌کند**.
- روز مراجعه، هر بیمار در **مراحل پذیرش** جلو می‌رود (رزرو، تایید منشی، مشاوره، مالی، تزریق/پزشک، خروج). مراحل قابل افزودن، حذف و تغییرند و در کادرهای رنگی نمایش داده می‌شوند.

### خواسته‌های اصلی صاحب پروژه (نباید فراموش شود)
1. **ASP.NET Core 10** و **SQL Server**.
2. **کاملا فارسی و راست‌چین (RTL)**؛ هیچ متن انگلیسی در رابط کاربری نباشد. تنها استثنا نام برند **Celavimed** است که باید انگلیسی نوشته شود (به‌خصوص در هدر).
3. **زیبایی و ریسپانسیو بودن مهم‌ترین اولویت است.**
4. پنل ادمین با **منوی سمت راست جمع‌شونده**.
5. پنج نقش: مدیر ارشد، مدیر، منشی، مالی، کاربر عادی.
6. هر پزشک: نام، مشخصات، عکس، و **چند تخصص/خدمت** (جدول جدا).
7. منشی برای هر پزشک **روزها و ساعت‌های کاری هفتگی** را تعیین می‌کند؛ هر نوبت پیش‌فرض **۱۵ دقیقه** و قابل تنظیم به دقیقه.
8. صفحه‌ی رزرو زیبا با تقویم شمسی؛ کلیک روی روز، ساعت‌های آزاد را نشان می‌دهد.
9. صفحه‌ی منشی برای دیدن رزروها، تماس و تایید.
10. مراحل بیمار در کادرهای زیبا؛ پزشک و منشی همه‌ی مراحل را می‌بینند و همه‌چیز را ویرایش می‌کنند.
11. صفحه‌ی ورود زیبا برای پنل.
12. صفحه‌ی **خلاصه‌ی وضعیت بیمار**: مشخصات بیمار و زیرش سوابق.
13. بدون وابستگی به CDN خارجی (دسترسی از ایران): فونت‌ها و آیکون‌ها داخل پروژه.

---

## ۲. فناوری‌ها

| بخش | انتخاب |
|---|---|
| فریم‌ورک | ASP.NET Core 10 MVC (Razor Views) + Area به نام `Admin` |
| ORM | EF Core 10.0.13 |
| دیتابیس | SQL Server (پیش‌فرض)؛ SQLite فقط برای تست محلی |
| احراز هویت | ASP.NET Core Identity با نقش‌ها، کوکی |
| فرانت | CSS و JavaScript ساده (بدون فریم‌ورک، بدون CDN) |
| تقویم | شمسی؛ سرور با `PersianCalendar`، مرورگر با الگوریتم jalaali در `wwwroot/js/jalali.js` |
| منطقه‌ی زمانی | همه‌ی ساعت‌ها به وقت تهران (`Asia/Tehran` یا `Iran Standard Time`) |

پکیج‌ها (`Celavimed.csproj`، همه 10.0.13): `Microsoft.AspNetCore.Identity.EntityFrameworkCore`، `Microsoft.EntityFrameworkCore.SqlServer`، `Microsoft.EntityFrameworkCore.Sqlite`، `Microsoft.EntityFrameworkCore.Design`، و `System.Security.Cryptography.Pkcs` (برای رفع هشدار امنیتی پین شده). ابزار: `dotnet-ef` 10.0.13 در `dotnet-tools.json`.

---

## ۳. ساختار پوشه‌ها

```
Celavimed.csproj, Program.cs, appsettings*.json, README.md
Controllers/
  HomeController.cs          صفحه‌ی رزرو (/) و صفحه‌ی خطا
  BookingApiController.cs    API رزرو عمومی
  AccountController.cs       ورود، خروج، تغییر رمز، عدم دسترسی
Areas/Admin/
  Controllers/  AdminController (پایه) + Dashboard, Board, Appointments, Patients,
                Doctors, Schedules, Services, Stages, Users
  Models/ViewModels.cs
  Views/  Shared/_AdminLayout, _JDate (انتخاب تاریخ شمسی), _DocPhoto ...
Models/Entities.cs, Models/Roles.cs
Data/AppDbContext.cs, Data/SeedData.cs, Data/Migrations/ (InitialCreate)
Services/
  Jalali.cs         تبدیل و قالب‌بندی تاریخ شمسی
  Clock.cs          زمان تهران (Clock.Today, Clock.NowIran)
  SlotService.cs    ساخت ساعت‌های آزاد
  WorkflowService.cs جابه‌جایی مراحل، کد پیگیری
  AccessScope.cs    محدوده‌ی دسترسی منشی به پزشکان خودش
  AppClaimsFactory.cs (claim به نام FullName) + PersianIdentityErrors
  Ui.cs             کمک‌کننده‌ها: Icon, Initial, Money, Mobile
Views/ Home/Index (ویزارد رزرو), Account/Login, Shared/_PublicLayout
database/celavimed-schema.sql   اسکریپت کامل و قابل اجرای چندباره‌ی جدول‌ها
wwwroot/
  css/base.css, admin.css, public.css
  js/jalali.js, booking.js, admin.js
  fonts/  Vazirmatn FD, Noto Naskh Arabic, Marcellus (+ مجوز OFL)
  img/icons.svg (اسپرایت آیکون Lucide)، favicon.svg
  uploads/doctors/  عکس پزشکان
```

---

## ۴. مدل داده (Models/Entities.cs)

| جدول | فیلدهای مهم | توضیح |
|---|---|---|
| `AspNetUsers` (ApplicationUser) | FullName, IsActive, CreatedAt | کاربران پنل |
| `Doctors` | FullName, Headline, Bio, MedicalCode, Phone, PhotoPath, IsActive, SortOrder | |
| `Services` (ClinicService) | Name, Description, IsActive, SortOrder | خدمت/تخصص: بوتاکس، فیلر، لیزر و ... |
| `DoctorServices` | DoctorId, ServiceId | چند خدمت برای هر پزشک |
| `DoctorSecretaries` | DoctorId, UserId | کدام منشی مسئول کدام پزشک است |
| `WeeklySchedules` | DoctorId, DayOfWeek, StartTime, EndTime, SlotMinutes=15, IsActive | هر روز می‌تواند چند بازه داشته باشد (مثلا ۱۰ تا ۱۳ و ۱۶ تا ۱۹) |
| `TimeOffs` (DoctorTimeOff) | DoctorId, Date, Reason | روز تعطیل/مرخصی؛ یکتا روی (DoctorId, Date) |
| `Patients` | FullName, Mobile (یکتا، ۱۱ رقم), Notes | هر شماره موبایل یک پرونده؛ Notes = یادداشت دائمی مثل حساسیت دارویی |
| `Stages` (WorkflowStage) | Name, Description, Color, Icon, SortOrder, IsActive | مراحل پذیرش، قابل ویرایش |
| `Appointments` | TrackingCode (یکتا), DoctorId, PatientId, ServiceId?, Date, StartTime, EndTime, StageId, Status, Notes, Amount, IsPaid, CreatedAt, ConfirmedAt | |
| `StageLogs` (AppointmentStageLog) | AppointmentId, StageId, ChangedBy, Note, ChangedAt | تاریخچه‌ی جابه‌جایی مراحل |

- `AppointmentStatus`: `Active = 0`، `Cancelled = 1`، `NoShow = 2` (بیمار نیامد).
- **جلوگیری از رزرو دوباره:** ایندکس یکتای فیلترشده روی `(DoctorId, Date, StartTime)` با فیلتر `[Status] = 0`، به‌علاوه‌ی بررسی هم‌پوشانی در `SlotService`.
- رفتار حذف: نوبت با حذف پزشک، مرحله یا بیمار حذف نمی‌شود (Restrict)؛ حذف خدمت، `ServiceId` را خالی می‌کند (SetNull).
- `Amount` عدد صحیح (long) است و در رابط کاربری «تومان» نمایش داده می‌شود.

### مراحل پیش‌فرض (SeedData)
| ترتیب | نام | رنگ | آیکون |
|---|---|---|---|
| 1 | رزرو | #7A5C8E | calendar-plus |
| 2 | تایید منشی | #2F7F78 | phone-call |
| 3 | مشاوره | #B07A2E | messages-square |
| 4 | مالی | #3F5F8F | wallet |
| 5 | تزریق / پزشک | #A8466B | syringe |
| 6 | خروج | #5B6660 | door-open |

قاعده‌ها: نوبت جدید در **مرحله‌ی اول** ثبت می‌شود. «تایید» یعنی رفتن به **مرحله‌ی دوم**. هر بار که نوبت از مرحله‌ی اول خارج شود، `ConfirmedAt` پر می‌شود. «تمام‌شده» یعنی در **آخرین مرحله** است.

---

## ۵. نقش‌ها و دسترسی‌ها (Models/Roles.cs)

| نقش | عنوان فارسی | دسترسی |
|---|---|---|
| `SuperAdmin` | مدیر ارشد | همه‌چیز، از جمله ساخت مدیر ارشد دیگر |
| `Admin` | مدیر | همه‌چیز به جز مدیر ارشد |
| `Secretary` | منشی | فقط پزشکانی که به او سپرده شده: برنامه‌ی کاری، روز تعطیل، خدمات، رزروها، تایید، نوبت تلفنی، جابه‌جایی مراحل |
| `Finance` | مالی | همه‌ی نوبت‌ها، ثبت مبلغ و پرداخت، جابه‌جایی مراحل |
| `User` | کاربر عادی | فقط مشاهده |

ترکیب‌های آماده برای `[Authorize(Roles = ...)]`: `Managers` (مدیر ارشد و مدیر)، `FrontDesk` (+ منشی)، `Staff` (+ مالی)، `Everyone` (+ کاربر عادی).
`AccessScope` منشی را به پزشکان مرتبط از طریق `DoctorSecretaries` محدود می‌کند (`DoctorIdsAsync`، `AppointmentsAsync`، `DoctorsAsync`). `CanEditFlow` برای مدیران، منشی و مالی درست است.

---

## ۶. صفحه‌ها و مسیرها

### عمومی
| مسیر | کار |
|---|---|
| `/` | ویزارد رزرو سه‌مرحله‌ای: ۱) پزشک ۲) روز و ساعت ۳) مشخصات؛ سپس صفحه‌ی «نوبت ثبت شد» با کد پیگیری |
| `GET /api/booking/availability?doctorId&from&to` | وضعیت روزهای ماه برای رنگ‌آمیزی تقویم (تعداد آزاد/کل) |
| `GET /api/booking/slots?doctorId&date` | ساعت‌های یک روز |
| `POST /api/booking/book` | ثبت نوبت (JSON: doctorId, date, start, serviceId, name, mobile, website) |
| `/account/login`، `/account/logout`، `/account/password`، `/account/denied` | ورود، خروج، تغییر رمز، عدم دسترسی |
| `/error/{code}` | صفحه‌ی خطا (با `[Route]` تا POSTهای خطادار هم کار کنند) |

بالای صفحه‌ی رزرو، **نزدیک‌ترین نوبت آزاد** (بین همه‌ی پزشکان، در ۱۴ روز آینده) داخل یک قاب طاقی نشان داده می‌شود. دکمه‌ی «همین نوبت را می‌خواهم» پزشک، روز و ساعت را انتخاب می‌کند و بیمار را مستقیم به مرحله‌ی مشخصات می‌برد (`HomeController.FindNextFreeAsync` و `#quick-pick` در `booking.js`).

**امنیت رزرو:** توکن ضدجعل در هدر `RequestVerificationToken` (از `<meta name="csrf">`)، محدودیت نرخ `booking` (۸ درخواست در ۱۰ دقیقه برای هر IP)، فیلد تله‌ی ربات به نام `website`، اعتبارسنجی موبایل `^(\+98|0)9\d{9}$` (ارقام فارسی و عربی به انگلیسی تبدیل می‌شوند). پاسخ 409 یعنی ساعت پر شده است.

### پنل (`/admin`، نیاز به ورود)
| مسیر | کار | نقش |
|---|---|---|
| `/admin` | داشبورد: کادرهای طاقی هر مرحله با تعداد امروز، نوبت‌های امروز، منتظر تایید تلفنی، آمار هفت روز آینده و ۲۴ ساعت گذشته | همه |
| `/admin/board?date&doctorId` | **برد مراحل بیماران**: هر ستون یک مرحله؛ کارت را بکشید یا با دکمه جابه‌جا کنید | همه (ویرایش: Staff) |
| `/admin/appointments` | فهرست رزروها با فیلتر روز/پزشک/مرحله/جستجو و تب‌های «پیش رو، منتظر تایید، همه، لغو/نیامده»؛ دکمه‌ی تماس و تایید | همه |
| `/admin/appointments/{id}` | پرونده‌ی نوبت: مسیر بیمار (استپر)، ویرایش همه‌چیز، تاریخچه، لغو، «نیامد»، برگرداندن | همه (ویرایش: Staff) |
| `POST /admin/appointments/move` | جابه‌جایی از برد (JSON) | Staff |
| `/admin/appointments/new?mobile&name` | نوبت تلفنی/حضوری توسط منشی | FrontDesk |
| `/admin/patients`، `/admin/patients/{id}` | فهرست بیماران و **خلاصه‌ی وضعیت بیمار** (مشخصات، یادداشت دائمی، آمار مراجعه و پرداخت، نوبت‌های پیش رو و زیرش سوابق) | Staff |
| `/admin/doctors` | پزشکان با عکس، خدمات و منشی‌ها (افزودن و ویرایش فقط مدیران) | FrontDesk |
| `/admin/schedules/{doctorId}` | برنامه‌ی هفتگی (چند بازه در روز، طول نوبت دلخواه) و روزهای تعطیل | FrontDesk |
| `/admin/services` | خدمات و تخصص‌ها | FrontDesk |
| `/admin/stages` | افزودن، حذف، تغییر نام، رنگ، آیکون و ترتیب مراحل | Managers |
| `/admin/users` | کاربران و نقش‌ها | Managers |

---

## ۷. منطق ساعت‌ها (Services/SlotService.cs)

- `Expand(rules)` هر بازه‌ی هفتگی را به قطعه‌های `SlotMinutes` تقسیم می‌کند.
- ساعت‌هایی که با نوبت فعال هم‌پوشانی دارند «پر» هستند. روز تعطیل هیچ ساعتی ندارد.
- برای رزرو عمومی: گذشته، ساعت‌های قبل از «الان + `Booking:MinLeadMinutes`» (پیش‌فرض ۳۰ دقیقه) و روزهای بعد از `Booking:WindowDays` (پیش‌فرض ۶۰ روز) حذف می‌شوند.
- `GetRangeAsync` خلاصه‌ی روزها برای تقویم را می‌دهد، `GetSlotsAsync` ساعت‌های یک روز را، و `IsBookableAsync` بررسی نهایی پیش از ثبت را.

---

## ۸. سیستم طراحی

حس کلی: آرام، لوکس و زنانه، اما نه کلیشه‌ای. نقش‌مایه‌ی اصلی **طاق (arch)** است، مثل درگاه‌های معماری کلاسیک: قاب عکس پزشکان، کارت «نزدیک‌ترین نوبت»، طاق صفحه‌ی ورود، کادرهای مراحل در داشبورد و استپر، آیکون ستون‌های برد و لوگو.

**رنگ‌ها** (`wwwroot/css/base.css`، متغیرهای `:root`)
| نام | مقدار | کاربرد |
|---|---|---|
| plum-900 | #24122A | زمینه‌ی تیره، هدر، سایدبار، دکمه‌ی اصلی |
| orchid | #7B4A82 | رنگ عمل و لینک |
| quartz | #EDD3D0 | صورتی کوارتز، روزهای آزاد، قاب‌ها |
| mineral | #2F7F78 | تایید و موفقیت |
| bg | #F6F4F8 | زمینه‌ی پنل |
| line | #E7E2EA | خطوط |

**فونت‌ها** (همه داخل `wwwroot/fonts`، بدون CDN)
- **Vazirmatn FD** (ارقام فارسی): متن و رابط کاربری.
- **Noto Naskh Arabic**: تیترهای فارسی (هیرو، عنوان صفحه‌های پنل، نام پزشکان). برای اعداد استفاده نشود.
- **Marcellus**: فقط نوشتار لاتین **Celavimed** (کلاس `.wordmark`) و حرف «C» لوگو.

**آیکون‌ها:** اسپرایت Lucide در `/img/icons.svg`؛ در Razor با `@Ui.Icon("name", "icon-sm")` و در JS با `<use href="/img/icons.svg#i-name">`.

**قاعده‌های طراحی** (از skill «frontend-design» Anthropic که روی این پروژه اجرا شد)
- جسارت فقط در یک جا (هیروی طاقی)؛ بقیه آرام و منظم.
- پرهیز از کیت کارت‌های SaaS، برچسب‌های تمام‌حروف‌بزرگ و بخش‌های سه‌ستونی «چطور کار می‌کند».
- حرکت فقط یک‌جا: باز شدن طاق هیرو هنگام بارگذاری؛ با `prefers-reduced-motion` غیرفعال می‌شود.
- ریسپانسیو تا ۳۹۰ پیکسل؛ در موبایل دکمه‌های ادامه‌ی ویزارد چسبان (sticky) هستند.

---

## ۹. نصب و اجرا

### ویندوز با SQL Server
1. پیش‌نیاز: .NET 10 SDK و SQL Server (Express هم کافی است).
2. رشته‌ی اتصال در `appsettings.json`: `Server=.;Database=Celavimed;Trusted_Connection=True;TrustServerCertificate=True` (برای Express: `Server=.\SQLEXPRESS;...`).
3. `dotnet run`. دیتابیس با Migration خودکار ساخته می‌شود. در حالت Development سه پزشک، شش خدمت و برنامه‌ی نمونه هم ساخته می‌شود.
4. ورود اول: نام کاربری `superadmin` و رمز `Celavi@1405`. بعد از ورود حتما رمز را عوض کنید. منشی نمونه: `monshi` با رمز `Monshi@1405` (فقط با DemoData).

### تست سریع بدون SQL Server
```
set Database__Provider=Sqlite
set ConnectionStrings__Default=Data Source=celavimed-dev.db
dotnet run
```
(با SQLite به‌جای Migrate از `EnsureCreated` استفاده می‌شود.)

### انتشار روی IIS
`dotnet publish -c Release -o publish`، نصب ASP.NET Core Hosting Bundle 10، `Seed:DemoData` برابر `false`، و پوشه‌ی `wwwroot/uploads` باید برای کاربر IIS قابل نوشتن باشد.

### تنظیمات (`appsettings.json`)
- `Database:Provider`: `SqlServer` یا `Sqlite`
- `Seed:SuperAdminUserName`، `Seed:SuperAdminPassword`، `Seed:DemoData`
- `Clinic:Name` (= Celavimed)، `Clinic:Tagline`، `Clinic:Phone`، `Clinic:Address`
- `Booking:WindowDays` (۶۰)، `Booking:MinLeadMinutes` (۳۰)

### ساخت Migration جدید
```
dotnet tool restore
dotnet ef migrations add <Name> -o Data/Migrations
```
بعد از تغییر مدل، `database/celavimed-schema.sql` را هم به‌روز کنید (`dotnet ef migrations script --idempotent`).

---

## ۱۰. نکته‌ها و تله‌های شناخته‌شده

- **توکن ضدجعل در فرم‌ها:** `FormTagHelper` برای فرم‌هایی که `action` صریح دارند توکن نمی‌گذارد؛ همه‌ی فرم‌های POST باید `asp-antiforgery="true"` داشته باشند.
- **Razor و متن فارسی چسبیده به `@`:** مثلا `ادامه@Ui.Icon(...)` را آدرس ایمیل حساب می‌کند. متن را در `<span>` بگذارید یا از `@(...)` استفاده کنید.
- **EF و مرتب‌سازی روی record:** `OrderBy` روی سازنده‌ی record ترجمه نمی‌شود؛ قبل از `Select` مرتب کنید.
- **صفحه‌های خطا:** `UseStatusCodePagesWithReExecute("/error/{0}")` و اکشن خطا `[Route]` دارد (نه `[HttpGet]`)، وگرنه خطای 400 در POST به 405 تبدیل می‌شود.
- **تقویم موبایل:** ستون‌ها `repeat(7, minmax(0,1fr))` تا از عرض بیرون نزنند.
- `[hidden] { display: none !important; }` در base.css لازم است.
- **ارقام:** فونت Vazirmatn FD ارقام لاتین را فارسی نمایش می‌دهد؛ در HTML ارقام لاتین بنویسید.
- ساعت‌ها به وقت محلی تهران ذخیره می‌شوند، نه UTC (به‌جز `CreatedAt`، `ConfirmedAt` و `ChangedAt` که UTC هستند).

---

## ۱۱. وضعیت فعلی

**انجام‌شده**
- همه‌ی خواسته‌های بخش ۱.
- نام برند در همه‌جا انگلیسی (Celavimed).
- دو دور بازطراحی با skill frontend-design: صفحه‌ی رزرو، ورود و پنل ادمین.
- همه‌ی صفحه‌ها با Playwright در دسکتاپ و موبایل تست شده‌اند: رزرو کامل، رزرو دوباره (409)، درخواست بدون توکن (400)، محدودیت نقش‌ها.

**مخزن:** https://github.com/rezarafee/celavimed، شاخه‌ی `main`. حساب گیت‌هاب متصل به Claude `rezarafee` است.

**ایده‌های بعدی (هنوز انجام نشده)**
- ارسال پیامک تایید و یادآوری نوبت (مثلا با کاوه‌نگار یا ملی‌پیامک).
- گزارش مالی روزانه و ماهانه.
- آپلود چند عکس قبل و بعد در پرونده‌ی بیمار.
- پرداخت آنلاین بیعانه.
- خروجی Excel از رزروها.

---

## ۱۲. راهنما برای هوش مصنوعی‌ای که ادامه می‌دهد

- پاسخ‌ها و متن رابط کاربری **فارسی** باشد. هیچ متن انگلیسی در UI نگذارید، به‌جز Celavimed.
- سبک کد موجود را حفظ کنید: Razor ساده، CSS با متغیرهای `:root`، JS بدون فریم‌ورک، و کامنت‌های کوتاه فارسی.
- هیچ CDN خارجی اضافه نکنید. فونت و کتابخانه را داخل `wwwroot` بگذارید.
- هر صفحه‌ی جدید باید در موبایل (۳۹۰ پیکسل) و دسکتاپ زیبا باشد و از نقش‌مایه‌ی طاق و پالت بالا پیروی کند.
- برای هر فرم POST، `asp-antiforgery="true"` بگذارید و دسترسی را با `[Authorize(Roles = Roles.X)]` و `AccessScope` محدود کنید.
- تاریخ‌ها را با `Jalali` و `Clock` نمایش دهید و محاسبه کنید، نه با `DateTime.Now`.
