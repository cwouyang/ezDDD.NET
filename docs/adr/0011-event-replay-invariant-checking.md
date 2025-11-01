# ADR-0011: Event Replay and Invariant Checking

## Status

**Accepted**

- **Date**: 2025-11-01
- **Deciders**: Teddy Chen, Claude Code
- **Status Date**: 2025-11-01

---

## Context

### Problem Statement

Event-sourced aggregates must be reconstructed from event history when loaded from the event store. The replay mechanism must ensure:

1. **Correctness**: Replayed state must be identical to original state
2. **Invariant enforcement**: Business rules checked during replay to catch corrupted events
3. **Performance**: Replay should be efficient for aggregates with long event histories
4. **Error handling**: Clear diagnostics when replay fails
5. **Consistency**: Same Apply() logic used for new events and replay
6. **Reflection support**: Repositories must instantiate aggregates via reflection

Key design questions:
- Should invariants be checked during replay or skipped for performance?
- How do we handle invariant violations during replay?
- Should replay use the same `Apply()` method as new events?
- How do repositories instantiate aggregates with private constructors?
- Should we support snapshots for performance optimization?
- How do we handle event schema evolution during replay?

### Relevant Context

- **Java ezddd Reference**:
  - Replay constructor: `protected EsAggregateRoot(List<E> events)`
  - Uses same `apply()` method with full invariant checking
  - `replayEvents()` iterates and calls `apply()` for each event
  - Clears domain events after replay (prevents re-publication)
  - Uses reflection to instantiate aggregates: `Constructor.newInstance(events)`

- **Event Sourcing Best Practices**:
  - Replay must produce identical state (determinism)
  - Invariant checking during replay catches data corruption
  - Performance optimization via snapshots (load snapshot + apply subsequent events)
  - Event versioning/upcasting for schema evolution

- **.NET Reflection**:
  - `Activator.CreateInstance()` for simple instantiation
  - `ConstructorInfo.Invoke()` for constructor with parameters
  - Constructor caching for performance
  - Expression trees for even faster instantiation

### Constraints

- **Semantic Parity**: Must match Java ezddd's replay behavior (ADR-0005)
- **Correctness over Performance**: Invariant checking during replay is mandatory
- **Same Apply() Logic**: Replay uses same `Apply()` method as new events (ADR-0010)
- **Determinism**: Replay must produce identical state given same events
- **Error Transparency**: Clear error messages for debugging

---

## Decision

We will implement **event replay via protected constructor** accepting `IEnumerable<TEvent>`, using the **same `Apply()` method with full invariant checking**, and support **reflection-based instantiation** by repositories. Snapshots are **not included in initial version** but design allows future addition.

### Details

#### Replay Constructor Pattern

