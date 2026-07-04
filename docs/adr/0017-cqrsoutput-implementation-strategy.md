# ADR-0017: CqrsOutput Implementation Strategy

## Status

**Accepted**

- **Date**: 2025-11-17
- **Deciders**: Project maintainers
- **Status Date**: 2025-11-17

---

## Context

### Problem Statement

How should `CqrsOutput` be implemented in C# to provide a type-safe fluent API for Command and Query outputs while maintaining semantic parity with Java ezddd's `CqrsOutput<T extends CqrsOutput<T>>` pattern?

### Relevant Context

**Java ezcqrs Implementation**:
```java
public class CqrsOutput<T extends CqrsOutput<T>> implements Output {
    private String id;
    private String message;
    private ExitCode exitCode;

    public String getId() { return id; }
    public T setId(String id) { this.id = id; return self(); }
    public String getMessage() { return message; }
    public T setMessage(String message) { this.message = message; return self(); }
    public ExitCode getExitCode() { return exitCode; }
    public T setExitCode(ExitCode exitCode) { this.exitCode = exitCode; return self(); }

    public T fail() { this.exitCode = ExitCode.FAILURE; return self(); }
    public T succeed() { this.exitCode = ExitCode.SUCCESS; return self(); }

    final T self() { return (T) this; }

    public static <T> T create(Class<T> clazz) {
        return clazz.getDeclaredConstructor().newInstance();
    }
}
```

**Phase 3 IOutput Interface** (from EzDdd.UseCase):
```csharp
public interface IOutput
{
    string Id { get; }
    string Message { get; }
    ExitCode ExitCode { get; }

    IOutput SetId(string id);
    IOutput SetMessage(string message);
    IOutput SetExitCode(ExitCode exitCode);
    IOutput Fail();
    IOutput Succeed();
}
```

**Key Design Questions**:
1. Should CqrsOutput be a class or interface?
2. How to implement self-referential generic `T extends CqrsOutput<T>` in C#?
3. How to integrate with Phase 3's `IOutput` interface?
4. Should we use properties or getters/setters?
5. Should we use reflection or `new()` constraint for factory method?

### Constraints

- Must maintain semantic parity with Java ezcqrs (~98% target)
- Must integrate with Phase 3 `IOutput` interface
- Must provide type-safe fluent API for subclasses
- Must follow .NET idioms and conventions
- Zero third-party dependencies (ADR-0004)

---

## Decision

**`CqrsOutput<T>` will be implemented as a base class (not interface) with self-referential generic constraint `where T : CqrsOutput<T>`, C# auto-properties, and `new()` constraint for factory method.**

### Details

**C# Implementation**:
```csharp
public class CqrsOutput<T> : IOutput
    where T : CqrsOutput<T>
{
    // C# properties (not Java getters/setters)
    public string Id { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ExitCode ExitCode { get; set; } = ExitCode.Success;

    // Factory method with new() constraint (not reflection)
    public static T Create() where T : new() => new T();

    // Fluent setter methods return concrete type T
    public T SetId(string id) { Id = id; return Self(); }
    public T SetMessage(string message) { Message = message; return Self(); }
    public T SetExitCode(ExitCode exitCode) { ExitCode = exitCode; return Self(); }
    public T Fail() { ExitCode = ExitCode.Failure; return Self(); }
    public T Succeed() { ExitCode = ExitCode.Success; return Self(); }

    // Type-safe cast for fluent API
    private T Self() => (T)this;

    // Explicit IOutput implementation for backward compatibility
    IOutput IOutput.SetId(string id) => SetId(id);
    IOutput IOutput.SetMessage(string message) => SetMessage(message);
    IOutput IOutput.SetExitCode(ExitCode exitCode) => SetExitCode(exitCode);
    IOutput IOutput.Fail() => Fail();
    IOutput IOutput.Succeed() => Succeed();
}
```

**Key Design Decisions**:

1. **Class (not interface)**: Provides concrete implementation for fluent builder pattern, stores field values, implements `Self()` method
2. **Self-Referential Generic**: `where T : CqrsOutput<T>` enables type-safe fluent API where subclass methods return the correct subclass type
3. **Implements IOutput**: Maintains compatibility with Phase 3, uses explicit interface implementation to avoid signature conflicts
4. **C# Properties**: Use auto-properties (`{ get; set; }`) instead of Java getters/setters - more concise and idiomatic
5. **new() Constraint**: Use generic constraint `where T : new()` instead of reflection for better performance and type safety
6. **Default Values**: Initialize properties to defaults (empty strings for Id/Message, `ExitCode.Success` for ExitCode)
7. **Explicit Interface Implementation**: `IOutput` methods explicitly implemented to return `IOutput`, while public methods return `T`

**Usage Example**:
```csharp
// Define custom output
public class CreateAccountOutput : CqrsOutput<CreateAccountOutput>
{
    public string AccountNumber { get; set; } = string.Empty;

    public CreateAccountOutput SetAccountNumber(string accountNumber)
    {
        AccountNumber = accountNumber;
        return this;
    }
}

// Usage with fluent API
var output = CreateAccountOutput.Create()
    .SetId("acc-123")
    .SetAccountNumber("1234567890")
    .Succeed()
    .SetMessage("Account created successfully");

// Type is CreateAccountOutput, not CqrsOutput<CreateAccountOutput>
```

