# ADR-0021: Generic Variance Annotations for CQRS Interfaces

## Status

**Accepted**

- **Date**: 2025-11-18
- **Deciders**: Development Team
- **Status Date**: 2025-11-18

---

## Context

### Problem Statement

Which CQRS interfaces should use C# generic variance annotations (`in` for contravariance, `out` for covariance) to maximize type flexibility while maintaining type safety?

### Relevant Context

- **Java limitation**: Java does not support declaration-site variance (only use-site variance with wildcards)
- **C# advantage**: Supports declaration-site variance with `in` (contravariant) and `out` (covariant) keywords
- **Phase 3 precedent**: `IUseCase<in TInput, TOutput>` established contravariant input pattern
- **CQRS interfaces**: ICommand, IQuery, IInquiry, IProjection, and IArchive all have generic type parameters
- **Type safety goal**: Enable more flexible type assignments without sacrificing compile-time safety

### Constraints

- **Semantic parity**: Should not violate Java ezcqrs design principles (even if syntax differs)
- **Compile-time safety**: Variance must not introduce runtime type errors
- **Usability**: Variance should provide real benefits, not just theoretical flexibility
- **Consistency**: Follow Phase 3 established patterns

---

## Decision

**Apply contravariance (`in`) to input type parameters and allow implicit covariance for output types across all CQRS interfaces where safe and beneficial.**

### Details

#### Variance Annotations Applied

```csharp
// Command side
public interface ICommand<in TInput, TOutput> : IUseCase<TInput, TOutput>
    where TInput : IInput
    where TOutput : CqrsOutput<TOutput>, new()
{
    // Contravariant TInput: can accept more general input types
    // TOutput is covariant by default (return type position)
}

public interface IInquiry<in TInput, TOutput>
{
    // Contravariant TInput: enables flexible input type hierarchies
    // TOutput covariant: enables flexible output type hierarchies
    Task<TOutput> QueryAsync(TInput input);
}

// Query side
public interface IQuery<in TInput, TOutput> : IUseCase<TInput, TOutput>
    where TInput : IInput
    where TOutput : CqrsOutput<TOutput>, new()
{
    // Contravariant TInput: can accept more general input types
    // TOutput is covariant by default (return type position)
}

public interface IProjection<in TInput, TOutput>
    where TInput : IProjectionInput
{
    // Contravariant TInput: enables flexible projection input hierarchies
    // TOutput covariant: enables flexible output type hierarchies
    Task<TOutput> QueryAsync(TInput input);
}

// Archive (query database)
public interface IArchive<TData, in TId>
{
    // TData: invariant (used in both input and output positions)
    // TId: contravariant (only in input position)
    Task<TData?> FindByIdAsync(TId id);
    Task SaveAsync(TData data);
    Task DeleteAsync(TData data);
}
```

#### Variance Rules Applied

| Interface | Type Parameter | Variance | Rationale |
|-----------|---------------|----------|-----------|
| ICommand | TInput | `in` (contravariant) | Inherited from IUseCase, input-only position |
| ICommand | TOutput | implicit covariant | Return type position |
| IQuery | TInput | `in` (contravariant) | Inherited from IUseCase, input-only position |
| IQuery | TOutput | implicit covariant | Return type position |
| IInquiry | TInput | `in` (contravariant) | Input parameter position only |
| IInquiry | TOutput | implicit covariant | Return type position |
| IProjection | TInput | `in` (contravariant) | Input parameter position only |
| IProjection | TOutput | implicit covariant | Return type position |
| IArchive | TData | invariant | Used in both in/out positions |
| IArchive | TId | `in` (contravariant) | Input parameter position only |

---

## Consequences

### Positive Consequences

- ✅ **Flexible Type Assignments**: Can assign interface variables with more general input types
- ✅ **Consistent with Phase 3**: Follows IUseCase variance pattern from Phase 3
- ✅ **Type Safety**: Compile-time variance checking prevents invalid assignments
- ✅ **Inheritance Hierarchies**: Enables natural input/output type hierarchies
- ✅ **Better C# Idioms**: Leverages C# platform features not available in Java
- ✅ **No Runtime Overhead**: Variance is compile-time only, zero performance cost

### Negative Consequences

- ❌ **Complexity**: Developers must understand covariance and contravariance concepts
- ❌ **Semantic Divergence**: Java ezcqrs cannot express this (syntax difference, not design difference)
- ❌ **Limited Practical Use**: Many applications may not need this flexibility

### Neutral Consequences

- ⚖️ **Documentation Burden**: Must explain variance benefits in API documentation
- ⚖️ **Generic Constraints**: Variance works with existing generic constraints (where clauses)

---

## Alternatives Considered

### Alternative 1: No Variance Annotations (Invariant All)

```csharp
public interface ICommand<TInput, TOutput> : IUseCase<TInput, TOutput>
    where TInput : IInput
    where TOutput : CqrsOutput<TOutput>, new()
{
    // No variance - strictly invariant
}
```

**Pros**:
- Simple - no variance complexity
- Matches Java ezcqrs exactly (Java has no declaration-site variance)

