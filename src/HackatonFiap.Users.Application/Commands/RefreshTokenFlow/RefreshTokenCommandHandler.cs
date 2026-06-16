using HackatonFiap.Users.Application.DTOs;
using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Abstractions;
using HackatonFiap.Users.Domain.Entities;

namespace HackatonFiap.Users.Application.Commands.RefreshTokenFlow;

public class RefreshTokenCommandHandler
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refresh;
    private readonly IRefreshTokenService _refreshSvc;
    private readonly IJwtTokenGenerator _jwt;

    public RefreshTokenCommandHandler(IUserRepository users, IRefreshTokenRepository refresh,
        IRefreshTokenService refreshSvc, IJwtTokenGenerator jwt)
    {
        _users = users;
        _refresh = refresh;
        _refreshSvc = refreshSvc;
        _jwt = jwt;
    }

    public async Task<Result<AuthResponse>> HandleAsync(RefreshTokenCommand command)
    {
        var hash = _refreshSvc.ComputeHash(command.RefreshToken);
        var stored = await _refresh.FindByHashAsync(hash);

        if (stored is null)
            return Result<AuthResponse>.Failure(UserErrors.InvalidRefreshToken);

        // Reuse of an already-revoked/rotated token => revoke the whole chain (RN01.12)
        if (!stored.IsActive)
        {
            await _refresh.RevokeAllForUserAsync(stored.UserId);
            return Result<AuthResponse>.Failure(UserErrors.InvalidRefreshToken);
        }

        var user = await _users.FindByIdAsync(stored.UserId);
        if (user is null || !user.IsActive)
            return Result<AuthResponse>.Failure(UserErrors.InvalidRefreshToken);

        var (raw, newHash) = _refreshSvc.Generate();
        var newToken = RefreshToken.Issue(user.Id, newHash, DateTime.UtcNow.AddDays(_refreshSvc.RefreshTokenDays));
        stored.Revoke(newToken.Id);
        await _refresh.AddAsync(newToken);
        await _refresh.SaveChangesAsync();

        var access = _jwt.GenerateAccessToken(user);
        var expiresIn = (int)(access.ExpiresAtUtc - DateTime.UtcNow).TotalSeconds;
        return Result<AuthResponse>.Success(new AuthResponse(access.Token, raw, expiresIn));
    }
}
