namespace EzDdd.UseCase.Port.InOut.Messaging;

/// <summary>
///     Producer for posting domain events to an event bus.
///     Implements the Adapter pattern to provide a simplified <see cref="IMessageProducer{TMessage}" />
///     interface over an <see cref="IMessageBus{TMessage}" /> for domain event posting.
/// </summary>
/// <remarks>
///     <para>
///         <c>EventBusProducer</c> is specifically designed for posting <see cref="DomainEventData" />
///         to an event bus. It wraps an <see cref="IMessageBus{TMessage}" /> and delegates all
///         posting operations to it.
///     </para>
///     <para>
///         This adapter is useful for components that need to publish domain events but don't need
///         to manage reactor registrations. Examples include use cases, commands, and aggregate repositories.
///     </para>
///     <para>
///         <b>Resource Management:</b>
///     </para>
///     <para>
///         This implementation uses an in-memory <see cref="IMessageBus{TMessage}" />, so the
///         <see cref="Dispose()" /> method is a no-op (no resources to release). However, the disposal
///         state is tracked to prevent usage after disposal, following the IDisposable contract.
///         External event bus adapters (e.g., Kafka, RabbitMQ) should release network connections
///         in their <see cref="Dispose()" /> implementations.
///     </para>
///     <para>
///         <b>Usage Example:</b>
///     </para>
///     <code>
/// // Create event bus and producer
/// var eventBus = new BlockingMessageBus&lt;DomainEventData&gt;();
/// using var eventProducer = new EventBusProducer(eventBus);
/// 
/// // Use in a repository or use case
/// public class OrderRepository
/// {
///     private readonly IMessageProducer&lt;DomainEventData&gt; _eventProducer;
/// 
///     public OrderRepository(IMessageProducer&lt;DomainEventData&gt; eventProducer)
///     {
///         _eventProducer = eventProducer;
///     }
/// 
///     public async Task SaveAsync(Order order)
///     {
///         // ... save order to database ...
/// 
///         // Publish domain events
///         foreach (var domainEvent in order.GetDomainEvents())
///         {
///             var eventData = DomainEventMapper.ToEventData(domainEvent);
///             await _eventProducer.PostAsync(eventData);
///         }
///     }
/// }
/// </code>
/// </remarks>
public class EventBusProducer : IMessageProducer<DomainEventData>
{
    private readonly IMessageBus<DomainEventData> _eventBus;
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventBusProducer" /> class.
    /// </summary>
    /// <param name="eventBus">The underlying event bus to delegate posting operations to.</param>
    public EventBusProducer(IMessageBus<DomainEventData> eventBus)
    {
        _eventBus = eventBus;
    }

    /// <summary>
    ///     Posts a domain event to the underlying event bus.
    ///     All registered reactors on the bus will receive and process this event.
    /// </summary>
    /// <param name="message">The domain event data to post.</param>
    /// <returns>
    ///     A task that completes when the event has been posted and all registered reactors have processed it.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    ///     Thrown if this producer has been disposed.
    /// </exception>
    /// <remarks>
    ///     This method simply delegates to the underlying <see cref="IMessageBus{TMessage}.PostAsync" /> method.
    ///     The behavior (synchronous/asynchronous, error handling, etc.) depends on the bus implementation.
    /// </remarks>
    public async Task PostAsync(DomainEventData message)
    {
        _ThrowIfDisposed();
        await _eventBus.PostAsync(message);
    }

    /// <summary>
    ///     Disposes the event bus producer.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For this in-memory implementation, disposal is a no-op as there are no external resources
    ///         to release. However, disposal state is tracked to prevent usage after disposal.
    ///     </para>
    ///     <para>
    ///         This method is idempotent and can be called multiple times safely.
    ///     </para>
    ///     <para>
    ///         External event bus adapters (e.g., <c>KafkaEventBusProducer</c>, <c>RabbitMQEventBusProducer</c>)
    ///         should override this method to close network connections and release resources.
    ///     </para>
    /// </remarks>
    public void Dispose()
    {
        _disposed = true;
    }

    private void _ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException
            (
                nameof(EventBusProducer),
                "Cannot post messages to a disposed event bus producer."
            );
        }
    }
}