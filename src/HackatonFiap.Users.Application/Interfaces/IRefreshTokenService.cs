namespace HackatonFiap.Users.Application.Interfaces;

public interface IRefreshTokenService
{
    (string RawToken, string TokenHash) Generate();
    string ComputeHash(string rawToken);
    int RefreshTokenDays { get; }
}
