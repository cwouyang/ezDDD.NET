using EzDdd.UseCase.Port.In;

namespace EzDdd.UseCase.Port.InOut.Messaging;

/// <summary>
///     Message bus interface for intra-process event distribution.
///     Implements the Observer pattern (Pub-Sub) for decoupled communication between components.
/// </summary>
/// <typeparam name="TMessage">The type of messages this bus handles.</typeparam>
/// <remarks>
///     <para>
///         The message bus allows components to communicate without direct dependencies.
///         Reactors can register to receive messages, and producers can post messages to all registered reactors.
///     </para>
///     <para>
///         This is a synchronous (blocking) message bus - all reactors are invoked sequentially.
///         For asynchronous non-blocking behavior, use an event queue implementation instead.
///     </para>
///     <para>
///         <b>Usage Example:</b>
///     </para>
///     <code>
/// // Create a message bus
/// IMessageBus&lt;DomainEventData&gt; eventBus = new BlockingMessageBus&lt;DomainEventData&gt;();
/// 
/// // Register reactors
/// eventBus.Register(new MyEventReactor());
/// eventBus.Register(new AnotherReactor());
/// 
/// // Post a message (all reactors will be invoked)
/// await eventBus.PostAsync(domainEventData);
/// 
/// // Unregister when done
/// eventBus.Unregister(myReactor);
/// </code>
/// </remarks>
public interface IMessageBus<TMessage>
{
    /// <summary>
    ///     Registers a reactor to receive messages posted to this bus.
    /// </summary>
    /// <param name="reactor">The reactor to register. Must not be null.</param>
    /// <remarks>
    ///     The same reactor can be registered multiple times, and will be invoked once per registration.
    ///     Thread-safe: can be called concurrently with other Register, Unregister, or PostAsync calls.
    /// </remarks>
    void Register(IReactor<TMessage> reactor);

    /// <summary>
    ///     Unregisters a reactor from receiving messages.
    /// </summary>
    /// <param name="reactor">The reactor to unregister. Must not be null.</param>
    /// <remarks>
    ///     If the reactor is registered multiple times, only one registration will be removed.
    ///     If the reactor is not registered, this method does nothing (no exception thrown).
    ///     Thread-safe: can be called concurrently with other Register, Unregister, or PostAsync calls.
    /// </remarks>
    void Unregister(IReactor<TMessage> reactor);

    /// <summary>
    ///     Posts a message to all registered reactors.
    ///     Reactors are invoked sequentially in the order they were registered.
    /// </summary>
    /// <param name="message">The message to post to all registered reactors.</param>
    /// <returns>
    ///     A task that completes when all registered reactors have finished processing the message.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         If no reactors are registered, this method completes immediately without error.
    ///     </para>
    ///     <para>
    ///         If a reactor throws an exception, the exception propagates to the caller
    ///         and subsequent reactors will not be invoked.
    ///     </para>
    ///     <para>
    ///         Thread-safe: Multiple threads can call PostAsync concurrently.
    ///         The set of reactors is captured at the start of the call, so reactors
    ///         registered or unregistered during execution will not affect the current post.
    ///     </para>
    /// </remarks>
    Task PostAsync(TMessage message);
}