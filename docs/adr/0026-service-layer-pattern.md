# ADR-0026: Service Layer Pattern for Complex Business Logic

## Status

**Accepted**

- **Date**: 2026-01-08
- **Deciders**: ezDDD.NET Architecture Team
- **Status Date**: 2026-01-08

---

## Context

### Problem Statement

As Use Cases grow in complexity, they often accumulate business logic that:
- Makes the Use Case hard to read and maintain (>40 lines)
- Cannot be reused across multiple Use Cases
- Is difficult to unit test in isolation
- Violates the Single Responsibility Principle (orchestration + domain logic)

**Example**: Transferring money between two bank accounts requires validation of balances, transfer limits, account status, and coordinating two aggregates. This results in 40+ lines of complex logic embedded in the Use Case.

Should ezDDD.NET provide guidance on extracting complex business logic to a dedicated Service layer?

### Relevant Context

**Java ezddd 4.1.0 Pattern**:
- Java ezddd introduces Service layer pattern in version 4.1.0
- Multiple commits refactor complex Use Cases to extract business logic to Services
- Pattern is **optional** (recommended, not required)
- Services are stateless classes that encapsulate reusable business logic

**ezDDD.NET Use Case Layer** (ADR-0003):
- Use Cases should be thin orchestration layers (<20 lines preferred)
- Complex domain logic currently has two options:
  1. Keep in Use Case (leads to fat Use Cases)
  2. Put in Aggregate (not always appropriate for cross-aggregate logic)

**Current Pain Points**:
- Use Cases with 40+ lines are hard to understand
- Transfer logic locked inside Use Case (cannot reuse)
- Integration tests are slow (must test through Use Case)
- Cross-aggregate operations lack a clear home

### Constraints

- **Zero External Dependencies**: Services must not require third-party libraries
- **Optional Pattern**: Must not be mandatory (developers can choose)
- **Clear Guidance**: Must provide decision criteria (when to use, when not to)
- **.NET Idioms**: Must follow .NET conventions (async/await, nullable types)
- **Testability**: Services must be easily unit-testable

---

## Decision

**We adopt the Service Layer Pattern as an optional architectural pattern for extracting complex business logic from Use Cases.**

Services are stateless classes that encapsulate domain logic which:
- Involves multiple aggregates (cross-aggregate operations)
- Is complex (>20 lines)
- Should be reusable across multiple Use Cases
- Requires focused unit testing

### Details

#### 1. Service Naming Convention

**Pattern**: `I<Verb><DomainConcept>Service`

```csharp
// ✅ Good naming
public interface ITransferMoneyService { ... }
public interface ICalculateInterestService { ... }
public interface IValidateCreditLimitService { ... }

// ❌ Bad naming
public interface IAccountService { ... }           // Too generic
public interface IMoneyHelper { ... }              // Not descriptive
public interface IAccountManager { ... }           // Avoid "Manager"
```

#### 2. Service Interface Design

Services must:
- Define clear contracts with domain-specific return types
- Document all exceptions via XML comments
- Use async/await for I/O operations
- Be stateless (no instance fields except dependencies)

```csharp
/// <summary>
/// Service for transferring money between two bank accounts.
/// </summary>
public interface ITransferMoneyService
{
    /// <summary>
    /// Transfers the specified amount from source to destination account.
    /// </summary>
    /// <exception cref="InsufficientBalanceException">When balance insufficient</exception>
    /// <exception cref="TransferLimitExceededException">When limit exceeded</exception>
    Task<TransferConfirmation> TransferAsync(
        AccountId fromAccountId,
        AccountId toAccountId,
        Money amount);
}
```

#### 3. Service Implementation

Services should:
- Inject dependencies via constructor (IRepository, other Services)
- Use private methods to break down complex validation
- Throw domain exceptions (not return error codes)
- Return domain-specific result types (not primitives)

