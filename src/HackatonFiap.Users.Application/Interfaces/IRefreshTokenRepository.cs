using HackatonFiap.Users.Domain.Entities;

namespace HackatonFiap.Users.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token);
    Task<RefreshToken?> FindByHashAsync(string tokenHash);
    Task<IReadOnlyList<RefreshToken>> FindActiveByUserAsync(Guid userId);
    Task SaveChangesAsync();
    Task RevokeAllForUserAsync(Guid userId);
}