---

## Consequences

### Positive Consequences

- ✅ **Type-Safe Fluent API**: Subclass methods return correct subclass type, enabling type-safe method chaining
- ✅ **Semantic Parity**: ~100% parity with Java ezcqrs design (same pattern, same behavior)
- ✅ **IOutput Integration**: Seamlessly integrates with Phase 3 IOutput interface via explicit implementation
- ✅ **Better Performance**: `new()` constraint avoids reflection overhead (faster than Java's `Class.newInstance()`)
- ✅ **C# Idioms**: Uses properties instead of getters/setters, follows .NET conventions
- ✅ **Compile-Time Safety**: Self-referential generic constraint enforced at compile time
- ✅ **Backward Compatible**: Can be used polymorphically as `IOutput` when needed

### Negative Consequences

- ⚠️ **Generic Complexity**: Self-referential generic pattern has learning curve for developers unfamiliar with the pattern
- ⚠️ **Explicit Interface Implementation**: Requires understanding of explicit vs implicit interface implementation in C#
- ⚠️ **Constraint Verbosity**: `where T : CqrsOutput<T>` must be repeated on methods with `new()` constraint

### Neutral Consequences

- ⚖️ **Subclass Pattern Required**: Developers must follow pattern `class MyOutput : CqrsOutput<MyOutput>` correctly
- ⚖️ **Testing Complexity**: Tests must verify subclass behavior, not just base class
- ⚖️ **Documentation Needed**: Pattern must be clearly documented with examples for users

---

## Alternatives Considered

### Alternative 1: Record Type

**Description**: Implement CqrsOutput as C# record with immutability and value equality

```csharp
public record CqrsOutput<T>(string Id, string Message, ExitCode ExitCode) : IOutput
    where T : CqrsOutput<T>
{
    public T SetId(string id) => (T)this with { Id = id };
    public T SetMessage(string message) => (T)this with { Message = message };
    // ... other fluent methods using 'with' expressions
}
```

**Pros**:
- Immutability enforced by design
- Value equality semantics (useful for testing)
- Concise syntax with primary constructor
- Modern C# feature

**Cons**:
- Fluent API awkward with records (`with` expressions create new instances, not ideal for builder pattern)
- Subclassing more complex with records (positional parameters)
- Java uses mutable class (semantic parity concern)
- Performance overhead from creating new instances for each setter call

**Why rejected**: CqrsOutput needs mutability for builder pattern. Java version is mutable, and creating new instances on each setter call violates semantic parity and adds unnecessary performance overhead.

---

### Alternative 2: Non-Generic Base Class

**Description**: Simple base class without self-referential generic

```csharp
public class CqrsOutput : IOutput
{
    public string Id { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ExitCode ExitCode { get; set; } = ExitCode.Success;

    public virtual CqrsOutput SetId(string id) { Id = id; return this; }
    public virtual CqrsOutput SetMessage(string message) { Message = message; return this; }
    // Subclasses need explicit casting:
    // public new CreateAccountOutput SetId(string id) => (CreateAccountOutput)base.SetId(id);
}
```

**Pros**:
- Simpler than self-referential generic
- Easier to understand for beginners
- No generic constraint complexity

**Cons**:
- Breaks type safety (methods return base type, not subclass type)
- Requires manual casting in subclasses
- Loses fluent API benefits (casting interrupts method chaining)
- Violates Java semantic parity (Java uses self-referential generic)

**Why rejected**: Loses type safety and fluent API benefits that are core to the design. Requires manual casting in every subclass, which is error-prone and defeats the purpose of the pattern.

---

### Alternative 3: Interface with Extension Methods

**Description**: Define ICqrsOutput interface with extension methods for fluent API

```csharp
public interface ICqrsOutput : IOutput
{
    // No methods here, just properties from IOutput
}

public static class CqrsOutputExtensions
{
    public static T SetId<T>(this T output, string id) where T : ICqrsOutput
    {
        output.Id = id;
        return output;
    }
    // ... other extension methods
}
```

**Pros**:
- Flexible (no inheritance required)
- Can be applied to any type implementing ICqrsOutput
- Fluent API via extension methods

**Cons**:
- No shared implementation (each subclass must implement properties)
- Extension methods cannot access private state
- Violates Java design (Java uses base class with shared implementation)
- Awkward semantics (extension methods for core behavior)

**Why rejected**: Too different from Java design, loses shared implementation, and extension methods are not idiomatic for core domain behavior. Semantic parity drops below 90%.

---

### Alternative 4: Reflection for Factory Method

**Description**: Use reflection like Java instead of `new()` constraint

```csharp
public static T Create<T>() where T : CqrsOutput<T>
{
    return (T)Activator.CreateInstance(typeof(T));
}
```

**Pros**:
- Matches Java approach exactly (uses reflection)
- More flexible (no parameterless constructor requirement)
- Same implementation strategy as Java

**Cons**:
- Runtime overhead (reflection slower than `new()`)
- Runtime errors instead of compile-time errors (if no parameterless constructor)
- Potential security issues in some environments (Code Access Security)
- C# has better solution with `new()` constraint

**Why rejected**: C# provides better solution with `new()` constraint. Using reflection when a compile-time type-safe solution exists violates C# best practices. Performance overhead is measurable (~10-100x slower than `new()`), and compile-time safety is preferred.

---

## Related Decisions

- **Related to ADR-0016**: Async/Await Throughout - While CqrsOutput itself has no async methods (pure data holder), Command/Query operations that return CqrsOutput use async methods (ExecuteAsync)
- **Depends on Phase 3 IOutput**: CqrsOutput implements IOutput interface from EzDdd.UseCase module, ensuring compatibility with Phase 3 use case infrastructure
- **Related to ADR-0021** (to be written): Generic Variance Annotations - CqrsOutput appears as output parameter constraint in ICommand/IQuery with covariance considerations

---

## Implementation Notes

### Subclass Pattern

Developers creating custom outputs must follow this pattern:

```csharp
// ✅ Correct: Self-referential generic parameter
public class MyOutput : CqrsOutput<MyOutput>
{
    public string CustomField { get; set; } = string.Empty;

    public MyOutput SetCustomField(string value)
    {
        CustomField = value;
        return this;  // Returns MyOutput, not CqrsOutput<MyOutput>
    }
}

// ❌ Incorrect: Wrong generic parameter
public class MyOutput : CqrsOutput<CqrsOutput<MyOutput>>  // Wrong!

// ❌ Incorrect: No generic parameter
public class MyOutput : CqrsOutput  // Won't compile
```

### Factory Method Usage

```csharp
// ✅ Correct: Using Create<T>() with new() constraint
var output = CreateAccountOutput.Create()
    .SetId("acc-123")
    .Succeed();

// ❌ Incorrect: Direct instantiation (works but less fluent)
var output = new CreateAccountOutput();
output.SetId("acc-123");
output.Succeed();
```

### IOutput Compatibility

```csharp
// Can be used polymorphically as IOutput
IOutput output = CreateAccountOutput.Create()
    .SetId("acc-123")
    .Succeed();  // Returns IOutput when used via interface

// But type-safe when used directly
CreateAccountOutput typedOutput = CreateAccountOutput.Create()
    .SetId("acc-123")
    .Succeed();  // Returns CreateAccountOutput
```

### Testing Considerations

```csharp
[Fact]
public void SetId_ShouldReturnSameInstanceType()
{
    var output = CreateAccountOutput.Create();
    var result = output.SetId("test");

    Assert.IsType<CreateAccountOutput>(result);  // Verify concrete type
    Assert.Same(output, result);  // Verify same instance (not new copy)
}

[Fact]
public void FluentApi_ShouldChainCorrectly()
{
    var output = CreateAccountOutput.Create()
        .SetId("acc-123")
        .SetAccountNumber("1234567890")
        .Succeed()
        .SetMessage("Success");

    Assert.Equal("acc-123", output.Id);
    Assert.Equal("1234567890", output.AccountNumber);
    Assert.Equal(ExitCode.Success, output.ExitCode);
    Assert.Equal("Success", output.Message);
}
```

---

## References

### Analysis Documents
- Phase 4 Java source analysis - Lines 256-325: Java CqrsOutput analysis (internal working note, not retained in the repository)
- Phase 4 API design notes - Lines 1053-1295: C# CqrsOutput design and comparison (internal working note, not retained in the repository)
- Phase 4 ADR planning notes - Lines 96-253: ADR-0017 planning details (internal working note, not retained in the repository)

### Source Code References
- [Phase 3 IOutput Interface](../../src/EzDdd.UseCase/Port/In/IOutput.cs) - Phase 3 output interface that CqrsOutput must implement
- Java ezcqrs: `src/main/java/tw/teddysoft/ezddd/cqrs/usecase/CqrsOutput.java` - Original Java implementation

### Related ADRs
- [ADR-0016: Async/Await Throughout](0016-async-await-throughout.md) - Establishes async pattern for I/O operations
- [ADR-0004: Zero Third-Party Dependency Principle](0004-zero-third-party-dependency-principle.md) - Constrains implementation to BCL only

### External References
- [C# Generics Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/generics) - Generic constraints and self-referential patterns
- [Fluent Interface Pattern](https://en.wikipedia.org/wiki/Fluent_interface) - Design pattern for fluent APIs
- [Explicit Interface Implementation](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/interfaces/explicit-interface-implementation) - C# explicit interface implementation

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2025-11-17 | Proposed    | Initial draft for Phase 4      |
| 2025-11-17 | Accepted    | Decision finalized before Phase 4 implementation |

---

*This ADR documents the CqrsOutput implementation strategy for ezDDD.NET Phase 4 (EzDdd.Cqrs module), establishing the foundation for all Command and Query outputs.*
