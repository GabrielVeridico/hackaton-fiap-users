using System.Security.Cryptography;
using HackatonFiap.Users.Application.Interfaces;

namespace HackatonFiap.Users.Infrastructure.Identity;

public class RefreshTokenService : IRefreshTokenService
{
    public int RefreshTokenDays => 7;

    public (string RawToken, string TokenHash) Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes);
        return (raw, ComputeHash(raw));
    }

    public string ComputeHash(string rawToken)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }
}