```csharp
namespace EzDdd.Entity;

public abstract class EsAggregateRoot<TId, TEvent> : AggregateRoot<TId, TEvent>
    where TEvent : InternalDomainEvent
{
    /// <summary>
    /// Protected parameterless constructor for new aggregate creation.
    /// Subclasses should provide private parameterless constructor and
    /// public factory methods.
    /// </summary>
    protected EsAggregateRoot()
    {
    }

    /// <summary>
    /// Public replay constructor for loading persisted aggregates.
    /// Repositories use reflection to invoke this constructor.
    /// </summary>
    /// <param name="events">The event history to replay</param>
    /// <remarks>
    /// <para>This constructor is PUBLIC to match Java ezddd's design.</para>
    /// <para><b>Replay Sequence:</b></para>
    /// <list type="number">
    ///   <item>Call parameterless constructor (initializes collections)</item>
    ///   <item>Call ReplayEvents() which applies each event via Apply()</item>
    ///   <item>Full invariant checking performed (R1/R2/R3 rules)</item>
    ///   <item>Clear domain events collection (prevent re-publication)</item>
    /// </list>
    /// <para>
    /// If any event violates invariants during replay, InvariantViolation
    /// exception is thrown with details about which event failed.
    /// </para>
    /// </remarks>
    public EsAggregateRoot(IEnumerable<TEvent> events)
        : this()
    {
        Contract.Require("Events cannot be null", () => events != null);

        try
        {
            ReplayEvents(events);
        }
        catch (Exception ex)
        {
            throw new AggregateReplayException(
                $"Failed to replay events for aggregate type {GetType().Name}",
                ex);
        }
        finally
        {
            // Always clear events, even on failure
            ClearDomainEvents();
        }
    }

    /// <summary>
    /// Replays a sequence of events to reconstruct aggregate state.
    /// Uses the same Apply() method with full R1/R2/R3 invariant checking.
    /// </summary>
    /// <param name="events">The events to replay</param>
    /// <remarks>
    /// This method is virtual to allow subclasses to customize replay behavior,
    /// such as adding event versioning/upcasting logic.
    /// </remarks>
    protected virtual void ReplayEvents(IEnumerable<TEvent> events)
    {
        var eventList = events.ToList();  // Materialize to detect errors early

        for (int i = 0; i < eventList.Count; i++)
        {
            var @event = eventList[i];
            try
            {
                Apply(@event);  // Uses sealed Apply() with R1/R2/R3 checking
            }
            catch (Exception ex)
            {
                throw new EventReplayException(
                    $"Failed to apply event {i + 1}/{eventList.Count} " +
                    $"of type {@event.GetType().Name} with ID {@event.Id}",
                    @event,
                    ex);
            }
        }
    }
}
```

#### Exception Types for Replay Errors

```csharp
namespace EzDdd.Entity;

/// <summary>
/// Exception thrown when aggregate replay fails.
/// </summary>
public class AggregateReplayException : Exception
{
    public AggregateReplayException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a specific event fails to apply during replay.
/// </summary>
public class EventReplayException : Exception
{
    /// <summary>
    /// Gets the event that failed to apply.
    /// </summary>
    public InternalDomainEvent FailedEvent { get; }

    public EventReplayException(
        string message,
        InternalDomainEvent failedEvent,
        Exception innerException)
        : base(message, innerException)
    {
        FailedEvent = failedEvent;
    }
}
```

#### Repository Reflection-Based Instantiation

```csharp
namespace EzDdd.UseCase;

/// <summary>
/// Generic event sourcing repository implementation.
/// </summary>
public class EsRepository<TAggregate, TId, TEvent> : IRepository<TAggregate, TId>
    where TAggregate : EsAggregateRoot<TId, TEvent>
    where TEvent : InternalDomainEvent
{
    private readonly IRepositoryPeer<DomainEventData, TId> _peer;
    private readonly DomainEventMapper _eventMapper;
    private static readonly ConstructorInfo? _replayConstructor;

    static EsRepository()
    {
        // Cache constructor for performance
        // Constructor is public (matches Java ezddd), so only Public flag needed
        _replayConstructor = typeof(TAggregate).GetConstructor(
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(IEnumerable<TEvent>) },
            null);

        if (_replayConstructor == null)
        {
            throw new InvalidOperationException(
                $"Aggregate type {typeof(TAggregate).Name} must have a public constructor " +
                $"accepting IEnumerable<{typeof(TEvent).Name}>");
        }
    }

    public async Task<TAggregate?> FindByIdAsync(TId id)
    {
        // Load events from event store via peer
        var eventDataList = await _peer.LoadEventsAsync(id);
        if (eventDataList == null || !eventDataList.Any())
            return null;

        // Deserialize events
        var events = eventDataList
            .Select(data => _eventMapper.ToEvent(data))
            .Cast<TEvent>()
            .ToList();

        // Instantiate aggregate via reflection
        var aggregate = (TAggregate)_replayConstructor!.Invoke(new object[] { events });

        // Set version from event count
        aggregate.Version = events.Count;

        return aggregate;
    }

    // SaveAsync, DeleteAsync implementations...
}
```

#### Invariant Checking During Replay - Rationale

**Why check invariants during replay?**

