using System.Diagnostics.CodeAnalysis;

namespace EzDdd.Entity;

/// <summary>
///     Abstract base class for aggregate roots.
/// </summary>
/// <remarks>
///     <para>
///         An aggregate root is the entry point to an aggregate and maintains a collection
///         of domain events representing state changes. In Domain-Driven Design (DDD), an
///         aggregate is a cluster of domain objects that can be treated as a single unit
///         for data changes.
///     </para>
///     <para>
///         This base class provides:
///         <list type="bullet">
///             <item>
///                 <description>
///                     <strong>Event Collection</strong>: Collects domain events raised during aggregate
///                     operations
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <strong>Version Control</strong>: Supports optimistic concurrency control via version
///                     number
///                 </description>
///             </item>
///             <item>
///                 <description><strong>Soft Delete</strong>: Tracks deletion state via <see cref="IsDeleted" /> flag</description>
///             </item>
///             <item>
///                 <description><strong>Thread Safety</strong>: Lock-based synchronization for event collection</description>
///             </item>
///             <item>
///                 <description>
///                     <strong>Template Method</strong>: <see cref="Apply" /> method can be overridden by
///                     subclasses
///                 </description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <strong>Version Semantics:</strong>
///         <list type="bullet">
///             <item>
///                 <description>Starts at <c>-1</c> (indicating "not yet persisted")</description>
///             </item>
///             <item>
///                 <description>
///                     Incremented by 1 each time a domain event is added via <see cref="_AddDomainEvent" />
///                 </description>
///             </item>
///             <item>
///                 <description>After first event: <c>Version = 0</c></description>
///             </item>
///             <item>
///                 <description>After second event: <c>Version = 1</c></description>
///             </item>
///             <item>
///                 <description>Version equals the number of events applied to the aggregate</description>
///             </item>
///         </list>
///         This matches Java ezddd's behavior where <c>version.incrementAndGet()</c> is called
///         in <c>addDomainEvent()</c>.
///     </para>
///     <para>
///         <strong>Interface Implementation:</strong>
///         This class implements two interfaces:
///         <list type="bullet">
///             <item>
///                 <description>
///                     <see cref="IEntity{TId}" />: Provides identity capability via <see cref="Id" /> property
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <see cref="IDomainEventSource{TEvent}" />: Provides event sourcing capability
///                     for collecting and managing domain events
///                 </description>
///             </item>
///         </list>
///     </para>
///     <para>
///         This class supports both state sourcing and event sourcing patterns.
///         For event sourcing with invariant checking, use <see cref="EsAggregateRoot{TId, TEvent}" />.
///     </para>
/// </remarks>
/// <typeparam name="TId">The type of the aggregate's unique identifier</typeparam>
/// <typeparam name="TEvent">The type of internal domain events this aggregate produces</typeparam>
/// <example>
///     <code>
/// // State sourcing aggregate
/// public class Order : AggregateRoot&lt;Guid, IInternalDomainEvent&gt;
/// {
///     private OrderStatus _status = OrderStatus.Draft;
///     private List&lt;OrderItem&gt; _items = new();
///
///     public Order(Guid orderId, Guid customerId)
///     {
///         Id = orderId;
///
///         var created = new OrderCreated(
///             Id: Guid.NewGuid(),
///             OccurredOn: DateTimeOffset.UtcNow,
///             Source: orderId.ToString(),
///             CustomerId: customerId,
///             Metadata: new Dictionary&lt;string, string&gt;());
///
///         Apply(created); // Adds event to collection, increments version
///         _status = OrderStatus.Created; // State mutation (state sourcing)
///     }
///
///     public void AddItem(string productId, int quantity)
///     {
///         var itemAdded = new OrderItemAdded(/* ... */);
///         Apply(itemAdded);
///         _items.Add(new OrderItem(productId, quantity));
///     }
/// }
/// </code>
/// </example>
public abstract class AggregateRoot<TId, TEvent> : IEntity<TId>, IDomainEventSource<TEvent>
    where TEvent : class, IInternalDomainEvent
{
    private readonly List<TEvent> _domainEvents = [];
    private readonly object _domainEventsLock = new();

    /// <summary>
    ///     Gets or sets the version of this aggregate for optimistic concurrency control.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Version starts at <c>-1</c> (indicating "not yet persisted") and is incremented
    ///         each time a domain event is added via <see cref="_AddDomainEvent" />.
    ///     </para>
    ///     <para>
    ///         <strong>Version Progression:</strong>
    ///         <list type="number">
    ///             <item>
    ///                 <description>Initial state: <c>Version = -1</c> (no events)</description>
    ///             </item>
    ///             <item>
    ///                 <description>After first event: <c>Version = 0</c></description>
    ///             </item>
    ///             <item>
    ///                 <description>After second event: <c>Version = 1</c></description>
    ///             </item>
    ///             <item>
    ///                 <description>After N events: <c>Version = N - 1</c></description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Repositories use this version number to detect concurrent modifications.
    ///         If the version in the database differs from the version in memory,
    ///         another process has modified the aggregate (optimistic locking violation).
    ///     </para>
    /// </remarks>
    /// <value>
    ///     The current version number. Starts at -1, increments on each event addition.
    /// </value>
    public long Version { get; protected set; } = -1;

    /// <summary>
    ///     Gets or sets whether this aggregate has been marked as deleted.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This supports soft delete scenarios where the aggregate is logically deleted
    ///         but still exists in the event stream or database. Aggregates marked as deleted
    ///         typically reject further commands except for administrative operations.
    ///     </para>
    ///     <para>
    ///         For event-sourced aggregates, the deletion is represented by a domain event
    ///         implementing <see cref="IInternalDomainEvent.IDestructionEvent" />.
    ///     </para>
    /// </remarks>
    /// <value>
    ///     <c>true</c> if the aggregate is deleted; otherwise, <c>false</c>.
    /// </value>
    public bool IsDeleted { get; protected set; }

    /// <summary>
    ///     Applies a domain event to this aggregate.
    /// </summary>
    /// <param name="event">The domain event to apply</param>
    /// <remarks>
    ///     <para>
    ///         This is a template method that can be overridden by subclasses to customize
    ///         event application behavior. The default implementation adds the event to the
    ///         event collection via <see cref="_AddDomainEvent" />.
    ///     </para>
    ///     <para>
    ///         <strong>State Sourcing:</strong> In state-sourced aggregates, <see cref="Apply" />
    ///         adds the event to the collection and subclasses mutate state directly in command methods.
    ///     </para>
    ///     <para>
    ///         <strong>Event Sourcing:</strong> Event-sourced aggregates (see <see cref="EsAggregateRoot{TId, TEvent}" />)
    ///         seal this method and add invariant checking and state mutation via <c>When()</c> method.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // State sourcing - Apply only adds event, state mutation is manual
    /// public void PlaceOrder()
    /// {
    ///     var orderPlaced = new OrderPlaced(/* ... */);
    ///     Apply(orderPlaced);  // Adds event to collection
    ///     _status = OrderStatus.Placed;  // Manual state mutation
    /// }
    /// </code>
    /// </example>
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "The parameter name 'event' is the established domain-event vocabulary inherited from Java ezddd's public API; C# callers use the escaped identifier @event. Renaming would break named-argument compatibility and cross-language parity."
    )]
    public virtual void Apply(TEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _AddDomainEvent(@event);
    }

    /// <summary>
    ///     Gets a read-only view of all domain events raised by this aggregate
    ///     since the last call to <see cref="ClearDomainEvents" />.
    /// </summary>
    /// <returns>A read-only list of domain events</returns>
    /// <remarks>
    ///     <para>
    ///         Returns a snapshot of the event collection at the time of the call.
    ///         Events added after this call will not appear in the returned list
    ///         (defensive copy pattern).
    ///     </para>
    ///     <para>
    ///         <strong>Thread Safety:</strong> Creates a defensive copy under lock to ensure
    ///         the returned snapshot is consistent even if events are added concurrently.
    ///     </para>
    ///     <para>
    ///         Repositories call this method to retrieve events for persistence or publication.
    ///     </para>
    /// </remarks>
    public IReadOnlyList<TEvent> GetDomainEvents()
    {
        lock (_domainEventsLock)
        {
            return _domainEvents.ToList().AsReadOnly();
        }
    }

    /// <summary>
    ///     Gets the last domain event raised by this aggregate, or <c>null</c> if no events.
    /// </summary>
    /// <returns>The most recent domain event, or <c>null</c> if the event collection is empty</returns>
    /// <remarks>
    ///     <para>
    ///         This is useful for:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>Checking if the last event is a destruction event</description>
    ///             </item>
    ///             <item>
    ///                 <description>Retrieving metadata from the most recent event</description>
    ///             </item>
    ///             <item>
    ///                 <description>Testing event application</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <strong>Thread Safety:</strong> Uses lock to ensure consistent read.
    ///     </para>
    /// </remarks>
    public TEvent? GetLastDomainEvent()
    {
        lock (_domainEventsLock)
        {
            return _domainEvents.Count > 0 ? _domainEvents[^1] : null;
        }
    }

    /// <summary>
    ///     Gets the number of domain events currently in the collection.
    /// </summary>
    /// <returns>The count of domain events</returns>
    /// <remarks>
    ///     This is useful for:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Testing that events are being collected</description>
    ///         </item>
    ///         <item>
    ///             <description>Checking if any events exist before clearing</description>
    ///         </item>
    ///         <item>
    ///             <description>Logging event collection size</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public int GetDomainEventSize()
    {
        lock (_domainEventsLock)
        {
            return _domainEvents.Count;
        }
    }

    /// <summary>
    ///     Clears all domain events from the collection.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Repositories call this method after successfully persisting events to prevent
    ///         duplicate event publication. The version number is NOT reset, as it represents
    ///         the aggregate's lifetime event count and is used for optimistic locking.
    ///     </para>
    ///     <para>
    ///         <strong>Typical Usage Pattern:</strong>
    ///         <code>
    /// // In repository:
    /// var events = aggregate.GetDomainEvents();
    /// await SaveEventsAsync(events);  // Persist to database
    /// aggregate.ClearDomainEvents();  // Clear after successful save
    /// </code>
    ///     </para>
    /// </remarks>
    public void ClearDomainEvents()
    {
        lock (_domainEventsLock)
        {
            _domainEvents.Clear();
        }
    }

    /// <summary>
    ///     Gets or sets the unique identifier of this aggregate.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Subclass constructors must set <see cref="Id" /> before returning.
    ///         The <c>default!</c> pattern is used to satisfy nullable reference types
    ///         while allowing deferred initialization in subclass constructors.
    ///     </para>
    ///     <para>
    ///         The <c>protected set</c> accessor allows subclasses to initialize the ID,
    ///         but prevents external modification (encapsulation).
    ///     </para>
    /// </remarks>
    /// <value>
    ///     The unique identifier of this aggregate.
    /// </value>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    ///     Adds a domain event to the event collection and increments version.
    /// </summary>
    /// <param name="event">The domain event to add</param>
    /// <remarks>
    ///     <para>
    ///         This method is <c>protected</c> to allow subclass access while preventing
    ///         external manipulation. It is non-virtual to ensure subclasses cannot bypass
    ///         event collection or version management.
    ///     </para>
    ///     <para>
    ///         <strong>Thread Safety:</strong> Uses lock-based synchronization to ensure
    ///         thread-safe event addition and version increment.
    ///     </para>
    ///     <para>
    ///         <strong>Version Semantics:</strong> Version is incremented on each event addition,
    ///         making <c>Version</c> equal to the number of events applied to the aggregate
    ///         (matching Java ezddd's behavior).
    ///     </para>
    /// </remarks>
    [SuppressMessage(
        "Naming",
        "CA1707:Identifiers should not contain underscores",
        Justification = "The leading underscore marks framework-internal template methods that only subclasses may call, mirroring Java ezddd's protected API surface; renaming would break semantic parity and the published protected contract."
    )]
    protected void _AddDomainEvent(TEvent @event)
    {
        lock (_domainEventsLock)
        {
            _domainEvents.Add(@event);
            Version++; // Increment version per event (matches Java ezddd)
        }
    }
}
