# ADR-0007: IEntity and IValueObject Design

## Status

**Accepted**

- **Date**: 2025-11-01
- **Deciders**: Teddy Chen, Claude Code
- **Status Date**: 2025-11-01

---

## Context

### Problem Statement

ezDDD.NET needs to define the foundational interfaces for Domain-Driven Design (DDD) tactical patterns: `IEntity<TId>` and `IValueObject`. These interfaces serve as the building blocks for all domain objects and must balance simplicity with clear semantic meaning.

The key design questions are:
1. Should we use **interface** or **abstract class**?
2. What members should these types expose?
3. How do we handle generic type parameters for entity identity?
4. Should we include .NET-specific features like `IEquatable<T>` or serialization markers?

### Relevant Context

- **Java ezddd Reference**: Uses `Entity<ID>` and `ValueObject` as marker interfaces
  - `Entity<ID>` has single method: `ID getId()`
  - `ValueObject` is a pure marker interface (zero methods)
  - Both extend `Serializable` for Java persistence compatibility
- **.NET Platform**: Has different conventions than Java
  - No `Serializable` marker (different serialization mechanisms)
  - Generic type parameters conventionally use `T` prefix (e.g., `TId`)
  - Properties preferred over getter methods
  - `IEquatable<T>` available for value equality
- **DDD Philosophy**:
  - Entities defined by unique identity, not attributes
  - Value objects defined by attributes, no unique identity
  - Value objects should be immutable

### Constraints

- **Semantic Parity**: Must maintain Java ezddd's design philosophy (ADR-0005)
- **.NET Conventions**: Must follow .NET naming and API design standards (ADR-0002)
- **Zero Dependencies**: Cannot introduce third-party dependencies (ADR-0004)
- **Minimal Surface**: Should be minimal marker types, not prescriptive frameworks
- **Type Safety**: Must leverage .NET's type system for compile-time safety

---

## Decision

We will implement **`IEntity<TId>`** and **`IValueObject`** as minimal marker interfaces following .NET conventions while preserving Java ezddd's design philosophy.

### Details

#### IEntity<TId> Interface

```csharp
namespace EzDdd.Entity;

/// <summary>
/// Marker interface for entities with unique identity.
/// An entity is defined by its unique identifier, not by its attributes.
/// Two entities with the same ID are considered the same entity,
/// regardless of attribute differences.
/// </summary>
/// <typeparam name="TId">The type of the entity's unique identifier</typeparam>
public interface IEntity<out TId>
{
    /// <summary>
    /// Gets the unique identifier of this entity.
    /// </summary>
    TId Id { get; }
}
```

**Design Rationale**:
- **Interface, not abstract class**: Maximum flexibility for inheritance hierarchies
- **Single property `Id { get; }`**: Minimal contract - entities MUST have unique identity
- **Covariant type parameter `<out TId>`**: Enables type variance for read-only scenarios
- **No `IEquatable<T>`**: Entity equality is complex (same ID = same entity, but state may differ). Leave equality to aggregate implementations.
- **No serialization markers**: .NET serialization is opt-in via attributes, not marker interfaces

#### IValueObject Interface

```csharp
namespace EzDdd.Entity;

/// <summary>
/// Marker interface for value objects.
/// A value object is defined by its attributes, not by a unique identity.
/// Two value objects with identical attribute values are considered equal.
/// Value objects should be immutable.
/// </summary>
/// <remarks>
/// Prefer using <see langword="record"/> types for value object implementations,
/// as records provide structural equality and immutability by default.
/// </remarks>
public interface IValueObject
{
    // Pure marker interface - zero methods
}
```

**Design Rationale**:
- **Pure marker interface**: Zero methods, maximum flexibility
- **No `IEquatable<T>`**: `record` types provide structural equality automatically
- **Documentation-driven immutability**: XML comments guide developers to use `record` types
- **Semantic clarity**: Marks intent without imposing implementation constraints

#### Generic Type Parameter Naming

