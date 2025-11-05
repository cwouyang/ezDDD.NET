using EzDdd.UseCase.Port.In;

namespace EzDdd.UseCase.Port.InOut.Messaging;

/// <summary>
///     Synchronous message bus implementation with thread-safe reactor management.
///     Uses the snapshot pattern to ensure that reactors registered or unregistered during
///     message posting do not affect the current post operation.
/// </summary>
/// <typeparam name="TEvent">The type of events/messages this bus handles.</typeparam>
/// <remarks>
///     <para>
///         <c>BlockingMessageBus</c> is a thread-safe, in-process message bus that invokes all
///         registered reactors sequentially (one after another) when a message is posted.
///     </para>
///     <para>
///         <b>Thread Safety:</b>
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Register and Unregister are protected by a lock</description>
///         </item>
///         <item>
///             <description>PostAsync uses snapshot pattern: captures reactor list at start, executes outside lock</description>
///         </item>
///         <item>
///             <description>Multiple threads can safely call Register, Unregister, or PostAsync concurrently</description>
///         </item>
///     </list>
///     <para>
///         <b>Snapshot Pattern:</b>
///     </para>
///     <para>
///         When PostAsync is called, the current list of reactors is copied (snapshot) inside a lock.
///         The execution then happens outside the lock using this snapshot. This ensures that:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Reactors registered during execution will not receive the current message</description>
///         </item>
///         <item>
///             <description>Reactors unregistered during execution will still receive the current message</description>
///         </item>
///         <item>
///             <description>The message bus remains responsive to new registrations/unregistrations</description>
///         </item>
///     </list>
///     <para>
///         <b>Usage Example:</b>
///     </para>
///     <code>
/// // Create message bus
/// var eventBus = new BlockingMessageBus&lt;DomainEventData&gt;();
/// 
/// // Register reactors
/// eventBus.Register(new EmailNotificationReactor());
/// eventBus.Register(new AuditLogReactor());
/// 
/// // Post event (both reactors will be invoked sequentially)
/// await eventBus.PostAsync(domainEventData);
/// </code>
/// </remarks>
public class BlockingMessageBus<TEvent> : IMessageBus<TEvent>
{
    private readonly object _lock = new();
    private readonly List<IReactor<TEvent>> _reactors = [];

    /// <summary>
    ///     Registers a reactor to receive messages posted to this bus.
    ///     The same reactor can be registered multiple times and will be invoked once per registration.
    /// </summary>
    /// <param name="reactor">The reactor to register. Must not be null.</param>
    /// <remarks>
    ///     Thread-safe: can be called concurrently with other Register, Unregister, or PostAsync calls.
    /// </remarks>
    public void Register(IReactor<TEvent> reactor)
    {
        lock (_lock)
        {
            _reactors.Add(reactor);
        }
    }

    /// <summary>
    ///     Unregisters a reactor from receiving messages.
    ///     If the reactor is registered multiple times, only one registration will be removed.
    /// </summary>
    /// <param name="reactor">The reactor to unregister. Must not be null.</param>
    /// <remarks>
    ///     If the reactor is not registered, this method does nothing (no exception thrown).
    ///     Thread-safe: can be called concurrently with other Register, Unregister, or PostAsync calls.
    /// </remarks>
    public void Unregister(IReactor<TEvent> reactor)
    {
        lock (_lock)
        {
            _reactors.Remove(reactor);
        }
    }

    /// <summary>
    ///     Posts a message to all registered reactors.
    ///     Reactors are invoked sequentially in the order they were registered.
    ///     Uses snapshot pattern: the list of reactors is captured at the start,
    ///     so registrations/unregistrations during execution do not affect this post.
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
    ///         Each call captures its own snapshot of reactors at the time of the call.
    ///     </para>
    /// </remarks>
    public async Task PostAsync(TEvent message)
    {
        IReactor<TEvent>[] snapshot;

        // Capture snapshot inside lock
        lock (_lock)
        {
            snapshot = _reactors.ToArray();
        }

        // Execute reactors outside lock (sequential execution)
        foreach (IReactor<TEvent> reactor in snapshot)
        {
            await reactor.ExecuteAsync(message);
        }
    }
}