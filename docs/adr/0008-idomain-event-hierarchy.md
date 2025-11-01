# ADR-0008: IDomainEvent Hierarchy Design

## Status

**Accepted**

- **Date**: 2025-11-01
- **Deciders**: Teddy Chen, Claude Code
- **Status Date**: 2025-11-01

---

## Context

### Problem Statement

ezDDD.NET needs a domain event hierarchy that supports both Event Sourcing and CQRS patterns. The design must:

1. **Distinguish event origins**: Events within a bounded context vs. events from external systems
2. **Support event sourcing lifecycle**: Identify construction (creation) and destruction (deletion) events
3. **Provide event metadata**: Event identity, timestamp, source aggregate, and contextual information
4. **Enable type-safe event handling**: Allow pattern matching and compile-time type checking

Key design questions:
- How should we structure the event hierarchy?
- What properties should all domain events expose?
- How do we mark construction and destruction events for event sourcing correctness?
- Should we use interfaces or abstract classes?
- How do we handle event metadata extensibility?

### Relevant Context

- **Java ezddd Reference**:
  - `DomainEvent` base interface: `id()`, `occurredOn()`, `source()`, `metadata()`
  - `InternalDomainEvent`: Events within bounded context
    - `ConstructionEvent`: Marker for first event (aggregate creation)
    - `DestructionEvent`: Marker for last event (aggregate deletion)
  - `ExternalDomainEvent`: Events from/to other bounded contexts
  - Uses nested marker interfaces for lifecycle events

- **.NET Platform Features**:
  - `record` types ideal for immutable events
  - Pattern matching enables elegant event handling
  - `DateTimeOffset` preferred over `DateTime` for timezone-aware timestamps
  - `IReadOnlyDictionary<K,V>` for immutable metadata

- **Event Sourcing Requirements**:
  - R1 (Construction): First event establishes aggregate
  - R2 (Command): Middle events maintain invariants
  - R3 (Destruction): Last event finalizes deletion
  - Event order and type must be enforceable

- **CQRS Requirements**:
  - Events trigger projections and read model updates
  - Events may be published to message buses
  - Metadata needed for correlation, causation, tracing

### Constraints

- **Semantic Parity**: Must match Java ezddd's event sourcing semantics (ADR-0005)
- **.NET Conventions**: Follow .NET naming (ADR-0002) and use modern C# features (ADR-0001)
- **Zero Dependencies**: No third-party event libraries (ADR-0004)
- **Immutability**: Events must be immutable (past cannot change)
- **Type Safety**: Leverage .NET type system for compile-time safety

---

## Decision

We will implement a **three-level interface hierarchy** for domain events: `IDomainEvent` (root), `InternalDomainEvent` and `ExternalDomainEvent` (context distinction), with nested marker interfaces for lifecycle events.

### Details

#### Level 1: IDomainEvent (Root Interface)

```csharp
namespace EzDdd.Entity;

/// <summary>
/// Base interface for all domain events.
/// A domain event represents something that happened in the domain
/// that domain experts care about.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of this event.
    /// Each event instance has a unique ID, even if multiple events
    /// represent the same domain occurrence.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the timestamp when this event occurred.
    /// Uses <see cref="DateTimeOffset"/> for timezone-aware timestamps.
    /// </summary>
    DateTimeOffset OccurredOn { get; }

    /// <summary>
    /// Gets the identifier of the aggregate that produced this event.
    /// For construction events, this is the ID of the newly created aggregate.
    /// </summary>
    string Source { get; }

    /// <summary>
    /// Gets the metadata associated with this event.
    /// Common metadata includes: correlation ID, causation ID, user ID,
    /// trace context, etc.
    /// </summary>
    IReadOnlyDictionary<string, string> Metadata { get; }
}
```

**Design Rationale**:
- **`Guid Id`**: Globally unique event identifier (not aggregate ID)
- **`DateTimeOffset OccurredOn`**: Timezone-aware timestamp (.NET best practice)
- **`string Source`**: Aggregate ID as string (flexible identity format)
- **`IReadOnlyDictionary<string, string> Metadata`**: Extensible key-value pairs for correlation, causation, tracing

#### Level 2a: InternalDomainEvent (Bounded Context Events)

