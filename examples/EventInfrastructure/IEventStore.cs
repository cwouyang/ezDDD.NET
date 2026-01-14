using EzDdd.Entity;

namespace EzDdd.Examples.EventInfrastructure;

/// <summary>
///     Interface for event stores that support relay polling.
/// </summary>
/// <remarks>
///     <para>
///         This interface is used by <see cref="EventStoreRelay" /> to poll for unpublished events
///         and publish them to a message broker. It provides a simple abstraction over event stores
///         that enables the Relay pattern for Transactional Outbox.
///     </para>
///     <para>
///         <strong>Implementation Notes:</strong>
///         Production implementations should:
///         <list type="bullet">
///             <item>
///                 <description>Return events in chronological order (by insertion order or sequence number)</description>
///             </item>
///             <item>
///                 <description>Support efficient range queries (avoid full table scans)</description>
///             </item>
///             <item>
///                 <description>Handle concurrent access safely (multiple relay instances)</description>
///             </item>
///             <item>
///                 <description>Consider using database indexes on sequence/timestamp columns</description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public interface IEventStore
{
    /// <summary>
    ///     Gets all events after the specified index.
    /// </summary>
    /// <param name="afterIndex">
    ///     The index to start from. Pass -1 to get all events from the beginning.
    ///     Events with index greater than this value will be returned.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     a read-only list of events after the specified index, in chronological order.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is called repeatedly by <see cref="EventStoreRelay" /> to poll for new events.
    ///         It should return events that have been persisted but not yet published.
    ///     </para>
    ///     <para>
    ///         <strong>Performance Consideration:</strong>
    ///         This method may be called frequently (e.g., every 100ms). Implementations should
    ///         use efficient queries (indexed columns) to avoid performance issues.
    ///     </para>
    /// </remarks>
    Task<IReadOnlyList<IInternalDomainEvent>> GetEventsAfterAsync(
        int afterIndex,
        CancellationToken cancellationToken = default);
}
