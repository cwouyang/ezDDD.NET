# ADR-0006: uContract.NET Integration for Design by Contract

## Status

**Accepted**

- **Date**: 2025-10-31
- **Deciders**: Project maintainers
- **Status Date**: 2025-10-31

---

## Context

### Problem Statement

Event-sourced aggregates in ezDDD.NET must enforce invariants according to three correctness rules (R1, R2, R3). We need a mechanism to express and enforce these invariants declaratively and consistently.

Key questions:
- How do we implement Design by Contract (DbC) for invariant checking?
- Should we use an external library or implement DbC inline?
- How do we maintain semantic parity with Java ezddd's contract checking?

This decision affects:
- **Invariant enforcement**: How aggregates validate business rules
- **Code clarity**: Readability of contract expressions
- **Semantic parity**: Alignment with Java ezddd's DbC approach
- **Dependency footprint**: Impact on zero-third-party-dependency principle

### Relevant Context

**Java ezddd Uses uContract 2.0.0**:

```java
// Java ezddd EsAggregateRoot
protected void ensureInvariant() {
    invariantNotNull("Workflow state", state);
    invariant("Not deleted", () -> !isDeleted());
}
```

Java ezddd depends on **uContract 2.0.0** (TeddySoft library) for Design by Contract support, providing:
- `requireNotNull()` / `require()` - Precondition checks
- `ensureNotNull()` / `ensure()` - Postcondition checks
- `invariantNotNull()` / `invariant()` - Invariant checks
- `check()` - General assertion

**Event Sourcing Correctness Rules**:

ezDDD enforces three invariant rules for event-sourced aggregates:

