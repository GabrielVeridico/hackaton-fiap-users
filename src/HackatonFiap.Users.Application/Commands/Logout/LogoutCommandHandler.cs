using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Abstractions;

namespace HackatonFiap.Users.Application.Commands.Logout;

public class LogoutCommandHandler
{
    private readonly IRefreshTokenRepository _refresh;
    private readonly IRefreshTokenService _refreshSvc;

    public LogoutCommandHandler(IRefreshTokenRepository refresh, IRefreshTokenService refreshSvc)
    {
        _refresh = refresh;
        _refreshSvc = refreshSvc;
    }

    public async Task<Result> HandleAsync(LogoutCommand command)
    {
        var hash = _refreshSvc.ComputeHash(command.RefreshToken);
        var stored = await _refresh.FindByHashAsync(hash);
        if (stored is not null && stored.IsActive)
        {
            stored.Revoke();
            await _refresh.SaveChangesAsync();
        }
        return Result.Success();
    }
}
