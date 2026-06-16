using HackatonFiap.Users.Application.DTOs;
using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Abstractions;
using HackatonFiap.Users.Domain.Entities;
using HackatonFiap.Users.Domain.Enums;
using HackatonFiap.Users.Domain.ValueObjects;

namespace HackatonFiap.Users.Application.Commands.CreateUser;

public record CreateUserCommand(
    PersonType PersonType, string Document, string Name, string Email, string Password, UserRole Role, bool CallerIsOwner, string CorrelationId);

public class CreateUserCommandHandler
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditService _audit;

    public CreateUserCommandHandler(IUserRepository users, IPasswordHasher hasher, IAuditService audit)
    {
        _users = users;
        _hasher = hasher;
        _audit = audit;
    }

    public async Task<Result<UserResponse>> HandleAsync(CreateUserCommand c)
    {
        if (c.Role == UserRole.GestorONG && !c.CallerIsOwner)
            return Result<UserResponse>.Failure(UserErrors.CannotManageGestor);
        if (!Password.IsValid(c.Password))
            return Result<UserResponse>.Failure(UserErrors.InvalidPassword);
        if (!Document.IsValid(c.Document, c.PersonType))
            return Result<UserResponse>.Failure(UserErrors.DocumentInvalid);

        var doc = Document.Create(c.Document, c.PersonType);
        var existing = await _users.FindByEmailIncludingInactiveAsync(c.Email)
            ?? await _users.FindByDocumentIncludingInactiveAsync(doc.Value);
        if (existing is not null)
            return Result<UserResponse>.Failure(existing.IsActive
                ? UserErrors.AlreadyRegisteredActive
                : UserErrors.AlreadyRegisteredInactive);

        var user = User.CreateByAdmin(c.PersonType, doc, c.Name, c.Email, new Password(_hasher.Hash(c.Password)), c.Role);
        await _users.SaveNewAsync(user);
        await _audit.AuditAsync("User", user.Id, "UserCreatedByAdmin", null,
            new { user.Id, user.Email, Role = user.Role.ToString() }, c.CorrelationId, null);
        return Result<UserResponse>.Success(UserResponse.From(user));
    }
}
