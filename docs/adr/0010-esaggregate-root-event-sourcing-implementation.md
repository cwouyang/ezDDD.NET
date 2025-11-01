# ADR-0010: EsAggregateRoot Event Sourcing Implementation (R1, R2, R3 Rules)

## Status

**Accepted**

- **Date**: 2025-11-01
- **Deciders**: Teddy Chen, Claude Code
- **Status Date**: 2025-11-01

---

## Context

### Problem Statement

ezDDD.NET needs an event-sourced aggregate root implementation that enforces correctness rules for event sourcing while supporting aggregate reconstruction from event history. The design must:

1. **Enforce event sourcing correctness rules**: R1 (Construction), R2 (Command), R3 (Destruction)
2. **Support event replay**: Reconstruct aggregate state from event history
3. **Maintain invariants**: Check business rules at appropriate lifecycle points
4. **Provide template method**: Separate framework invariant checking from domain logic
5. **Enable stream naming**: Support event store stream identification
6. **Prevent misuse**: Ensure subclasses cannot bypass invariant checking

The three correctness rules from formal specification:

- **R1 (Construction)**: `{pre₀} fun₀ {post₀ & INV}` - No precondition check, postcondition + invariant checked
- **R2 (Command)**: `{preₜ & INV} funₜ {postₜ & INV}` - Both precondition + postcondition invariants checked
- **R3 (Destruction)**: `{preᵤ & INV} funᵤ {postᵤ}` - Precondition + invariant checked, no postcondition check

Key design questions:
- How do we enforce R1/R2/R3 at framework level?
- Should `Apply()` be sealed or virtual?
- How do we distinguish construction vs command vs destruction events?
- When exactly should `EnsureInvariant()` be called?
- How do we implement event replay constructor?
- How do we handle invariant violations during replay?

### Relevant Context

- **Java ezddd Reference**:
  - `EsAggregateRoot<ID, E extends InternalDomainEvent>` extends `AggregateRoot<ID, E>`
  - `apply(E event)` is **final** - enforces R1/R2/R3 via template method
  - `when(E event)` is **abstract** - subclasses implement state mutation
  - `ensureInvariant()` is **protected** with default no-op - subclasses override
  - Constructor accepts `List<E>` for replay
  - Uses `instanceof` to check `ConstructionEvent` / `DestructionEvent`
  - Stream naming: `getCategory() + "-" + getId()`

- **.NET Platform Features**:
  - Pattern matching with `is` and switch expressions
  - `sealed override` to prevent further overriding
  - `abstract` methods must be implemented by subclasses
  - No checked exceptions (different error handling)

- **uContract.NET Integration** (ADR-0006):
  - `Contract.Invariant(string message, Func<bool> predicate)` for invariant checking
  - Throws `InvariantViolation` on failure
  - Provides clear violation messages

### Constraints

- **Semantic Parity**: Must match Java ezddd's R1/R2/R3 enforcement (ADR-0005)
- **uContract.NET Integration**: Use uContract.NET for invariant checking (ADR-0006)
- **Type Safety**: Leverage .NET type system and pattern matching
- **Framework Correctness**: Framework must prevent incorrect event sourcing usage
- **Reconstruction Correctness**: Replay must produce identical state

---

## Decision

We will implement **`EsAggregateRoot<TId, TEvent>`** extending `AggregateRoot<TId, TEvent>` with a **sealed `Apply()` template method** that enforces R1/R2/R3 rules via marker interface checking and strategic `EnsureInvariant()` calls. Subclasses implement **abstract `When()`** for state mutation and **virtual `EnsureInvariant()`** for business rule checking.

### Details

#### EsAggregateRoot<TId, TEvent> Abstract Class

