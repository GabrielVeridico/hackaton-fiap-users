using System.Text.Json;
using FGC.Users.Application.Interfaces;
using FGC.Users.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;

namespace FGC.Users.Infrastructure.Audit;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AuditService(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task AuditAsync(string aggregateType, Guid aggregateId, string eventType, object? before, object? after, string correlationId, string? userId)
    {
        var traceId = _http.HttpContext?.TraceIdentifier;
        var entry = new AuditEvent
        {
            Id = Guid.NewGuid(),
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            EventType = eventType,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after),
            CreatedAtUtc = DateTime.UtcNow,
            CorrelationId = correlationId,
            TraceId = traceId,
            UserId = userId
        };
        _db.AuditEvents.Add(entry);
        await _db.SaveChangesAsync();
    }
}
