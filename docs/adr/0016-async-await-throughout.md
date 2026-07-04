# ADR-0016: Async/Await Throughout (All I/O Operations)

## Status

**Accepted**

- **Date**: 2025-11-10
- **Deciders**: Project maintainers
- **Status Date**: 2025-11-10

---

## Context

### Problem Statement

Modern .NET applications require asynchronous programming for I/O operations to achieve:
- **Scalability**: Handle thousands of concurrent requests without thread exhaustion
- **Responsiveness**: Keep UI threads responsive in client applications
- **Resource efficiency**: Free threads during I/O waits instead of blocking
- **Cloud-native readiness**: Optimize for serverless and containerized environments

However, Java ezddd uses synchronous APIs throughout (`void execute()`, `Optional<T> findById()`). The question: **Should C# ezddd adopt async/await for all I/O operations, or maintain synchronous APIs for Java parity?**

### Relevant Context

**Java ezddd APIs** (Synchronous):
```java
// Java: All synchronous
public interface UseCase<I, O> {
    O execute(I input) throws UseCaseFailureException;
}

public interface Repository<T, ID> {
    Optional<T> findById(ID id);
    void save(T aggregate);
}

public interface Reactor<Input> {
    void execute(Input input);  // Synchronous
}
```

**Initial C# Design Question** (Phase 3 Planning):
- Option 1: Keep synchronous (Java parity)
- Option 2: Add async everywhere (modern .NET)
- Option 3: Provide both sync and async APIs

**.NET Async/Await Advantages**:
- **Thread pool efficiency**: Threads return to pool during I/O waits
- **Scalability**: ASP.NET Core can handle 10,000+ concurrent requests
- **Cancellation support**: Built-in `CancellationToken` for timeout/cancellation
- **Exception propagation**: Async exceptions properly captured in `Task<T>`
- **Composability**: Async methods compose naturally (`await` keyword)

**Constraints**:
- Must maintain semantic parity with Java (behavior, not API surface)
- Must remain idiomatic .NET (async/await is standard practice)
- Must not force async for non-I/O operations (domain logic remains sync)
- Must provide acceptable performance (async overhead < 1%)

---

## Decision

**All I/O operations in ezDDD.NET MUST be asynchronous using the async/await pattern. In-memory operations (domain logic, validation) remain synchronous.**

### Details

**Async API Design**:

1. **IUseCase<TInput, TOutput>**:
```csharp
public interface IUseCase<TInput, TOutput>
    where TInput : IInput
    where TOutput : IOutput
{
    Task<TOutput> ExecuteAsync(TInput input);  // Async
}
```

2. **IRepository<TAggregate, TId, TEvent>**:
```csharp
public interface IRepository<TAggregate, TId, TEvent>
{
    Task<TAggregate?> FindByIdAsync(TId id);      // Async
    Task SaveAsync(TAggregate aggregate);          // Async
    Task DeleteAsync(TAggregate aggregate);        // Async
}
```

3. **IReactor<TInput>**:
```csharp
public interface IReactor<in TInput>
{
    Task ExecuteAsync(TInput input);  // ✅ Async (Java uses void execute())
}
```

4. **IMessageBus<TMessage>**:
```csharp
public interface IMessageBus<TMessage>
{
    Task PostAsync(TMessage message);                    // Async
    Task RegisterAsync(IReactor<TMessage> reactor);      // Async
    Task UnregisterAsync(IReactor<TMessage> reactor);    // Async
}
```

**Synchronous Operations** (No I/O):
```csharp
// Domain logic: Synchronous
public class BankAccount : AggregateRoot<AccountId, InternalDomainEvent>
{
    public void Deposit(Money amount)  // Sync (no I/O)
    {
        Contract.Require(amount.Value > 0);
        var @event = new MoneyDeposited(amount);
        Apply(@event);  // Sync
    }
}

// Validation: Synchronous
public class DepositInput : IInput
{
    public void Validate()  // Sync (no I/O)
    {
        Contract.Require(Amount > 0);
    }
}
```

**Key Rules**:

1. **I/O Operations → Async**:
   - Database queries/commands (Repository)
   - HTTP API calls (IMessageProducer)
   - File operations (EventStore)
   - Message bus operations (IMessageBus)
   - Use case execution (IUseCase)

2. **In-Memory Operations → Sync**:
   - Domain logic (aggregate methods)
   - Validation (input/output validation)
   - Event application (When() method)
   - Business rule checks (invariants)
   - Object mapping (ToData/ToDomain)

