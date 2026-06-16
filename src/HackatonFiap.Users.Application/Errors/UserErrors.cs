using HackatonFiap.Users.Domain.Abstractions;

namespace HackatonFiap.Users.Application.Errors;

public static class UserErrors
{
    public static Error EmailAlreadyRegistered => new("User.EmailExists", "Email already registered.");
    public static Error InvalidCredentials => new("User.InvalidCredentials", "Invalid email or password.");
    public static Error NotFound => new("User.NotFound", "User not found.");
    public static Error InvalidPassword => new("User.InvalidPassword", "Password must be at least 8 characters with letters, digits, and special characters.");
    public static Error DocumentInvalid => new("User.DocumentInvalid", "Document (CPF/CNPJ) is invalid.");
    public static Error AlreadyRegisteredActive => new("User.AlreadyRegistered", "Email or document already registered.");
    public static Error AlreadyRegisteredInactive => new("User.InactiveAccount", "Account exists but is inactive. Contact an administrator to reactivate.");
    public static Error InvalidRefreshToken => new("Auth.InvalidRefreshToken", "Refresh token is invalid, expired or revoked.");
    public static Error Forbidden => new("User.Forbidden", "Operation not allowed for this role.");
    public static Error OwnerImmutable => new("User.OwnerImmutable", "The Owner cannot be modified.");
    public static Error CannotManageGestor => new("User.CannotManageGestor", "Only the Owner can manage GestorONG users.");
}