```csharp
public sealed class TransferMoneyService : ITransferMoneyService
{
    private readonly IRepository<BankAccount, AccountId, IInternalDomainEvent> _repository;
    private static readonly Money TransferLimit = new(10000, "USD");

    public TransferMoneyService(IRepository<BankAccount, AccountId, IInternalDomainEvent> repository)
    {
        _repository = repository;
    }

    public async Task<TransferConfirmation> TransferAsync(...)
    {
        // 1. Validate inputs (fail fast)
        ValidateTransferAmount(amount);
        ValidateDifferentAccounts(fromAccountId, toAccountId);

        // 2. Load aggregates
        var fromAccount = await _repository.FindByIdAsync(fromAccountId);
        var toAccount = await _repository.FindByIdAsync(toAccountId);

        // 3. Validate business rules
        ValidateAccountsNotClosed(fromAccount, toAccount);
        ValidateSufficientBalance(fromAccount, amount);

        // 4. Execute domain operations
        fromAccount.Withdraw(amount);
        toAccount.Deposit(amount);

        // 5. Persist changes
        await _repository.SaveAsync(fromAccount);
        await _repository.SaveAsync(toAccount);

        // 6. Return domain result
        return new TransferConfirmation(...);
    }

    #region Private Validation Methods
    private static void ValidateTransferAmount(Money amount) { ... }
    private static void ValidateDifferentAccounts(...) { ... }
    // ... other validations
    #endregion
}
```

#### 4. Use Case with Service

Use Cases delegate to Services and map exceptions to CQRS outputs:

```csharp
public sealed class TransferMoneyUseCase : ICommand<TransferMoneyInput, CqrsOutput<TransferMoneyOutput>>
{
    private readonly ITransferMoneyService _transferService;

    public TransferMoneyUseCase(ITransferMoneyService transferService)
    {
        _transferService = transferService;
    }

    public async Task<CqrsOutput<TransferMoneyOutput>> ExecuteAsync(TransferMoneyInput input)
    {
        try
        {
            // Delegate to Service (business logic encapsulated)
            var confirmation = await _transferService.TransferAsync(
                input.FromAccountId,
                input.ToAccountId,
                input.Amount);

            return CqrsOutput<TransferMoneyOutput>.Success(...);
        }
        catch (AccountNotFoundException ex)
        {
            return CqrsOutput<TransferMoneyOutput>.Failure(
                ExitCode.ResourceNotFoundFailure, ex.Message);
        }
        // ... map other exceptions to CQRS outputs
    }
}
```

#### 5. Decision Criteria

**✅ Extract to Service When**:
- Business logic >20 lines in Use Case
- Cross-aggregate operations (multiple aggregates involved)
- Reusable logic needed in multiple Use Cases
- Complex business rules that need focused testing
- Algorithmic complexity that would clutter Use Case

**❌ Keep in Use Case When**:
- Simple orchestration (<20 lines total)
- Single aggregate operation (no cross-aggregate logic)
- One-time logic (no reuse expected)
- CRUD operations (no complex business rules)

#### 6. Testing Strategy

**Services**: Fast unit tests (5-10ms per test)
```csharp
[Fact]
public async Task TransferAsync_InsufficientBalance_ThrowsException()
{
    // Arrange: Simple setup with in-memory repository
    var repository = new InMemoryRepository<BankAccount, AccountId>();
    var service = new TransferMoneyService(repository);
    // ... setup accounts

    // Act & Assert: Test exception directly
    await Assert.ThrowsAsync<InsufficientBalanceException>(...);
}
```

**Use Cases**: Integration tests (optional, can test Service directly)

---

## Consequences

### Positive Consequences

- ✅ **Improved Testability**: Services easily unit-tested in isolation (10x faster than integration tests)
- ✅ **Reusability**: Business logic reusable across multiple Use Cases
- ✅ **Maintainability**: Use Cases remain thin (orchestration only, <20 lines)
- ✅ **Single Responsibility**: Clear separation of orchestration (Use Case) vs business logic (Service)
- ✅ **Clarity**: Private validation methods self-document business rules
- ✅ **Flexibility**: Optional pattern (developers choose when to use)

