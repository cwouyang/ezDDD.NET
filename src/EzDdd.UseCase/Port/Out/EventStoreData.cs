using EzDdd.Entity;

namespace EzDdd.UseCase.Port.Out;

/// <summary>
///     Data structure for event sourcing persistence.
///     Stores only events (no aggregate state) for reconstructing aggregates via event replay.
/// </summary>
/// <typeparam name="TId">The type of the aggregate identifier</typeparam>
/// <remarks>
///     <para>
///         EventStoreData is used with event sourcing repositories to persist event streams.
///         Unlike state sourcing (which stores current aggregate state), event sourcing stores
///         only the sequence of events that led to the current state.
///     </para>
///     <para>
///         The aggregate is reconstructed by replaying all events through the aggregate's
///         event handler methods (typically via a constructor that accepts an event sequence).
///     </para>
///     <para>
///         <strong>Event Sourcing Benefits:</strong>
///         <list type="bullet">
///             <item>
///                 <description>Complete audit trail (every state change recorded)</description>
///             </item>
///             <item>
///                 <description>Temporal queries (reconstruct state at any point in time)</description>
///             </item>
///             <item>
///                 <description>Event replay for debugging and testing</description>
///             </item>
///             <item>
///                 <description>Natural fit for event-driven architectures</description>
///             </item>
///         </list>
///     </para>
/// </remarks>
/// <example>
///     <code>
/// var eventStoreData = new EventStoreData&lt;AccountId&gt;
/// {
///     Id = accountId,
///     Version = 0,
///     Events = new List&lt;IDomainEvent&gt;
///     {
///         new AccountCreated(id, occurredOn, source, owner, balance, metadata),
///         new MoneyDeposited(id, occurredOn, source, 100m, metadata),
///         new MoneyWithdrawn(id, occurredOn, source, 50m, metadata)
///     },
///     StreamName = "account-123"
/// };
/// 
/// // Reconstruct aggregate from events
/// var account = new BankAccount(eventStoreData.Events);
/// </code>
/// </example>
public class EventStoreData<TId> : IStoreData<TId>
{
    /// <summary>
    ///     Gets or sets the aggregate version (number of events persisted before current batch).
    /// </summary>
    /// <remarks>
    ///     The version represents the number of events that have been successfully persisted
    ///     to the event store before the current batch of events. It is used for optimistic
    ///     locking to detect concurrent modifications.
    /// </remarks>
    public long Version { get; set; }

    /// <summary>
    ///     Gets or sets the aggregate identifier.
    /// </summary>
    public TId Id { get; set; } = default!;

    /// <summary>
    ///     Gets or sets the collection of domain events for this aggregate.
    /// </summary>
    /// <remarks>
    ///     Events are stored in chronological order. The first event should implement
    ///     <see cref="IInternalDomainEvent.IConstructionEvent" /> and the last event
    ///     (if aggregate is deleted) should implement <see cref="IInternalDomainEvent.IDestructionEvent" />.
    /// </remarks>
    public IReadOnlyList<IDomainEvent> Events { get; set; } = [];

    /// <summary>
    ///     Gets or sets the event stream name (format: "{category}-{id}").
    /// </summary>
    /// <remarks>
    ///     Stream naming convention follows the pattern: lowercase category name + hyphen + aggregate ID.
    ///     For example: "account-123", "order-456", "user-789".
    /// </remarks>
    public string StreamName { get; set; } = string.Empty;

    /// <summary>
    ///     Calculates the optimistic lock version as Version + Events.Count.
    /// </summary>
    /// <returns>The sum of the version and the number of events</returns>
    /// <remarks>
    ///     <para>
    ///         The optimistic lock version represents the total number of events that will
    ///         exist after the current batch is persisted. It is calculated as:
    ///     </para>
    ///     <code>
    /// OptimisticLockVersion = Version + Events.Count
    /// </code>
    ///     <para>
    ///         This value is used by the event store to detect concurrent modifications.
    ///         If another process has modified the aggregate (added events) since this
    ///         data was loaded, the version check will fail and the save operation will
    ///         be rejected, preventing lost updates.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Aggregate loaded with 5 persisted events
    /// var data = new EventStoreData&lt;AccountId&gt;
    /// {
    ///     Version = 5,  // 5 events already persisted
    ///     Events = new List&lt;IDomainEvent&gt;
    ///     {
    ///         new MoneyDeposited(...),  // New event 1
    ///         new MoneyWithdrawn(...)   // New event 2
    ///     }
    /// };
    /// 
    /// // Expected version after save: 5 + 2 = 7
    /// var expectedVersion = data.GetOptimisticLockVersion(); // Returns 7
    /// </code>
    /// </example>
    public long GetOptimisticLockVersion()
    {
        return Version + Events.Count;
    }
}