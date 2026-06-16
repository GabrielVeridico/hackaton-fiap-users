using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Abstractions;
using HackatonFiap.Users.Domain.ValueObjects;

namespace HackatonFiap.Users.Application.Commands.ResetMyPassword;

public record ResetMyPasswordCommand(Guid UserId, string CurrentPassword, string NewPassword, string CorrelationId);

public class ResetMyPasswordCommandHandler
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public ResetMyPasswordCommandHandler(IUserRepository users, IPasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    public async Task<Result> HandleAsync(ResetMyPasswordCommand c)
    {
        var user = await _users.FindByIdAsync(c.UserId);
        if (user is null) return Result.Failure(UserErrors.NotFound);
        if (!_hasher.Verify(c.CurrentPassword, user.Password.HashValue))
            return Result.Failure(UserErrors.InvalidCredentials);
        if (!Password.IsValid(c.NewPassword))
            return Result.Failure(UserErrors.InvalidPassword);

        user.ChangePassword(new Password(_hasher.Hash(c.NewPassword)));
        await _users.UpdateAsync(user);
        return Result.Success();
    }
}
