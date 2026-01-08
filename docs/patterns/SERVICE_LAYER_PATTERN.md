# Service Layer Pattern in ezDDD.NET

**Pattern Type**: Domain Service Pattern
**Layer**: Use Cases / Application Layer
**Since**: ezDDD.NET 1.0.0 (based on Java ezddd 4.1.0)
**Status**: Recommended (Optional)

---

## Table of Contents

1. [Pattern Overview](#pattern-overview)
2. [When to Use Services](#when-to-use-services)
3. [Decision Tree](#decision-tree)
4. [Implementation Guidelines](#implementation-guidelines)
5. [Best Practices](#best-practices)
6. [Examples](#examples)
7. [Anti-Patterns](#anti-patterns)
8. [Related Patterns](#related-patterns)
9. [References](#references)

---

## Pattern Overview

### What is a Service?

A **Service** in Domain-Driven Design (DDD) is a stateless object that encapsulates domain logic that:
- Doesn't naturally belong to any single Entity or Value Object
- Operates on multiple aggregates
- Contains complex business rules that would bloat Use Cases
- Needs to be reused across multiple Use Cases

### Service vs Use Case

| Aspect | Use Case (IUseCase) | Service (Domain Service) |
|--------|---------------------|--------------------------|
| **Purpose** | Orchestrates application workflows | Encapsulates domain logic |
| **Layer** | Application Layer | Domain / Application boundary |
| **State** | Stateless (per execution) | Stateless (always) |
| **Reusability** | Single workflow | Multiple Use Cases |
| **Dependencies** | Repositories, Services, Queries | Repositories, other Services |
| **Complexity** | Simple orchestration (<20 lines) | Complex logic (>20 lines) |
| **Testing** | Integration-focused | Unit-testable logic |

### Why Extract Services?

**Benefits**:
1. ✅ **Reusability**: Share complex logic across multiple Use Cases
2. ✅ **Testability**: Isolate and unit-test complex business rules
3. ✅ **Maintainability**: Keep Use Cases thin (orchestration only)
4. ✅ **Single Responsibility**: Each Service has one clear purpose
5. ✅ **Composability**: Combine services to build complex workflows

**Trade-offs**:
- ⚠️ **Indirection**: Adds another layer (cognitive overhead)
- ⚠️ **Over-engineering**: Can lead to premature abstraction
- ⚠️ **Discoverability**: More classes to navigate

**Golden Rule**: Extract Services when they provide clear value, not by default.

---

## When to Use Services

### ✅ Use a Service When

1. **Complex Business Logic (>20 lines)**
   - Logic is too complex to inline in a Use Case
   - Would make the Use Case hard to understand
   - Example: Multi-step validation with branching logic

2. **Cross-Aggregate Operations**
   - Logic involves multiple aggregates of different types
   - Coordination between aggregates is non-trivial
   - Example: Transfer money between two accounts (two aggregates)

3. **Reusable Domain Logic**
   - Same logic needed in multiple Use Cases
   - Logic represents a domain concept
   - Example: Currency conversion, tax calculation

4. **Domain Rules Enforcement**
   - Complex business rules that don't fit in a single aggregate
   - Rules span multiple bounded contexts
   - Example: Credit limit check across account and customer

5. **Algorithmic Complexity**
   - Logic involves non-trivial algorithms
   - Multiple decision points or calculations
   - Example: Interest calculation, risk scoring

### ❌ Don't Use a Service When

1. **Simple CRUD Operations**
   - Direct aggregate save/load without logic
   - Use Case is <20 lines
   - Example: Simple "CreateAccount" with no validation

2. **Single Aggregate Operations**
   - Logic naturally belongs to one aggregate
   - Put it in the aggregate itself
   - Example: "Deposit money" (single account operation)

3. **Presentation Logic**
   - Formatting, UI concerns, view models
   - This belongs in Projections or Controllers
   - Example: Formatting currency for display

4. **Infrastructure Concerns**
   - Database transactions, messaging, logging
   - Handle at IRepositoryPeer or infrastructure layer
   - Example: Transaction management (use IRepositoryPeer)

5. **Premature Abstraction**
   - Logic used in only one place
   - No clear reuse or complexity benefit
   - YAGNI (You Aren't Gonna Need It)

---

## Decision Tree

Use this decision tree to decide: **Use Case only vs Use Case + Service**

```
START: I need to implement business logic
│
├─ Is the logic >20 lines?
│  ├─ NO → Keep in Use Case
│  └─ YES → Continue ↓
│
├─ Does it involve multiple aggregates?
│  ├─ NO → Consider putting in Aggregate
│  └─ YES → Continue ↓
│
├─ Will it be reused in multiple Use Cases?
│  ├─ NO → Continue ↓
│  └─ YES → ✅ Extract Service
│
├─ Is it complex domain logic (algorithms, rules)?
│  ├─ NO → Keep in Use Case (one-time orchestration)
│  └─ YES → ✅ Extract Service
│
└─ Is it UI/infrastructure concern?
   ├─ YES → NOT a Service (belongs elsewhere)
   └─ NO → ✅ Extract Service (if still complex)
```

---

## Implementation Guidelines

### 1. Service Naming Conventions

**Pattern**: `<Verb><DomainConcept>Service`

**Good Examples**:
- ✅ `TransferMoneyService` (clear action + domain concept)
- ✅ `CalculateInterestService` (clear action + domain concept)
- ✅ `ValidateCreditLimitService` (clear action + domain concept)

**Bad Examples**:
- ❌ `AccountService` (too generic, not a verb)
- ❌ `MoneyTransferrer` (not following naming convention)
- ❌ `Helper`, `Manager`, `Utility` (meaningless names)

### 2. Service Interface Design

**Template**:
```csharp
/// <summary>
/// Service for [clear description of domain capability].
/// </summary>
public interface I<Verb><DomainConcept>Service
{
    /// <summary>
    /// [Description of what this method does and why].
    /// </summary>
    /// <param name="...">...</param>
    /// <returns>...</returns>
    /// <exception cref="DomainException">When [specific condition]</exception>
    Task<TResult> <Verb>Async(...);
}
```

**Example**:
```csharp
/// <summary>
/// Service for transferring money between two bank accounts.
/// </summary>
public interface ITransferMoneyService
{
    /// <summary>
    /// Transfers the specified amount from the source account to the destination account.
    /// Validates sufficient balance, enforces transfer limits, and ensures atomic operation.
    /// </summary>
    /// <param name="fromAccountId">The source account identifier</param>
    /// <param name="toAccountId">The destination account identifier</param>
    /// <param name="amount">The amount to transfer (must be positive)</param>
    /// <returns>A transfer confirmation with transaction ID</returns>
    /// <exception cref="InsufficientBalanceException">When source account has insufficient balance</exception>
    /// <exception cref="TransferLimitExceededException">When amount exceeds daily limit</exception>
    Task<TransferConfirmation> TransferAsync(
        AccountId fromAccountId,
        AccountId toAccountId,
        Money amount);
}
```

### 3. Service Implementation Structure

**Key Principles**:
1. ✅ **Stateless**: No instance fields (except injected dependencies)
2. ✅ **Async**: All I/O operations use `async/await`
3. ✅ **Single Responsibility**: One clear purpose
4. ✅ **Dependency Injection**: Constructor injection for dependencies
5. ✅ **Error Handling**: Throw domain exceptions for business rule violations

**Template**:
```csharp
public class <Verb><DomainConcept>Service : I<Verb><DomainConcept>Service
{
    private readonly IRepository<Aggregate1, Id1> _repository1;
    private readonly IRepository<Aggregate2, Id2> _repository2;
    // ... other dependencies

    public <Verb><DomainConcept>Service(
        IRepository<Aggregate1, Id1> repository1,
        IRepository<Aggregate2, Id2> repository2)
    {
        _repository1 = repository1;
        _repository2 = repository2;
    }

    public async Task<TResult> <Verb>Async(...)
    {
        // 1. Validate inputs (fail fast)
        Contract.Require("description", () => condition);

        // 2. Load aggregates
        var aggregate1 = await _repository1.FindByIdAsync(id1);
        var aggregate2 = await _repository2.FindByIdAsync(id2);

        // 3. Validate domain rules (throw domain exceptions)
        if (!businessRule)
        {
            throw new DomainException("message");
        }

        // 4. Execute domain logic
        aggregate1.DoSomething(...);
        aggregate2.DoSomething(...);

        // 5. Persist changes (repositories handle transactions at IRepositoryPeer level)
        await _repository1.SaveAsync(aggregate1);
        await _repository2.SaveAsync(aggregate2);

        // 6. Return result
        return new TResult(...);
    }
}
```

### 4. Service Registration (Dependency Injection)

**ASP.NET Core Example**:
```csharp
// Startup.cs or Program.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register services as scoped (per request)
    services.AddScoped<ITransferMoneyService, TransferMoneyService>();
    services.AddScoped<ICalculateInterestService, CalculateInterestService>();

    // Repositories (scoped for transaction management)
    services.AddScoped<IRepository<BankAccount, AccountId>, EsRepository<BankAccount, AccountId, InternalDomainEvent>>();
}
```

**Service Lifetime**:
- ✅ **Scoped** (Recommended): Per HTTP request or per operation
- ❌ **Singleton**: Avoid (services should be stateless but scoped is safer)
- ⚠️ **Transient**: Acceptable but creates more instances

### 5. Service Testing Strategy

**Unit Tests** (Isolated, Fast):
```csharp
public class TransferMoneyServiceTests
{
    [Fact]
    public async Task TransferAsync_WithSufficientBalance_Success()
    {
        // Arrange: Use in-memory repositories or test doubles
        var fromAccount = BankAccount.Open(...);
        var toAccount = BankAccount.Open(...);
        var fromRepo = new InMemoryRepository<BankAccount, AccountId>();
        var toRepo = new InMemoryRepository<BankAccount, AccountId>();
        await fromRepo.SaveAsync(fromAccount);
        await toRepo.SaveAsync(toAccount);

        var service = new TransferMoneyService(fromRepo, toRepo);

        // Act
        var result = await service.TransferAsync(
            fromAccount.Id,
            toAccount.Id,
            Money.Of(100, "USD"));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TransferStatus.Success, result.Status);

        var updatedFrom = await fromRepo.FindByIdAsync(fromAccount.Id);
        Assert.Equal(Money.Of(900, "USD"), updatedFrom!.Balance);
    }

    [Fact]
    public async Task TransferAsync_InsufficientBalance_ThrowsException()
    {
        // Arrange
        var fromAccount = BankAccount.Open(..., initialBalance: Money.Of(50, "USD"));
        // ... setup

        var service = new TransferMoneyService(fromRepo, toRepo);

        // Act & Assert
        await Assert.ThrowsAsync<InsufficientBalanceException>(
            () => service.TransferAsync(fromAccount.Id, toAccount.Id, Money.Of(100, "USD")));
    }
}
```

---

## Best Practices

### ✅ DO

1. **Keep Services Stateless**
   - No instance fields (except injected dependencies)
   - All state passed as method parameters
   - Enables thread-safe, reusable services

2. **Use Constructor Injection**
   - All dependencies injected via constructor
   - Makes dependencies explicit and testable
   - Enables easy mocking in tests

3. **Validate Early (Fail Fast)**
   - Use `Contract.Require()` for precondition validation
   - Throw domain exceptions for business rule violations
   - Return domain-specific result types (not generic exceptions)

4. **One Public Method Per Service (Preferred)**
   - Service name and method name should align
   - `ITransferMoneyService.TransferAsync()` ✅
   - If multiple methods needed, consider splitting into multiple services

5. **Document Domain Logic**
   - Clear XML documentation on interfaces
   - Explain business rules and invariants
   - Document exceptions and edge cases

6. **Return Domain-Specific Types**
   - `TransferConfirmation` not `bool` ✅
   - `InterestCalculationResult` not `decimal` ✅
   - Provides context and enables future extension

### ❌ DON'T

1. **Don't Add State to Services**
   - ❌ `private decimal _runningTotal;`
   - ✅ All state passed as parameters or returned

2. **Don't Manage Transactions in Services**
   - ❌ Starting/committing transactions in service
   - ✅ Transactions handled at IRepositoryPeer level

3. **Don't Let Services Call Other Use Cases**
   - ❌ `ITransferMoneyService` calling `IWithdrawMoneyUseCase`
   - ✅ Services can call other services or repositories

4. **Don't Create Generic "Manager" or "Helper" Services**
   - ❌ `AccountManager`, `BankingHelper`
   - ✅ Specific, focused services with clear names

5. **Don't Put Presentation Logic in Services**
   - ❌ Currency formatting, date formatting
   - ✅ Services contain pure domain logic only

6. **Don't Create Services for Simple Operations**
   - ❌ `ICreateAccountService` for single aggregate creation
   - ✅ Keep simple operations directly in Use Cases

---

## Examples

### Example 1: Transfer Money Between Accounts (Cross-Aggregate)

**Scenario**: Transfer money from one account to another (requires coordinating two aggregates).

#### Without Service (Anti-Pattern)

```csharp
// TransferMoneyUseCase.cs (BEFORE - Too much logic in Use Case)
public class TransferMoneyUseCase : ICommand<TransferMoneyInput, TransferMoneyOutput>
{
    private readonly IRepository<BankAccount, AccountId> _repository;

    public async Task<TransferMoneyOutput> ExecuteAsync(TransferMoneyInput input)
    {
        // 1. Load both accounts
        var fromAccount = await _repository.FindByIdAsync(input.FromAccountId);
        var toAccount = await _repository.FindByIdAsync(input.ToAccountId);

        if (fromAccount == null) throw new AccountNotFoundException(input.FromAccountId);
        if (toAccount == null) throw new AccountNotFoundException(input.ToAccountId);

        // 2. Validate transfer rules (COMPLEX LOGIC - SHOULD BE IN SERVICE)
        if (fromAccount.Balance < input.Amount)
        {
            throw new InsufficientBalanceException(fromAccount.Id, fromAccount.Balance, input.Amount);
        }

        if (input.Amount > Money.Of(10000, "USD"))
        {
            throw new TransferLimitExceededException(input.Amount);
        }

        if (fromAccount.Status == AccountStatus.Frozen)
        {
            throw new AccountFrozenException(fromAccount.Id);
        }

        // 3. Execute transfer (DOMAIN LOGIC)
        fromAccount.Withdraw(input.Amount, $"Transfer to {input.ToAccountId}");
        toAccount.Deposit(input.Amount, $"Transfer from {input.FromAccountId}");

        // 4. Save both accounts
        await _repository.SaveAsync(fromAccount);
        await _repository.SaveAsync(toAccount);

        return new TransferMoneyOutput
        {
            TransactionId = Guid.NewGuid(),
            Status = TransferStatus.Success
        };
    }
}
```

**Problems**:
- ❌ Use Case has 30+ lines of complex logic
- ❌ Transfer logic cannot be reused in other Use Cases
- ❌ Hard to test transfer rules in isolation
- ❌ Violates Single Responsibility Principle

#### With Service (Recommended)

```csharp
// ITransferMoneyService.cs (NEW - Extract Service Interface)
public interface ITransferMoneyService
{
    Task<TransferConfirmation> TransferAsync(
        AccountId fromAccountId,
        AccountId toAccountId,
        Money amount);
}

// TransferMoneyService.cs (NEW - Service Implementation)
public class TransferMoneyService : ITransferMoneyService
{
    private readonly IRepository<BankAccount, AccountId> _repository;

    public TransferMoneyService(IRepository<BankAccount, AccountId> repository)
    {
        _repository = repository;
    }

    public async Task<TransferConfirmation> TransferAsync(
        AccountId fromAccountId,
        AccountId toAccountId,
        Money amount)
    {
        // 1. Load accounts
        var fromAccount = await _repository.FindByIdAsync(fromAccountId);
        var toAccount = await _repository.FindByIdAsync(toAccountId);

        if (fromAccount == null) throw new AccountNotFoundException(fromAccountId);
        if (toAccount == null) throw new AccountNotFoundException(toAccountId);

        // 2. Validate transfer rules
        ValidateTransfer(fromAccount, amount);

        // 3. Execute transfer
        fromAccount.Withdraw(amount, $"Transfer to {toAccountId}");
        toAccount.Deposit(amount, $"Transfer from {fromAccountId}");

        // 4. Save both accounts
        await _repository.SaveAsync(fromAccount);
        await _repository.SaveAsync(toAccount);

        // 5. Return confirmation
        return new TransferConfirmation(
            TransactionId: Guid.NewGuid(),
            Status: TransferStatus.Success,
            FromAccountId: fromAccountId,
            ToAccountId: toAccountId,
            Amount: amount,
            Timestamp: DateTimeOffset.UtcNow);
    }

    private void ValidateTransfer(BankAccount fromAccount, Money amount)
    {
        if (fromAccount.Balance < amount)
        {
            throw new InsufficientBalanceException(fromAccount.Id, fromAccount.Balance, amount);
        }

        if (amount > Money.Of(10000, "USD"))
        {
            throw new TransferLimitExceededException(amount);
        }

        if (fromAccount.Status == AccountStatus.Frozen)
        {
            throw new AccountFrozenException(fromAccount.Id);
        }
    }
}

// TransferMoneyUseCase.cs (AFTER - Thin Orchestration)
public class TransferMoneyUseCase : ICommand<TransferMoneyInput, TransferMoneyOutput>
{
    private readonly ITransferMoneyService _transferService;

    public TransferMoneyUseCase(ITransferMoneyService transferService)
    {
        _transferService = transferService;
    }

    public async Task<TransferMoneyOutput> ExecuteAsync(TransferMoneyInput input)
    {
        // Use Case now just orchestrates (thin layer)
        var confirmation = await _transferService.TransferAsync(
            input.FromAccountId,
            input.ToAccountId,
            input.Amount);

        return new TransferMoneyOutput
        {
            TransactionId = confirmation.TransactionId,
            Status = confirmation.Status
        };
    }
}
```

**Benefits**:
- ✅ Use Case reduced to 10 lines (simple orchestration)
- ✅ Transfer logic reusable in other Use Cases (e.g., scheduled transfers)
- ✅ Service easily unit-testable in isolation
- ✅ Clear separation of concerns (orchestration vs domain logic)

---

### Example 2: Simple Account Creation (No Service Needed)

**Scenario**: Create a new bank account (single aggregate, simple logic).

#### Correct Approach (No Service)

```csharp
// OpenAccountUseCase.cs (Use Case only - no service needed)
public class OpenAccountUseCase : ICommand<OpenAccountInput, OpenAccountOutput>
{
    private readonly IRepository<BankAccount, AccountId> _repository;

    public OpenAccountUseCase(IRepository<BankAccount, AccountId> repository)
    {
        _repository = repository;
    }

    public async Task<OpenAccountOutput> ExecuteAsync(OpenAccountInput input)
    {
        // Simple logic - no need for a service
        var account = BankAccount.Open(
            AccountId.NewId(),
            input.Owner,
            Money.Of(input.InitialBalance, input.Currency));

        await _repository.SaveAsync(account);

        return new OpenAccountOutput
        {
            AccountId = account.Id,
            Status = "Success"
        };
    }
}
```

**Why No Service?**
- ✅ Single aggregate operation (BankAccount only)
- ✅ Simple logic (<20 lines)
- ✅ No reuse needed (one-time operation)
- ✅ No complex business rules

---

## Anti-Patterns

### ❌ Anti-Pattern 1: "God Service"

**Problem**: One service doing too many things.

```csharp
// ❌ BAD: AccountManagementService (does everything)
public interface IAccountManagementService
{
    Task<BankAccount> OpenAccountAsync(...);
    Task CloseAccountAsync(...);
    Task TransferMoneyAsync(...);
    Task CalculateInterestAsync(...);
    Task FreezeAccountAsync(...);
    Task UnfreezeAccountAsync(...);
    Task ValidateCreditLimitAsync(...);
    // ... 20 more methods
}
```

**Solution**: Split into focused services.

```csharp
// ✅ GOOD: Focused services
public interface ITransferMoneyService { ... }
public interface ICalculateInterestService { ... }
public interface IValidateCreditLimitService { ... }
```

---

### ❌ Anti-Pattern 2: "Anemic Service"

**Problem**: Service that just delegates to aggregate without adding value.

```csharp
// ❌ BAD: Service adds no value
public class DepositMoneyService
{
    public async Task DepositAsync(AccountId accountId, Money amount)
    {
        var account = await _repository.FindByIdAsync(accountId);
        account.Deposit(amount);  // Just delegates to aggregate
        await _repository.SaveAsync(account);
    }
}
```

**Solution**: Keep in Use Case or aggregate.

```csharp
// ✅ GOOD: Direct Use Case (no service needed)
public class DepositMoneyUseCase : ICommand<DepositInput, DepositOutput>
{
    public async Task<DepositOutput> ExecuteAsync(DepositInput input)
    {
        var account = await _repository.FindByIdAsync(input.AccountId);
        account.Deposit(input.Amount);
        await _repository.SaveAsync(account);
        return new DepositOutput { Success = true };
    }
}
```

---

### ❌ Anti-Pattern 3: "Stateful Service"

**Problem**: Service holding state between calls.

```csharp
// ❌ BAD: Service with state
public class AccountService
{
    private BankAccount _currentAccount;  // State!

    public void LoadAccount(AccountId id) { ... }
    public void Withdraw(Money amount) { _currentAccount.Withdraw(amount); }
    public void Save() { ... }
}
```

**Solution**: Services must be stateless.

```csharp
// ✅ GOOD: Stateless service
public class WithdrawMoneyService
{
    public async Task WithdrawAsync(AccountId accountId, Money amount)
    {
        var account = await _repository.FindByIdAsync(accountId);
        account.Withdraw(amount);
        await _repository.SaveAsync(account);
    }
}
```

---

## Related Patterns

### Service vs Use Case

| Pattern | Purpose | When to Use |
|---------|---------|-------------|
| **Use Case** | Orchestrate workflow | Always (application entry point) |
| **Service** | Encapsulate domain logic | Complex/reusable logic (optional) |

**Relationship**: Use Cases call Services (not the other way around).

### Service vs Aggregate Method

| Pattern | Purpose | When to Use |
|---------|---------|-------------|
| **Aggregate Method** | Single aggregate behavior | Logic for one aggregate |
| **Service** | Cross-aggregate logic | Logic spanning multiple aggregates |

**Example**:
- `BankAccount.Withdraw()` → Aggregate method ✅
- `TransferMoneyService.TransferAsync()` → Service ✅ (involves two accounts)

### Service vs Repository

| Pattern | Purpose | Layer |
|---------|---------|-------|
| **Repository** | Aggregate persistence | Infrastructure boundary |
| **Service** | Domain logic | Domain / Application |

**Relationship**: Services use Repositories (not the other way around).

---

## References

### Domain-Driven Design (DDD)

- **Eric Evans - Domain-Driven Design** (2003)
  - Chapter 5: "A Model Expressed in Software" (Services)
  - Chapter 6: "The Life Cycle of a Domain Object"

- **Vaughn Vernon - Implementing Domain-Driven Design** (2013)
  - Chapter 7: "Services"

### Clean Architecture

- **Robert C. Martin - Clean Architecture** (2017)
  - Use Cases vs Entities (Chapter 20)

### Java ezddd 4.1.0

- **Service Layer Pattern Refactoring**
  - Commit: `27b7c6e` - Extract complex business logic to Service classes
  - Rationale: Keep Use Cases thin (orchestration only)

### ezDDD.NET Documentation

- **[DOTNET_PORT.md](../../DOTNET_PORT.md)** - Java 4.1.0 Synchronization Plan (Stage S4)
- **[ADR-0026](../adr/0026-service-layer-pattern.md)** - Service Layer Pattern Decision
- **[TRANSACTION_BOUNDARY_GUIDE.md](../TRANSACTION_BOUNDARY_GUIDE.md)** - Where transactions belong

---

**Last Updated**: 2026-01-08 (ezDDD.NET 1.0.0 - Stage S4)
