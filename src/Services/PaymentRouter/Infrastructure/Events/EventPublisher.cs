using PaymentRouter.Domain.Events;
using Microsoft.Extensions.Logging;

namespace PaymentRouter.Infrastructure.Events;

/// <summary>
/// In-memory event publisher (for testing/stub)
/// In production, this would publish to a message broker like RabbitMQ or Kafka
/// </summary>
public sealed class EventPublisher : IEventPublisher
{
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(ILogger<EventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) where TEvent : IEvent
    {
        _logger.LogInformation("Event published: {EventType} - {@Event}", typeof(TEvent).Name, @event);

        // In production, publish to message broker
        // Example: await _messageBus.PublishAsync(@event, cancellationToken);

        return Task.CompletedTask;
    }
}