```csharp
namespace EzDdd.Entity;

using UContract;

/// <summary>
/// Abstract base class for event-sourced aggregate roots.
/// Enforces event sourcing correctness rules (R1, R2, R3) through
/// template method pattern with invariant checking.
/// </summary>
/// <typeparam name="TId">The type of the aggregate's unique identifier</typeparam>
/// <typeparam name="TEvent">The type of internal domain events this aggregate produces</typeparam>
/// <remarks>
/// <para><b>Event Sourcing Correctness Rules:</b></para>
/// <list type="bullet">
///   <item><b>R1 (Construction)</b>: {pre₀} fun₀ {post₀ &amp; INV} - First event establishes state, no precondition check</item>
///   <item><b>R2 (Command)</b>: {preₜ &amp; INV} funₜ {postₜ &amp; INV} - Middle events maintain invariants before and after</item>
///   <item><b>R3 (Destruction)</b>: {preᵤ &amp; INV} funᵤ {postᵤ} - Last event finalizes, no postcondition check</item>
/// </list>
/// </remarks>
public abstract class EsAggregateRoot<TId, TEvent> : AggregateRoot<TId, TEvent>
    where TEvent : InternalDomainEvent
{
    /// <summary>
    /// Initializes a new instance (for new aggregate creation).
    /// Subclasses should call this, then apply construction event.
    /// </summary>
    protected EsAggregateRoot()
    {
    }

    /// <summary>
    /// Initializes an aggregate by replaying events from history.
    /// This is the primary constructor for loading persisted aggregates.
    /// </summary>
    /// <param name="events">The event history to replay</param>
    /// <remarks>
    /// <para>This constructor is PUBLIC to match Java ezddd's design:</para>
    /// <code>
    /// public EsAggregateRoot(List&lt;E&gt; events)  // Java version is public
    /// </code>
    /// <para>
    /// Events are applied in order via <see cref="Apply"/>, which enforces
    /// invariant checking. After replay, domain events are cleared to prevent
    /// re-publication.
    /// </para>
    /// <para>
    /// Repositories use reflection to invoke this constructor when loading
    /// aggregates from the event store.
    /// </para>
    /// </remarks>
    public EsAggregateRoot(IEnumerable<TEvent> events)
        : this()
    {
        Contract.Require("Events cannot be null", () => events != null);

        ReplayEvents(events);
        ClearDomainEvents();  // Replayed events should not be re-published
    }

    /// <summary>
    /// Applies a domain event to this aggregate with invariant checking.
    /// This method is SEALED to enforce R1/R2/R3 correctness rules.
    /// Subclasses must implement <see cref="When"/> for state mutation.
    /// </summary>
    /// <param name="event">The domain event to apply</param>
    /// <remarks>
    /// <para><b>Invariant Checking Logic:</b></para>
    /// <list type="number">
    ///   <item>If NOT ConstructionEvent: Check precondition invariants (R2, R3)</item>
    ///   <item>Call When(event) to mutate state</item>
    ///   <item>If NOT DestructionEvent: Check postcondition invariants (R1, R2)</item>
    ///   <item>Add event to collection</item>
    /// </list>
    /// </remarks>
    public sealed override void Apply(TEvent @event)
    {
        // R1 (Construction): Skip precondition check for first event
        // R2 (Command): Check precondition for normal events
        // R3 (Destruction): Check precondition for last event
        if (@event is not InternalDomainEvent.IConstructionEvent)
        {
            EnsureInvariant();
        }

        // Apply state changes (abstract method - subclass implements)
        try
        {
            When(@event);
        }
        catch (Exception ex)
        {
            // If When() throws during construction, don't check postcondition
            if (@event is InternalDomainEvent.IConstructionEvent)
            {
                throw;  // R1: No postcondition check on construction failure
            }

            // For command/destruction events, ensure invariant and chain exception
            try
            {
                EnsureInvariant();
            }
            catch (Exception invariantEx)
            {
                throw new InvalidOperationException(
                    $"Invariant violation after event {@event.GetType().Name}",
                    new AggregateException(ex, invariantEx));
            }

            throw;
        }

        // R1 (Construction): Check postcondition + invariant for first event
        // R2 (Command): Check postcondition + invariant for normal events
        // R3 (Destruction): Skip postcondition check for last event
        if (@event is not InternalDomainEvent.IDestructionEvent)
        {
            EnsureInvariant();
        }

        // Add event to collection
        AddDomainEvent(@event);
    }

    /// <summary>
    /// Abstract method that subclasses implement to mutate aggregate state
    /// in response to events.
    /// </summary>
    /// <param name="event">The domain event to handle</param>
    /// <remarks>
    /// <para>This method should ONLY mutate state - no business logic, no new events.</para>
    /// <para>Use pattern matching to handle different event types:</para>
    /// <code>
    /// protected override void When(InternalDomainEvent @event)
    /// {
    ///     switch (@event)
    ///     {
    ///         case OrderCreated e:
    ///             Id = OrderId.Parse(e.Source);
    ///             _customerId = e.CustomerId;
    ///             break;
    ///         case OrderItemAdded e:
    ///             _items.Add(new OrderItem(e.ProductId, e.Quantity));
    ///             break;
    ///         default:
    ///             throw new InvalidOperationException($"Unknown event: {@event.GetType().Name}");
    ///     }
    /// }
    /// </code>
    /// </remarks>
    protected abstract void When(TEvent @event);

    /// <summary>
    /// Checks business invariants for this aggregate.
    /// Default implementation is no-op - subclasses should override to add checks.
    /// </summary>
    /// <remarks>
    /// <para>Use uContract.NET to declare invariants:</para>
    /// <code>
    /// protected override void EnsureInvariant()
    /// {
    ///     Contract.Invariant("Order must have items", () => _items.Count > 0);
    ///     Contract.Invariant("Total amount must be positive", () => _totalAmount > 0);
    /// }
    /// </code>
    /// <para><b>Do NOT:</b></para>
    /// <list type="bullet">
    ///   <item>Mutate state in this method</item>
    ///   <item>Raise new events</item>
    ///   <item>Perform I/O operations</item>
    /// </list>
    /// </remarks>
    protected virtual void EnsureInvariant()
    {
        // Default: no-op
        // Subclasses override to add specific business rule checks
    }

    /// <summary>
    /// Replays a sequence of events to reconstruct aggregate state.
    /// </summary>
    /// <param name="events">The events to replay</param>
    /// <remarks>
    /// Events are applied via <see cref="Apply"/>, which enforces invariant
    /// checking during replay. This ensures reconstructed state is valid.
    /// </remarks>
    protected virtual void ReplayEvents(IEnumerable<TEvent> events)
    {
        foreach (var @event in events)
        {
            Apply(@event);
        }
    }

    /// <summary>
    /// Gets the category name for this aggregate type.
    /// Used for event stream naming: "{category}-{id}".
    /// </summary>
    /// <returns>The category string (e.g., "order", "user", "payment")</returns>
    /// <remarks>
    /// <para>Convention: lowercase, singular noun representing aggregate type.</para>
    /// <para>Examples: "order", "customer", "invoice", "shipment"</para>
    /// </remarks>
    public abstract string GetCategory();

    /// <summary>
    /// Gets the event stream name for this aggregate.
    /// Format: "{category}-{id}".
    /// </summary>
    /// <returns>The stream name</returns>
    /// <example>
    /// <code>
    /// // For Order aggregate with Id = Guid("550e8400...")
    /// string streamName = order.GetStreamName();
    /// // Returns: "order-550e8400-e29b-41d4-a716-446655440000"
    /// </code>
    /// </example>
    public string GetStreamName()
    {
        return $"{GetCategory()}-{Id}";
    }
}
```

