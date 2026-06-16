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
    DateTime UpdatedAtUtc);
