namespace EzDdd.UseCase.Port.InOut.Messaging;

/// <summary>
///     Message producer interface for posting messages to an event bus or message broker.
///     This is the PRIMARY interface for components that need to publish messages.
/// </summary>
/// <typeparam name="TMessage">The type of messages this producer posts.</typeparam>
/// <remarks>
///     <para>
///         <c>IMessageProducer</c> follows the Single Responsibility Principle - it ONLY handles
///         message posting. Subscription management (reactor registration) is handled separately
///         by application-layer services (e.g., BackgroundService, Relay pattern).
///     </para>
///     <para>
///         <b>Design Philosophy:</b>
///     </para>
///     <para>
///         This interface provides a pure "producer-only" pattern, aligned with Java ezddd 4.1.0's
///         MessageProducer design. It deliberately excludes subscription management to maintain
///         clear separation of concerns between message posting and message consumption.
///     </para>
///     <para>
///         <b>Resource Management:</b>
///     </para>
///     <para>
///         This interface extends <see cref="IDisposable" /> to support resource cleanup for external
///         message bus adapters (e.g., Kafka, RabbitMQ, Azure Service Bus). Implementations should
///         release network connections, file handles, and other resources in the <c>Dispose()</c> method.
///         For in-memory implementations (e.g., <see cref="InMemoryMessageProducer{TMessage}" />),
///         the <c>Dispose()</c> method may perform cleanup of internal state.
///     </para>
///     <para>
///         <b>Usage Example:</b>
///     </para>
///     <code>
/// // In a repository
/// public class EsRepository&lt;TAggregate, TId&gt;
/// {
///     private readonly IMessageProducer&lt;DomainEventData&gt; _eventProducer;
/// 
///     public EsRepository(
///         IRepositoryPeer&lt;EventStoreData&lt;TId&gt;, TId&gt; peer,
///         IMessageProducer&lt;DomainEventData&gt;? eventProducer = null)
///     {
///         _peer = peer;
///         _eventProducer = eventProducer;
///     }
/// 
///     public async Task SaveAsync(TAggregate aggregate)
///     {
///         // ... save to event store ...
/// 
///         // Publish domain events (if producer is configured)
///         if (_eventProducer != null)
///         {
///             foreach (var domainEvent in aggregate.GetDomainEvents())
///             {
///                 var eventData = DomainEventMapper.ToEventData(domainEvent);
///                 await _eventProducer.PostAsync(eventData);
///             }
///         }
///     }
/// }
/// 
/// // External event bus adapter example
/// using (var kafkaProducer = new KafkaMessageProducer(config))
/// {
///     await kafkaProducer.PostAsync(eventData);
/// } // Dispose() closes Kafka connection
/// </code>
/// </remarks>
public interface IMessageProducer<in TMessage> : IDisposable
{
    /// <summary>
    ///     Posts a message to the message bus or message broker.
    /// </summary>
    /// <param name="message">The message to post.</param>
    /// <returns>
    ///     A task that completes when the message has been successfully posted to the message infrastructure.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when message is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if this producer has been disposed.</exception>
    /// <exception cref="Exceptions.PostEventFailureException">
    ///     Thrown if the message could not be posted due to infrastructure failure
    ///     (e.g., network error, broker unavailable).
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         The behavior of this method depends on the underlying message infrastructure.
    ///         For in-memory implementations (e.g., <see cref="InMemoryMessageProducer{TMessage}" />),
    ///         this method completes immediately after storing the message.
    ///         For external brokers (e.g., Kafka, RabbitMQ), this method completes after the broker
    ///         acknowledges receipt of the message.
    ///     </para>
    ///     <para>
    ///         This method does NOT wait for message consumers to process the message.
    ///         Message posting and message consumption are decoupled.
    ///     </para>
    /// </remarks>
    Task PostAsync(TMessage message);
}