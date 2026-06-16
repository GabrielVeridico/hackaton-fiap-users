namespace FGC.Users.Application.Interfaces;

public interface IAuditService
{
    Task AuditAsync(string aggregateType, Guid aggregateId, string eventType, object? before, object? after, string correlationId, string? userId);
}
