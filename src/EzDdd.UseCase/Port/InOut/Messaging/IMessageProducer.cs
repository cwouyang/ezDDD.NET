namespace EzDdd.UseCase.Port.InOut.Messaging;

/// <summary>
///     Message producer interface for posting messages to a message bus.
///     Provides a simplified adapter pattern for components that only need to send messages,
///     without requiring knowledge of reactor registration or message bus management.
/// </summary>
/// <typeparam name="TMessage">The type of messages this producer posts.</typeparam>
/// <remarks>
///     <para>
///         <c>IMessageProducer</c> is a simplified interface compared to <see cref="IMessageBus{TMessage}" />.
///         It only exposes the posting capability, hiding the registration/unregistration complexity.
///         This makes it ideal for use cases, commands, and other components that only need to publish messages.
///     </para>
///     <para>
///         <b>Comparison with IMessageBus:</b>
///     </para>
///     <list type="bullet">
///         <item>
///             <description><b>IMessageBus</b>: Full interface with Register, Unregister, and PostAsync</description>
///         </item>
///         <item>
///             <description><b>IMessageProducer</b>: Simplified interface with only PostAsync</description>
///         </item>
///     </list>
///     <para>
///         <b>Resource Management:</b>
///     </para>
///     <para>
///         This interface extends <see cref="IDisposable" /> to support resource cleanup for external
///         message bus adapters (e.g., Kafka, RabbitMQ, Azure Service Bus). Implementations should
///         release network connections, file handles, and other resources in the <c>Dispose()</c> method.
///         For in-memory message bus implementations (e.g., <c>BlockingMessageBus</c>), the <c>Dispose()</c>
///         method may be a no-op.
///     </para>
///     <para>
///         <b>Usage Example:</b>
///     </para>
///     <code>
/// // In a use case or command handler
/// public class CreateOrderCommand : IUseCase&lt;CreateOrderInput, CreateOrderOutput&gt;
/// {
///     private readonly IMessageProducer&lt;DomainEventData&gt; _eventProducer;
/// 
///     public CreateOrderCommand(IMessageProducer&lt;DomainEventData&gt; eventProducer)
///     {
///         _eventProducer = eventProducer;
///     }
/// 
///     public async Task&lt;CreateOrderOutput&gt; ExecuteAsync(CreateOrderInput input)
///     {
///         // ... create order logic ...
/// 
///         // Publish domain event
///         await _eventProducer.PostAsync(orderCreatedEvent);
/// 
///         return output;
///     }
/// }
/// 
/// // External event bus adapter example
/// using (var kafkaProducer = new KafkaEventBusProducer(config))
/// {
///     await kafkaProducer.PostAsync(eventData);
/// } // Dispose() closes Kafka connection
/// </code>
/// </remarks>
public interface IMessageProducer<in TMessage> : IDisposable
{
    /// <summary>
    ///     Posts a message to the underlying message bus.
    /// </summary>
    /// <param name="message">The message to post.</param>
    /// <returns>
    ///     A task that completes when the message has been posted and all registered reactors have processed it.
    /// </returns>
    /// <remarks>
    ///     The behavior of this method depends on the underlying message bus implementation.
    ///     For synchronous message buses (e.g., <c>BlockingMessageBus</c>), this method will complete
    ///     only after all registered reactors have finished processing the message.
    /// </remarks>
    Task PostAsync(TMessage message);
}