3. **Method Naming**:
   - Async methods: `ExecuteAsync()`, `FindByIdAsync()`, `SaveAsync()`
   - Sync methods: `Deposit()`, `Validate()`, `Apply()`

4. **CancellationToken**:
   - Optional parameter for long-running operations
   - Example: `Task<TOutput> ExecuteAsync(TInput input, CancellationToken ct = default)`

**Java vs C# Comparison**:

| Operation | Java | C# | Reasoning |
|-----------|------|-----|-----------|
| UseCase.execute() | Sync | **Async** | I/O operation (calls Repository) |
| Repository.findById() | Sync | **Async** | Database I/O |
| Repository.save() | Sync | **Async** | Database I/O |
| Reactor.execute() | Sync | **Async** | I/O operation (calls Repository) |
| MessageBus.post() | Sync | **Async** | Potentially I/O (external bus) |
| Aggregate.deposit() | Sync | **Sync** | In-memory domain logic |
| Input.validate() | Sync | **Sync** | In-memory validation |

**Example Usage**:

```csharp
// Use Case (Async I/O)
public class DepositMoneyCommand : ICommand<DepositInput, DepositOutput>
{
    private readonly IRepository<BankAccount, AccountId, InternalDomainEvent> _repository;

    public async Task<DepositOutput> ExecuteAsync(DepositInput input)
    {
        // 1. Async I/O: Load aggregate from database
        var account = await _repository.FindByIdAsync(input.AccountId);
        if (account == null)
            return new DepositOutput { ExitCode = ExitCode.ResourceNotFoundFailure };

        // 2. Sync domain logic: Execute business rule
        account.Deposit(input.Amount);  // Synchronous!

        // 3. Async I/O: Save aggregate to database
        try
        {
            await _repository.SaveAsync(account);
        }
        catch (RepositorySaveException ex)
        {
            return new DepositOutput { ExitCode = ExitCode.ConflictFailure };
        }

        return new DepositOutput { ExitCode = ExitCode.Success };
    }
}

// Reactor (Async I/O)
public class AccountCreatedReactor : IReactor<AccountCreated>
{
    private readonly IRepository<BankAccount, AccountId, InternalDomainEvent> _repository;

    public async Task ExecuteAsync(AccountCreated input)
    {
        // Async I/O: Reactor performs side effects (database operations)
        var account = await _repository.FindByIdAsync(input.AccountId);
        // ... perform side effect ...
        await _repository.SaveAsync(account);
    }
}
```

**ASP.NET Core Integration**:
```csharp
[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly ICommand<DepositInput, DepositOutput> _depositCommand;

    [HttpPost("{id}/deposit")]
    public async Task<IActionResult> Deposit(string id, DepositRequest request)
    {
        var input = new DepositInput { AccountId = new AccountId(id), Amount = request.Amount };

        // Async all the way: Controller → UseCase → Repository → Database
        var output = await _depositCommand.ExecuteAsync(input);

        return output.ExitCode == ExitCode.Success
            ? Ok(output)
            : BadRequest(output);
    }
}
```

---

## Consequences

### Positive Consequences

- ✅ **Scalability**: ASP.NET Core can handle 10,000+ concurrent requests with async I/O
- ✅ **Thread pool efficiency**: Threads return to pool during I/O waits (not blocked)
- ✅ **Modern .NET idiom**: Follows .NET Core/5+/6+/8 best practices
- ✅ **Cancellation support**: Built-in timeout and cancellation via `CancellationToken`
- ✅ **Composability**: Async methods compose naturally with `await`
- ✅ **Exception handling**: Async exceptions properly propagated in `Task<T>`
- ✅ **Cloud-native ready**: Optimized for serverless (Azure Functions, AWS Lambda)
- ✅ **Responsive UI**: Client applications (Blazor, WPF, WinForms) remain responsive

### Negative Consequences

- ❌ **API surface difference from Java**: `ExecuteAsync()` vs `execute()` (naming divergence)
- ❌ **Async complexity**: Developers must understand async/await (learning curve)
- ❌ **Async propagation**: Async "infects" calling code (must await all the way up)
- ❌ **Performance overhead**: Small overhead (~1-5μs) for async state machine
- ❌ **Debugging complexity**: Async stack traces can be harder to read

### Neutral Consequences

- ⚖️ **Semantic parity maintained**: Behavior identical to Java (only API signature differs)
- ⚖️ **Domain logic remains sync**: Aggregate methods still synchronous (no I/O)
- ⚖️ **Testing**: Async tests with `Task` assertions (e.g., `await act.Should().ThrowAsync()`)

