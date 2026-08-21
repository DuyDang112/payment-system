namespace Identity.Infrastructure.Events;

/// <summary>
/// Interface for publishing events
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event asynchronously
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
}

/// <summary>
/// In-memory event publisher for development purposes
/// </summary>
public class InMemoryEventPublisher : IEventPublisher
{
    private readonly ILogger<InMemoryEventPublisher> _logger;

    public InMemoryEventPublisher(ILogger<InMemoryEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        _logger.LogInformation("Event published: {EventType}", typeof(TEvent).Name);
        return Task.CompletedTask;
    }
}