1. **Data Integrity**: Detects corrupted events in event store
2. **Migration Safety**: Catches errors when event schema changes
3. **Business Rule Evolution**: Ensures old events still satisfy current rules
4. **Debugging**: Clear error messages identify problematic events
5. **Consistency**: Same Apply() logic ensures identical behavior

**Performance Considerations**:
- Invariant checking is in-memory (fast)
- No I/O operations in EnsureInvariant()
- Typical aggregate: 10-100 events, negligible overhead
- For long histories: Use snapshots (future enhancement)

---

## Consequences

### Positive Consequences

- ✅ **Correctness guarantee**: Replay produces valid state or fails clearly
- ✅ **Data corruption detection**: Invalid events caught during load
- ✅ **Consistent behavior**: Same Apply() logic for new events and replay
- ✅ **Debuggable**: Exception chain shows exact event that failed
- ✅ **Type-safe**: Reflection checked at static initialization
- ✅ **Deterministic**: Same events always produce same state
- ✅ **Semantic parity**: Matches Java ezddd behavior

### Negative Consequences

- ❌ **Reflection overhead**: Constructor lookup via reflection (mitigated by caching)
- ❌ **Performance cost**: Invariant checking on every replayed event
- ❌ **No snapshot support**: Long event histories replay all events (future enhancement)

### Neutral Consequences

- ⚖️ **Constructor requirement**: Aggregates must have replay constructor (enforced at startup)
- ⚖️ **Event materialization**: ToList() call materializes IEnumerable (necessary for error reporting)
- ⚖️ **Virtual ReplayEvents()**: Allows customization but could be misused

---

## Alternatives Considered

### Alternative 1: Skip Invariant Checking During Replay

**Description**: Use flag to bypass EnsureInvariant() during replay for performance.

```csharp
private bool _isReplaying = false;

protected void ReplayEvents(IEnumerable<TEvent> events)
{
    _isReplaying = true;
    try
    {
        foreach (var e in events) Apply(e);
    }
    finally
    {
        _isReplaying = false;
    }
}

public sealed override void Apply(TEvent @event)
{
    if (!_isReplaying && @event is not IConstructionEvent)
        EnsureInvariant();  // Skip during replay

    When(@event);

    if (!_isReplaying && @event is not IDestructionEvent)
        EnsureInvariant();  // Skip during replay

    AddDomainEvent(@event);
}
```

**Pros**:
- Faster replay (no invariant checking)
- Reduced CPU usage

**Cons**:
- **Dangerous**: Corrupted events produce invalid aggregates silently
- **Breaks determinism**: Replay and new events behave differently
- **Debugging nightmare**: Invalid state only discovered later
- **Semantic parity violation**: Java ezddd checks invariants during replay

**Why rejected**:
- **Correctness over performance**: Invalid state is unacceptable
- Invariant checking is cheap (in-memory, no I/O)
- Data corruption must be detected immediately
- Snapshots are better performance solution

---

### Alternative 2: Separate ReplayApply() Method

**Description**: Different method for replay vs new events.

```csharp
public sealed override void Apply(TEvent @event)
{
    // Full invariant checking for new events
    if (@event is not IConstructionEvent) EnsureInvariant();
    When(@event);
    if (@event is not IDestructionEvent) EnsureInvariant();
    AddDomainEvent(@event);
}

protected void ReplayApply(TEvent @event)
{
    // Simplified for replay (no invariant checking)
    When(@event);
    // Don't add to domain events (already persisted)
}

protected virtual void ReplayEvents(IEnumerable<TEvent> events)
{
    foreach (var e in events) ReplayApply(e);  // Different method
}
```

**Pros**:
- Clear separation of concerns
- Can optimize replay separately

**Cons**:
- Breaks consistency (different code paths)
- Risk of divergence (Apply and ReplayApply drift apart)
- More code to maintain
- Still violates semantic parity

**Why rejected**:
- Single Apply() ensures consistent behavior
- Code duplication risk
- Semantic parity requires same logic

---

### Alternative 3: Expression Tree Compilation for Instantiation

**Description**: Use compiled expression trees instead of reflection.