```csharp
namespace EzDdd.Entity;

/// <summary>
/// Marker interface for domain events that occur within a single bounded context.
/// Internal events are used for event sourcing and maintaining aggregate state.
/// </summary>
public interface InternalDomainEvent : IDomainEvent
{
    /// <summary>
    /// Marker interface for construction events.
    /// A construction event MUST be the first event in an event-sourced
    /// aggregate's lifecycle. It establishes the aggregate's initial state
    /// and identity.
    /// </summary>
    /// <remarks>
    /// Event sourcing rule R1: Construction events do not have precondition
    /// invariant checks, but must satisfy postcondition invariants.
    /// </remarks>
    public interface IConstructionEvent
    {
        // Pure marker interface - no additional members
    }

    /// <summary>
    /// Marker interface for destruction events.
    /// A destruction event MUST be the last event in an event-sourced
    /// aggregate's lifecycle. It represents the aggregate's deletion
    /// or termination.
    /// </summary>
    /// <remarks>
    /// Event sourcing rule R3: Destruction events must satisfy precondition
    /// invariants, but do not have postcondition invariant checks.
    /// </remarks>
    public interface IDestructionEvent
    {
        // Pure marker interface - no additional members
    }
}
```

**Design Rationale**:
- **Marker interface**: Extends `IDomainEvent` but adds no new members
- **Nested marker interfaces**: `IConstructionEvent` and `IDestructionEvent` for lifecycle semantics
- **Semantic clarity**: Clearly distinguishes events within a bounded context
- **Event sourcing support**: Enables R1/R2/R3 rule enforcement

#### Level 2b: ExternalDomainEvent (Deferred to Phase 3)

> **NOTE**: `ExternalDomainEvent` belongs to the `EzDdd.UseCase` module (integration layer), not the `EzDdd.Entity` module (domain layer). This matches the Java ezddd architecture where `ExternalDomainEvent` is located in the `ezddd-usecase` module.
>
> Design and implementation of `ExternalDomainEvent` will be documented in a future ADR during Phase 3 (EzDdd.UseCase implementation).
>
> **TODO**: Write ADR for ExternalDomainEvent in Phase 3 (UseCase layer)

#### Event Hierarchy Diagram

```
IDomainEvent (root)
└── InternalDomainEvent (within bounded context)
    ├── IConstructionEvent (nested marker - first event)
    └── IDestructionEvent (nested marker - last event)

Note: ExternalDomainEvent will be defined in EzDdd.UseCase module (Phase 3)
```

#### Usage with Record Types

```csharp
// Construction event example (first event, creates aggregate)
public record OrderCreated(
    Guid Id,
    DateTimeOffset OccurredOn,
    string Source,  // OrderId
    Guid CustomerId,
    IReadOnlyDictionary<string, string> Metadata
) : InternalDomainEvent, InternalDomainEvent.IConstructionEvent;

// Command event example (middle events, maintain state)
public record OrderItemAdded(
    Guid Id,
    DateTimeOffset OccurredOn,
    string Source,  // OrderId
    string ProductId,
    int Quantity,
    IReadOnlyDictionary<string, string> Metadata
) : InternalDomainEvent;

// Destruction event example (last event, deletes aggregate)
public record OrderCancelled(
    Guid Id,
    DateTimeOffset OccurredOn,
    string Source,  // OrderId
    string Reason,
    IReadOnlyDictionary<string, string> Metadata
) : InternalDomainEvent, InternalDomainEvent.IDestructionEvent;

// Note: ExternalDomainEvent examples will be provided in Phase 3 (UseCase layer)
```

---

## Consequences

### Positive Consequences