---

## Alternatives Considered

### Alternative 1: Synchronous APIs (Java Parity)

**Description**: Keep all APIs synchronous to match Java ezddd exactly

**Implementation**:
```csharp
public interface IUseCase<TInput, TOutput>
{
    TOutput Execute(TInput input);  // Synchronous
}

public interface IRepository<TAggregate, TId, TEvent>
{
    TAggregate? FindById(TId id);  // Synchronous
    void Save(TAggregate aggregate);
}
```

**Pros**:
- Perfect API parity with Java
- Simpler implementation (no async complexity)
- Easier for Java developers to understand
- No async state machine overhead

**Cons**:
- **Violates .NET best practices**: Blocking I/O is anti-pattern in .NET Core
- **Poor scalability**: Thread-per-request model limits concurrency
- **ASP.NET Core incompatibility**: Blocking calls in async contexts cause deadlocks
- **Thread pool exhaustion**: Blocked threads cannot serve other requests
- **Not cloud-native**: Serverless functions penalized for thread blocking
- **Ecosystem friction**: Most .NET libraries are async (Entity Framework Core, HttpClient)

**Why rejected**: Goes against fundamental .NET design principles. Modern .NET applications require async I/O for scalability and resource efficiency. Blocking calls would make ezDDD.NET a poor citizen in the .NET ecosystem.

---

### Alternative 2: Provide Both Sync and Async APIs

**Description**: Offer both synchronous and asynchronous versions of all APIs

**Implementation**:
```csharp
public interface IUseCase<TInput, TOutput>
{
    TOutput Execute(TInput input);              // Sync
    Task<TOutput> ExecuteAsync(TInput input);   // Async
}

public interface IRepository<TAggregate, TId, TEvent>
{
    TAggregate? FindById(TId id);                    // Sync
    Task<TAggregate?> FindByIdAsync(TId id);         // Async

    void Save(TAggregate aggregate);                 // Sync
    Task SaveAsync(TAggregate aggregate);            // Async
}
```

**Pros**:
- Maximum flexibility (users choose sync or async)
- Java parity available (sync APIs)
- Async available for modern scenarios

**Cons**:
- **API explosion**: 2x method count for every I/O operation
- **Implementation duplication**: Sync methods wrap async (or vice versa)
- **Maintenance burden**: Both versions must be tested and documented
- **Deadlock risk**: Sync methods calling async can cause deadlocks
- **Confusion**: Users unsure which version to use
- **False parity**: Sync methods would internally use async (not truly synchronous)

**Why rejected**: Over-engineering with significant maintenance burden. The sync methods would likely just wrap async methods with `.Result` or `Task.Run()`, creating deadlock risks and performance issues. Better to commit fully to async.

---

### Alternative 3: Async Everything (Including Domain Logic)

**Description**: Make even domain logic asynchronous

**Implementation**:
```csharp
public class BankAccount : AggregateRoot<AccountId, InternalDomainEvent>
{
    public async Task DepositAsync(Money amount)  // Async domain logic
    {
        await Task.CompletedTask;  // No actual I/O
        Contract.Require(amount.Value > 0);
        var @event = new MoneyDeposited(amount);
        Apply(@event);
    }
}
```

**Pros**:
- Uniform async API surface
- Future-proof if domain logic needs I/O (e.g., calling external validation service)

**Cons**:
- **Unnecessary complexity**: Domain logic has no I/O (pure memory operations)
- **Performance overhead**: Async state machine for no benefit
- **Confusing intent**: Async implies I/O, but domain logic is in-memory
- **Harder to reason about**: Aggregate methods should be fast, synchronous, deterministic
- **Test complexity**: Async tests for synchronous behavior

**Why rejected**: Domain logic (aggregate methods, validation, invariant checks) is purely in-memory and should remain synchronous. Async overhead provides no benefit for CPU-bound operations. Clear separation: I/O = async, domain = sync.

---

## Related Decisions

- **Related to**: [ADR-0001](0001-target-framework.md) - .NET 8 has mature async/await support
- **Related to**: [ADR-0005](0005-complete-reimplementation-approach.md) - Complete reimplementation allows platform-specific optimizations
- **Related to**: [ADR-0013](0013-transaction-boundaries-repository-pattern.md) - Async transactions at IRepositoryPeer layer
- **Influences**: All API designs (IUseCase, IRepository, IReactor, IMessageBus, IRepositoryPeer)

---

## Implementation Notes

### Implementation Checklist (Phase 3 - Completed 2025-11-06)

