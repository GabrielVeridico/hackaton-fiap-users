namespace HackatonFiap.Users.Application.Commands.Logout;

public record LogoutCommand(string RefreshToken, string CorrelationId);