### Negative Consequences

- ❌ **Indirection**: Adds one more layer (cognitive overhead)
- ❌ **Over-Engineering Risk**: May lead to premature abstraction if misused
- ❌ **Discovery**: More classes to navigate in codebase
- ❌ **Learning Curve**: Developers must learn when to use Services vs Use Cases vs Aggregate methods

### Neutral Consequences

- ⚖️ **Code Volume**: Total lines increase (~75%) but better organized
  - Before: 60 lines in Use Case
  - After: 35 lines Use Case + 70 lines Service = 105 lines total
  - Trade-off: More code but better structure
- ⚖️ **Test Distribution**: Unit tests on Services, integration tests optional
  - Before: Integration tests only (slow)
  - After: Fast unit tests + optional integration tests

---

## Alternatives Considered

### Alternative 1: Keep All Logic in Use Cases

**Description**: Do not introduce Service layer, keep all business logic in Use Cases.

**Pros**:
- Simpler architecture (fewer layers)
- No indirection
- Easier for beginners

**Cons**:
- Fat Use Cases (40+ lines)
- Cannot reuse logic across Use Cases
- Difficult to unit test complex rules
- Violates Single Responsibility Principle

**Why rejected**: Pain points become unmanageable as Use Cases grow complex. Fat Use Cases are hard to maintain and test.

---

### Alternative 2: Put All Logic in Aggregates

**Description**: Move all business logic to Aggregate methods instead of Services.

**Pros**:
- Domain logic stays in domain layer
- No extra layer

**Cons**:
- Doesn't work for cross-aggregate operations (TransferMoney involves two aggregates)
- Aggregates become bloated with multi-aggregate coordination logic
- Violates aggregate boundaries (one aggregate should not manage another)

**Why rejected**: Cross-aggregate operations don't naturally belong to a single aggregate. Services are the correct pattern for this.

---

### Alternative 3: Mandatory Service Layer (Always Extract)

**Description**: Require all Use Cases to delegate to Services (no logic in Use Cases).

**Pros**:
- Consistent architecture
- Forces separation of concerns

**Cons**:
- Over-engineering for simple Use Cases
- Creates unnecessary indirection for CRUD operations
- Adds boilerplate for trivial operations
- Conflicts with YAGNI principle

**Why rejected**: Simple Use Cases (<20 lines, single aggregate) don't benefit from extraction. Pattern should be optional.

---

### Alternative 4: Domain Services in Entity Layer

**Description**: Put Services in `EzDdd.Entity` layer (like Java DDD Domain Services).

**Pros**:
- Follows classic DDD terminology
- Services are "domain layer" conceptually

**Cons**:
- `EzDdd.Entity` has zero dependencies (cannot reference IRepository)
- Services need repository access (cross-aggregate coordination)
- Breaks layered architecture (Entity → UseCase dependency)

**Why rejected**: Services need IRepository (Use Case layer abstraction). Putting Services in Entity layer creates circular dependency.

**Decision**: Services belong at the **Use Case / Application boundary** (can use IRepository).

---

## Related Decisions

- **Related to**: [ADR-0003 - Module Architecture](0003-module-architecture-dependency-chain.md) - Services belong to Use Case layer boundary
- **Depends on**: [ADR-0003](0003-module-architecture-dependency-chain.md) - Use Case layer must exist before Services
- **Related to**: [ADR-0017 - CqrsOutput Implementation Strategy](0017-cqrsoutput-implementation-strategy.md) - Use Cases map Service exceptions to CqrsOutput

---

## Implementation Notes

### 1. **Service Location**

Services belong at the **Use Case / Application layer boundary**:
- Can use `IRepository<TAggregate, TId, TEvent>` (Use Case abstraction)
- Can call other Services
- Cannot be called by Aggregates (direction: UseCase → Service, not Aggregate → Service)

### 2. **Transaction Management**

Services **do NOT manage transactions**:
- Transaction boundaries remain at `IRepositoryPeer` level (ADR-0013)
- Services call `repository.SaveAsync()` for each aggregate
- Actual transaction is at infrastructure layer

