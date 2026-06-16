namespace FGC.Users.Domain.Events;

public record UserProfileUpdated(Guid UserId, string Name);