- **R1 (Construction)**: `{pre₀} fun₀ {post₀ & INV}`
  - Construction events establish initial invariants
  - **No precondition invariant check** (aggregate doesn't exist yet)
  - **Postcondition invariant check** (after `When()`)

- **R2 (Command)**: `{preₜ & INV} funₜ {postₜ & INV}`
  - Command events must maintain invariants
  - **Precondition invariant check** (before `When()`)
  - **Postcondition invariant check** (after `When()`)

- **R3 (Destruction)**: `{preᵤ & INV} funᵤ {postᵤ}`
  - Destruction events may break invariants (final operation)
  - **Precondition invariant check** (before `When()`)
  - **No postcondition invariant check** (aggregate is being deleted)

**uContract.NET Availability**:

uContract.NET (v1.0.0+) is available as a TeddySoft ecosystem library providing:
- `Contract.Require()` / `Contract.RequireNotNull()` - Preconditions
- `Contract.Ensure()` / `Contract.EnsureNotNull()` - Postconditions
- `Contract.Invariant()` / `Contract.InvariantNotNull()` - Invariants
- `Contract.Check()` - General assertions

**Alternative Approaches**:
1. Inline contract checks (if/throw pattern)
2. Custom lightweight DbC implementation
3. Depend on uContract.NET

### Constraints

- Must enforce R1, R2, R3 correctness rules for event sourcing
- Must maintain semantic parity with Java ezddd (which uses uContract 2.0.0)
- Should be declarative and readable
- Must align with zero-third-party-dependency principle (ecosystem dependencies allowed per ADR-0004)

---

## Decision

**We will depend on uContract.NET (v1.0.0+) for Design by Contract support in ezDDD.NET.**

### Details

**Dependency**:
```xml
<ItemGroup>
  <PackageReference Include="uContract" Version="1.0.0" />
</ItemGroup>
```

**Usage in EsAggregateRoot**:

```csharp
public abstract class EsAggregateRoot<TId, TEvent>
    where TEvent : InternalDomainEvent
{
    // Template method enforcing R1, R2, R3 rules
    protected void Apply(TEvent @event)
    {
        Contract.RequireNotNull("Domain event", @event);

        if (@event is IConstructionEvent)
        {
            // R1: No precondition check
            When(@event);
            EnsureInvariant(); // Postcondition check
        }
        else if (@event is IDestructionEvent)
        {
            // R3: Precondition check, no postcondition
            EnsureInvariant();
            When(@event);
        }
        else
        {
            // R2: Both precondition and postcondition checks
            EnsureInvariant();
            When(@event);
            EnsureInvariant();
        }

        Contract.Ensure("Aggregate id cannot be null", () => Id != null);
    }

    // Override to define invariants
    protected abstract void EnsureInvariant();
}
```

**Usage in Concrete Aggregates**:

```csharp
public class Workflow : EsAggregateRoot<WorkflowId, InternalDomainEvent>
{
    private WorkflowState _state = null!;

    protected override void EnsureInvariant()
    {
        Contract.InvariantNotNull("Workflow state", _state);
        Contract.Invariant("Not deleted", () => !IsDeleted);
    }

    protected override void When(InternalDomainEvent @event)
    {
        switch (@event)
        {
            case WorkflowCreated e:
                _state = new WorkflowState(e.WorkflowId, e.Name);
                break;
            case WorkflowDeleted:
                IsDeleted = true;
                break;
        }
    }
}
```

**Benefits**:
- ✅ Semantic parity with Java ezddd (both use uContract)
- ✅ Declarative, readable invariant expressions
- ✅ Consistent contract violation reporting
- ✅ Ecosystem dependency (not third-party per ADR-0004)
- ✅ Battle-tested implementation
- ✅ Avoids code duplication

---

## Consequences

### Positive Consequences

- ✅ **Semantic Parity**: Java ezddd uses uContract 2.0.0; ezDDD.NET uses uContract.NET (parallel design)
- ✅ **Correctness**: Proven DbC implementation for R1, R2, R3 rule enforcement
- ✅ **Readability**: Declarative contract expressions (`Contract.Invariant("Not deleted", () => !IsDeleted)`)
- ✅ **Ecosystem Consistency**: Both uContract.NET and ezDDD.NET are TeddySoft libraries
- ✅ **Avoid Duplication**: Reuse existing, tested DbC implementation
- ✅ **Rich Features**: Contract violation metadata, custom messages, source location tracking
- ✅ **Minimal Overhead**: uContract.NET has zero dependencies itself

### Negative Consequences

- ❌ **Not Truly Zero Dependency**: Adds one ecosystem dependency (but acceptable per ADR-0004)
- ❌ **Slight Installation Overhead**: Users must install uContract.NET (though automatic via NuGet)

### Neutral Consequences

- ⚖️ **Ecosystem Coupling**: ezDDD.NET and uContract.NET are coupled, but both maintained by TeddySoft
- ⚖️ **Learning Curve**: Users must understand uContract.NET API (though simple and documented)

---

## Alternatives Considered

### Alternative 1: Inline Contract Checks (if/throw Pattern)

**Description**: Implement invariant checks inline without a DbC library

```csharp
protected override void EnsureInvariant()
{
    if (_state == null)
        throw new InvariantViolationException("Workflow state cannot be null");

    if (IsDeleted)
        throw new InvariantViolationException("Workflow is deleted");
}
```

**Pros**:
- Zero dependencies (truly zero-dependency)
- Simple, direct implementation
- No external library learning curve

**Cons**:
- **Semantic parity loss**: Java ezddd uses uContract 2.0.0; not matching
- **Verbose**: if/throw pattern is less declarative than `Contract.Invariant()`
- **Code duplication**: Reimplementing DbC features (messages, source location, metadata)
- **Less expressive**: Cannot express lambda-based conditions cleanly
- **Maintenance burden**: Must maintain DbC logic across ezDDD.NET
- **Inconsistent ecosystem**: uContract.NET exists for this purpose; not using it is inconsistent

**Why rejected**: Java ezddd depends on uContract 2.0.0 for Design by Contract. To maintain semantic parity, ezDDD.NET should use the .NET equivalent. Inline checks would duplicate functionality already provided by uContract.NET, creating ecosystem inconsistency.

---

### Alternative 2: Custom Lightweight DbC Implementation

**Description**: Create a minimal DbC library embedded in ezDDD.NET

```csharp
// Internal DbC implementation
internal static class ContractCheck
{
    public static void InvariantNotNull(string message, object value)
    {
        if (value == null)
            throw new InvariantViolationException(message);
    }

    public static void Invariant(string message, Func<bool> condition)
    {
        if (!condition())
            throw new InvariantViolationException(message);
    }
}
```

**Pros**:
- Zero external dependencies
- Full control over implementation
- Tailored to ezDDD.NET needs

**Cons**:
- **Reinventing the wheel**: uContract.NET already provides this
- **Code duplication**: Duplicates uContract.NET functionality
- **Semantic parity loss**: Java ezddd uses external uContract library, not internal implementation
- **Missing features**: No contract violation metadata, source location tracking, ESC/Java-style checking
- **Maintenance burden**: Must maintain DbC implementation alongside DDD patterns
- **Ecosystem inconsistency**: Contradicts existence of uContract.NET
- **Less battle-tested**: New implementation vs. proven uContract.NET

**Why rejected**: Reimplementing DbC duplicates uContract.NET's functionality and creates maintenance burden. Java ezddd depends on external uContract library (not inline implementation), so semantic parity requires using uContract.NET. Creating a custom DbC would contradict the purpose of uContract.NET and create ecosystem fragmentation.

---

### Alternative 3: Use .NET Diagnostics.Debug.Assert

**Description**: Use built-in .NET assertions for invariant checking

```csharp
protected override void EnsureInvariant()
{
    Debug.Assert(_state != null, "Workflow state cannot be null");
    Debug.Assert(!IsDeleted, "Workflow is deleted");
}
```

**Pros**:
- Built-in .NET framework (zero dependencies)
- Familiar to .NET developers

**Cons**:
- **Debug-only**: Assertions removed in Release builds (invariants not enforced in production!)
- **Semantic difference**: Java ezddd uses runtime contract checking, not debug-only assertions
- **No contract violation exceptions**: Debug.Assert doesn't throw InvariantViolationException
- **Poor error messages**: Assertion failures are not structured domain exceptions
- **Not suitable for DbC**: Debug assertions are for catching programming errors, not enforcing domain invariants

**Why rejected**: `Debug.Assert` is debug-only and removed in Release builds, making it unsuitable for enforcing domain invariants in production. Java ezddd uses runtime contract checking (uContract 2.0.0), not debug-only assertions. Domain invariants must be enforced in all builds.

---

### Alternative 4: Use Null-Coalescing + Exceptions

**Description**: Use null-coalescing operators and manual exceptions

```csharp
protected override void EnsureInvariant()
{
    _ = _state ?? throw new InvariantViolationException("Workflow state cannot be null");

    if (IsDeleted)
        throw new InvariantViolationException("Workflow is deleted");
}
```

**Pros**:
- Uses modern C# syntax (null-coalescing)
- Zero dependencies
- Concise for null checks

**Cons**:
- **Semantic parity loss**: Java ezddd uses uContract library
- **Limited expressiveness**: Null-coalescing only works for null checks, not general conditions
- **Mixed patterns**: Combines null-coalescing with if/throw (inconsistent)
- **No metadata**: Missing contract violation context
- **Code duplication**: Still reimplementing DbC features

**Why rejected**: Limited to null checks and doesn't provide a consistent DbC API. Java ezddd uses uContract library for all contract types (preconditions, postconditions, invariants). Mixing syntax patterns creates inconsistency.

---

## Related Decisions

- **Depends on**: ADR-0004 (Zero Third-Party Dependency Principle) - Establishes that ecosystem dependencies are acceptable
- **Enables**: EsAggregateRoot invariant enforcement (R1, R2, R3 rules)
- **Related to**: ADR-0011 (EsAggregateRoot Design) - Will reference this ADR for DbC usage
- **Influences**: All aggregate implementations that need invariant checking

---

## Implementation Notes

### Package Reference

All projects requiring DbC (EzDdd.Entity at minimum) must reference uContract.NET:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <PackageId>ezDDD.Entity</PackageId>
  </PropertyGroup>

  <ItemGroup>
    <!-- Ecosystem dependency: Design by Contract -->
    <PackageReference Include="uContract" Version="1.0.0" />
  </ItemGroup>
</Project>
```

### Contract API Mapping

**Java uContract 2.0.0 → uContract.NET 1.0.0**:

| Java uContract 2.0.0 | uContract.NET 1.0.0 |
|----------------------|---------------------|
| `requireNotNull()` | `Contract.RequireNotNull()` |
| `require()` | `Contract.Require()` |
| `ensureNotNull()` | `Contract.EnsureNotNull()` |
| `ensure()` | `Contract.Ensure()` |
| `invariantNotNull()` | `Contract.InvariantNotNull()` |
| `invariant()` | `Contract.Invariant()` |
| `check()` | `Contract.Check()` |

### Usage Patterns

**Preconditions** (Use Case layer):
```csharp
public async Task<CqrsOutput<WorkflowId>> ExecuteAsync(CreateWorkflowInput input)
{
    Contract.RequireNotNull("Input", input);
    Contract.Require("Name not empty", () => !string.IsNullOrWhiteSpace(input.Name));

    // Implementation
}
```

**Postconditions** (Repository layer):
```csharp
public async Task<TAggregate?> FindByIdAsync(TId id)
{
    var aggregate = await _peer.FindByIdAsync(id);
    Contract.EnsureNotNull("Aggregate", aggregate);
    return aggregate;
}
```

**Invariants** (Aggregate layer):
```csharp
protected override void EnsureInvariant()
{
    Contract.InvariantNotNull("Workflow state", _state);
    Contract.Invariant("Not deleted", () => !IsDeleted);
    Contract.Invariant("Has valid stages", () => _state.Stages.Count > 0);
}
```

### Testing Contract Violations

```csharp
[Fact]
public void Apply_CommandEvent_ThrowsInvariantViolation_WhenInvariantBroken()
{
    // Arrange
    var workflow = new Workflow(events);
    var invalidEvent = new InvalidCommandEvent();

    // Act & Assert
    var exception = Assert.Throws<InvariantViolationException>(
        () => workflow.Apply(invalidEvent)
    );

    Assert.Contains("Workflow state", exception.Message);
}
```

---

## Documentation Requirements

### README.md

```markdown
## Dependencies

ezDDD.NET depends on:
- **.NET 8+**: Target framework (LTS until 2026)
- **uContract.NET**: Design by Contract support for invariant checking

### Installation

```bash
dotnet add package ezDDD.Core
# uContract.NET is automatically installed as transitive dependency
```
```

### Migration Guide (Java → .NET)

Document contract API mapping for Java developers:

```markdown
## Contract Checking

Java ezddd uses uContract 2.0.0; ezDDD.NET uses uContract.NET.

**Java**:
```java
invariantNotNull("State", state);
invariant("Not deleted", () -> !isDeleted());
```

**.NET**:
```csharp
Contract.InvariantNotNull("State", _state);
Contract.Invariant("Not deleted", () => !IsDeleted);
```
```

---

## Long-Term Considerations

### If uContract.NET is Unavailable

If uContract.NET becomes unmaintained:
- **Option 1**: Fork and maintain uContract.NET within TeddySoft
- **Option 2**: Implement inline DbC (semantic parity loss)
- **Recommendation**: Option 1 (maintain ecosystem consistency)

### Version Compatibility

- ezDDD.NET 1.x will depend on uContract.NET 1.x
- Breaking changes in uContract.NET may require ezDDD.NET major version bump
- Coordinate releases with uContract.NET maintainers

---

## References

- [uContract.NET Repository](../../../uContract.NET)
- [uContract.NET Documentation](../../../uContract.NET/README.md)
- [Java ezddd - uContract 2.0.0 Usage](https://gitlab.com/TeddyChen/ezddd)
- [DOTNET_PORT.md - 最小依賴原則](../../DOTNET_PORT.md#2-最小依賴原則)
- [ADR-0004: Zero Third-Party Dependency Principle](0004-zero-third-party-dependency-principle.md)
- [Design by Contract (Bertrand Meyer)](https://en.wikipedia.org/wiki/Design_by_contract)

---

## Revision History

| Date       | Status   | Notes                                  |
|------------|----------|----------------------------------------|
| 2025-10-31 | Accepted | Decision finalized and documented      |

---
