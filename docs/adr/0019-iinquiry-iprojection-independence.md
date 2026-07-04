# ADR-0019: IInquiry and IProjection Independence from IUseCase

## Status

**Accepted**

- **Date**: 2025-11-17
- **Deciders**: Project maintainers
- **Status Date**: 2025-11-17

---

## Context

### Problem Statement

Should `IInquiry<TInput, TOutput>` and `IProjection<TInput, TOutput>` extend `IUseCase<TInput, TOutput>`, or should they be independent interfaces? This decision affects interface hierarchy, constraints, and usage patterns in the CQRS module.

### Relevant Context

**Java ezcqrs Design**:
```java
// Inquiry does NOT extend UseCase
public interface Inquiry<I, O> {
    O query(I input);
}

// Projection does NOT extend UseCase
public interface Projection<I extends ProjectionInput, O> {
    O query(I input);
}

// But Command and Query DO extend UseCase
public interface Command<I extends Input, O extends CqrsOutput> extends UseCase<I, O> {
}

public interface Query<I extends Input, O extends CqrsOutput> extends UseCase<I, O> {
}
```

**Phase 3 IUseCase Interface** (from EzDdd.UseCase):
```csharp
public interface IUseCase<in TInput, TOutput>
    where TInput : IInput
    where TOutput : IOutput
{
    Task<TOutput> ExecuteAsync(TInput input);
}
```

**Design Rationale in Java ezcqrs**:
- **ICommand/IQuery**: Full use cases representing application operations, require CqrsOutput for standardized results
- **IInquiry**: Lightweight validation queries used *within* commands, no need for full use case infrastructure
- **IProjection**: Specialized view builders used *within* queries, focus on transforming read models

**Key Questions**:
1. Should IInquiry and IProjection extend IUseCase in C#?
2. What are the benefits of NOT extending IUseCase?
3. What constraints would we lose by not extending IUseCase?
4. Should method names differ (QueryAsync vs ExecuteAsync)?

### Constraints

- Must maintain semantic parity with Java ezcqrs (~98% target)
- Must provide clear separation of concerns
- Must support flexible output types for validation queries
- Must be consistent with CQRS design philosophy

---

## Decision

**`IInquiry<TInput, TOutput>` and `IProjection<TInput, TOutput>` will NOT extend `IUseCase<TInput, TOutput>`. They are lightweight, specialized query interfaces with their own method `QueryAsync()` and flexible output types.**

### Details

**C# Implementation**:

**IInquiry (Does NOT extend IUseCase)**:
```csharp
namespace EzDdd.Cqrs.Command;

/// <summary>
///     IInquiry is an interface for validation queries primarily used within commands.
/// </summary>
public interface IInquiry<in TInput, TOutput>
{
    /// <summary>
    ///     Executes the inquiry query with the given input.
    /// </summary>
    Task<TOutput> QueryAsync(TInput input);
}
```

**IProjection (Does NOT extend IUseCase)**:
```csharp
namespace EzDdd.Cqrs.Query;

/// <summary>
///     IProjection is an interface for building read models from the query database.
/// </summary>
public interface IProjection<in TInput, TOutput>
    where TInput : IProjectionInput
{
    /// <summary>
    ///     Executes the projection query to build a read model view.
    /// </summary>
    Task<TOutput> QueryAsync(TInput input);
}
```

**For Comparison - ICommand/IQuery (DO extend IUseCase)**:
```csharp
// ICommand DOES extend IUseCase
public interface ICommand<in TInput, TOutput> : IUseCase<TInput, TOutput>
    where TInput : IInput
    where TOutput : CqrsOutput<TOutput>
{
    // Inherits ExecuteAsync from IUseCase
}

// IQuery DOES extend IUseCase
public interface IQuery<in TInput, TOutput> : IUseCase<TInput, TOutput>
    where TInput : IInput
    where TOutput : CqrsOutput<TOutput>
{
    // Inherits ExecuteAsync from IUseCase
}
```

**Key Design Decisions**:

1. **Independence from IUseCase**: IInquiry and IProjection do NOT extend IUseCase - simpler than full use cases
2. **No Infrastructure Overhead**: Don't require IInput/IOutput constraints or use case exception handling
3. **Flexible Output Types**:
   - IInquiry can return `bool`, `int`, `string`, or any DTO (no CqrsOutput constraint)
   - IProjection can return any view model or DTO (no CqrsOutput constraint)