Following .NET conventions (ADR-0002):
- Java: `Entity<ID>` → .NET: `IEntity<TId>`
- Rationale: .NET convention uses `T` prefix for generic parameters
- Consistency: `TId`, `TEvent`, `TAggregate`, `TInput`, `TOutput` throughout codebase

#### No Serialization Markers

Unlike Java's `Serializable`:
- .NET serialization is **opt-in** via `[Serializable]` attribute or JSON serialization
- No need for marker interfaces
- Allows flexibility in choosing serialization strategy (System.Text.Json, protobuf, etc.)

---

## Consequences

### Positive Consequences

- ✅ **Minimal and flexible**: Does not constrain implementation details
- ✅ **Type-safe**: Generics provide compile-time identity type checking
- ✅ **Semantic clarity**: Clearly marks entities vs value objects
- ✅ **.NET idiomatic**: Follows .NET property conventions
- ✅ **Covariance support**: `<out TId>` enables type variance
- ✅ **Record-friendly**: Works seamlessly with C# `record` types for value objects
- ✅ **Framework-agnostic**: No assumptions about persistence or serialization

### Negative Consequences

- ❌ **No enforced immutability**: `IValueObject` cannot enforce immutability at compile time
- ❌ **No enforced equality**: Developers must implement equality for value objects manually (mitigated by recommending `record` types)
- ❌ **Marker only**: Does not provide helper methods or base implementations

### Neutral Consequences

- ⚖️ **Developer discipline required**: Relies on developers following conventions (immutability, equality)
- ⚖️ **Documentation-driven**: Uses XML comments to guide proper usage
- ⚖️ **Minimalist philosophy**: Intentionally minimal to avoid over-engineering

---

## Alternatives Considered

### Alternative 1: Abstract Base Classes

**Description**: Use `Entity<TId>` and `ValueObject<T>` abstract base classes instead of interfaces.

**Example**:
```csharp
public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;

    public override bool Equals(object? obj) { /* ID-based equality */ }
    public override int GetHashCode() { /* ID-based hash */ }
}

public abstract class ValueObject<T> : IEquatable<T>
{
    public abstract bool Equals(T? other);
    // Force structural equality implementation
}
```

**Pros**:
- Enforces equality implementation
- Provides default implementations
- Central place for common behavior

