using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FGC.Users.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FGC.Users.Infrastructure.EventPublishers;

public class ServiceBusEventPublisher : IEventPublisher
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusEventPublisher> _logger;

    public ServiceBusEventPublisher(ServiceBusClient client, ILogger<ServiceBusEventPublisher> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task PublishAsync(string topic, object @event, string correlationId)
    {
        try
        {
            var sender = _client.CreateSender(topic);
            var msg = new ServiceBusMessage(JsonSerializer.Serialize(@event));
            msg.ApplicationProperties["x-correlation-id"] = correlationId;
            await sender.SendMessageAsync(msg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {Topic}", topic);
        }
    }
}
