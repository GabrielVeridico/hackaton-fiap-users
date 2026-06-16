namespace FGC.Users.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(string topic, object @event, string correlationId);
}