```csharp
private static readonly Func<IEnumerable<TEvent>, TAggregate> _factory;

static EsRepository()
{
    var ctor = typeof(TAggregate).GetConstructor(/* ... */);
    var param = Expression.Parameter(typeof(IEnumerable<TEvent>), "events");
    var newExpr = Expression.New(ctor, param);
    var lambda = Expression.Lambda<Func<IEnumerable<TEvent>, TAggregate>>(newExpr, param);
    _factory = lambda.Compile();
}

public async Task<TAggregate?> FindByIdAsync(TId id)
{
    var events = /* load events */;
    var aggregate = _factory(events);  // Faster than reflection
    return aggregate;
}
```

**Pros**:
- 10-100x faster than reflection after compilation
- Near-native performance

**Cons**:
- More complex code
- Compilation overhead at startup
- Marginal benefit (aggregate loading not a hot path)
- Over-optimization for typical use case

**Why rejected**:
- Constructor invocation happens once per aggregate load
- Not a performance bottleneck (I/O dominates)
- Premature optimization
- Can add later if profiling shows need

---

### Alternative 4: Require Public Constructor

**Description**: Force public replay constructor instead of protected.

```csharp
public class Order : EsAggregateRoot<Guid, InternalDomainEvent>
{
    // PUBLIC constructor
    public Order(IEnumerable<InternalDomainEvent> events)
        : base(events)
    {
    }
}
```

**Pros**:
- No reflection needed (can use Activator.CreateInstance)
- Slightly faster instantiation
- Simpler code

**Cons**:
- Exposes constructor to domain layer users (confusion)
- Users might call constructor directly (bypass factory methods)
- Violates encapsulation (aggregate creation should use factory methods)
- Semantic parity: Java uses protected constructor

**Why rejected**:
- Aggregates should enforce creation via factory methods
- Replay constructor is infrastructure concern, not domain API
- Reflection overhead is acceptable

---

### Alternative 5: Include Snapshot Support in Initial Design

**Description**: Add snapshot mechanism from the start.

```csharp
public abstract class EsAggregateRoot<TId, TEvent>
{
    protected EsAggregateRoot(
        IEnumerable<TEvent> events,
        Snapshot? snapshot = null)
    {
        if (snapshot != null)
        {
            RestoreFromSnapshot(snapshot);
            // Apply only events after snapshot
            var subsequentEvents = events.Where(e => e.Version > snapshot.Version);
            ReplayEvents(subsequentEvents);
        }
        else
        {
            ReplayEvents(events);
        }
    }

    protected abstract void RestoreFromSnapshot(Snapshot snapshot);
    protected abstract Snapshot CreateSnapshot();
}
```

**Pros**:
- Performance optimization for long event histories
- Reduces replay time

**Cons**:
- Significantly more complex
- Snapshot serialization concerns
- Snapshot invalidation on business rule changes
- Not needed for most aggregates (typical: <100 events)
- Can add later without breaking changes

