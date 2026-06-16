namespace FGC.Users.Application.DTOs;

public record UserResponse(
    Guid Id,
    string Email,
    string Name,
    string Role,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
