namespace HackatonFiap.Users.Application.DTOs;

public record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn);