- ✅ **Clear event lifecycle**: Construction/Destruction markers enable R1/R2/R3 enforcement
- ✅ **Immutable by default**: `record` types provide structural immutability
- ✅ **Pattern matching friendly**: Hierarchy enables clean switch expressions
- ✅ **Metadata extensibility**: Dictionary allows arbitrary contextual information
- ✅ **Timezone-aware**: `DateTimeOffset` prevents timezone bugs
- ✅ **Type-safe**: Compile-time checking of event types
- ✅ **Improved immutability**: `IReadOnlyDictionary<string, string>` for Metadata (vs Java's mutable `Map<String, String>`) - intentional improvement for event immutability

### Negative Consequences

- ❌ **Metadata type limitation**: `IReadOnlyDictionary<string, string>` limits values to strings (no complex objects)
- ❌ **No enforced event order**: Framework cannot enforce construction-first/destruction-last at compile time
- ❌ **Marker interface verbosity**: Multiple interface implementations required for lifecycle events

### Neutral Consequences

- ⚖️ **Developer discipline required**: Must remember to mark construction/destruction events
- ⚖️ **Record boilerplate**: Event definitions require repetitive property declarations
- ⚖️ **Nested interface syntax**: `InternalDomainEvent.IConstructionEvent` is slightly verbose

---

## Alternatives Considered

### Alternative 1: Abstract Base Classes

**Description**: Use abstract classes instead of interfaces for event hierarchy.

```csharp
public abstract class DomainEvent
{
    public Guid Id { get; init; }
    public DateTimeOffset OccurredOn { get; init; }
    public string Source { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
}

public abstract class InternalDomainEvent : DomainEvent { }
public abstract class ConstructionEvent : InternalDomainEvent { }
public abstract class DestructionEvent : InternalDomainEvent { }
```

**Pros**:
- Provides default implementations
- Single inheritance chain
- Enforces property presence

**Cons**:
- Prevents event multi-inheritance (e.g., event being both Internal and External)
- Less flexible than interfaces
- Harder to integrate with existing class hierarchies
- Record types cannot extend abstract classes with state

**Why rejected**:
- Violates semantic parity with Java ezddd (uses interfaces)
- C# single inheritance too restrictive
- Record types work better with interfaces
- Over-prescriptive for marker types

---

### Alternative 2: Enum-Based Event Classification

**Description**: Use enum property instead of marker interfaces.

```csharp
public enum EventType { Construction, Command, Destruction }

public interface IDomainEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredOn { get; }
    string Source { get; }
    EventType Type { get; }  // Classification via enum
    IReadOnlyDictionary<string, string> Metadata { get; }
}
```

**Pros**:
- Simple classification
- No multiple interface implementations
- Easy to check event type

**Cons**:
- Runtime checking instead of compile-time
- Cannot use type system for constraints
- Breaks with Java ezddd philosophy
- Enum values can be misassigned

**Why rejected**:
- Loses compile-time type safety
- Cannot constrain generic types (e.g., `where T : IConstructionEvent`)
- Violates semantic parity requirement
- Marker interfaces more idiomatic in DDD

---

### Alternative 3: Attribute-Based Markers

**Description**: Use attributes instead of marker interfaces.

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class ConstructionEventAttribute : Attribute { }

[ConstructionEvent]
public record OrderCreated(...) : InternalDomainEvent;
```

**Pros**:
- No additional interface implementations
- Cleaner event definitions
- Metadata-driven approach

**Cons**:
- Requires reflection to check markers (performance cost)
- No compile-time type constraints
- Cannot use in generic `where` clauses
- Non-idiomatic for marker types in .NET

**Why rejected**:
- Reflection overhead in hot paths (Apply loop)
- Loses type safety
- Attributes not suitable for type constraints
- Interfaces more idiomatic

---

### Alternative 4: DateTime Instead of DateTimeOffset

**Description**: Use `DateTime` for `OccurredOn` property.

```csharp
public interface IDomainEvent
{
    DateTime OccurredOn { get; }  // Instead of DateTimeOffset
}
```

**Pros**:
- Simpler type
- Matches Java's `Instant` somewhat
- Less verbose

**Cons**:
- Timezone ambiguity (Kind property confusion)
- UTC vs local time issues
- Not recommended by .NET guidelines

**Why rejected**:
- `DateTimeOffset` is .NET best practice for timestamps
- Prevents timezone-related bugs
- Explicit timezone handling
- Recommended by Microsoft design guidelines

---

### Alternative 5: Strongly-Typed Metadata

**Description**: Use generic metadata type instead of string dictionary.

```csharp
public interface IDomainEvent<TMetadata>
{
    TMetadata Metadata { get; }
}
```

**Pros**:
- Type-safe metadata access
- No dictionary lookups
- Compile-time checking

**Cons**:
- Complicates event hierarchy
- Different events need different metadata types
- Harder to work with generic event handlers
- Breaks with Java ezddd (uses `Map<String, String>`)

**Why rejected**:
- Over-complicates type system
- Dictionary provides sufficient flexibility
- Semantic parity with Java requires string map
- Generic metadata constrains extensibility

---

## Related Decisions

- **Depends on**:
  - [ADR-0001: Target Framework](0001-target-framework.md) - .NET 8 enables `record` types and modern C# features
  - [ADR-0002: Package Naming and Structure](0002-package-naming-and-structure.md) - Establishes namespace conventions
  - [ADR-0005: Complete Reimplementation Approach](0005-complete-reimplementation-approach.md) - Requires semantic parity with Java ezddd
  - [ADR-0007: IEntity and IValueObject Design](0007-ientity-ivalueobject-design.md) - Establishes marker interface pattern

- **Related to**:
  - [ADR-0009: AggregateRoot Base Class Design](0009-aggregate-root-base-class-design.md) - Aggregates raise internal domain events
  - [ADR-0010: EsAggregateRoot Event Sourcing Implementation](0010-esaggregate-root-event-sourcing-implementation.md) - R1/R2/R3 rules depend on construction/destruction markers
  - [ADR-0013: Record Types for Immutability](planned) - Events should be implemented as records

---

## Implementation Notes

### Event Creation Helper (Optional)

```csharp
// Helper for creating metadata
public static class DomainEventMetadata
{
    public static IReadOnlyDictionary<string, string> Create(
        string? correlationId = null,
        string? causationId = null,
        string? userId = null)
    {
        var metadata = new Dictionary<string, string>();
        if (correlationId != null) metadata["CorrelationId"] = correlationId;
        if (causationId != null) metadata["CausationId"] = causationId;
        if (userId != null) metadata["UserId"] = userId;
        return metadata;
    }

    public static readonly IReadOnlyDictionary<string, string> Empty =
        new Dictionary<string, string>();
}
```

### Pattern Matching in When() Method

```csharp
protected override void When(InternalDomainEvent @event)
{
    switch (@event)
    {
        case OrderCreated e:
            _orderId = OrderId.Parse(e.Source);
            _customerId = e.CustomerId;
            _items = new List<OrderItem>();
            break;

        case OrderItemAdded e:
            _items.Add(new OrderItem(e.ProductId, e.Quantity));
            break;

        case OrderCancelled e:
            _status = OrderStatus.Cancelled;
            _cancellationReason = e.Reason;
            break;

        default:
            throw new InvalidOperationException(
                $"Unknown event type: {@event.GetType().Name}");
    }
}
```

### Testing Construction/Destruction Markers

```csharp
[Fact]
public void OrderCreated_ImplementsConstructionEvent()
{
    var @event = new OrderCreated(/* ... */);

    Assert.IsAssignableFrom<InternalDomainEvent.IConstructionEvent>(@event);
}

[Fact]
public void OrderCancelled_ImplementsDestructionEvent()
{
    var @event = new OrderCancelled(/* ... */);

    Assert.IsAssignableFrom<InternalDomainEvent.IDestructionEvent>(@event);
}
```

---

## References

- **Java ezddd Source**:
  - `path/to/local/checkout Frame\ezddd\ezddd-entity\src\main\java\tw\teddysoft\ezddd\entity\DomainEvent.java`
  - `path/to/local/checkout Frame\ezddd\ezddd-entity\src\main\java\tw\teddysoft\ezddd\entity\InternalDomainEvent.java`
  - `path/to/local/checkout Frame\ezddd\ezddd-entity\src\main\java\tw\teddysoft\ezddd\entity\ExternalDomainEvent.java`

- **Event Sourcing**:
  - Vernon, Vaughn. *Implementing Domain-Driven Design*. Addison-Wesley, 2013. Chapter 8: "Domain Events"
  - Young, Greg. ["Event Sourcing"](https://cqrs.wordpress.com/documents/events-as-storage-mechanism/)
  - Fowler, Martin. ["Event Sourcing"](https://martinfowler.com/eaaDev/EventSourcing.html)

- **.NET Design Guidelines**:
  - [Choosing Between DateTime, DateTimeOffset, TimeSpan, and TimeZoneInfo](https://learn.microsoft.com/en-us/dotnet/standard/datetime/choosing-between-datetime)
  - [Record Types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
  - [Pattern Matching](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/pattern-matching)

- **Internal Documents**:
  - [DOTNET_PORT.md](../../DOTNET_PORT.md) - Section "核心介面 > 實體層 > IDomainEvent"
  - [CLAUDE.md](../../CLAUDE.md) - Section "Module Architecture > EzDdd.Entity"

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2025-11-01 | Accepted    | Initial decision documented    |

---
