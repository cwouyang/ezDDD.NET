# ADR-0009: AggregateRoot Base Class Design

## Status

**Accepted**

- **Date**: 2025-11-01
- **Deciders**: Teddy Chen, Claude Code
- **Status Date**: 2025-11-01

---

## Context

### Problem Statement

ezDDD.NET needs a base class for aggregate roots that supports both state sourcing and event sourcing patterns. The design must:

1. **Manage domain events**: Collect events raised during aggregate operations
2. **Support versioning**: Enable optimistic locking for concurrency control
3. **Track deletion state**: Provide soft delete capability
4. **Enforce encapsulation**: Prevent external code from directly manipulating events
5. **Enable event replay**: Support reconstruction from event history (for event sourcing)
6. **Provide template method**: Allow subclasses to customize behavior while maintaining framework invariants

Key design questions:
- Should we use abstract class or interface?
- How should we manage the event collection (thread-safety, immutability)?
- How should version control work (.NET doesn't have AtomicLong)?
- Should `Apply()` be synchronous or asynchronous?
- How do we handle generic type parameters?
- What members should be `protected` vs `public`?

### Relevant Context

- **Java ezddd Reference**:
  - `AggregateRoot<ID, E extends InternalDomainEvent>` abstract class
  - Event collection: `CopyOnWriteArrayList<E>` (thread-safe, read-optimized)
  - Version: `AtomicLong` (atomic operations for concurrency)
  - Template Method Pattern: `apply(E event)` adds event and delegates to subclass
  - Protected methods: `addDomainEvent(E event)` - final to prevent override
  - Public methods: `getDomainEvents()`, `clearDomainEvents()`, `getVersion()`, `isDeleted()`

- **.NET Platform Features**:
  - No direct `CopyOnWriteArrayList` equivalent
  - Options: `List<T>` + `lock`, `ConcurrentBag<T>`, `ImmutableList<T>`
  - Long operations not automatically atomic (unlike Java's `AtomicLong`)
  - Properties preferred over getter methods
  - `IReadOnlyList<T>` for exposing collections

- **DDD Best Practices**:
  - Aggregates are consistency boundaries
  - Events represent state changes that have occurred
  - Version supports optimistic concurrency control
  - Aggregate roots are the only entry points to aggregates

### Constraints

- **Semantic Parity**: Must match Java ezddd's behavior and capabilities (ADR-0005)
- **.NET Conventions**: Follow .NET naming and property patterns (ADR-0002)
- **Synchronous Domain Logic**: Domain event application should be synchronous (no I/O)
- **Thread Safety**: Event collection must be thread-safe for concurrent access
- **Encapsulation**: Subclasses should not directly manipulate events

---

## Decision

We will implement **`AggregateRoot<TId, TEvent>`** as an abstract base class using a `List<TEvent>` with `lock`-based synchronization, providing template method pattern for event application.

### Details

#### AggregateRoot<TId, TEvent> Abstract Class

```csharp
namespace EzDdd.Entity;

/// <summary>
/// Abstract base class for aggregate roots.
/// An aggregate root is the entry point to an aggregate and maintains
/// a collection of domain events representing state changes.
/// </summary>
/// <typeparam name="TId">The type of the aggregate's unique identifier</typeparam>
/// <typeparam name="TEvent">The type of internal domain events this aggregate produces</typeparam>
public abstract class AggregateRoot<TId, TEvent> : IEntity<TId>
    where TEvent : InternalDomainEvent
{
    private readonly List<TEvent> _domainEvents = new();
    private readonly object _domainEventsLock = new();

    /// <summary>
    /// Gets or sets the unique identifier of this aggregate.
    /// </summary>
    /// <remarks>
    /// Use <c>default!</c> pattern for initialization - subclass constructors
    /// must set Id before returning.
    /// </remarks>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Gets or sets the version of this aggregate for optimistic concurrency control.
    /// </summary>
    /// <remarks>
    /// Version starts at -1 (indicating "not yet persisted") and is incremented
    /// each time a domain event is added via <see cref="AddDomainEvent"/>.
    /// This matches Java ezddd's behavior where version equals the number of events.
    /// Repositories use this for detecting concurrent modifications.
    /// </remarks>
    public long Version { get; protected set; } = -1;

    /// <summary>
    /// Gets or sets whether this aggregate has been marked as deleted.
    /// </summary>
    /// <remarks>
    /// This supports soft delete scenarios where the aggregate is logically
    /// deleted but still exists in the event stream.
    /// </remarks>
    public bool IsDeleted { get; protected set; }

    /// <summary>
    /// Applies a domain event to this aggregate.
    /// This is a template method that can be overridden by subclasses
    /// to customize event application behavior.
    /// </summary>
    /// <param name="event">The domain event to apply</param>
    /// <remarks>
    /// The default implementation adds the event to the event collection.
    /// Event-sourced aggregates (EsAggregateRoot) override this to add
    /// invariant checking and state mutation.
    /// </remarks>
    public virtual void Apply(TEvent @event)
    {
        AddDomainEvent(@event);
    }

    /// <summary>
    /// Adds a domain event to the event collection and increments version.
    /// This method is non-virtual to prevent subclasses from
    /// bypassing event collection or version management.
    /// </summary>
    /// <param name="event">The domain event to add</param>
    /// <remarks>
    /// Matches Java ezddd's behavior: version is incremented on each event addition,
    /// making version equal to the number of events applied to the aggregate.
    /// </remarks>
    protected void AddDomainEvent(TEvent @event)
    {
        lock (_domainEventsLock)
        {
            _domainEvents.Add(@event);
            Version++;  // Increment version per event (matches Java ezddd)
        }
    }

    /// <summary>
    /// Gets a read-only view of all domain events raised by this aggregate
    /// since the last call to <see cref="ClearDomainEvents"/>.
    /// </summary>
    /// <returns>A read-only list of domain events</returns>
    public IReadOnlyList<TEvent> GetDomainEvents()
    {
        lock (_domainEventsLock)
        {
            return _domainEvents.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the last domain event raised by this aggregate, or null if no events.
    /// </summary>
    /// <returns>The most recent domain event, or null</returns>
    public TEvent? GetLastDomainEvent()
    {
        lock (_domainEventsLock)
        {
            return _domainEvents.Count > 0 ? _domainEvents[^1] : null;
        }
    }

    /// <summary>
    /// Gets the number of domain events currently in the collection.
    /// </summary>
    /// <returns>The event count</returns>
    public int GetDomainEventCount()
    {
        lock (_domainEventsLock)
        {
            return _domainEvents.Count;
        }
    }

    /// <summary>
    /// Clears all domain events from the collection.
    /// </summary>
    /// <remarks>
    /// Repositories call this after persisting events to prevent
    /// duplicate event publication.
    /// </remarks>
    public void ClearDomainEvents()
    {
        lock (_domainEventsLock)
        {
            _domainEvents.Clear();
        }
    }
}
```

**Design Rationale**:

1. **Abstract Class, Not Interface**:
   - Needs to maintain state (`_domainEvents`, `Version`, `IsDeleted`)
   - Provides default implementations (Template Method Pattern)
   - Prevents multiple inheritance issues

2. **Thread Safety via Lock**:
   - `List<T>` + `lock` is simpler and more predictable than concurrent collections
   - Read-optimized pattern not critical (aggregates typically accessed by single thread)
   - .NET `lock` statement provides same mutual exclusion as Java's `synchronized`

3. **Version as `long` with Event-Based Increment**:
   - Starts at `-1` (not yet persisted), incremented in `AddDomainEvent()`
   - Matches Java ezddd's behavior: `version.incrementAndGet()` in `addDomainEvent()`
   - Version equals number of events applied (matches Java semantics)
   - `long` provides sufficient range (2^63 versions)
   - No `AtomicLong` needed - lock in `AddDomainEvent()` provides atomicity

4. **Properties, Not Methods**:
   - `Id { get; protected set; }` vs Java's `getId()` / `setId()`
   - .NET idiomatic property syntax
   - `protected set` allows subclass initialization

5. **Template Method Pattern**:
   - `Apply()` is `virtual` - base class provides default, subclasses can override
   - `AddDomainEvent()` is non-virtual - prevents bypassing event collection
   - `EsAggregateRoot` will seal `Apply()` and add invariant checking

6. **Return `IReadOnlyList<T>`**:
   - Prevents external modification of event collection
   - Thread-safe snapshot via `ToList()` + `AsReadOnly()`

7. **Nullable Reference Types**:
   - `Id = default!` - requires subclass initialization (enforced by DDD patterns)
   - `GetLastDomainEvent()` returns `TEvent?` - explicit null possibility

---

## Consequences

### Positive Consequences

- ✅ **Semantic parity**: Matches Java ezddd's capabilities and behavior
- ✅ **Thread-safe**: Lock-based synchronization prevents race conditions
- ✅ **Encapsulated**: Events cannot be manipulated externally
- ✅ **Flexible**: Template Method Pattern allows customization
- ✅ **Type-safe**: Generic parameters enforce event type constraints
- ✅ **.NET idiomatic**: Uses properties, IReadOnlyList, nullable references
- ✅ **Version control**: Supports optimistic locking

### Negative Consequences

- ❌ **Lock overhead**: Every event access requires lock acquisition (minimal cost)
- ❌ **List copy on read**: `GetDomainEvents()` creates defensive copy (small overhead)
- ❌ **Single inheritance**: Subclasses cannot extend other base classes

### Neutral Consequences

- ⚖️ **Synchronous domain logic**: `Apply()` is not async (appropriate for pure domain logic)
- ⚖️ **Manual version management**: Repositories must increment version
- ⚖️ **Soft delete pattern**: `IsDeleted` flag requires discipline

---

## Alternatives Considered

### Alternative 1: ConcurrentBag<T> for Event Collection

**Description**: Use `ConcurrentBag<T>` instead of `List<T>` + `lock`.

```csharp
private readonly ConcurrentBag<TEvent> _domainEvents = new();
```

**Pros**:
- Lock-free concurrent operations
- Built-in thread safety
- No explicit locking code

**Cons**:
- Unordered collection (no guaranteed event order)
- No indexer access (cannot get last event efficiently)
- Slightly slower for single-threaded scenarios
- Overkill for typical aggregate usage (single thread access)

**Why rejected**:
- **Event order matters** for event sourcing (replay must be sequential)
- Aggregates typically accessed by single thread (command handler)
- `ConcurrentBag` designed for producer-consumer scenarios, not our use case
- Lock-based approach is clearer and maintains order guarantees

---

### Alternative 2: ImmutableList<T> for Event Collection

**Description**: Use `ImmutableList<T>` with builder pattern.

```csharp
private ImmutableList<TEvent> _domainEvents = ImmutableList<TEvent>.Empty;

protected void AddDomainEvent(TEvent @event)
{
    _domainEvents = _domainEvents.Add(@event);  // Creates new immutable list
}
```

**Pros**:
- Thread-safe by nature (immutable)
- No locking required
- Functional programming style

**Cons**:
- Creates new list on every addition (allocation overhead)
- Slower than mutable list for frequent additions
- Requires `System.Collections.Immutable` package (external dependency)
- Reassignment not atomic (still needs lock or Interlocked)

**Why rejected**:
- Performance overhead for event addition (O(log n) instead of O(1))
- Violates zero-dependency principle (ADR-0004) if using NuGet package
- Still needs synchronization for reassignment
- Unnecessary complexity for our use case

---

### Alternative 3: Interlocked.Increment for Version

**Description**: Use `Interlocked.Increment` for atomic version updates.

```csharp
private long _version;

public void IncrementVersion()
{
    Interlocked.Increment(ref _version);
}
```

**Pros**:
- Atomic increment without locking
- Thread-safe version updates
- Matches Java's `AtomicLong` semantics

**Cons**:
- `Interlocked` operations more complex than simple lock
- Version already protected by `AddDomainEvent()` lock
- Over-engineering for typical aggregate usage

**Why rejected**:
- The current design increments version in `AddDomainEvent()` which already uses lock
- `Interlocked` provides no additional benefit when lock is already present
- Java's `AtomicLong` was used because `CopyOnWriteArrayList` is lock-free; .NET uses lock throughout
- Semantic behavior is preserved (version incremented per event)

---

### Alternative 4: Async Apply Method

**Description**: Make `Apply()` method asynchronous.

```csharp
public virtual async Task ApplyAsync(TEvent @event)
{
    AddDomainEvent(@event);
    await Task.CompletedTask;
}
```

**Pros**:
- Consistent with repository async methods
- Future-proofs for async domain logic

**Cons**:
- Domain logic should be synchronous (no I/O)
- Async/await overhead for pure CPU-bound operations
- Complicates event replay (must await each event)
- Breaks with DDD best practice (domain layer is pure logic)

**Why rejected**:
- Domain event application is pure state mutation (synchronous)
- Async/await belongs at I/O boundaries (repository, use cases)
- Sync/async boundary should be at use case layer, not domain layer
- Performance overhead for no benefit

---

### Alternative 5: Interface Instead of Abstract Class

**Description**: Use `IAggregateRoot<TId, TEvent>` interface instead of base class.

```csharp
public interface IAggregateRoot<TId, TEvent> : IEntity<TId>
    where TEvent : InternalDomainEvent
{
    long Version { get; set; }
    bool IsDeleted { get; set; }
    void Apply(TEvent @event);
    IReadOnlyList<TEvent> GetDomainEvents();
    void ClearDomainEvents();
}
```

**Pros**:
- Maximum flexibility (multiple interface implementation)
- No inheritance constraints
- Testability (can mock interfaces)

**Cons**:
- No default implementation (boilerplate in every aggregate)
- Cannot enforce `AddDomainEvent` being final
- No shared event collection logic
- Violates DRY principle

**Why rejected**:
- Loses shared implementation (event collection management)
- Cannot use Template Method Pattern effectively
- Every aggregate would duplicate event management code
- Semantic parity with Java requires abstract base class

---

## Related Decisions

- **Depends on**:
  - [ADR-0001: Target Framework](0001-target-framework.md) - .NET 8 features enable nullable references
  - [ADR-0002: Package Naming and Structure](0002-package-naming-and-structure.md) - Generic parameter naming (TId, TEvent)
  - [ADR-0005: Complete Reimplementation Approach](0005-complete-reimplementation-approach.md) - Semantic parity requirement
  - [ADR-0007: IEntity and IValueObject Design](0007-ientity-ivalueobject-design.md) - Implements IEntity<TId>
  - [ADR-0008: IDomainEvent Hierarchy](0008-idomain-event-hierarchy.md) - Constrains TEvent parameter

- **Related to**:
  - [ADR-0010: EsAggregateRoot Event Sourcing Implementation](0010-esaggregate-root-event-sourcing-implementation.md) - Extends AggregateRoot
  - [ADR-0011: Async/Await Throughout](planned) - Why Apply() is synchronous

---

## Implementation Notes

### Usage Example: State-Sourced Aggregate

```csharp
public class Order : AggregateRoot<Guid, InternalDomainEvent>
{
    private List<OrderItem> _items = new();
    private OrderStatus _status = OrderStatus.Draft;

    public Order(Guid orderId, Guid customerId)
    {
        // Create construction event
        var created = new OrderCreated(
            Id: Guid.NewGuid(),
            OccurredOn: DateTimeOffset.UtcNow,
            Source: orderId.ToString(),
            CustomerId: customerId,
            Metadata: DomainEventMetadata.Empty
        );

        // Apply event (adds to collection, no state mutation)
        Apply(created);

        // Set aggregate state directly (state sourcing)
        Id = orderId;
        _status = OrderStatus.Created;
    }

    public void AddItem(string productId, int quantity)
    {
        // Create command event
        var itemAdded = new OrderItemAdded(
            Id: Guid.NewGuid(),
            OccurredOn: DateTimeOffset.UtcNow,
            Source: Id.ToString(),
            ProductId: productId,
            Quantity: quantity,
            Metadata: DomainEventMetadata.Empty
        );

        // Apply event
        Apply(itemAdded);

        // Mutate state directly (state sourcing)
        _items.Add(new OrderItem(productId, quantity));
    }

    // State sourcing: Load from current state
    public static Order LoadFromState(Guid id, OrderStatus status, List<OrderItem> items, long version)
    {
        var order = new Order(id, Guid.Empty);
        order._status = status;
        order._items = items;
        order.Version = version;
        order.ClearDomainEvents();  // No new events
        return order;
    }
}
```

### Repository Usage Pattern

```csharp
public class OrderRepository : IRepository<Order, Guid>
{
    public async Task SaveAsync(Order aggregate)
    {
        // Begin transaction
        // Save aggregate current state

        // Get domain events
        var events = aggregate.GetDomainEvents();

        // Save events to outbox
        await _outbox.SaveEventsAsync(events);

        // Note: Version is already updated by AddDomainEvent() calls
        // Repository performs optimistic locking check using current version

        // Clear events after successful save
        aggregate.ClearDomainEvents();

        // Commit transaction
    }
}
```

### Thread Safety Test

```csharp
[Fact]
public void AggregateRoot_ThreadSafe_ConcurrentEventAddition()
{
    var aggregate = new TestAggregate(Guid.NewGuid());
    var tasks = new List<Task>();

    // Spawn 100 tasks adding events concurrently
    for (int i = 0; i < 100; i++)
    {
        var taskId = i;
        tasks.Add(Task.Run(() =>
        {
            aggregate.AddTestEvent($"Event-{taskId}");
        }));
    }

    Task.WaitAll(tasks.ToArray());

    // All 100 events should be collected
    Assert.Equal(100, aggregate.GetDomainEventCount());
}
```

---

## References

- **Java ezddd Source**:
  - `path/to/local/checkout Frame\ezddd\ezddd-entity\src\main\java\tw\teddysoft\ezddd\entity\AggregateRoot.java`
  - `path/to/local/checkout Frame\ezddd\ezddd-entity\src\main\java\tw\teddysoft\ezddd\entity\DomainEventSource.java`

- **DDD Patterns**:
  - Evans, Eric. *Domain-Driven Design*. Addison-Wesley, 2003. Chapter 6: "Aggregates"
  - Vernon, Vaughn. *Implementing Domain-Driven Design*. Addison-Wesley, 2013. Chapter 10: "Aggregates"

- **Concurrency in .NET**:
  - [Threading (C#)](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/threading/)
  - [lock statement](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock)
  - [System.Collections.Concurrent Namespace](https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent)

- **Design Patterns**:
  - Gamma et al. *Design Patterns*. Addison-Wesley, 1994. "Template Method" pattern

- **Internal Documents**:
  - [DOTNET_PORT.md](../../DOTNET_PORT.md) - Section "核心介面 > 實體層 > AggregateRoot"
  - [CLAUDE.md](../../CLAUDE.md) - Section "Important Implementation Rules > Event Sourcing Implementation"

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2025-11-01 | Accepted    | Initial decision documented    |

---
