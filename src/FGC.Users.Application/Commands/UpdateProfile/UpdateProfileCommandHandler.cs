using FGC.Users.Application.DTOs;
using FGC.Users.Application.Errors;
using FGC.Users.Application.Interfaces;
using FGC.Users.Domain.Abstractions;
using FGC.Users.Domain.Events;
using FGC.Users.Domain.ValueObjects;

namespace FGC.Users.Application.Commands.UpdateProfile;

public class UpdateProfileCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;
    private readonly IEventPublisher _eventPublisher;

    public UpdateProfileCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuditService auditService,
        IEventPublisher eventPublisher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<UserResponse>> HandleAsync(UpdateProfileCommand command)
    {
        var user = await _userRepository.FindByIdAsync(command.UserId);
        if (user is null)
            return Result<UserResponse>.Failure(UserErrors.NotFound);

        var before = new { user.Id, user.Email, user.Name };

        Password? newPassword = null;
        if (!string.IsNullOrEmpty(command.Password))
        {
            if (!Password.IsValid(command.Password))
                return Result<UserResponse>.Failure(UserErrors.InvalidPassword);

            var hash = _passwordHasher.Hash(command.Password);
            newPassword = new Password(hash);
        }

        var name = command.Name ?? user.Name;
        user.UpdateProfile(name, newPassword);

        await _userRepository.UpdateAsync(user);

        var after = new { user.Id, user.Email, user.Name };

        await _auditService.AuditAsync(
            "User", user.Id, "UserProfileUpdated",
            before, after,
            command.CorrelationId, command.UserId.ToString());

        await _eventPublisher.PublishAsync(
            "user-profile-updated",
            new UserProfileUpdated(user.Id, user.Name),
            command.CorrelationId);

        return Result<UserResponse>.Success(new UserResponse(
            user.Id, user.Email, user.Name, user.Role.ToString(),
            user.CreatedAtUtc, user.UpdatedAtUtc));
    }
}
