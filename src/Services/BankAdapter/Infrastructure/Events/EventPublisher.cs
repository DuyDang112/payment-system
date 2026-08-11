using System.Text.Json;
using BankAdapter.Shared;

namespace BankAdapter.Infrastructure.Events;

/// <summary>
/// Interface for event publishing
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent;
    Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory event publisher for development/testing
/// </summary>
public sealed class InMemoryEventPublisher(
    ILogger<InMemoryEventPublisher> logger) : IEventPublisher
{
    private readonly List<IEvent> _eventHistory = new();

    public async Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Publishing event {EventType}: {@Event}",
            @event.EventType,
            @event);

        _eventHistory.Add(@event);

        await Task.CompletedTask;
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
    {
        return PublishAsync((IEvent)@event, cancellationToken);
    }

    /// <summary>
    /// Get event history for testing
    /// </summary>
    public IReadOnlyList<IEvent> GetEventHistory()
    {
        return _eventHistory.AsReadOnly();
    }

    /// <summary>
    /// Clear event history
    /// </summary>
    public void ClearHistory()
    {
        _eventHistory.Clear();
    }
}

/// <summary>
/// Message broker event publisher (RabbitMQ, Kafka, etc.)
/// </summary>
public sealed class MessageBrokerEventPublisher(
    ILogger<MessageBrokerEventPublisher> logger,
    IConfiguration configuration) : IEventPublisher
{
    public async Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Publishing event {EventType} to message broker",
                @event.EventType);

            // Placeholder for actual message broker integration
            // Would serialize and publish to RabbitMQ/Kafka/Azure Service Bus/etc.

            var message = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            logger.LogDebug("Event payload: {Message}", message);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish event {EventType}", @event.EventType);
            throw;
        }
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
    {
        return PublishAsync((IEvent)@event, cancellationToken);
    }
}
