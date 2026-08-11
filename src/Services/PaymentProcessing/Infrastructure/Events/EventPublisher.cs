using PaymentProcessing.Domain.Events;

namespace PaymentProcessing.Infrastructure.Events;

/// <summary>
/// In-memory event publisher (can be replaced with message bus implementation)
/// </summary>
public sealed class EventPublisher(ILogger<EventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Publishing domain event: {EventType}",
            domainEvent.GetType().Name);

        return Task.CompletedTask;
    }

    public Task PublishAsync(IEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Publishing integration event: {EventType}",
            integrationEvent.GetType().Name);

        return Task.CompletedTask;
    }

    public Task PublishDomainEventsAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        var tasks = domainEvents.Select(e => PublishAsync(e, cancellationToken));
        return Task.WhenAll(tasks);
    }
}