**Design Rationale**:

1. **Sealed Apply() - Template Method Pattern**:
   - Framework controls invariant checking logic
   - Subclasses cannot bypass R1/R2/R3 enforcement
   - Clear separation: framework (Apply) vs domain (When)

2. **Abstract When() - Domain Logic Hook**:
   - Subclasses implement state mutation
   - Pure state changes, no events, no side effects
   - Pattern matching enables clean event handling

3. **Virtual EnsureInvariant() - Business Rules**:
   - Default no-op allows aggregates without invariants
   - Subclasses override to add specific checks
   - Called at precise points per R1/R2/R3

4. **R1/R2/R3 Enforcement via Marker Interfaces**:
   - Uses `is` pattern matching to check `IConstructionEvent` / `IDestructionEvent`
   - Compile-time type safety (events must implement markers)
   - Runtime branching for precondition/postcondition logic

5. **Exception Handling During When()**:
   - If When() throws during construction → rethrow (R1: no postcondition check)
   - If When() throws during command/destruction → check invariant and chain exceptions
   - Provides debugging context

6. **Replay Constructor**:
   - Primary constructor for loading persisted aggregates
   - Calls `ReplayEvents()` which uses same `Apply()` logic
   - Clears events after replay (no re-publication)

7. **Stream Naming Convention**:
   - `GetCategory()` returns aggregate type (lowercase, singular)
   - `GetStreamName()` combines category + id
   - Enables event store partitioning and querying

