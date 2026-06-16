using HackatonFiap.Users.Application.DTOs;
using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Abstractions;

namespace HackatonFiap.Users.Application.Commands.UpdateMyProfile;

public record UpdateMyProfileCommand(Guid UserId, string Name, string CorrelationId);

public class UpdateMyProfileCommandHandler
{
    private readonly IUserRepository _users;

    public UpdateMyProfileCommandHandler(IUserRepository users) => _users = users;

    public async Task<Result<UserResponse>> HandleAsync(UpdateMyProfileCommand c)
    {
        var user = await _users.FindByIdAsync(c.UserId);
        if (user is null) return Result<UserResponse>.Failure(UserErrors.NotFound);

        user.UpdateProfile(c.Name);
        await _users.UpdateAsync(user);
        return Result<UserResponse>.Success(UserResponse.From(user));
    }
}
