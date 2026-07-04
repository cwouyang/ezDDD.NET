namespace EzDdd.Entity;

/// <summary>
///     Base interface for all domain events.
/// </summary>
/// <remarks>
///     <para>
///         A domain event represents something that happened in the domain
///         that domain experts care about. Events are facts that cannot be changed
///         (the past cannot be altered), so all domain events should be immutable.
///     </para>
///     <para>
///         In Domain-Driven Design (DDD) and Event Sourcing, domain events are used to:
///         <list type="bullet">
///             <item>
///                 <description>Reconstruct aggregate state from event history</description>
///             </item>
///             <item>
///                 <description>Communicate state changes to other bounded contexts</description>
///             </item>
///             <item>
///                 <description>Trigger side effects and projections</description>
///             </item>
///             <item>
///                 <description>Provide audit trail and time travel debugging</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <strong>Recommendation:</strong> Implement domain events as C# <see langword="record" /> types
///         to ensure immutability and structural equality by default.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// public record OrderCreated(
///     Guid Id,
///     DateTimeOffset OccurredOn,
///     string Source,  // OrderId
///     Guid CustomerId,
///     decimal TotalAmount,
///     IReadOnlyDictionary&lt;string, string&gt; Metadata
/// ) : IInternalDomainEvent;
/// </code>
/// </example>
public interface IDomainEvent
{
    /// <summary>
    ///     Gets the unique identifier of this event.
    /// </summary>
    /// <remarks>
    ///     Each event instance has a unique ID, even if multiple events
    ///     represent the same domain occurrence. This is the event's identity,
    ///     not the aggregate's identity (see <see cref="Source" />).
    /// </remarks>
    /// <value>
    ///     A globally unique identifier for this specific event instance.
    /// </value>
    Guid Id { get; }

    /// <summary>
    ///     Gets the timestamp when this event occurred.
    /// </summary>
    /// <remarks>
    ///     Uses <see cref="DateTimeOffset" /> instead of <see cref="DateTime" />
    ///     to preserve timezone information and avoid timezone-related bugs.
    ///     This follows .NET best practices for timestamp representation.
    /// </remarks>
    /// <value>
    ///     The date and time when this event occurred, with timezone offset.
    /// </value>
    DateTimeOffset OccurredOn { get; }

    /// <summary>
    ///     Gets the identifier of the aggregate that produced this event.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This identifies which aggregate instance raised this event.
    ///         For construction events, this is the ID of the newly created aggregate.
    ///     </para>
    ///     <para>
    ///         The source is represented as a string to allow flexible identity formats
    ///         (GUID, composite keys, natural keys, etc.).
    ///     </para>
    /// </remarks>
    /// <value>
    ///     The string representation of the aggregate's unique identifier.
    /// </value>
    string Source { get; }

    /// <summary>
    ///     Gets the metadata associated with this event.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Metadata provides contextual information about the event that is not
    ///         part of the core domain data. Common metadata includes:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description><strong>CorrelationId</strong>: Links related events across aggregates</description>
    ///             </item>
    ///             <item>
    ///                 <description><strong>CausationId</strong>: Links cause-and-effect event chains</description>
    ///             </item>
    ///             <item>
    ///                 <description><strong>UserId</strong>: Identifies who triggered the event</description>
    ///             </item>
    ///             <item>
    ///                 <description><strong>TraceContext</strong>: Distributed tracing information</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Uses <see cref="IReadOnlyDictionary{TKey, TValue}" /> to enforce immutability
    ///         (intentional improvement over Java ezddd's mutable Map).
    ///     </para>
    /// </remarks>
    /// <value>
    ///     A read-only dictionary of metadata key-value pairs (both keys and values are strings).
    /// </value>
    IReadOnlyDictionary<string, string> Metadata { get; }
}