#### Correctness Rules Detailed Flowchart

```
Apply(event):
  ┌─────────────────────────────────────────────────────┐
  │ Is event IConstructionEvent?                        │
  │ NO  → EnsureInvariant() [Precondition R2/R3]       │
  │ YES → Skip [R1: No precondition check]             │
  └─────────────────────────────────────────────────────┘
                        ↓
  ┌─────────────────────────────────────────────────────┐
  │ When(event) [State Mutation]                        │
  │ (May throw exception)                               │
  └─────────────────────────────────────────────────────┘
                        ↓
         ┌──────────────┴──────────────┐
         │ Exception thrown?           │
         ├─────────────┬────────────────┤
        YES           NO
         │             │
         │             ↓
         │    Is event IDestructionEvent?
         │    NO  → EnsureInvariant() [Postcondition R1/R2]
         │    YES → Skip [R3: No postcondition check]
         │             │
         │             ↓
         │    AddDomainEvent(event)
         │
         ↓
    Is event IConstructionEvent?
    YES → Throw [R1: No postcondition check]
    NO  → EnsureInvariant() + Chain Exception
         Throw
```

---

## Consequences

### Positive Consequences

- ✅ **Correctness guarantee**: R1/R2/R3 rules enforced by framework, not developer discipline
- ✅ **Semantic parity**: Matches Java ezddd's behavior exactly
- ✅ **Clear separation**: Framework logic (Apply) vs domain logic (When)
- ✅ **Type-safe**: Marker interfaces checked at compile time
- ✅ **Debuggable**: Exception chaining provides clear error context
- ✅ **Replay integrity**: Same Apply() logic ensures consistent reconstruction
- ✅ **Stream naming**: Consistent convention for event store

### Negative Consequences

- ❌ **Apply() complexity**: Nested conditionals for R1/R2/R3 checking
- ❌ **Exception handling overhead**: Try-catch in hot path (minimal cost)
- ❌ **Sealed Apply()**: Subclasses cannot customize event application

### Neutral Consequences

- ⚖️ **EnsureInvariant() called multiple times**: Per-event overhead (necessary for correctness)
- ⚖️ **Marker interface requirement**: Developers must remember to implement IConstructionEvent/IDestructionEvent
- ⚖️ **Pattern matching verbosity**: When() requires switch expression with all event types

---

## Alternatives Considered

### Alternative 1: Attributes Instead of Marker Interfaces

**Description**: Use `[ConstructionEvent]` and `[DestructionEvent]` attributes.

```csharp
[ConstructionEvent]
public record OrderCreated(...) : InternalDomainEvent;

public sealed override void Apply(TEvent @event)
{
    var isConstruction = @event.GetType().GetCustomAttribute<ConstructionEventAttribute>() != null;
    // ...
}
```

**Pros**:
- No additional interface implementations
- Cleaner event definitions

**Cons**:
- Requires reflection (performance cost in Apply hot path)
- No compile-time type checking
- Cannot use in generic constraints

**Why rejected**:
- Reflection overhead in critical Apply() loop
- Loses type safety (marker interfaces are compile-time checked)
- Cannot constrain generic types (e.g., `where T : IConstructionEvent`)

---

