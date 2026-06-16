namespace HackatonFiap.Users.Application.Commands.CreateUser;

public record CreateUserCommand(string Name, string Email, string Password, string CorrelationId);
