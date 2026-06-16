using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Abstractions;
using HackatonFiap.Users.Domain.Enums;

namespace HackatonFiap.Users.Application.Commands.ChangeUserRole;

public record ChangeUserRoleCommand(Guid UserId, UserRole NewRole, bool CallerIsOwner, string CorrelationId);

public class ChangeUserRoleCommandHandler
{
    private readonly IUserRepository _users;
    private readonly IAuditService _audit;

    public ChangeUserRoleCommandHandler(IUserRepository users, IAuditService audit)
    {
        _users = users;
        _audit = audit;
    }

    public async Task<Result> HandleAsync(ChangeUserRoleCommand c)
    {
        if (!c.CallerIsOwner) return Result.Failure(UserErrors.Forbidden);
        var user = await _users.FindByIdIncludingInactiveAsync(c.UserId);
        if (user is null) return Result.Failure(UserErrors.NotFound);
        if (user.IsOwner) return Result.Failure(UserErrors.OwnerImmutable);

        var before = user.Role.ToString();
        user.ChangeRole(c.NewRole);
        await _users.UpdateAsync(user);
        await _audit.AuditAsync("User", user.Id, "UserRoleChanged",
            new { Role = before }, new { Role = user.Role.ToString() }, c.CorrelationId, null);
        return Result.Success();
    }
}