**Cons**:
- Restricts inheritance hierarchy (C# single inheritance)
- More prescriptive (less flexibility)
- Harder to integrate with existing class hierarchies

**Why rejected**:
- Violates "minimal marker" philosophy of Java ezddd
- C# single inheritance is too restrictive
- `record` types already provide value equality
- Over-engineering for simple concept

---

### Alternative 2: IEntity<TId> with IEquatable<IEntity<TId>>

**Description**: Force entity equality implementation via interface.

**Example**:
```csharp
public interface IEntity<TId> : IEquatable<IEntity<TId>>
{
    TId Id { get; }
    // Forces implementation of Equals(IEntity<TId>? other)
}
```

**Pros**:
- Compile-time enforcement of equality
- Consistent equality semantics

**Cons**:
- Entity equality is nuanced (ID equality ≠ state equality)
- Different aggregate types need different equality
- Over-prescriptive for a marker interface

**Why rejected**:
- Entity equality is context-dependent (version, state, etc.)
- Aggregate roots will implement their own equality logic
- Marker interface should not impose implementation contracts

---

### Alternative 3: Structural Typing (Duck Typing)

**Description**: No marker interfaces at all - rely on structural compatibility.

**Example**:
```csharp
// No IEntity or IValueObject
// Just use classes/records with Id property or values

public class User
{
    public Guid Id { get; set; }
}
```

**Pros**:
- Ultimate flexibility
- No framework constraints
- POCO-friendly

**Cons**:
- No semantic clarity
- Loses type-checking benefits
- Cannot constrain generic types (e.g., `where T : IEntity<TId>`)
- Breaks with Java ezddd design philosophy

**Why rejected**:
- Violates semantic parity requirement (ADR-0005)
- Loses type safety
- Cannot express DDD concepts clearly in code

---

### Alternative 4: Add Serialization Attributes

**Description**: Include `[Serializable]` or other serialization markers.

**Example**:
```csharp
[Serializable]
public interface IEntity<TId>
{
    TId Id { get; }
}
```

**Pros**:
- Marks objects as serializable
- Mirrors Java's `Serializable`

**Cons**:
- .NET serialization is opt-in, not marker-based
- Restricts serialization strategy choices
- Unnecessary in modern .NET (System.Text.Json, records)

**Why rejected**:
- .NET serialization works differently than Java
- Forces specific serialization approach
- Violates zero-dependency principle (serialization should be external concern)

---

## Related Decisions

- **Depends on**:
  - [ADR-0002: Package Naming and Structure](0002-package-naming-and-structure.md) - Establishes `IEntity` prefix convention and `TId` naming
  - [ADR-0003: Module Architecture and Dependency Chain](0003-module-architecture-dependency-chain.md) - Places these interfaces in EzDdd.Entity module
  - [ADR-0005: Complete Reimplementation Approach](0005-complete-reimplementation-approach.md) - Requires semantic parity with Java ezddd

- **Related to**:
  - [ADR-0008: IDomainEvent Hierarchy](0008-idomain-event-hierarchy.md) - Domain events are similar marker types
  - [ADR-0009: AggregateRoot Base Class Design](0009-aggregate-root-base-class-design.md) - Aggregates implement IEntity<TId>
  - ADR-0013: Record Types for Immutability (planned for future ADR stage) - Records are recommended for IValueObject implementations

---

## Implementation Notes

### For Developers Implementing Entities

```csharp
// Entity example - NOT a value object (identity matters)
public class Order : IEntity<Guid>
{
    public Guid Id { get; private set; }
    public string CustomerName { get; private set; }
    public decimal TotalAmount { get; private set; }

    // Entity equality based on ID
    public override bool Equals(object? obj) =>
        obj is Order other && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}
```

### For Developers Implementing Value Objects

```csharp
// Value object - prefer record types for structural equality
public record Money(decimal Amount, string Currency) : IValueObject
{
    // Record provides structural equality automatically
    // Money(100, "USD") == Money(100, "USD") → true
}

// Alternative: class-based value object (more work)
public class Email : IValueObject
{
    public string Value { get; }

    public Email(string value)
    {
        // Validation
        Value = value;
    }

    // Must implement Equals/GetHashCode for structural equality
    public override bool Equals(object? obj) =>
        obj is Email other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();
}
```

### Generic Constraints Usage

```csharp
// Constrain generic types to entities
public interface IRepository<TAggregate, TId>
    where TAggregate : IEntity<TId>
{
    Task<TAggregate?> FindByIdAsync(TId id);
}

// Covariance example
IEntity<Guid> entity = new Order();  // Works due to <out TId>
```

---

## References

- **Java ezddd Source**:
  - `ezddd-entity/src/main/java/tw/teddysoft/ezddd/entity/Entity.java`
  - `ezddd-entity/src/main/java/tw/teddysoft/ezddd/entity/ValueObject.java`

- **DDD Reference**:
  - Evans, Eric. *Domain-Driven Design: Tackling Complexity in the Heart of Software*. Addison-Wesley, 2003.
  - Chapter 5: "A Model Expressed in Software" (Entity vs Value Object distinction)

- **.NET Design Guidelines**:
  - [Interface Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/interface-design)
  - [Generic Type Parameter Naming](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-type-parameters)
  - [Covariance and Contravariance](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/covariance-contravariance/)

- **Internal Documents**:
  - Internal porting notes (not retained) - Section "API 設計 > 核心介面 > 實體層"
  - [CLAUDE.md](../../CLAUDE.md) - Section "Module Architecture > EzDdd.Entity"

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2025-11-01 | Accepted    | Initial decision documented    |

---