4. **Different Method Name**: `QueryAsync()` instead of `ExecuteAsync()` - distinguishes intent (query vs use case execution)
5. **Semantic Parity**: Matches Java ezcqrs design philosophy (~100% parity on this decision)

**Rationale**:

- **Inquiries**: Used for validation within commands (e.g., "Does account exist?", "Is username available?"). These are simple lookups that don't need:
  - CqrsOutput wrapper (just return bool or simple value)
  - IInput/IOutput constraints (unnecessary formality)
  - Full use case infrastructure (exception handling, output formatting)

- **Projections**: Used to build complex views from multiple read models. These are data transformers that don't need:
  - CqrsOutput wrapper (return view models directly)
  - Full use case execution semantics (they're helpers, not top-level operations)
  - Use case exception handling (failures are technical, not business)

**Usage Examples**:

**IInquiry Usage (validation within command)**:
```csharp
// Simple inquiry returning bool
public class AccountExistsInquiry : IInquiry<AccountId, bool>
{
    private readonly IArchive<AccountReadModel, AccountId> _archive;

    public async Task<bool> QueryAsync(AccountId input)
    {
        var account = await _archive.FindByIdAsync(input);
        return account != null;
    }
}

// Use within command
public class TransferMoneyCommand : ICommand<TransferInput, TransferOutput>
{
    private readonly IInquiry<AccountId, bool> _accountExistsInquiry;

    public async Task<TransferOutput> ExecuteAsync(TransferInput input)
    {
        // Validate using inquiry (returns bool directly)
        if (!await _accountExistsInquiry.QueryAsync(input.TargetAccountId))
        {
            throw new UseCaseFailureException("Target account does not exist");
        }

        // Execute transfer logic...
    }
}
```

**IProjection Usage (view building within query)**:
```csharp
// Projection returning view model
public class CustomerSummaryProjection
    : IProjection<CustomerSummaryInput, CustomerSummaryView>
{
    private readonly IArchive<CustomerReadModel, Guid> _customerArchive;
    private readonly IArchive<OrderReadModel, Guid> _orderArchive;

    public async Task<CustomerSummaryView> QueryAsync(CustomerSummaryInput input)
    {
        var customer = await _customerArchive.FindByIdAsync(input.CustomerId);
        // Aggregate data from multiple archives...

        return new CustomerSummaryView(
            customer.Name,
            customer.Email,
            orderCount: 5,
            totalPurchases: 1000m
        );
    }
}

// Use within query
public class GetCustomerSummaryQuery
    : IQuery<GetCustomerSummaryInput, GetCustomerSummaryOutput>
{
    private readonly IProjection<CustomerSummaryInput, CustomerSummaryView> _projection;

    public async Task<GetCustomerSummaryOutput> ExecuteAsync(GetCustomerSummaryInput input)
    {
        // Use projection to build view (returns view model directly)
        var view = await _projection.QueryAsync(new CustomerSummaryInput(input.CustomerId));

        return GetCustomerSummaryOutput.Create()
            .SetCustomerSummary(view)
            .Succeed();
    }
}
```

---

## Consequences

### Positive Consequences

- ✅ **Semantic Parity**: ~100% parity with Java ezcqrs design philosophy
- ✅ **Lightweight Queries**: No unnecessary use case infrastructure overhead
- ✅ **Flexible Output Types**: Can return simple types (bool, int) or DTOs without CqrsOutput wrapper
- ✅ **Clear Intent**: `QueryAsync()` clearly distinguishes validation/projection from `ExecuteAsync()` use cases
- ✅ **Reduced Complexity**: No need for IInput/IOutput constraints for simple validation queries
- ✅ **Separation of Concerns**: Clear distinction between full use cases (ICommand/IQuery) and helper queries (IInquiry/IProjection)

### Negative Consequences

- ⚠️ **Cannot Treat as IUseCase Polymorphically**: Cannot pass IInquiry/IProjection where IUseCase is expected
- ⚠️ **Different Method Name**: `QueryAsync()` vs `ExecuteAsync()` requires understanding the distinction
- ⚠️ **Two Query Patterns**: Developers must understand when to use IQuery (full use case) vs IInquiry/IProjection (helper queries)

### Neutral Consequences

- ⚖️ **Documentation Critical**: Must clearly document the distinction and usage patterns
- ⚖️ **Examples Needed**: Code examples essential to show proper usage of IInquiry/IProjection within ICommand/IQuery
- ⚖️ **Learning Curve**: Developers need to understand CQRS pattern hierarchy (use cases vs helper queries)

---

## Alternatives Considered

### Alternative 1: Extend IUseCase

**Description**: Make IInquiry and IProjection extend IUseCase with full constraints

```csharp
public interface IInquiry<in TInput, TOutput> : IUseCase<TInput, TOutput>
    where TInput : IInput
    where TOutput : IOutput
{
    // Inherits ExecuteAsync from IUseCase
}

public interface IProjection<in TInput, TOutput> : IUseCase<TInput, TOutput>
    where TInput : IInput
    where TOutput : IOutput
{
    // Inherits ExecuteAsync from IUseCase
}
```

**Pros**:
- Unified interface hierarchy (all queries extend IUseCase)
- Can be used polymorphically as IUseCase
- Consistent method name (ExecuteAsync everywhere)
- Single pattern to learn

**Cons**:
- Violates Java semantic parity (Java: Inquiry/Projection do NOT extend UseCase)
- Unnecessary constraints (IInput, IOutput) for simple validation queries
- Forces CqrsOutput wrapper for bool returns (over-engineering)
- Infrastructure overhead for lightweight queries
- Loses semantic distinction between use cases and helper queries

**Why rejected**: Violates Java design philosophy and adds unnecessary complexity. A simple "Does account exist?" query shouldn't require implementing IInput, IOutput, and wrapping a bool in CqrsOutput. Semantic parity drops below 90%.

---

### Alternative 2: Keep ExecuteAsync Method Name

**Description**: Independent interfaces but use same method name as IUseCase

```csharp
public interface IInquiry<in TInput, TOutput>
{
    Task<TOutput> ExecuteAsync(TInput input);  // Same as IUseCase
}

public interface IProjection<in TInput, TOutput>
    where TInput : IProjectionInput
{
    Task<TOutput> ExecuteAsync(TInput input);  // Same as IUseCase
}
```

**Pros**:
- Consistent naming across all interfaces
- Familiar to developers (all async operations are ExecuteAsync)
- Could enable duck typing scenarios

**Cons**:
- Doesn't distinguish intent (query vs use case execution)
- Same method name confuses purpose
- Java uses `query()` method name (semantic difference)
- Loses opportunity to clarify distinction with naming

**Why rejected**: Method names should express intent. `QueryAsync()` better expresses "lightweight query" than `ExecuteAsync()` which implies full use case execution. While consistency has value, clarity of intent is more important.

---

### Alternative 3: Add Output Constraints

**Description**: Independent interfaces but require IOutput constraint

```csharp
public interface IInquiry<in TInput, TOutput>
    where TInput : IInput
    where TOutput : IOutput
{
    Task<TOutput> QueryAsync(TInput input);
}
```

**Pros**:
- Consistent with ICommand/IQuery output constraints
- Forces standardized output format
- Enables common output handling infrastructure

**Cons**:
- Limits flexibility (can't return bool, int for simple validation)
- Over-engineering for lightweight queries
- Requires wrapping simple types in output objects
- Violates Java design (no output constraint in Java)

**Why rejected**: Flexibility is a key benefit of IInquiry/IProjection independence. Forcing bool into IOutput wrapper adds unnecessary complexity. For "Is username available?" returning `bool` is simpler than `Task<UsernameAvailabilityOutput>`.

---

### Alternative 4: No IProjectionInput Constraint

**Description**: Make IProjection fully unconstrained like IInquiry

```csharp
public interface IProjection<in TInput, TOutput>
{
    Task<TOutput> QueryAsync(TInput input);
}
```

**Pros**:
- Symmetric to IInquiry (both unconstrained)
- More flexible for projection inputs
- Simpler interface definition

**Cons**:
- Loses type safety for projection inputs
- Java Projection has `I extends ProjectionInput` constraint
- Reduces semantic clarity (what distinguishes projection input from inquiry input?)

**Why rejected**: IProjectionInput constraint provides type safety and semantic clarity while maintaining Java parity. The constraint is lightweight (marker interface) and helps distinguish projection inputs from inquiry inputs in code.

---

## Related Decisions

- **Related to ADR-0017**: CqrsOutput Implementation Strategy - Commands/Queries require CqrsOutput, but Inquiries/Projections don't
- **Depends on Phase 3 IUseCase**: Defines what "full use case" means (IInput/IOutput constraints, ExecuteAsync method)
- **Related to ADR-0021** (to be written): Generic Variance Annotations - Variance applies consistently across all interfaces

---

## Implementation Notes

### Interface Hierarchy Summary

```
IUseCase<TInput, TOutput>                    (Phase 3 - Use Case Layer)
    ↓ extends
ICommand<TInput, TOutput>                     (Phase 4 - Write operations)
IQuery<TInput, TOutput>                       (Phase 4 - Read operations)

Independent (do NOT extend IUseCase):
IInquiry<TInput, TOutput>                     (Phase 4 - Validation queries)
IProjection<TInput, TOutput>                  (Phase 4 - View builders)
```

### When to Use Each Interface

| Interface | Purpose | Used By | Output Type | Method Name |
|-----------|---------|---------|-------------|-------------|
| **ICommand** | Write operations (modify state) | Application layer | `CqrsOutput<T>` | `ExecuteAsync()` |
| **IQuery** | Read operations (top-level queries) | Application layer | `CqrsOutput<T>` | `ExecuteAsync()` |
| **IInquiry** | Validation queries (within commands) | ICommand implementations | Any (bool, int, DTO) | `QueryAsync()` |
| **IProjection** | View builders (within queries) | IQuery implementations | Any (view models, DTOs) | `QueryAsync()` |

### Testing Patterns

```csharp
// Test IInquiry
[Fact]
public async Task AccountExistsInquiry_WhenAccountExists_ShouldReturnTrue()
{
    var inquiry = new AccountExistsInquiry(_archive);
    var accountId = AccountId.New();

    await _archive.SaveAsync(new AccountReadModel(accountId, 100m));

    var result = await inquiry.QueryAsync(accountId);

    Assert.True(result);  // Simple bool return
}

// Test IProjection
[Fact]
public async Task CustomerSummaryProjection_ShouldAggregateData()
{
    var projection = new CustomerSummaryProjection(_customerArchive, _orderArchive);
    var customerId = Guid.NewGuid();

    // Setup test data...

    var view = await projection.QueryAsync(new CustomerSummaryInput(customerId));

    Assert.Equal("John Doe", view.Name);  // View model directly
    Assert.Equal(5, view.OrderCount);
}
```

### Common Mistake to Avoid

```csharp
// ❌ Incorrect: Trying to use IInquiry as IUseCase
IUseCase<AccountId, bool> useCase = new AccountExistsInquiry(_archive);  // Won't compile!

// ✅ Correct: Use IInquiry interface directly
IInquiry<AccountId, bool> inquiry = new AccountExistsInquiry(_archive);
var exists = await inquiry.QueryAsync(accountId);
```

---

## References

### Analysis Documents
- Phase 4 Java source analysis - Lines 75-186: Java Inquiry and Projection analysis (internal working note, not retained in the repository)
- Phase 4 API design notes - Lines 210-627: C# IInquiry and IProjection design (internal working note, not retained in the repository)
- Phase 4 ADR planning notes - Lines 395-560: ADR-0019 planning details (internal working note, not retained in the repository)

### Source Code References
- [Phase 3 IUseCase Interface](../../src/EzDdd.UseCase/Port/In/IUseCase.cs) - Use case interface that ICommand/IQuery extend (but IInquiry/IProjection don't)
- Java ezcqrs: `src/main/java/tw/teddysoft/ezddd/cqrs/usecase/command/Inquiry.java` - Original Java Inquiry
- Java ezcqrs: `src/main/java/tw/teddysoft/ezddd/cqrs/usecase/query/Projection.java` - Original Java Projection

### Related ADRs
- [ADR-0017: CqrsOutput Implementation Strategy](0017-cqrsoutput-implementation-strategy.md) - Establishes CqrsOutput constraint for ICommand/IQuery (but not IInquiry/IProjection)

### External References
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html) - Martin Fowler on CQRS separation
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) - Uncle Bob on use cases and boundaries

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2025-11-17 | Proposed    | Initial draft for Phase 4      |
| 2025-11-17 | Accepted    | Decision finalized before Phase 4 implementation |

---

*This ADR documents the decision for IInquiry and IProjection to remain independent from IUseCase in ezDDD.NET Phase 4 (EzDdd.Cqrs module), establishing clear separation between full use cases and lightweight helper queries.*