### Alternative 2: Virtual Apply() with Protected Hooks

**Description**: Keep Apply() virtual with Before/After hooks.

```csharp
public virtual void Apply(TEvent @event)
{
    BeforeApply(@event);  // Hook for precondition
    When(@event);
    AfterApply(@event);   // Hook for postcondition
    AddDomainEvent(@event);
}

protected virtual void BeforeApply(TEvent @event) { EnsureInvariant(); }
protected virtual void AfterApply(TEvent @event) { EnsureInvariant(); }
```

**Pros**:
- More flexible (subclasses can customize hooks)
- Less complex Apply() logic

**Cons**:
- Subclasses can override Apply() and bypass R1/R2/R3
- Framework correctness depends on developer discipline
- Easy to misuse

**Why rejected**:
- Violates "framework enforces correctness" principle
- Subclasses could accidentally break R1/R2/R3
- Semantic parity requires sealed Apply()

---

### Alternative 3: Separate ApplyConstruction/ApplyCommand/ApplyDestruction Methods

**Description**: Different methods for different event types.

```csharp
public void ApplyConstruction(TEvent @event)  // R1 logic
public void ApplyCommand(TEvent @event)       // R2 logic
public void ApplyDestruction(TEvent @event)   // R3 logic
```

**Pros**:
- Explicit method names
- Separate logic for each rule

**Cons**:
- Breaks unified Apply() interface
- Cannot replay events generically (need type checking)
- More API surface to learn

**Why rejected**:
- Complicates replay (need to determine which method to call)
- Marker interfaces already distinguish event types
- Unified Apply() is simpler and matches Java ezddd

---

### Alternative 4: No Invariant Checking During Replay

**Description**: Skip EnsureInvariant() during replay for performance.

```csharp
private bool _isReplaying = false;

public sealed override void Apply(TEvent @event)
{
    if (!_isReplaying && @event is not IConstructionEvent)
        EnsureInvariant();

    When(@event);

    if (!_isReplaying && @event is not IDestructionEvent)
        EnsureInvariant();

    AddDomainEvent(@event);
}

protected void ReplayEvents(IEnumerable<TEvent> events)
{
    _isReplaying = true;
    foreach (var e in events) Apply(e);
    _isReplaying = false;
}
```

**Pros**:
- Faster replay (no invariant checking overhead)
- Assumes persisted events are valid

**Cons**:
- Dangerous: corrupted event store produces invalid aggregates
- Violates "replay produces same state" principle
- Debugging nightmare (invalid state silently loaded)

**Why rejected**:
- **Correctness over performance**: Invariant checking during replay catches corrupted events
- Semantic parity requires same Apply() logic for new events and replay
- Performance impact minimal (in-memory operations)

---

### Alternative 5: Async When() Method

**Description**: Make When() asynchronous.

```csharp
protected abstract Task WhenAsync(TEvent @event);

public sealed override async Task ApplyAsync(TEvent @event)
{
    if (@event is not IConstructionEvent)
        EnsureInvariant();

    await WhenAsync(@event);

    if (@event is not IDestructionEvent)
        EnsureInvariant();

    AddDomainEvent(@event);
}
```

**Pros**:
- Consistent with async repository methods

**Cons**:
- Domain logic should be synchronous (no I/O)
- Complicates event replay (must await each event)
- Async/await overhead for pure CPU operations

**Why rejected**:
- State mutation is pure CPU logic (synchronous)
- Async belongs at I/O boundaries, not domain layer
- Significant performance cost for no benefit

---

## Related Decisions

- **Depends on**:
  - [ADR-0006: uContract.NET Integration](0006-ucontract-integration-design-by-contract.md) - Uses Contract.Invariant() for invariant checking
  - [ADR-0008: IDomainEvent Hierarchy](0008-idomain-event-hierarchy.md) - Relies on IConstructionEvent/IDestructionEvent markers
  - [ADR-0009: AggregateRoot Base Class Design](0009-aggregate-root-base-class-design.md) - Extends AggregateRoot