### 3. **Error Handling**

Services **throw domain exceptions**:
- Use Cases catch exceptions and map to `ExitCode` (CQRS layer)
- Domain exceptions are more precise than exit codes
- Exception types document business rules

### 4. **Dependency Injection**

Services registered as **Scoped** (per request):
```csharp
// ASP.NET Core example
services.AddScoped<ITransferMoneyService, TransferMoneyService>();
```

### 5. **Documentation**

All Services must have:
- Complete pattern documentation: [docs/patterns/SERVICE_LAYER_PATTERN.md](../patterns/SERVICE_LAYER_PATTERN.md)
- Before/After comparison: [docs/patterns/SERVICE_LAYER_BEFORE_AFTER.md](../patterns/SERVICE_LAYER_BEFORE_AFTER.md)
- Example implementation: [tests/EzDdd.UseCase.Tests/Integration/Services/](../../tests/EzDdd.UseCase.Tests/Integration/Services/)

---

## References

### Internal Documentation
- [SERVICE_LAYER_PATTERN.md](../patterns/SERVICE_LAYER_PATTERN.md) - Complete pattern documentation (564 lines)
- [SERVICE_LAYER_BEFORE_AFTER.md](../patterns/SERVICE_LAYER_BEFORE_AFTER.md) - Before/After comparison (672 lines)
- [TransferMoneyService.cs](../../tests/EzDdd.UseCase.Tests/Integration/Services/TransferMoneyService.cs) - Example implementation
- [TransferMoneyServiceTests.cs](../../tests/EzDdd.UseCase.Tests/Integration/Services/TransferMoneyServiceTests.cs) - 13 unit tests

### Java ezddd 4.1.0
- Java commit: `27b7c6e` - Extract complex business logic to Service classes
- Rationale: Keep Use Cases thin (orchestration only)

### Domain-Driven Design References
- Eric Evans - Domain-Driven Design (2003), Chapter 5: "Services"
- Vaughn Vernon - Implementing Domain-Driven Design (2013), Chapter 7: "Services"
- Robert C. Martin - Clean Architecture (2017), Chapter 20: "Use Cases vs Entities"

### Related ADRs
- [ADR-0003 - Module Architecture](0003-module-architecture-dependency-chain.md)
- [ADR-0017 - CqrsOutput Implementation Strategy](0017-cqrsoutput-implementation-strategy.md)
- [ADR-0013 - Transaction Boundaries in Repository Pattern](0013-transaction-boundaries-repository-pattern.md)

---

## Revision History

| Date       | Status   | Notes                                      |
|------------|----------|--------------------------------------------|
| 2026-01-08 | Accepted | Initial decision with complete documentation |

---

## Summary

**Service Layer Pattern** is an **optional** architectural pattern for extracting complex business logic (>20 lines, cross-aggregate, reusable) from Use Cases to dedicated Service classes.

**Benefits**:
- 10x faster tests (unit tests vs integration tests)
- Reusable logic across multiple Use Cases
- Thin Use Cases (<20 lines orchestration)
- Clear separation of concerns

**When to Use**:
- Business logic >20 lines
- Cross-aggregate operations
- Reusable logic needed
- Complex rules needing focused testing

**When NOT to Use**:
- Simple orchestration (<20 lines)
- Single aggregate operations
- One-time logic (no reuse)
- CRUD operations

**Key Design Principles**:
- Stateless (no instance fields except dependencies)
- Async/await throughout (I/O operations)
- Throw domain exceptions (not return error codes)
- Return domain-specific types (not primitives)
- Private validation methods (self-documenting)

**Example**: `TransferMoneyService` reduces Use Case from 60 lines → 15 lines, enables fast unit tests (5ms vs 50ms), and makes transfer logic reusable across scheduled transfers, batch transfers, etc.

---

*Last updated: 2026-01-08 (ezDDD.NET 1.0.0 - Phase 6 Stage S4)*
