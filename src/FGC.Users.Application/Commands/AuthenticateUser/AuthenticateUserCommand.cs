namespace FGC.Users.Application.Commands.AuthenticateUser;

public record AuthenticateUserCommand(string Email, string Password, string CorrelationId);
