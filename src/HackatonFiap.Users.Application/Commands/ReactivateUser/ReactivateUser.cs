using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Abstractions;
using HackatonFiap.Users.Domain.Enums;

namespace HackatonFiap.Users.Application.Commands.ReactivateUser;

public record ReactivateUserCommand(Guid UserId, bool CallerIsOwner, string CorrelationId);

public class ReactivateUserCommandHandler
{
    private readonly IUserRepository _users;

    public ReactivateUserCommandHandler(IUserRepository users) => _users = users;

    public async Task<Result> HandleAsync(ReactivateUserCommand c)
    {
        var user = await _users.FindByIdIncludingInactiveAsync(c.UserId);
        if (user is null) return Result.Failure(UserErrors.NotFound);
        if (user.Role == UserRole.GestorONG && !c.CallerIsOwner)
            return Result.Failure(UserErrors.CannotManageGestor);

        user.Reactivate();
        await _users.UpdateAsync(user);
        return Result.Success();
    }
}