- **Related to**:
  - [ADR-0011: Event Replay and Invariant Checking](0011-event-replay-invariant-checking.md) - Details replay mechanism
  - [ADR-0016: Reflection for Aggregate Reconstruction](planned) - How repositories instantiate aggregates via replay constructor

---

## Implementation Notes

### Complete Example: Event-Sourced Order Aggregate

```csharp
public class Order : EsAggregateRoot<Guid, InternalDomainEvent>
{
    private Guid _customerId;
    private List<OrderItem> _items = new();
    private decimal _totalAmount;
    private OrderStatus _status = OrderStatus.Draft;
    private string? _cancellationReason;

    // Constructor for new aggregate
    private Order() { }

    // Replay constructor for loading from events
    public Order(IEnumerable<InternalDomainEvent> events)
        : base(events)
    {
    }

    // Factory method for creating new order
    public static Order Create(Guid orderId, Guid customerId)
    {
        var order = new Order();

        var created = new OrderCreated(
            Id: Guid.NewGuid(),
            OccurredOn: DateTimeOffset.UtcNow,
            Source: orderId.ToString(),
            CustomerId: customerId,
            Metadata: DomainEventMetadata.Empty
        );

        order.Apply(created);  // R1: No precondition, postcondition checked
        return order;
    }

    // Command method
    public void AddItem(string productId, int quantity, decimal price)
    {
        // Precondition checks (business rules)
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive");
        if (price <= 0)
            throw new ArgumentException("Price must be positive");
        if (_status != OrderStatus.Draft)
            throw new InvalidOperationException("Cannot modify submitted order");

        var itemAdded = new OrderItemAdded(
            Id: Guid.NewGuid(),
            OccurredOn: DateTimeOffset.UtcNow,
            Source: Id.ToString(),
            ProductId: productId,
            Quantity: quantity,
            Price: price,
            Metadata: DomainEventMetadata.Empty
        );

        Apply(itemAdded);  // R2: Pre and post invariants checked
    }

    // Destruction command
    public void Cancel(string reason)
    {
        if (_status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Order already cancelled");

        var cancelled = new OrderCancelled(
            Id: Guid.NewGuid(),
            OccurredOn: DateTimeOffset.UtcNow,
            Source: Id.ToString(),
            Reason: reason,
            Metadata: DomainEventMetadata.Empty
        );

        Apply(cancelled);  // R3: Precondition checked, no postcondition
    }

    // State mutation (abstract method implementation)
    protected override void When(InternalDomainEvent @event)
    {
        switch (@event)
        {
            case OrderCreated e:
                Id = Guid.Parse(e.Source);
                _customerId = e.CustomerId;
                _status = OrderStatus.Draft;
                break;

            case OrderItemAdded e:
                _items.Add(new OrderItem(e.ProductId, e.Quantity, e.Price));
                _totalAmount += e.Quantity * e.Price;
                break;

            case OrderCancelled e:
                _status = OrderStatus.Cancelled;
                _cancellationReason = e.Reason;
                IsDeleted = true;  // Soft delete
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown event type: {@event.GetType().Name}");
        }
    }

    // Business invariants
    protected override void EnsureInvariant()
    {
        // Skip invariant checks for terminal states
        if (_status == OrderStatus.Cancelled)
            return;

        Contract.Invariant(
            "Order must have at least one item",
            () => _items.Count > 0);

        Contract.Invariant(
            "Total amount must match sum of item prices",
            () => _totalAmount == _items.Sum(i => i.Quantity * i.Price));

        Contract.Invariant(
            "Total amount must be positive",
            () => _totalAmount > 0);
    }

    // Stream category
    public override string GetCategory() => "order";
}

// Events
public record OrderCreated(
    Guid Id,
    DateTimeOffset OccurredOn,
    string Source,
    Guid CustomerId,
    IReadOnlyDictionary<string, string> Metadata
) : InternalDomainEvent, InternalDomainEvent.IConstructionEvent;

public record OrderItemAdded(
    Guid Id,
    DateTimeOffset OccurredOn,
    string Source,
    string ProductId,
    int Quantity,
    decimal Price,
    IReadOnlyDictionary<string, string> Metadata
) : InternalDomainEvent;

public record OrderCancelled(
    Guid Id,
    DateTimeOffset OccurredOn,
    string Source,
    string Reason,
    IReadOnlyDictionary<string, string> Metadata
) : InternalDomainEvent, InternalDomainEvent.IDestructionEvent;
```

