using System.Diagnostics.CodeAnalysis;

namespace EzDdd.Entity;

/// <summary>
///     Interface representing the capability of applying and storing domain events.
/// </summary>
/// <typeparam name="TEvent">The type of internal domain events</typeparam>
/// <remarks>
///     <para>
///         This interface abstracts the event sourcing capability, separating it from
///         the identity capability (<see cref="IEntity{TId}" />). Classes implementing
///         this interface can collect, apply, and manage domain events.
///     </para>
///     <para>
///         <strong>Primary Implementation:</strong>
///         <see cref="AggregateRoot{TId, TEvent}" /> implements this interface to provide
///         event collection and management for aggregate roots.
///     </para>
///     <para>
///         <strong>Design Rationale:</strong>
///         This interface follows the Interface Segregation Principle (ISP), allowing
///         other classes (e.g., Saga, ProcessManager) to implement event sourcing capability
///         independently of aggregate root semantics.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // AggregateRoot implements both IEntity and IDomainEventSource
/// public abstract class AggregateRoot&lt;TId, TEvent&gt;
///     : IEntity&lt;TId&gt;, IDomainEventSource&lt;TEvent&gt;
///     where TEvent : class, IInternalDomainEvent
/// {
///     // Event sourcing implementation...
/// }
///
/// // Usage: polymorphic event source
/// IDomainEventSource&lt;IInternalDomainEvent&gt; eventSource = aggregateRoot;
/// IReadOnlyList&lt;IInternalDomainEvent&gt; events = eventSource.GetDomainEvents();
/// </code>
/// </example>
public interface IDomainEventSource<TEvent>
    where TEvent : class, IInternalDomainEvent
{
    /// <summary>
    ///     Applies a domain event to the aggregate, adding it to the pending events collection.
    /// </summary>
    /// <param name="event">The domain event to apply</param>
    /// <remarks>
    ///     This method adds the event to the internal collection and may trigger state changes
    ///     in event-sourced aggregates (via <see cref="EsAggregateRoot{TId, TEvent}" />).
    ///     For state-sourced aggregates, this method only collects the event without
    ///     automatically mutating state.
    /// </remarks>
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "The parameter name 'event' is the established domain-event vocabulary inherited from Java ezddd's public API; C# implementers use the escaped identifier @event. Renaming would break named-argument compatibility and cross-language parity."
    )]
    void Apply(TEvent @event);

    /// <summary>
    ///     Clears all pending domain events from the collection.
    /// </summary>
    /// <remarks>
    ///     Called by repositories after successfully persisting events to prevent
    ///     duplicate event publication. Should NOT be called if persistence fails.
    /// </remarks>
    void ClearDomainEvents();

    /// <summary>
    ///     Gets all pending domain events that have been applied but not yet persisted.
    /// </summary>
    /// <returns>
    ///     Read-only list of domain events in the order they were applied.
    /// </returns>
    IReadOnlyList<TEvent> GetDomainEvents();

    /// <summary>
    ///     Gets the most recently applied domain event.
    /// </summary>
    /// <returns>
    ///     The last domain event in the collection, or null if no events have been applied.
    /// </returns>
    TEvent? GetLastDomainEvent();

    /// <summary>
    ///     Gets the number of pending domain events.
    /// </summary>
    /// <returns>The count of events in the pending events collection.</returns>
    int GetDomainEventSize();
}
