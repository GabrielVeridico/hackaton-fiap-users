namespace FGC.Users.Domain.Events;

public record UserRegistered(Guid UserId, string Email, string Name);