**Cons**:
- **Loses Phase 3 consistency** - IUseCase already uses contravariant TInput
- **Less flexible** - cannot leverage C# type system capabilities
- **Inconsistent** - Phase 3 established variance pattern, Phase 4 would break it

**Why rejected**: Breaks consistency with Phase 3 IUseCase. Once Phase 3 introduced contravariant input, Phase 4 must maintain this pattern for ICommand/IQuery (which extend IUseCase).

---

### Alternative 2: Covariant Output Type Parameters

```csharp
public interface ICommand<in TInput, out TOutput> : IUseCase<TInput, TOutput>
    where TInput : IInput
    where TOutput : CqrsOutput<TOutput>, new()
{
    // Explicit out TOutput
}
```

**Pros**:
- Explicitly documents covariance intent
- Maximum flexibility for both input and output

**Cons**:
- **Conflicts with new() constraint** - `out TOutput` cannot have `new()` constraint
- **Breaks factory pattern** - CqrsOutput.Create<T>() requires `new()`
- **Unnecessary** - TOutput is already covariant by being in return position

**Why rejected**: The `new()` constraint on TOutput is required for CqrsOutput.Create<T>() factory method (ADR-0017). Explicit `out` keyword conflicts with this constraint.

---

### Alternative 3: Covariant TData in IArchive

```csharp
public interface IArchive<out TData, in TId>
{
    Task<TData?> FindByIdAsync(TId id);  // ✅ TData in output position
    Task SaveAsync(TData data);          // ❌ TData in input position - INVALID
    Task DeleteAsync(TData data);        // ❌ TData in input position - INVALID
}
```

**Pros**:
- Would enable flexible read model type hierarchies

**Cons**:
- **Invalid variance** - TData appears in input positions (SaveAsync, DeleteAsync)
- **Compilation error** - C# compiler rejects covariant type in input position
- **Not possible** - Technically cannot be implemented

**Why rejected**: Violates C# variance safety rules. TData must remain invariant because it appears in both input and output positions.

---

## Related Decisions

- **Depends on**: [ADR-0016](0016-async-await-throughout.md) (All methods return Task, affecting variance)
- **Related to**: Phase 3 IUseCase variance pattern (contravariant TInput)
- **Related to**: [ADR-0017](0017-cqrsoutput-implementation-strategy.md) (TOutput constraint affects covariance)
- **Related to**: [ADR-0018](0018-iarchive-async-method-design.md) (IArchive methods affect TData variance)

---

## Implementation Notes

### Variance Usage Examples

#### Contravariant Input (TInput)

```csharp
// Input hierarchy
public interface IInput { }
public record GeneralInput : IInput;
public record SpecificInput : GeneralInput;

// Command hierarchy
public interface ICommand<in TInput, TOutput> : IUseCase<TInput, TOutput>
    where TInput : IInput
    where TOutput : CqrsOutput<TOutput>, new();

public class GeneralCommand : ICommand<GeneralInput, GeneralOutput> { }

// Contravariance allows this assignment
ICommand<SpecificInput, GeneralOutput> specific = new GeneralCommand();
// More general input (GeneralInput) can handle more specific input (SpecificInput)
```

#### Covariant Output (TOutput)

```csharp
// Output hierarchy
public class GeneralOutput : CqrsOutput<GeneralOutput> { }
public class SpecificOutput : GeneralOutput { }

public class SpecificQuery : IQuery<QueryInput, SpecificOutput> { }

// Covariance allows this assignment
IQuery<QueryInput, GeneralOutput> general = new SpecificQuery();
// More specific output (SpecificOutput) can be treated as general output (GeneralOutput)
```

#### Contravariant TId in IArchive

```csharp
// ID hierarchy
public record GeneralId(string Value);
public record SpecificId(string Value) : GeneralId(Value);

// Archive for general ID
public class GeneralArchive : IArchive<ReadModel, GeneralId> { }

// Contravariance allows this assignment
IArchive<ReadModel, SpecificId> specific = new GeneralArchive();
// Archive accepting general ID can also accept specific ID
```

### When Variance Matters

Variance is most useful when:
1. **Command/Query handlers**: Single handler processes multiple input types
2. **Dependency injection**: Register more general handler for specific interface
3. **Test doubles**: Use test implementation with broader input types
4. **Type hierarchies**: Application has natural input/ID type hierarchies

### When Variance Doesn't Matter

Variance provides no benefit when:
1. **Flat type hierarchies**: No inheritance among inputs/outputs/IDs
2. **Single implementations**: Each interface has exactly one implementation
3. **Simple CRUD**: Basic create/read/update/delete with no type variance

---

## References

- **C# Language Specification**: [Generic Variance](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/covariance-contravariance/)
- **[PHASE4_API_DESIGN.md](../PHASE4_API_DESIGN.md)** - Generic variance design notes
- **Phase 3 IUseCase**: `src/EzDdd.UseCase/Port/In/IUseCase.cs` - Establishes contravariant TInput pattern
- **[ADR-0017](0017-cqrsoutput-implementation-strategy.md)** - CqrsOutput generic constraints

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2025-11-18 | Accepted    | Initial decision after Phase 4 Iterations 1-5 implementation |

---
