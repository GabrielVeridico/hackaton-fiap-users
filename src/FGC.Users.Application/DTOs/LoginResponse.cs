namespace FGC.Users.Application.DTOs;

public record LoginResponse(string Token, DateTime ExpiresAtUtc);
