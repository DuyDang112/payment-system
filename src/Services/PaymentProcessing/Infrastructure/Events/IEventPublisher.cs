using PaymentProcessing.Domain.Events;

namespace PaymentProcessing.Infrastructure.Events;

/// <summary>
/// Interface for publishing domain and integration events
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task PublishAsync(IEvent integrationEvent, CancellationToken cancellationToken = default);
    Task PublishDomainEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
