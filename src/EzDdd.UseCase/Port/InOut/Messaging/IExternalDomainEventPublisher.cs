namespace EzDdd.UseCase.Port.InOut.Messaging;

/// <summary>
///     An <c>IExternalDomainEventPublisher</c> is an out-port abstraction for publishing
///     external domain events (i.e., integration events) to external systems, such as
///     a message broker (e.g., Kafka), downstream bounded contexts, or front-ends.
/// </summary>
/// <remarks>
///     <para>
///         A typical publisher is invoked by a notifier (see <c>INotifier</c> in EzDdd.Cqrs),
///         which converts internal domain events into <see cref="IExternalDomainEvent" />
///         instances before dispatching them outward. Keeping publication behind this
///         out-port upholds the cross-layer principle of Clean Architecture: use cases
///         depend on the abstraction, while concrete messaging adapters live in the
///         frameworks and drivers layer.
///     </para>
///     <para>
///         Publishing to external systems is inherently I/O-bound, so this out-port is
///         asynchronous: <c>PublishAsync(TEvent)</c> corresponds to the Java
///         <c>ExternalDomainEventPublisher.publish(E)</c> method.
///     </para>
/// </remarks>
/// <typeparam name="TEvent">The type of external domain event this publisher publishes.</typeparam>
public interface IExternalDomainEventPublisher<in TEvent> where TEvent : IExternalDomainEvent
{
    /// <summary>
    ///     Publishes the given external domain event to an external system asynchronously.
    /// </summary>
    /// <param name="event">The external domain event to publish.</param>
    /// <returns>A task representing the asynchronous publish operation.</returns>
    Task PublishAsync(TEvent @event);
}
