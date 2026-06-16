using HackatonFiap.Users.Domain.Entities;

namespace HackatonFiap.Users.Application.DTOs;

public record UserResponse(
    Guid Id,
    string PersonType,
    string Document,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    bool IsOwner,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static UserResponse From(User u) => new(
        u.Id, u.PersonType.ToString(), u.Document.Value, u.Name, u.Email,
        u.Role.ToString(), u.IsActive, u.IsOwner, u.CreatedAtUtc, u.UpdatedAtUtc);
}