### Testing R1/R2/R3 Rules

```csharp
[Fact]
public void Apply_ConstructionEvent_NoPreConditionCheck()
{
    // R1: Construction event should not check precondition invariant
    var order = Order.Create(Guid.NewGuid(), Guid.NewGuid());

    // Order created successfully even though it has no items
    // (invariant would fail, but R1 skips precondition check)
    Assert.NotNull(order);
}

[Fact]
public void Apply_CommandEvent_ChecksPreAndPostInvariants()
{
    // R2: Command events check both pre and post invariants
    var order = Order.Create(Guid.NewGuid(), Guid.NewGuid());

    // This will succeed (invariants maintained)
    order.AddItem("PROD-001", 2, 10.00m);

    Assert.Equal(1, order.GetDomainEventCount());
}

[Fact]
public void Apply_DestructionEvent_NoPostConditionCheck()
{
    // R3: Destruction event should not check postcondition invariant
    var order = Order.Create(Guid.NewGuid(), Guid.NewGuid());
    order.AddItem("PROD-001", 1, 10.00m);

    // Cancel destroys aggregate - invariants may be broken
    order.Cancel("Customer requested");

    // Order cancelled successfully even though it would fail invariants
    Assert.True(order.IsDeleted);
}

[Fact]
public void ReplayEvents_ReconstructsCorrectState()
{
    var orderId = Guid.NewGuid();
    var customerId = Guid.NewGuid();

    var events = new List<InternalDomainEvent>
    {
        new OrderCreated(Guid.NewGuid(), DateTimeOffset.UtcNow, orderId.ToString(), customerId, DomainEventMetadata.Empty),
        new OrderItemAdded(Guid.NewGuid(), DateTimeOffset.UtcNow, orderId.ToString(), "PROD-001", 2, 10.00m, DomainEventMetadata.Empty),
        new OrderItemAdded(Guid.NewGuid(), DateTimeOffset.UtcNow, orderId.ToString(), "PROD-002", 1, 5.00m, DomainEventMetadata.Empty)
    };

    var order = new Order(events);

    Assert.Equal(orderId, order.Id);
    Assert.Equal(0, order.GetDomainEventCount());  // Events cleared after replay
    Assert.Equal("order-" + orderId, order.GetStreamName());
}
```

---

## References

- **Java ezddd Source**:
  - `path/to/local/checkout Frame\ezddd\ezddd-entity\src\main\java\tw\teddysoft\ezddd\entity\EsAggregateRoot.java`

- **Event Sourcing Correctness**:
  - Chen, Teddy. "Aggregate Correctness Rules in Event Sourcing" (internal specification)
  - Young, Greg. ["Event Sourcing"](https://cqrs.wordpress.com/documents/events-as-storage-mechanism/)
  - Vernon, Vaughn. *Implementing Domain-Driven Design*. Chapter 8: "Domain Events"

- **Design Patterns**:
  - Gamma et al. *Design Patterns*. "Template Method" pattern
  - Fowler, Martin. ["Design by Contract"](https://martinfowler.com/bliki/ContractTest.html)

- **.NET Pattern Matching**:
  - [Pattern Matching Overview](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/pattern-matching)
  - [Type Patterns](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/type-testing-and-cast#type-testing-with-pattern-matching)

- **Internal Documents**:
  - [DOTNET_PORT.md](../../DOTNET_PORT.md) - Section "重要實作規則 > Aggregate Correctness Rules"
  - [CLAUDE.md](../../CLAUDE.md) - Section "Event Sourcing Rules (Priority: 43)"

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2025-11-01 | Accepted    | Initial decision documented    |

---
