using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Abstractions;
using HackatonFiap.Users.Domain.Enums;

namespace HackatonFiap.Users.Application.Commands.DeactivateUser;

public record DeactivateUserCommand(Guid UserId, bool CallerIsOwner, string CorrelationId);

public class DeactivateUserCommandHandler
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refresh;

    public DeactivateUserCommandHandler(IUserRepository users, IRefreshTokenRepository refresh)
    {
        _users = users;
        _refresh = refresh;
    }

    public async Task<Result> HandleAsync(DeactivateUserCommand c)
    {
        var user = await _users.FindByIdIncludingInactiveAsync(c.UserId);
        if (user is null) return Result.Failure(UserErrors.NotFound);
        if (user.IsOwner) return Result.Failure(UserErrors.OwnerImmutable);
        if (user.Role == UserRole.GestorONG && !c.CallerIsOwner)
            return Result.Failure(UserErrors.CannotManageGestor);

        user.Deactivate();
        await _users.UpdateAsync(user);
        await _refresh.RevokeAllForUserAsync(user.Id);
        return Result.Success();
    }
}
