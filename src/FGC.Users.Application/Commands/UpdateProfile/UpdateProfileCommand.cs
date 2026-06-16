namespace FGC.Users.Application.Commands.UpdateProfile;

public record UpdateProfileCommand(Guid UserId, string? Name, string? Password, string CorrelationId);
