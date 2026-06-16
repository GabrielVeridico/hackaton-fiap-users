namespace HackatonFiap.Users.Application.Commands.RefreshTokenFlow;

public record RefreshTokenCommand(string RefreshToken, string CorrelationId);
