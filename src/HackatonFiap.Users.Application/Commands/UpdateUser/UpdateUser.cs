using HackatonFiap.Users.Application.DTOs;
using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Abstractions;
using HackatonFiap.Users.Domain.Enums;

namespace HackatonFiap.Users.Application.Commands.UpdateUser;

public record UpdateUserCommand(Guid UserId, string Name, bool CallerIsOwner, string CorrelationId);

public class UpdateUserCommandHandler
{
    private readonly IUserRepository _users;

    public UpdateUserCommandHandler(IUserRepository users) => _users = users;

    public async Task<Result<UserResponse>> HandleAsync(UpdateUserCommand c)
    {
        var user = await _users.FindByIdIncludingInactiveAsync(c.UserId);
        if (user is null) return Result<UserResponse>.Failure(UserErrors.NotFound);
        if (user.IsOwner) return Result<UserResponse>.Failure(UserErrors.OwnerImmutable);
        if (user.Role == UserRole.GestorONG && !c.CallerIsOwner)
            return Result<UserResponse>.Failure(UserErrors.CannotManageGestor);

        user.UpdateProfile(c.Name);
        await _users.UpdateAsync(user);
        return Result<UserResponse>.Success(UserResponse.From(user));
    }
}
