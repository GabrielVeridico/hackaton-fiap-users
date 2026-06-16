using FGC.Users.Application.DTOs;
using FGC.Users.Application.Errors;
using FGC.Users.Application.Interfaces;
using FGC.Users.Domain.Abstractions;
using FGC.Users.Domain.Entities;
using FGC.Users.Domain.Events;
using FGC.Users.Domain.ValueObjects;

namespace FGC.Users.Application.Commands.CreateUser;

public class CreateUserCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;
    private readonly IEventPublisher _eventPublisher;

    public CreateUserCommandHandler(
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

    public async Task<Result<UserResponse>> HandleAsync(CreateUserCommand command)
    {
        if (!Password.IsValid(command.Password))
            return Result<UserResponse>.Failure(UserErrors.InvalidPassword);

        var existing = await _userRepository.FindByEmailAsync(command.Email);
        if (existing is not null)
            return Result<UserResponse>.Failure(UserErrors.EmailAlreadyRegistered);

        var hash = _passwordHasher.Hash(command.Password);
        var password = new Password(hash);
        var user = User.Create(command.Name, command.Email, password);

        await _userRepository.SaveNewAsync(user);

        await _auditService.AuditAsync(
            "User", user.Id, "UserRegistered",
            null,
            new { user.Id, user.Email, user.Name },
            command.CorrelationId, null);

        await _eventPublisher.PublishAsync(
            "user-registered",
            new UserRegistered(user.Id, user.Email, user.Name),
            command.CorrelationId);

        return Result<UserResponse>.Success(new UserResponse(
            user.Id, user.Email, user.Name, user.Role.ToString(),
            user.CreatedAtUtc, user.UpdatedAtUtc));
    }
}