**Why rejected**:
- YAGNI (You Aren't Gonna Need It) - most aggregates don't need snapshots
- Adds significant complexity to initial implementation
- Can be added later as opt-in enhancement
- Semantic parity: Java ezddd doesn't include snapshots in base class

---

## Related Decisions

- **Depends on**:
  - [ADR-0008: IDomainEvent Hierarchy](0008-idomain-event-hierarchy.md) - Events must be InternalDomainEvent
  - [ADR-0010: EsAggregateRoot Event Sourcing Implementation](0010-esaggregate-root-event-sourcing-implementation.md) - Defines Apply() and R1/R2/R3 rules

- **Related to**:
  - [ADR-0016: Reflection for Aggregate Reconstruction](planned) - Details constructor caching and optimization
  - [ADR-0018: Exception Hierarchy Design](planned) - AggregateReplayException and EventReplayException

---

## Implementation Notes

### Complete Replay Flow

```
Repository.FindByIdAsync(id)
    ↓
Load DomainEventData[] from event store
    ↓
Deserialize to TEvent[]
    ↓
Reflect to find constructor(IEnumerable<TEvent>)
    ↓
Invoke constructor with events
    ↓
    Inside Constructor:
        ↓
    Call base(events)
        ↓
    Call this() (parameterless constructor)
        ↓
    Initialize collections/state
        ↓
    Call ReplayEvents(events)
        ↓
        For each event:
            ↓
        Call Apply(event)
            ↓
        [R1/R2/R3 invariant checking]
            ↓
        Call When(event)
            ↓
        [State mutation]
            ↓
        AddDomainEvent(event)
        ↓
    Clear domain events (prevent re-publication)
        ↓
    Set Version = event count
        ↓
Return aggregate
```

### Error Handling Example

```csharp
[Fact]
public async Task FindByIdAsync_CorruptedEvent_ThrowsReplayException()
{
    var orderId = Guid.NewGuid();
    var events = new List<InternalDomainEvent>
    {
        new OrderCreated(/* valid construction */),
        new OrderItemAdded(/* INVALID: negative quantity */),
    };

    // This will throw during replay
    var exception = await Assert.ThrowsAsync<AggregateReplayException>(
        async () => await _repository.FindByIdAsync(orderId)
    );

    // Exception chain provides debugging context
    Assert.IsType<EventReplayException>(exception.InnerException);
    var replayEx = (EventReplayException)exception.InnerException;
    Assert.IsType<OrderItemAdded>(replayEx.FailedEvent);
    Assert.Contains("negative quantity", exception.Message);
}
```

### Performance Test

```csharp
[Fact]
public void Replay_LargeEventHistory_PerformanceAcceptable()
{
    var events = Enumerable.Range(0, 1000)
        .Select(i => new OrderItemAdded(/* event data */))
        .Cast<InternalDomainEvent>()
        .ToList();

    // Prepend construction event
    events.Insert(0, new OrderCreated(/* ... */));

    var stopwatch = Stopwatch.StartNew();
    var order = new Order(events);
    stopwatch.Stop();

    // 1000 events should replay in under 100ms
    Assert.True(stopwatch.ElapsedMilliseconds < 100,
        $"Replay took {stopwatch.ElapsedMilliseconds}ms");
}
```

### Custom Replay Logic (Event Upcasting)

```csharp
public class Order : EsAggregateRoot<Guid, InternalDomainEvent>
{
    protected override void ReplayEvents(IEnumerable<InternalDomainEvent> events)
    {
        // Upcast old event versions during replay
        var upcastedEvents = events.Select(e => e switch
        {
            OrderCreatedV1 old => new OrderCreated(/* map fields */),
            OrderItemAddedV1 old => new OrderItemAdded(/* map fields */),
            _ => e  // No upcasting needed
        });

        base.ReplayEvents(upcastedEvents);
    }
}
```

---

## References

- **Java ezddd Source**:
  - `path/to/local/checkout Frame\ezddd\ezddd-entity\src\main\java\tw\teddysoft\ezddd\entity\EsAggregateRoot.java`

- **Event Sourcing References**:
  - Young, Greg. ["Versioning in an Event Sourced System"](https://leanpub.com/esversioning)
  - Fowler, Martin. ["Event Sourcing"](https://martinfowler.com/eaaDev/EventSourcing.html)
  - Vernon, Vaughn. *Implementing Domain-Driven Design*. Chapter 8: "Domain Events"

- **.NET Reflection**:
  - [Reflection (C#)](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/reflection)
  - [ConstructorInfo Class](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.constructorinfo)
  - [Expression Trees](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/expression-trees/)

- **Performance**:
  - Richter, Jeffrey. *CLR via C#*. Chapter on "Reflection" performance characteristics

- **Internal Documents**:
  - [DOTNET_PORT.md](../../DOTNET_PORT.md) - Section "技術實作細節 > 1. 事件溯源實作"
  - [CLAUDE.md](../../CLAUDE.md) - Section "Event Sourcing Implementation"

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2025-11-01 | Accepted    | Initial decision documented    |

---
