using HackatonFiap.Users.Application.DTOs;
using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Abstractions;
using HackatonFiap.Users.Domain.Entities;

namespace HackatonFiap.Users.Application.Commands.AuthenticateUser;

public class AuthenticateUserCommandHandler
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IRefreshTokenRepository _refresh;
    private readonly IRefreshTokenService _refreshSvc;

    public AuthenticateUserCommandHandler(
        IUserRepository users, IPasswordHasher hasher, IJwtTokenGenerator jwt,
        IRefreshTokenRepository refresh, IRefreshTokenService refreshSvc)
    {
        _users = users; _hasher = hasher; _jwt = jwt; _refresh = refresh; _refreshSvc = refreshSvc;
    }

    public async Task<Result<AuthResponse>> HandleAsync(AuthenticateUserCommand command)
    {
        var user = await _users.FindByEmailAsync(command.Email);
        if (user is null || !_hasher.Verify(command.Password, user.Password.HashValue) || !user.IsActive)
            return Result<AuthResponse>.Failure(UserErrors.InvalidCredentials);

        var access = _jwt.GenerateAccessToken(user);
        var (raw, hash) = _refreshSvc.Generate();
        var refreshToken = RefreshToken.Issue(user.Id, hash, DateTime.UtcNow.AddDays(_refreshSvc.RefreshTokenDays));
        await _refresh.AddAsync(refreshToken);

        var expiresIn = (int)(access.ExpiresAtUtc - DateTime.UtcNow).TotalSeconds;
        return Result<AuthResponse>.Success(new AuthResponse(access.Token, raw, expiresIn));
    }
}
