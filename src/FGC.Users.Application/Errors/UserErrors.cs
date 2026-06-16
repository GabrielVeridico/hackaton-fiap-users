using FGC.Users.Domain.Abstractions;

namespace FGC.Users.Application.Errors;

public static class UserErrors
{
    public static Error EmailAlreadyRegistered => new("User.EmailExists", "Email already registered.");
    public static Error InvalidCredentials => new("User.InvalidCredentials", "Invalid email or password.");
    public static Error NotFound => new("User.NotFound", "User not found.");
    public static Error InvalidPassword => new("User.InvalidPassword", "Password must be at least 8 characters with letters, digits, and special characters.");
}
