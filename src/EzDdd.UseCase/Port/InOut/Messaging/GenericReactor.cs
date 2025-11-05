using EzDdd.UseCase.Port.In;

namespace EzDdd.UseCase.Port.InOut.Messaging;

/// <summary>
///     Generic reactor implementation using functional interface.
///     Converts a <see cref="Func{TMessage, Task}" /> delegate into an <see cref="IReactor{TInput}" />
///     implementation, enabling lambda-friendly reactor creation.
/// </summary>
/// <typeparam name="TMessage">The type of messages this reactor processes.</typeparam>
/// <remarks>
///     <para>
///         <c>GenericReactor</c> provides a convenient way to create reactors without implementing
///         the <see cref="IReactor{TInput}" /> interface. Instead, you can pass a lambda expression
///         or method reference that processes the message.
///     </para>
///     <para>
///         This functional adapter pattern simplifies reactor creation and makes the code more concise,
///         especially for simple use cases or when prototyping.
///     </para>
///     <para>
///         <b>Usage Examples:</b>
///     </para>
///     <code>
/// // Example 1: Simple lambda reactor
/// var bus = new BlockingMessageBus&lt;DomainEventData&gt;();
/// 
/// bus.Register(new GenericReactor&lt;DomainEventData&gt;(async @event =&gt;
/// {
///     Console.WriteLine($"Event received: {@event.EventType}");
///     await Task.CompletedTask;
/// }));
/// 
/// // Example 2: Reactor with async processing
/// bus.Register(new GenericReactor&lt;DomainEventData&gt;(async @event =&gt;
/// {
///     await _emailService.SendNotificationAsync(@event);
///     await _auditLog.RecordAsync(@event);
/// }));
/// 
/// // Example 3: Method reference
/// bus.Register(new GenericReactor&lt;DomainEventData&gt;(HandleDomainEvent));
/// 
/// private async Task HandleDomainEvent(DomainEventData @event)
/// {
///     // ... processing logic ...
/// }
/// </code>
///     <para>
///         <b>When to Use:</b>
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Quick prototyping and testing</description>
///         </item>
///         <item>
///             <description>Simple event handlers without complex state</description>
///         </item>
///         <item>
///             <description>Inline reactor definitions</description>
///         </item>
///         <item>
///             <description>When you want to avoid creating a full class for a simple handler</description>
///         </item>
///     </list>
///     <para>
///         <b>When NOT to Use:</b>
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Complex reactors with multiple dependencies</description>
///         </item>
///         <item>
///             <description>Reactors that need to maintain state across invocations</description>
///         </item>
///         <item>
///             <description>When you need better testability and separation of concerns</description>
///         </item>
///     </list>
/// </remarks>
public class GenericReactor<TMessage> : IReactor<TMessage>
{
    private readonly Func<TMessage, Task> _handler;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GenericReactor{TMessage}" /> class.
    /// </summary>
    /// <param name="handler">
    ///     The asynchronous handler function to invoke when a message is received.
    ///     Must not be null.
    /// </param>
    /// <remarks>
    ///     The handler will be invoked with the message whenever <see cref="ExecuteAsync" /> is called.
    ///     Any exceptions thrown by the handler will propagate to the caller.
    /// </remarks>
    public GenericReactor(Func<TMessage, Task> handler)
    {
        _handler = handler;
    }

    /// <summary>
    ///     Executes the reactor logic by invoking the provided handler with the message.
    /// </summary>
    /// <param name="input">The input message to process.</param>
    /// <returns>
    ///     A task that completes when the handler has finished processing the message.
    /// </returns>
    /// <remarks>
    ///     This method simply delegates to the handler function provided in the constructor.
    ///     Any exceptions thrown by the handler will propagate to the caller without modification.
    /// </remarks>
    public async Task ExecuteAsync(TMessage input)
    {
        await _handler(input);
    }
}