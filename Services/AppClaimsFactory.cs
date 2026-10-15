using System.Security.Claims;
using Celavimed.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Celavimed.Services;

public class AppClaimsFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public AppClaimsFactory(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles, IOptions<IdentityOptions> options)
        : base(users, roles, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var id = await base.GenerateClaimsAsync(user);
        id.AddClaim(new Claim("FullName", user.FullName ?? user.UserName ?? ""));
        return id;
    }
}

public class PersianIdentityErrors : IdentityErrorDescriber
{
    public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = $"نام کاربری «{userName}» قبلا ثبت شده است." };
    public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = "نام کاربری فقط می‌تواند شامل حروف انگلیسی، عدد و . _ - باشد." };
    public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"رمز عبور باید حداقل {length} کاراکتر باشد." };
    public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = "رمز عبور باید حداقل یک عدد داشته باشد." };
    public override IdentityError PasswordRequiresLower() => new() { Code = nameof(PasswordRequiresLower), Description = "رمز عبور باید حداقل یک حرف کوچک انگلیسی داشته باشد." };
    public override IdentityError PasswordRequiresUpper() => new() { Code = nameof(PasswordRequiresUpper), Description = "رمز عبور باید حداقل یک حرف بزرگ انگلیسی داشته باشد." };
    public override IdentityError PasswordRequiresNonAlphanumeric() => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "رمز عبور باید حداقل یک نماد مثل @ یا # داشته باشد." };
    public override IdentityError PasswordMismatch() => new() { Code = nameof(PasswordMismatch), Description = "رمز عبور فعلی درست نیست." };
}
