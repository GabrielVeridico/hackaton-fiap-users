using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HackatonFiap.Users.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _db;

    public RefreshTokenRepository(ApplicationDbContext db) => _db = db;

    public async Task AddAsync(RefreshToken token)
    {
        await _db.RefreshTokens.AddAsync(token);
        await _db.SaveChangesAsync();
    }

    public Task<RefreshToken?> FindByHashAsync(string tokenHash) =>
        _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

    public async Task<IReadOnlyList<RefreshToken>> FindActiveByUserAsync(Guid userId) =>
        await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync();

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync();

        foreach (var token in active)
        {
            token.Revoke();
        }

        await _db.SaveChangesAsync();
    }
}
