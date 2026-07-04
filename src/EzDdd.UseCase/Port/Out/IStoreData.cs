using EzDdd.Entity;

namespace EzDdd.UseCase.Port.Out;

/// <summary>
///     Marker interface for persistence data structures.
///     Provides common properties for all persistence formats (event sourcing and state sourcing).
/// </summary>
/// <typeparam name="TId">The type of the aggregate identifier</typeparam>
/// <remarks>
///     This interface supports both event sourcing and state sourcing patterns:
///     <list type="bullet">
///         <item>
///             <term>Version</term>
///             <description>Used for optimistic locking (-1 for new aggregates, 0+ for existing)</description>
///         </item>
///         <item>
///             <term>Events</term>
///             <description>Supports Transactional Outbox pattern for domain event publishing</description>
///         </item>
///         <item>
///             <term>StreamName</term>
///             <description>Event stream naming convention: "{category}-{id}"</description>
///         </item>
///     </list>
/// </remarks>
public interface IStoreData<TId>
{
    /// <summary>
    ///     Gets or sets the aggregate version for optimistic locking.
    /// </summary>
    /// <value>
    ///     Version number:
    ///     <list type="bullet">
    ///         <item><c>-1</c> for new aggregates (not yet persisted)</item>
    ///         <item><c>0+</c> for existing aggregates</item>
    ///     </list>
    /// </value>
    long Version { get; set; }

    /// <summary>
    ///     Gets or sets the aggregate identifier.
    /// </summary>
    TId Id { get; set; }

    /// <summary>
    ///     Gets or sets the domain events to be published.
    /// </summary>
    /// <value>
    ///     List of pending domain events (Transactional Outbox pattern).
    ///     Should be cleared after successful persistence.
    /// </value>
    IReadOnlyList<IDomainEvent> Events { get; set; }

    /// <summary>
    ///     Gets or sets the event stream name for event sourcing.
    /// </summary>
    /// <value>
    ///     Stream name following the convention: "{category}-{id}"
    ///     (e.g., "account-123", "order-456")
    /// </value>
    string StreamName { get; set; }

    /// <summary>
    ///     Gets the optimistic lock version for database operations.
    /// </summary>
    /// <returns>
    ///     Expected version after save, calculated as: <c>Version + Events.Count</c>
    /// </returns>
    /// <remarks>
    ///     <para>This default implementation calculates the expected version after persistence:</para>
    ///     <code>
    /// // New aggregate
    /// Version = -1, Events = [CreatedEvent]
    /// OptimisticLockVersion = -1 + 1 = 0  (expected version after save)
    ///
    /// // After save
    /// Version = 0, Events = [] (cleared)
    ///
    /// // After command
    /// Version = 0, Events = [CommandEvent]
    /// OptimisticLockVersion = 0 + 1 = 1  (expected version after save)
    /// </code>
    ///     <para>This version is used for optimistic locking in database UPDATE operations:</para>
    ///     <code>
    /// UPDATE table SET version = @expectedVersion WHERE id = @id AND version = @currentVersion
    /// </code>
    /// </remarks>
    long GetOptimisticLockVersion()
    {
        return Version + Events.Count;
    }
}
