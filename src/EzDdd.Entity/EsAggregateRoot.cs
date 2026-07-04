using System.Diagnostics.CodeAnalysis;
using uContract;

namespace EzDdd.Entity;

/// <summary>
///     Abstract base class for event-sourced aggregate roots.
/// </summary>
/// <remarks>
///     <para>
///         Enforces event sourcing correctness rules (R1, R2, R3) through template method pattern
///         with strategic invariant checking. Event-sourced aggregates reconstruct their state
///         by replaying a sequence of domain events from an event store.
///     </para>
///     <para>
///         <strong>Event Sourcing Correctness Rules:</strong>
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <strong>R1 (Construction):</strong> <c>{pre₀} fun₀ {post₀ &amp; INV}</c>
///                 <para>
///                     The first event (implementing <see cref="IInternalDomainEvent.IConstructionEvent" />)
///                     establishes the aggregate's initial state. No precondition invariant check is performed
///                     (aggregate doesn't exist yet), but postcondition invariants must be satisfied.
///                 </para>
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>R2 (Command):</strong> <c>{preₜ &amp; INV} funₜ {postₜ &amp; INV}</c>
///                 <para>
///                     Middle events (most events) maintain invariants before and after application.
///                     Both precondition and postcondition invariant checks are performed.
///                 </para>
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>R3 (Destruction):</strong> <c>{preᵤ &amp; INV} funᵤ {postᵤ}</c>
///                 <para>
///                     The last event (implementing <see cref="IInternalDomainEvent.IDestructionEvent" />)
///                     finalizes the aggregate's deletion. Precondition invariants are checked, but
///                     postcondition checks are skipped (aggregate is being deleted).
///                 </para>
///             </description>
///         </item>
///     </list>
///     <para>
///         <strong>Template Method Pattern:</strong>
///         <list type="bullet">
///             <item>
///                 <description><see cref="Apply" /> is <c>sealed</c> - enforces framework rules, cannot be overridden</description>
///             </item>
///             <item>
///                 <description><see cref="_When" /> is <c>abstract</c> - subclasses implement state mutation logic</description>
///             </item>
///             <item>
///                 <description>
///                     <see cref="_EnsureInvariant" /> is <c>virtual</c> - subclasses override to add business
///                     rule checks
///                 </description>
///             </item>
///         </list>
///     </para>
/// </remarks>
/// <typeparam name="TId">The type of the aggregate's unique identifier</typeparam>
/// <typeparam name="TEvent">The type of internal domain events this aggregate produces</typeparam>
/// <example>
///     <code>
/// public class Account : EsAggregateRoot&lt;Guid, IInternalDomainEvent&gt;
/// {
///     private string _owner = string.Empty;
///     private decimal _balance;
///
///     // Constructor for new aggregate
///     public Account(Guid id, string owner, decimal initialBalance)
///     {
///         var created = new AccountCreated(/* ... */);
///         Apply(created); // Enforces R1
///     }
///
///     // Constructor for event replay (REQUIRED)
///     public Account(IEnumerable&lt;IInternalDomainEvent&gt; events) : base(events) { }
///
///     public void Deposit(decimal amount)
///     {
///         var deposited = new MoneyDeposited(/* ... */);
///         Apply(deposited); // Enforces R2
///     }
///
///     protected override void When(IInternalDomainEvent @event)
///     {
///         switch (@event)
///         {
///             case AccountCreated e:
///                 Id = Guid.Parse(e.Source);
///                 _owner = e.Owner;
///                 _balance = e.InitialBalance;
///                 break;
///             case MoneyDeposited e:
///                 _balance += e.Amount;
///                 break;
///         }
///     }
///
///     protected override void EnsureInvariant()
///     {
///         if (IsDeleted) return;
///         if (_balance &lt; 0)
///             throw new InvalidOperationException("Balance cannot be negative");
///     }
///
///     public override string GetCategory() =&gt; "account";
/// }
/// </code>
/// </example>
public abstract class EsAggregateRoot<TId, TEvent> : AggregateRoot<TId, TEvent>
    where TEvent : class, IInternalDomainEvent
{
    /// <summary>
    ///     Initializes a new instance for new aggregate creation.
    /// </summary>
    /// <remarks>
    ///     Subclasses should call this default constructor, then apply a construction event
    ///     implementing <see cref="IInternalDomainEvent.IConstructionEvent" />.
    /// </remarks>
    protected EsAggregateRoot() { }

    /// <summary>
    ///     Initializes an aggregate by replaying events from history.
    /// </summary>
    /// <param name="events">The event history to replay</param>
    /// <remarks>
    ///     <para>
    ///         This is the primary constructor for loading persisted aggregates from an event store.
    ///         Events are applied in order via <see cref="Apply" />, which enforces invariant checking
    ///         during replay to ensure reconstructed state is valid.
    ///     </para>
    ///     <para>
    ///         <strong>Why public?</strong> This constructor is <c>public</c> to match Java ezddd's design
    ///         and allow repositories to use reflection for aggregate reconstruction.
    ///     </para>
    ///     <para>
    ///         After replay, <see cref="AggregateRoot{TId, TEvent}.ClearDomainEvents" /> is called
    ///         to prevent re-publication of historical events.
    ///     </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="events" /> is null</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when event replay violates invariants or encounters unknown event types
    /// </exception>
    [SuppressMessage(
        "Design",
        "MA0017:Abstract types should not have public or internal constructors",
        Justification = "The replay constructor is deliberately public so repositories (EsRepository) can reconstruct aggregates via reflection, matching Java ezddd's design (documented in the XML remarks above)."
    )]
    [SuppressMessage(
        "Design",
        "MA0056:Do not call overridable members in constructor",
        Justification = "Event-sourcing replay is the designed exception: the constructor rebuilds state by replaying history through the _ReplayEvents/_When template methods under the R1-R3 invariant rules. Derived classes start from default field values, which is exactly what replay expects; consequently derived _When() implementations must not depend on fields initialized in their own constructor."
    )]
    public EsAggregateRoot(IEnumerable<TEvent> events)
        : this()
    {
        Contract.Require("Events cannot be null", () => events != null);

        // ReSharper disable once VirtualMemberCallInConstructor
        // Rationale: Virtual member call (_ReplayEvents → Apply → _When/_EnsureInvariant) in constructor
        // is safe and intentional in event sourcing. Derived classes are expected to start from default
        // field values, then rebuild state through event replay. This is the correct event sourcing pattern.
        _ReplayEvents(events);
        ClearDomainEvents(); // Replayed events should not be re-published
    }

    /// <summary>
    ///     Applies a domain event to this aggregate with invariant checking.
    /// </summary>
    /// <param name="event">The domain event to apply</param>
    /// <remarks>
    ///     <para>
    ///         This method is <c>sealed</c> to enforce R1/R2/R3 correctness rules at the framework level.
    ///         Subclasses cannot override this method - they must implement <see cref="_When" /> instead
    ///         for state mutation logic.
    ///     </para>
    ///     <para>
    ///         <strong>Invariant Checking Logic:</strong>
    ///     </para>
    ///     <list type="number">
    ///         <item>
    ///             <description>
    ///                 <strong>Precondition Check:</strong> If NOT <see cref="IInternalDomainEvent.IConstructionEvent" />,
    ///                 call <see cref="_EnsureInvariant" /> (R2, R3 rules).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <strong>State Mutation:</strong> Call <see cref="_When" /> to mutate aggregate state.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <strong>Postcondition Check:</strong> If NOT <see cref="IInternalDomainEvent.IDestructionEvent" />,
    ///                 call <see cref="_EnsureInvariant" /> (R1, R2 rules).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <strong>Event Collection:</strong> Add event to domain events collection via
    ///                 <see cref="AggregateRoot{TId,TEvent}._AddDomainEvent" />.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when invariants are violated or <see cref="_When" /> throws an exception
    /// </exception>
    public sealed override void Apply(TEvent @event)
    {
        // R1 (Construction): Skip precondition check for first event
        // R2 (Command): Check precondition for normal events
        // R3 (Destruction): Check precondition for last event
        if (@event is not IInternalDomainEvent.IConstructionEvent)
        {
            _EnsureInvariant();
        }

        // Apply state changes (abstract method - subclass implements)
        _When(@event);

        // R1 (Construction): Check postcondition + invariant for first event
        // R2 (Command): Check postcondition + invariant for normal events
        // R3 (Destruction): Skip postcondition check for last event
        if (@event is not IInternalDomainEvent.IDestructionEvent)
        {
            _EnsureInvariant();
        }

        // Add event to collection and increment version
        _AddDomainEvent(@event);
    }

    /// <summary>
    ///     Abstract method that subclasses implement to mutate aggregate state in response to events.
    /// </summary>
    /// <param name="event">The domain event to handle</param>
    /// <remarks>
    ///     <para>
    ///         <strong>This method should ONLY mutate state</strong> - no business logic, no raising new events,
    ///         no invariant checking. Keep it simple and deterministic.
    ///     </para>
    ///     <para>
    ///         <strong>Recommended Pattern:</strong> Use pattern matching (switch expression) to handle different event types.
    ///     </para>
    ///     <para>
    ///         <strong>Error Handling:</strong> Throw <see cref="InvalidOperationException" /> for unknown event types
    ///         to catch programming errors early.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// protected override void When(IInternalDomainEvent @event)
    /// {
    ///     switch (@event)
    ///     {
    ///         case AccountCreated e:
    ///             Id = Guid.Parse(e.Source);
    ///             _owner = e.Owner;
    ///             _balance = e.InitialBalance;
    ///             break;
    ///
    ///         case MoneyDeposited e:
    ///             _balance += e.Amount;
    ///             break;
    ///
    ///         case MoneyWithdrawn e:
    ///             _balance -= e.Amount;
    ///             break;
    ///
    ///         case AccountClosed e:
    ///             IsDeleted = true;
    ///             break;
    ///
    ///         default:
    ///             throw new InvalidOperationException(
    ///                 $"Unknown event type: {@event.GetType().Name}");
    ///     }
    /// }
    /// </code>
    /// </example>
    [SuppressMessage(
        "Naming",
        "CA1707:Identifiers should not contain underscores",
        Justification = "The leading underscore marks framework-internal template methods that only subclasses may call, mirroring Java ezddd's protected API surface; renaming would break semantic parity and the published protected contract."
    )]
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "The parameter name 'event' is the established domain-event vocabulary inherited from Java ezddd's public API; C# implementers use the escaped identifier @event. Renaming would break named-argument compatibility and cross-language parity."
    )]
    protected abstract void _When(TEvent @event);

    /// <summary>
    ///     Checks business invariants for this aggregate.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The default implementation is a no-op. Subclasses should override this method
    ///         to add specific business rule checks using assertions or exception throwing.
    ///     </para>
    ///     <para>
    ///         <strong>When Called:</strong> This method is called by <see cref="Apply" /> before
    ///         and/or after state mutation, depending on the event type (R1/R2/R3 rules).
    ///     </para>
    ///     <para>
    ///         <strong>What to Check:</strong> Verify business rules such as:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>Value ranges (e.g., balance >= 0)</description>
    ///             </item>
    ///             <item>
    ///                 <description>Required fields (e.g., owner must be set)</description>
    ///             </item>
    ///             <item>
    ///                 <description>Relationships (e.g., order must have items)</description>
    ///             </item>
    ///             <item>
    ///                 <description>State consistency (e.g., status transitions are valid)</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <strong>What NOT to Do:</strong>
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>❌ DO NOT mutate state in this method</description>
    ///             </item>
    ///             <item>
    ///                 <description>❌ DO NOT raise new events</description>
    ///             </item>
    ///             <item>
    ///                 <description>❌ DO NOT perform I/O operations</description>
    ///             </item>
    ///             <item>
    ///                 <description>❌ DO NOT check invariants on deleted aggregates (use <c>if (IsDeleted) return;</c>)</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when an invariant is violated. Include a descriptive message explaining which rule failed.
    /// </exception>
    /// <example>
    ///     <code>
    /// protected override void EnsureInvariant()
    /// {
    ///     // Skip invariant checks for deleted aggregates
    ///     if (IsDeleted) return;
    ///
    ///     // Check business rules
    ///     if (Id == Guid.Empty)
    ///         throw new InvalidOperationException("Account ID must be set");
    ///
    ///     if (string.IsNullOrEmpty(_owner))
    ///         throw new InvalidOperationException("Account must have owner");
    ///
    ///     if (_balance &lt; 0)
    ///         throw new InvalidOperationException("Balance cannot be negative");
    /// }
    /// </code>
    /// </example>
    [SuppressMessage(
        "Naming",
        "CA1707:Identifiers should not contain underscores",
        Justification = "The leading underscore marks framework-internal template methods that only subclasses may call, mirroring Java ezddd's protected API surface; renaming would break semantic parity and the published protected contract."
    )]
    protected virtual void _EnsureInvariant()
    {
        // Default: no-op
        // Subclasses override to add specific business rule checks
    }

    /// <summary>
    ///     Replays a sequence of events to reconstruct aggregate state.
    /// </summary>
    /// <param name="events">The events to replay in chronological order</param>
    /// <remarks>
    ///     <para>
    ///         Events are applied via <see cref="Apply" />, which enforces invariant checking
    ///         during replay. This ensures the reconstructed state is valid and consistent
    ///         with the business rules.
    ///     </para>
    ///     <para>
    ///         This method is <c>protected virtual</c> to allow subclasses to customize
    ///         replay behavior if needed (e.g., skip certain events, apply optimizations).
    ///     </para>
    /// </remarks>
    [SuppressMessage(
        "Naming",
        "CA1707:Identifiers should not contain underscores",
        Justification = "The leading underscore marks framework-internal template methods that only subclasses may call, mirroring Java ezddd's protected API surface; renaming would break semantic parity and the published protected contract."
    )]
    protected virtual void _ReplayEvents(IEnumerable<TEvent> events)
    {
        foreach (TEvent @event in events)
        {
            Apply(@event);
        }
    }

    /// <summary>
    ///     Gets the category name for this aggregate type.
    /// </summary>
    /// <returns>The category string (e.g., "order", "user", "payment")</returns>
    /// <remarks>
    ///     <para>
    ///         The category is used for event stream naming in event stores:
    ///         <c>{category}-{id}</c> (see <see cref="GetStreamName" />).
    ///     </para>
    ///     <para>
    ///         <strong>Convention:</strong> Use lowercase, singular noun representing the aggregate type.
    ///     </para>
    ///     <para>
    ///         <strong>Examples:</strong>
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>"order" for Order aggregate</description>
    ///             </item>
    ///             <item>
    ///                 <description>"customer" for Customer aggregate</description>
    ///             </item>
    ///             <item>
    ///                 <description>"invoice" for Invoice aggregate</description>
    ///             </item>
    ///             <item>
    ///                 <description>"shipment" for Shipment aggregate</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public abstract string GetCategory();

    /// <summary>
    ///     Gets the event stream name for this aggregate.
    /// </summary>
    /// <returns>The stream name in format <c>{category}-{id}</c></returns>
    /// <remarks>
    ///     <para>
    ///         Event stores use stream names to organize events by aggregate instance.
    ///         Each aggregate instance has its own stream containing its complete event history.
    ///     </para>
    ///     <para>
    ///         <strong>Format:</strong> <c>{category}-{id}</c>
    ///     </para>
    ///     <para>
    ///         Where:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description><c>{category}</c> is from <see cref="GetCategory" /></description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <c>{id}</c> is the string representation of <see cref="AggregateRoot{TId, TEvent}.Id" />
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // For Order aggregate with Id = Guid("550e8400-e29b-41d4-a716-446655440000")
    /// var order = new Order(/* ... */);
    /// string streamName = order.GetStreamName();
    /// // Returns: "order-550e8400-e29b-41d4-a716-446655440000"
    /// </code>
    /// </example>
    public string GetStreamName()
    {
        return $"{GetCategory()}-{Id}";
    }
}
