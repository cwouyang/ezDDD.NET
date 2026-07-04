namespace EzDdd.Entity;

/// <summary>
///     Marker interface for domain events that occur within a single bounded context.
/// </summary>
/// <remarks>
///     <para>
///         Internal domain events are used for event sourcing and maintaining aggregate state
///         within a bounded context. They are not intended for cross-context integration
///         (use <c>ExternalDomainEvent</c> for that purpose).
///     </para>
///     <para>
///         Internal events form the event stream for event-sourced aggregates and are used
///         to reconstruct aggregate state through event replay. They are typically stored
///         in an event store and never modified once persisted.
///     </para>
///     <para>
///         This interface includes two nested marker interfaces for event lifecycle management:
///         <list type="bullet">
///             <item>
///                 <description><see cref="IConstructionEvent" />: Marks the first event (aggregate creation)</description>
///             </item>
///             <item>
///                 <description><see cref="IDestructionEvent" />: Marks the last event (aggregate deletion)</description>
///             </item>
///         </list>
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Construction event (R1 rule - first event)
/// public record AccountCreated(
///     Guid Id,
///     DateTimeOffset OccurredOn,
///     string Source,  // AccountId
///     string Owner,
///     decimal InitialBalance,
///     IReadOnlyDictionary&lt;string, string&gt; Metadata
/// ) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent;
///
/// // Command event (R2 rule - middle events)
/// public record MoneyDeposited(
///     Guid Id,
///     DateTimeOffset OccurredOn,
///     string Source,  // AccountId
///     decimal Amount,
///     IReadOnlyDictionary&lt;string, string&gt; Metadata
/// ) : IInternalDomainEvent;
///
/// // Destruction event (R3 rule - last event)
/// public record AccountClosed(
///     Guid Id,
///     DateTimeOffset OccurredOn,
///     string Source,  // AccountId
///     string Reason,
///     IReadOnlyDictionary&lt;string, string&gt; Metadata
/// ) : IInternalDomainEvent, IInternalDomainEvent.IDestructionEvent;
/// </code>
/// </example>
public interface IInternalDomainEvent : IDomainEvent
{
    /// <summary>
    ///     Marker interface for construction events.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A construction event <strong>MUST</strong> be the first event in an event-sourced
    ///         aggregate's lifecycle. It establishes the aggregate's initial state and identity.
    ///     </para>
    ///     <para>
    ///         <strong>Event Sourcing Rule R1 (Construction):</strong>
    ///         <code>
    /// {pre₀} fun₀ {post₀ &amp; INV}
    /// </code>
    ///         Construction events do not have precondition invariant checks (no pre-existing state),
    ///         but must satisfy postcondition invariants (establish valid initial state).
    ///     </para>
    ///     <para>
    ///         Only one event in an aggregate's lifetime should implement this interface.
    ///         Multiple construction events for the same aggregate indicate a design error.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// public record UserRegistered(
    ///     Guid Id,
    ///     DateTimeOffset OccurredOn,
    ///     string Source,  // UserId
    ///     string Email,
    ///     string Name,
    ///     IReadOnlyDictionary&lt;string, string&gt; Metadata
    /// ) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent;
    /// </code>
    /// </example>
    interface IConstructionEvent
    {
        // Pure marker interface - no additional members
        // Semantic meaning: This event is the first in aggregate lifecycle
    }

    /// <summary>
    ///     Marker interface for destruction events.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A destruction event <strong>MUST</strong> be the last event in an event-sourced
    ///         aggregate's lifecycle. It represents the aggregate's deletion or termination.
    ///     </para>
    ///     <para>
    ///         <strong>Event Sourcing Rule R3 (Destruction):</strong>
    ///         <code>
    /// {preᵤ &amp; INV} funᵤ {postᵤ}
    /// </code>
    ///         Destruction events must satisfy precondition invariants (aggregate in valid state
    ///         before deletion), but do not have postcondition invariant checks (no state after deletion).
    ///     </para>
    ///     <para>
    ///         After a destruction event, the aggregate is considered deleted and no further
    ///         events should be applied. Commands on deleted aggregates should be rejected.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// public record UserDeactivated(
    ///     Guid Id,
    ///     DateTimeOffset OccurredOn,
    ///     string Source,  // UserId
    ///     string Reason,
    ///     IReadOnlyDictionary&lt;string, string&gt; Metadata
    /// ) : IInternalDomainEvent, IInternalDomainEvent.IDestructionEvent;
    /// </code>
    /// </example>
    interface IDestructionEvent
    {
        // Pure marker interface - no additional members
        // Semantic meaning: This event is the last in aggregate lifecycle
    }
}