- ✅ All I/O interfaces use async/await:
  - `IUseCase<TInput, TOutput>.ExecuteAsync()`
  - `IRepository<TAggregate, TId, TEvent>.FindByIdAsync()`
  - `IRepository<TAggregate, TId, TEvent>.SaveAsync()`
  - `IRepository<TAggregate, TId, TEvent>.DeleteAsync()`
  - `IReactor<TInput>.ExecuteAsync()`  ← Key difference from Java's sync execute()
  - `IMessageBus<TMessage>.PostAsync()`
  - `IRepositoryPeer<TData, TId>.FindByIdAsync()`
  - `IRepositoryPeer<TData, TId>.SaveAsync()`
  - `IRepositoryPeer<TData, TId>.DeleteAsync()`
- ✅ All domain logic remains synchronous:
  - `AggregateRoot.Apply()`
  - `EsAggregateRoot.When()`
  - Aggregate business methods (Deposit, Withdraw, etc.)
  - Input/Output validation
- ✅ All tests use async test methods with `Task` return type
- ✅ All 433 tests passing with async/await
- ✅ Zero new compiler warnings

### Async Best Practices

**DO**:
- ✅ Use `async Task<T>` for methods with I/O
- ✅ Use `await` for all async calls (never `.Result` or `.Wait()`)
- ✅ Name async methods with `Async` suffix
- ✅ Provide `CancellationToken` for long-running operations
- ✅ Use `ConfigureAwait(false)` in library code (not application code)

**DON'T**:
- ❌ Use async for CPU-bound domain logic
- ❌ Block on async calls with `.Result` or `.Wait()`
- ❌ Use `Task.Run()` to wrap synchronous code as async
- ❌ Return `Task` from methods without I/O (return `T` instead)

**Example: Proper Async/Await**:
```csharp
// ✅ CORRECT: Async all the way
public async Task<DepositOutput> ExecuteAsync(DepositInput input)
{
    var account = await _repository.FindByIdAsync(input.AccountId);  // Await
    account.Deposit(input.Amount);  // Sync domain logic
    await _repository.SaveAsync(account);  // Await
    return new DepositOutput { ExitCode = ExitCode.Success };
}

// ❌ WRONG: Blocking with .Result
public DepositOutput Execute(DepositInput input)
{
    var account = _repository.FindByIdAsync(input.AccountId).Result;  // Deadlock risk!
    account.Deposit(input.Amount);
    _repository.SaveAsync(account).Wait();  // Deadlock risk!
    return new DepositOutput { ExitCode = ExitCode.Success };
}
```

### Testing Async Code

```csharp
[Fact]
public async Task ExecuteAsync_ValidInput_ReturnsSuccess()
{
    // Arrange
    var command = new DepositMoneyCommand(mockRepository);
    var input = new DepositInput { AccountId = "123", Amount = 100 };

    // Act
    var output = await command.ExecuteAsync(input);

    // Assert
    output.ExitCode.Should().Be(ExitCode.Success);
}

[Fact]
public async Task ExecuteAsync_InvalidAccount_ThrowsException()
{
    // Arrange
    var command = new DepositMoneyCommand(mockRepository);
    var input = new DepositInput { AccountId = "invalid", Amount = 100 };

    // Act & Assert
    await command.Invoking(c => c.ExecuteAsync(input))
        .Should().ThrowAsync<UseCaseFailureException>();
}
```

---

## References

- [Async/Await Best Practices](https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming) - Stephen Cleary
- [Task-based Asynchronous Pattern (TAP)](https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap)
- Phase 3 Group 2 review - IUseCase async verification (internal working note, not retained in the repository)
- Phase 3 Group 6 review - OutboxRepository async confirmation (internal working note, not retained in the repository)
- [IReactor.cs](../../src/EzDdd.UseCase/Port/In/IReactor.cs) - Async ExecuteAsync (lines 17)
- [Java Reactor.java](../../../../ezddd/ezddd-usecase/src/main/java/tw/teddysoft/ezddd/usecase/port/in/interactor/Reactor.java) - Sync execute() (line 16)
- [IRepository.cs](../../src/EzDdd.UseCase/Port/Out/IRepository.cs) - Async repository APIs
- Phase 3 post-review session notes - Phase 3 completion, line 247 (internal working note, not retained in the repository)

---

## Revision History

| Date       | Status   | Notes                          |
|------------|----------|--------------------------------|
| 2025-11-10 | Accepted | Decision finalized, Phase 3 implementation complete |

---
