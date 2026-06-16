using HackatonFiap.Users.Application.DTOs;
using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Abstractions;
using HackatonFiap.Users.Domain.Entities;
using HackatonFiap.Users.Domain.ValueObjects;

namespace HackatonFiap.Users.Application.Commands.RegisterDonor;

public class RegisterDonorCommandHandler
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditService _audit;

    public RegisterDonorCommandHandler(IUserRepository users, IPasswordHasher hasher, IAuditService audit)
    {
        _users = users;
        _hasher = hasher;
        _audit = audit;
    }

    public async Task<Result<UserResponse>> HandleAsync(RegisterDonorCommand command)
    {
        if (!Password.IsValid(command.Password))
            return Result<UserResponse>.Failure(UserErrors.InvalidPassword);

        if (!Document.IsValid(command.Document, command.PersonType))
            return Result<UserResponse>.Failure(UserErrors.DocumentInvalid);

        var document = Document.Create(command.Document, command.PersonType);

        var byEmail = await _users.FindByEmailIncludingInactiveAsync(command.Email);
        var byDoc = await _users.FindByDocumentIncludingInactiveAsync(document.Value);
        var existing = byEmail ?? byDoc;
        if (existing is not null)
            return Result<UserResponse>.Failure(existing.IsActive
                ? UserErrors.AlreadyRegisteredActive
                : UserErrors.AlreadyRegisteredInactive);

        var password = new Password(_hasher.Hash(command.Password));
        var user = User.RegisterDonor(command.PersonType, document, command.Name, command.Email, password);

        await _users.SaveNewAsync(user);
        await _audit.AuditAsync("User", user.Id, "DonorRegistered", null,
            new { user.Id, user.Email, DocumentValue = user.Document.Value }, command.CorrelationId, null);

        return Result<UserResponse>.Success(new UserResponse(
            user.Id, user.PersonType.ToString(), user.Document.Value, user.Name, user.Email,
            user.Role.ToString(), user.IsActive, user.IsOwner, user.CreatedAtUtc, user.UpdatedAtUtc));
    }
}
