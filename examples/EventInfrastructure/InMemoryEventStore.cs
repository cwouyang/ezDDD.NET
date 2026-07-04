using System.Collections.Concurrent;
using EzDdd.Entity;

namespace EzDdd.Examples.EventInfrastructure;

/// <summary>
///     In-memory implementation of <see cref="IEventStore" /> for testing and demonstration purposes.
/// </summary>
/// <remarks>
///     <para>
///         This implementation stores events in a thread-safe <see cref="ConcurrentQueue{T}" />
///         and supports polling via <see cref="GetEventsAfterAsync" />. It is suitable for:
///         <list type="bullet">
///             <item>
///                 <description>Unit and integration testing</description>
///             </item>
///             <item>
///                 <description>Demonstration and examples</description>
///             </item>
///             <item>
///                 <description>Prototyping and proof-of-concept applications</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <strong>Production Use:</strong>
///         This implementation is NOT suitable for production use because:
///         <list type="bullet">
///             <item>
///                 <description>Events are stored in memory and lost on restart</description>
///             </item>
///             <item>
///                 <description>No persistence or durability guarantees</description>
///             </item>
///             <item>
///                 <description>Limited scalability (single process only)</description>
///             </item>
///         </list>
///         For production, use a database-backed event store (SQL Server, PostgreSQL, EventStore, etc.).
///     </para>
/// </remarks>
public class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentQueue<IInternalDomainEvent> _events = new();

    /// <summary>
    ///     Gets the total number of events in the store.
    /// </summary>
    /// <remarks>
    ///     This property is useful for testing and debugging. Note that due to concurrent access,
    ///     the count may change between reading this property and performing other operations.
    /// </remarks>
    public int Count => _events.Count;

    /// <summary>
    ///     Gets all events after the specified index.
    /// </summary>
    /// <param name="afterIndex">
    ///     The index to start from. Pass -1 to get all events from the beginning.
    /// </param>
    /// <param name="cancellationToken">Cancellation token (not used in this implementation)</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     all events after the specified index.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This implementation converts the queue to a list and returns events starting from
    ///         <paramref name="afterIndex" /> + 1. The index is zero-based.
    ///     </para>
    ///     <para>
    ///         <strong>Thread Safety:</strong>
    ///         This method is thread-safe. However, due to the nature of <see cref="ConcurrentQueue{T}" />,
    ///         the snapshot may not include events added during the ToList() operation.
    ///     </para>
    /// </remarks>
    public Task<IReadOnlyList<IInternalDomainEvent>> GetEventsAfterAsync(
        int afterIndex,
        CancellationToken cancellationToken = default
    )
    {
        // Convert queue to list (thread-safe snapshot)
        List<IInternalDomainEvent> allEvents = _events.ToList();

        // Return events after the specified index
        IReadOnlyList<IInternalDomainEvent> result =
            afterIndex < 0 ? allEvents.AsReadOnly() : allEvents.Skip(afterIndex + 1).ToList().AsReadOnly();

        return Task.FromResult(result);
    }

    /// <summary>
    ///     Appends an event to the event store.
    /// </summary>
    /// <param name="event">The event to append</param>
    /// <exception cref="ArgumentNullException">Thrown when event is null</exception>
    /// <remarks>
    ///     <para>
    ///         This method adds the event to the end of the queue. Events are stored in
    ///         insertion order, which is used by <see cref="GetEventsAfterAsync" /> to
    ///         return events chronologically.
    ///     </para>
    ///     <para>
    ///         <strong>Thread Safety:</strong>
    ///         This method is thread-safe and can be called concurrently from multiple threads.
    ///     </para>
    /// </remarks>
    public void Append(IInternalDomainEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        _events.Enqueue(@event);
    }

    /// <summary>
    ///     Clears all events from the store.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is useful for cleaning up between tests. Note that clearing
    ///         while <see cref="EventStoreRelay" /> is running may cause events to be lost.
    ///     </para>
    ///     <para>
    ///         <strong>Thread Safety:</strong>
    ///         This method creates a new queue instance, which is thread-safe. However,
    ///         concurrent Append operations may still add events to the old queue.
    ///     </para>
    /// </remarks>
    public void Clear()
    {
        _events.Clear();
    }
}
