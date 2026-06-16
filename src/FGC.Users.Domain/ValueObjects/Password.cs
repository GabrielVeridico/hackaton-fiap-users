using System.Text.RegularExpressions;

namespace FGC.Users.Domain.ValueObjects;

public sealed class Password
{
    public string HashValue { get; private set; }

    private Password() => HashValue = string.Empty;

    public Password(string hashValue)
    {
        HashValue = hashValue;
    }

    public static bool IsValid(string rawPassword)
    {
        if (string.IsNullOrWhiteSpace(rawPassword) || rawPassword.Length < 8)
            return false;

        if (!Regex.IsMatch(rawPassword, @"[a-zA-Z]"))
            return false;

        if (!Regex.IsMatch(rawPassword, @"\d"))
            return false;

        if (!Regex.IsMatch(rawPassword, @"[^a-zA-Z0-9]"))
            return false;

        return true;
    }
}
