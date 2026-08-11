using PaymentRouter.Domain.Events;

namespace PaymentRouter.Domain.Events;

/// <summary>
/// Interface for publishing domain events
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) where TEvent : IEvent;
}
