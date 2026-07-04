# Migration Guide: Java ezddd → .NET ezDDD.NET

Complete guide for migrating from Java ezddd to .NET ezDDD.NET.

> **Version**: 1.0.0-alpha.1
> **Last Updated**: 2025-11-22
> **Java Version**: ezddd 2.x
> **Target Audience**: Java developers familiar with ezddd

---

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Language Differences](#language-differences)
  - [Syntax Comparison](#syntax-comparison)
  - [Async/Await (Java sync → .NET async)](#asyncawait-java-sync--net-async)
  - [Nullable Reference Types](#nullable-reference-types)
  - [Record Types vs Java Records](#record-types-vs-java-records)
  - [Collections and Generics](#collections-and-generics)
- [API Mapping](#api-mapping)
  - [Common Module](#common-module)
  - [Entity Module](#entity-module)
  - [UseCase Module](#usecase-module)
  - [Cqrs Module](#cqrs-module)
- [Pattern Changes](#pattern-changes)
  - [Event Handler (instanceof → pattern matching)](#event-handler-instanceof--pattern-matching)
  - [Repository Pattern (sync → async)](#repository-pattern-sync--async)
  - [Exception Handling](#exception-handling)
  - [Threading and Concurrency](#threading-and-concurrency)
- [Module Structure Differences](#module-structure-differences)
- [Dependency Injection](#dependency-injection)
- [Testing](#testing)
- [Code Examples](#code-examples)
  - [1. Event-Sourced Aggregate](#1-event-sourced-aggregate)
  - [2. Use Case Implementation](#2-use-case-implementation)
  - [3. Repository Implementation](#3-repository-implementation)
  - [4. CQRS Command](#4-cqrs-command)
  - [5. Value Object](#5-value-object)
- [Common Gotchas](#common-gotchas)
- [Migration Checklist](#migration-checklist)

---

## Overview

ezDDD.NET is a faithful .NET port of Java ezddd with **~98% semantic parity**. Core patterns remain unchanged:

✅ **Same Design Philosophy**
- Tactical DDD patterns (Entity, ValueObject, AggregateRoot)
- Event Sourcing with R1/R2/R3 correctness rules
- State Sourcing with Transactional Outbox
- CQRS patterns (Command, Query, Projection)
- Clean Architecture layers

✅ **Same Patterns**
- Bridge Pattern (IRepository ↔ IRepositoryPeer)
- Template Method (EsAggregateRoot)
- Observer Pattern (MessageBus)
- Command Pattern (IUseCase)

✅ **.NET Platform Improvements**
- Async/await throughout (all I/O operations)
- Nullable reference types (compile-time null safety)
- Record types (immutable events/value objects)
- Pattern matching (cleaner event handling)

**Key Difference**: All I/O methods are async in .NET (`FindById` → `FindByIdAsync`, `Execute` → `ExecuteAsync`).

---

## Quick Start

### Java ezddd
```xml
<!-- pom.xml -->
<dependency>
    <groupId>com.teddysoft</groupId>
    <artifactId>ezddd-entity</artifactId>
    <version>2.0.0</version>
</dependency>
<dependency>
    <groupId>com.teddysoft</groupId>
    <artifactId>ezddd-usecase</artifactId>
    <version>2.0.0</version>
</dependency>
```

### .NET ezDDD.NET
```bash
# .csproj or CLI
dotnet add package ezDDD.Core
# Or specific modules
dotnet add package ezDDD.Entity
dotnet add package ezDDD.UseCase
dotnet add package ezDDD.Cqrs
```

---

## Language Differences

### Syntax Comparison

| Aspect | Java | C# (.NET) |
|--------|------|-----------|
| **Naming** | camelCase (fields), PascalCase (methods) | _camelCase (private fields), PascalCase (public) |
| **Interfaces** | `IEntity` or no prefix | `IEntity` (always 'I' prefix) |
| **Generics** | `<ID, E>` | `<TId, TEvent>` ('T' prefix) |
| **Null Safety** | `@Nullable`, `Optional<T>` | `T?` (nullable reference types) |
| **Immutability** | `record` (Java 14+) | `record` (similar but different syntax) |
| **Collections** | `List<T>`, `Map<K,V>` | `List<T>`, `Dictionary<TKey, TValue>` |
| **Async** | Synchronous (blocking I/O) | `async Task<T>` (non-blocking I/O) |

### Async/Await (Java sync → .NET async)

**Java (Synchronous)**:
```java
public class MyUseCase implements UseCase<Input, Output> {
    @Override
    public Output execute(Input input) {
        // Load aggregate (blocking I/O)
        Optional<Account> accountOpt = repository.findById(id);
        Account account = accountOpt.orElseThrow();

        // Execute business logic
        account.deposit(input.getAmount());

        // Save aggregate (blocking I/O)
        repository.save(account);

        return new Output();
    }
}
```

**C# (Asynchronous)**:
```csharp
public class MyUseCase : IUseCase<Input, Output>
{
    public async Task<Output> ExecuteAsync(Input input)
    {
        // Load aggregate (non-blocking I/O)
        Account? account = await repository.FindByIdAsync(id);
        if (account is null)
            throw new NotFoundException();

        // Execute business logic (same as Java)
        account.Deposit(input.Amount);

        // Save aggregate (non-blocking I/O)
        await repository.SaveAsync(account);

        return new Output();
    }
}
```

**Key Points**:
- All I/O methods are `async` in .NET
- Use `await` keyword for async operations
- Return `Task<T>` instead of `T`
- Method names end with `Async` suffix

### Nullable Reference Types

**Java**:
```java
@Nullable
public Account findById(String id) {
    return accounts.get(id);
}

public void process(@NotNull Account account) {
    // account guaranteed non-null
}
```

**C#**:
```csharp
#nullable enable

public async Task<Account?> FindByIdAsync(string id)
{
    return accounts.GetValueOrDefault(id);
}

public void Process(Account account) // non-nullable by default
{
    // account guaranteed non-null (compile-time check)
}
```

**Key Points**:
- `T?` means nullable reference type
- Non-nullable by default (requires `<Nullable>enable</Nullable>` in .csproj)
- Compiler warnings for potential null references
- No need for `Optional<T>` - use `T?` instead

### Record Types vs Java Records

**Java**:
```java
public record AccountCreated(
    UUID id,
    ZonedDateTime occurredOn,
    String accountId,
    String owner,
    BigDecimal initialBalance
) implements InternalDomainEvent { }
```

**C#**:
```csharp
public record AccountCreated(
    Guid Id,
    DateTimeOffset OccurredOn,
    AccountId AccountId,
    string Owner,
    decimal InitialBalance
) : IInternalDomainEvent;
```

**Key Points**:
- Similar syntax but `:` instead of `implements`
- C# uses `;` at end, Java uses `{ }`
- C# properties are PascalCase
- C# has `init` keyword for immutable properties

### Collections and Generics

**Java**:
```java
List<DomainEvent> events = new ArrayList<>();
Map<String, Account> accounts = new HashMap<>();
Optional<Account> maybeAccount = repository.findById(id);

// Stream API
List<String> names = accounts.values().stream()
    .map(Account::getName)
    .collect(Collectors.toList());
```

**C#**:
```csharp
List<DomainEvent> events = new();
Dictionary<string, Account> accounts = new();
Account? maybeAccount = await repository.FindByIdAsync(id);

// LINQ
List<string> names = accounts.Values
    .Select(a => a.Name)
    .ToList();
```

**Key Points**:
- `List<T>` → `List<T>` (same name)
- `Map<K,V>` → `Dictionary<TKey, TValue>`
- `Optional<T>` → `T?` (nullable reference)
- `new()` target-typed new expression
- Stream API → LINQ

---

## API Mapping

### Common Module

| Java | C# (.NET) | Notes |
|------|-----------|-------|
| `BiMap<K, V>` | `BiMap<TKey, TValue>` | Same functionality, thread-safe |
| `Converter<S, T>` | `Converter<TSource, TTarget>` | Delegate in C#, not interface |
| `JsonUtil.deepCopy(obj)` | `JsonUtil.DeepCopy(obj)` | PascalCase method name |

**Key Difference**: C# `Converter` is a delegate (`Func<TSource, TTarget>`), not an interface.

### Entity Module

| Java | C# (.NET) | Notes |
|------|-----------|-------|
| `IEntity<ID>` | `IEntity<TId>` | Covariant: `out TId` |
| `IValueObject` | `IValueObject` | Same |
| `IDomainEvent` | `IDomainEvent` | Same properties |
| `InternalDomainEvent` | `IInternalDomainEvent` | Interface in C# |
| `IConstructionEvent` | `IInternalDomainEvent.IConstructionEvent` | Nested interface |
| `IDestructionEvent` | `IInternalDomainEvent.IDestructionEvent` | Nested interface |
| `AggregateRoot<ID, E>` | `AggregateRoot<TId, TEvent>` | Same pattern |
| `EsAggregateRoot<ID, E>` | `EsAggregateRoot<TId, TEvent>` | Async constructors |
| `when(E event)` | `When(TEvent @event)` | Protected virtual |
| `ensureInvariant()` | `EnsureInvariant()` | Protected virtual |
| `getCategory()` | `GetCategory()` | Public abstract |
| `getDomainEvents()` | `GetDomainEvents()` | Returns `IReadOnlyList<TEvent>` |
| `clearDomainEvents()` | `ClearDomainEvents()` | Same |

### UseCase Module

| Java | C# (.NET) | Notes |
|------|-----------|-------|
| `UseCase<I, O>` | `IUseCase<TInput, TOutput>` | Contravariant/Covariant |
| `O execute(I input)` | `Task<TOutput> ExecuteAsync(TInput input)` | **Async** |
| `IInput` | `IInput` | Same |
| `IOutput` | `IOutput` | Same |
| `IRepository<A, ID>` | `IRepository<TAggregate, TId, TEvent>` | **Async methods** |
| `Optional<A> findById(ID)` | `Task<TAggregate?> FindByIdAsync(TId)` | Nullable + Async |
| `void save(A)` | `Task SaveAsync(TAggregate)` | **Async** |
| `void delete(ID)` | `Task DeleteAsync(TAggregate)` | **Async** |
| `IRepositoryPeer<D, ID>` | `IRepositoryPeer<TData, TId>` | **Async methods** |
| `EsRepository<A, ID, E>` | `EsRepository<TAggregate, TId>` | Async + Reflection |
| `OutboxRepository<A, ID, E>` | `OutboxRepository<TAggregate, TData, TId>` | Async |
| `BlockingMessageBus` | `BlockingMessageBus` | Thread-safe |
| `IMessageBus.send(event)` | `IMessageBus.SendAsync(event)` | **Async** |

**Critical Change**: All repository methods are **async** in .NET.

### Cqrs Module

| Java | C# (.NET) | Notes |
|------|-----------|-------|
| `ICommand<I, O>` | `ICommand<TInput, TOutput>` | Marker interface |
| `IQuery<I, O>` | `IQuery<TInput, TOutput>` | Marker interface |
| `IInquiry<I, O>` | `IInquiry<TInput, TOutput>` | Marker interface |
| `IProjection<I, O>` | `IProjection<TInput, TOutput>` | Async |
| `IProjector` | `IProjector` | Background service marker |
| `IArchive<D, ID>` | `IArchive<TData, TId>` | **Async methods** |
| `CqrsOutput<T>` | `CqrsOutput<T>` | Fluent API with records |

---

## Pattern Changes

### Event Handler (instanceof → pattern matching)

**Java (instanceof)**:
```java
@Override
protected void when(InternalDomainEvent event) {
    if (event instanceof AccountCreated e) {
        this.id = e.accountId();
        this.owner = e.owner();
        this.balance = e.initialBalance();
    } else if (event instanceof MoneyDeposited e) {
        this.balance = this.balance.add(e.amount());
    } else if (event instanceof MoneyWithdrawn e) {
        this.balance = this.balance.subtract(e.amount());
    } else if (event instanceof AccountClosed e) {
        this.isClosed = true;
    } else {
        throw new IllegalArgumentException("Unknown event: " + event.getClass());
    }
}
```

**C# (pattern matching)**:
```csharp
protected override void _When(IInternalDomainEvent @event)
{
    switch (@event)
    {
        case AccountCreated e:
            Id = e.AccountId;
            _owner = e.Owner;
            _balance = e.InitialBalance;
            break;

        case MoneyDeposited e:
            _balance = _balance.Add(e.Amount);
            break;

        case MoneyWithdrawn e:
            _balance = _balance.Subtract(e.Amount);
            break;

        case AccountClosed e:
            _isClosed = true;
            break;

        default:
            throw new InvalidOperationException(
                $"Unknown event type: {@event.GetType().Name}");
    }
}
```

**Key Points**:
- C# switch is cleaner than Java if-else chain
- `@event` escapes reserved keyword
- `break` required in C# (not expression-based)
- Pattern matching is more concise and readable

### Repository Pattern (sync → async)

**Java (Synchronous)**:
```java
public class DepositMoneyCommand implements Command<DepositInput, DepositOutput> {
    private final Repository<BankAccount, String> repository;

    @Override
    public DepositOutput execute(DepositInput input) {
        // Load aggregate (blocking)
        Optional<BankAccount> accountOpt = repository.findById(input.getAccountId());
        if (accountOpt.isEmpty()) {
            return new DepositOutput().setExitCode(ExitCode.RESOURCE_NOT_FOUND_FAILURE);
        }

        BankAccount account = accountOpt.get();

        // Execute domain logic
        account.deposit(input.getAmount());

        // Save aggregate (blocking)
        try {
            repository.save(account);
        } catch (RepositorySaveException e) {
            return new DepositOutput().setExitCode(ExitCode.CONFLICT_FAILURE);
        }

        return new DepositOutput().succeed();
    }
}
```

**C# (Asynchronous)**:
```csharp
public class DepositMoneyCommand : ICommand<DepositInput, DepositOutput>
{
    private readonly IRepository<BankAccount, AccountId, IInternalDomainEvent> _repository;

    public async Task<DepositOutput> ExecuteAsync(DepositInput input)
    {
        // Load aggregate (non-blocking)
        BankAccount? account = await _repository.FindByIdAsync(input.AccountId);
        if (account is null)
        {
            return new DepositOutput().SetExitCode(ExitCode.ResourceNotFoundFailure);
        }

        // Execute domain logic (same as Java)
        account.Deposit(input.Amount);

        // Save aggregate (non-blocking)
        try
        {
            await _repository.SaveAsync(account);
        }
        catch (RepositorySaveException)
        {
            return new DepositOutput().SetExitCode(ExitCode.ConflictFailure);
        }

        return new DepositOutput().Succeed();
    }
}
```

**Key Points**:
- Add `async` keyword to method signature
- Change return type to `Task<T>`
- Add `await` before async calls
- Add `Async` suffix to method names
- Nullable check: `is null` instead of `isEmpty()`

### Exception Handling

**Java**:
```java
try {
    repository.save(account);
} catch (RepositorySaveException e) {
    logger.error("Save failed", e);
    throw new UseCaseFailureException("Failed to save account", e);
}
```

**C#**:
```csharp
try
{
    await repository.SaveAsync(account);
}
catch (RepositorySaveException ex)
{
    logger.LogError(ex, "Save failed");
    throw new UseCaseFailureException("Failed to save account", ex);
}
```

**Key Points**:
- Same try-catch-throw structure
- Use `ex` instead of `e` (C# convention)
- `await` for async methods
- `logger.LogError()` instead of `logger.error()`

### Threading and Concurrency

**Java (CopyOnWriteArrayList)**:
```java
public class BlockingMessageBus<Event> implements MessageBus<Event> {
    private final List<Reactor<Event>> reactors = new CopyOnWriteArrayList<>();

    @Override
    public void post(Event event) {
        // Thread-safe iteration (no explicit locking needed)
        for (Reactor<Event> reactor : reactors) {
            reactor.execute(event);
        }
    }
}
```

**C# (Lock + Snapshot)**:
```csharp
public class BlockingMessageBus<TEvent> : IMessageBus<TEvent>
{
    private readonly List<IReactor<TEvent>> _reactors = new();
    private readonly object _lock = new();

    public async Task PostAsync(TEvent message)
    {
        // Create snapshot inside lock
        IReactor<TEvent>[] snapshot;

        lock (_lock)
        {
            snapshot = _reactors.ToArray();
        }

        // Execute outside lock (non-blocking)
        foreach (var reactor in snapshot)
        {
            await reactor.ExecuteAsync(message);
        }
    }
}
```

**Key Points**:
- `CopyOnWriteArrayList` → `List<T>` + `lock` (C# doesn't have CopyOnWriteArrayList)
- Snapshot pattern: Copy reactors array inside lock, execute outside lock
- Async execution: `await reactor.ExecuteAsync(message)`
- `lock` statement for thread safety

---

## Module Structure Differences

**Java Package Structure**:
```
com.teddysoft.ezddd
├── common
│   ├── BiMap
│   └── Converter
├── entity
│   ├── IEntity
│   ├── AggregateRoot
│   └── EsAggregateRoot
├── usecase
│   ├── IUseCase
│   ├── IRepository
│   └── EsRepository
└── cqrs
    ├── ICommand
    └── IQuery
```

**C# Namespace Structure**:
```
EzDdd
├── Common
│   ├── BiMap
│   └── Converter
├── Entity
│   ├── IEntity
│   ├── AggregateRoot
│   └── EsAggregateRoot
├── UseCase
│   ├── IUseCase
│   ├── IRepository
│   └── EsRepository
└── Cqrs
    ├── ICommand
    └── IQuery
```

**Key Points**:
- Java uses lowercase package names
- C# uses PascalCase namespace names
- Same logical structure

---

## Dependency Injection

**Java (Spring)**:
```java
@Service
public class DepositMoneyCommand implements Command<DepositInput, DepositOutput> {
    private final Repository<BankAccount, String> repository;

    @Autowired
    public DepositMoneyCommand(Repository<BankAccount, String> repository) {
        this.repository = repository;
    }
}
```

**C# (ASP.NET Core)**:
```csharp
// Startup.cs or Program.cs
services.AddScoped<ICommand<DepositInput, DepositOutput>, DepositMoneyCommand>();
services.AddScoped<IRepository<BankAccount, AccountId, IInternalDomainEvent>,
    EsRepository<BankAccount, AccountId>>();

// Use case
public class DepositMoneyCommand : ICommand<DepositInput, DepositOutput>
{
    private readonly IRepository<BankAccount, AccountId, IInternalDomainEvent> _repository;

    public DepositMoneyCommand(
        IRepository<BankAccount, AccountId, IInternalDomainEvent> repository)
    {
        _repository = repository;
    }
}
```

**Key Points**:
- Both use constructor injection
- .NET uses `services.AddScoped/AddTransient/AddSingleton`
- No `@Autowired` annotation needed
- C# DI is built into ASP.NET Core

---

## Testing

**Java (JUnit 5)**:
```java
@Test
void shouldCreateAccount() {
    // Arrange
    var input = new CreateAccountInput("Alice", new BigDecimal("100"));
    var useCase = new CreateAccountCommand(repository);

    // Act
    var output = useCase.execute(input);

    // Assert
    assertNotNull(output.accountId());
    assertEquals(ExitCode.SUCCESS, output.exitCode());
}
```

**C# (xUnit)**:
```csharp
[Fact]
public async Task ShouldCreateAccount()
{
    // Arrange
    var input = new CreateAccountInput("Alice", 100m);
    var useCase = new CreateAccountCommand(repository);

    // Act
    var output = await useCase.ExecuteAsync(input);

    // Assert
    Assert.NotNull(output.AccountId);
    Assert.Equal(ExitCode.Success, output.ExitCode);
}
```

**Key Points**:
- `@Test` → `[Fact]`
- `void` → `async Task` for async tests
- `assertNotNull` → `Assert.NotNull`
- `await` for async methods
- xUnit uses PascalCase assertions

---

## Code Examples

### 1. Event-Sourced Aggregate

**Java**:
```java
public class BankAccount extends EsAggregateRoot<String, InternalDomainEvent> {
    private String owner;
    private BigDecimal balance;
    private boolean isClosed;

    // Constructor for creation
    public BankAccount(String id, String owner, BigDecimal initialBalance) {
        AccountCreated event = new AccountCreated(
            UUID.randomUUID(),
            ZonedDateTime.now(),
            id,
            owner,
            initialBalance
        );
        apply(event);
    }

    // Constructor for event replay (REQUIRED)
    public BankAccount(List<InternalDomainEvent> events) {
        super(events);
    }

    public void deposit(BigDecimal amount) {
        if (amount.compareTo(BigDecimal.ZERO) <= 0) {
            throw new IllegalArgumentException("Deposit amount must be positive");
        }

        MoneyDeposited event = new MoneyDeposited(
            UUID.randomUUID(),
            ZonedDateTime.now(),
            getId(),
            amount
        );
        apply(event);
    }

    @Override
    protected void when(InternalDomainEvent event) {
        if (event instanceof AccountCreated e) {
            this.id = e.accountId();
            this.owner = e.owner();
            this.balance = e.initialBalance();
        } else if (event instanceof MoneyDeposited e) {
            this.balance = this.balance.add(e.amount());
        } else if (event instanceof MoneyWithdrawn e) {
            this.balance = this.balance.subtract(e.amount());
        } else {
            throw new IllegalArgumentException("Unknown event: " + event.getClass());
        }
    }

    @Override
    protected void ensureInvariant() {
        if (isClosed) return;

        if (balance.compareTo(BigDecimal.ZERO) < 0) {
            throw new IllegalStateException("Balance cannot be negative");
        }

        if (owner == null || owner.isEmpty()) {
            throw new IllegalStateException("Owner cannot be empty");
        }
    }

    @Override
    public String getCategory() {
        return "account";
    }
}
```

**C#**:
```csharp
public sealed class BankAccount : EsAggregateRoot<AccountId, IInternalDomainEvent>
{
    private string _owner = string.Empty;
    private Money _balance = new(0);
    private bool _isClosed;

    // Constructor for creation
    public BankAccount(AccountId id, string owner, Money initialBalance)
    {
        Id = id;
        var @event = new AccountCreated(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            id,
            owner,
            initialBalance
        );
        Apply(@event);
    }

    // Constructor for event replay (REQUIRED)
    public BankAccount(IEnumerable<IInternalDomainEvent> events)
        : base(events)
    {
    }

    // Public properties for testing
    public string Owner => _owner;
    public Money Balance => _balance;
    public bool IsClosed => _isClosed;

    public void Deposit(Money amount)
    {
        if (amount.Amount <= 0)
        {
            throw new InvalidOperationException("Deposit amount must be positive");
        }

        var @event = new MoneyDeposited(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Id,
            amount
        );
        Apply(@event);
    }

    protected override void _When(IInternalDomainEvent @event)
    {
        switch (@event)
        {
            case AccountCreated e:
                Id = e.AccountId;
                _owner = e.Owner;
                _balance = e.InitialBalance;
                break;

            case MoneyDeposited e:
                _balance = _balance.Add(e.Amount);
                break;

            case MoneyWithdrawn e:
                _balance = _balance.Subtract(e.Amount);
                break;

            case AccountClosed e:
                _isClosed = true;
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown event type: {@event.GetType().Name}");
        }
    }

    protected override void _EnsureInvariant()
    {
        if (_isClosed) return;

        if (_balance.Amount < 0)
        {
            throw new InvalidOperationException(
                $"Account balance cannot be negative: {_balance}");
        }

        if (string.IsNullOrWhiteSpace(_owner))
        {
            throw new InvalidOperationException("Account owner cannot be empty");
        }
    }

    public override string GetCategory()
    {
        return "account";
    }
}
```

### 2. Use Case Implementation

**Java**:
```java
public class DepositMoneyCommand implements Command<DepositInput, DepositOutput> {
    private final Repository<BankAccount, String> repository;

    public DepositMoneyCommand(Repository<BankAccount, String> repository) {
        this.repository = repository;
    }

    @Override
    public DepositOutput execute(DepositInput input) {
        // Load aggregate
        Optional<BankAccount> accountOpt = repository.findById(input.getAccountId());
        if (accountOpt.isEmpty()) {
            return new DepositOutput()
                .setExitCode(ExitCode.RESOURCE_NOT_FOUND_FAILURE)
                .setMessage("Account not found");
        }

        BankAccount account = accountOpt.get();

        // Execute domain logic
        try {
            account.deposit(input.getAmount());
        } catch (IllegalArgumentException e) {
            return new DepositOutput()
                .setExitCode(ExitCode.VALIDATION_FAILURE)
                .setMessage(e.getMessage());
        }

        // Save aggregate
        try {
            repository.save(account);
        } catch (RepositorySaveException e) {
            return new DepositOutput()
                .setExitCode(ExitCode.CONFLICT_FAILURE)
                .setMessage("Concurrent modification detected");
        }

        return new DepositOutput()
            .succeed()
            .setId(account.getId())
            .setMessage("Deposit successful");
    }
}
```

**C#**:
```csharp
public class DepositMoneyCommand : ICommand<DepositInput, DepositOutput>
{
    private readonly IRepository<BankAccount, AccountId, IInternalDomainEvent> _repository;

    public DepositMoneyCommand(
        IRepository<BankAccount, AccountId, IInternalDomainEvent> repository)
    {
        _repository = repository;
    }

    public async Task<DepositOutput> ExecuteAsync(DepositInput input)
    {
        // Load aggregate
        BankAccount? account = await _repository.FindByIdAsync(input.AccountId);
        if (account is null)
        {
            return new DepositOutput()
                .SetExitCode(ExitCode.ResourceNotFoundFailure)
                .SetMessage("Account not found");
        }

        // Execute domain logic
        try
        {
            account.Deposit(input.Amount);
        }
        catch (InvalidOperationException ex)
        {
            return new DepositOutput()
                .SetExitCode(ExitCode.ValidationFailure)
                .SetMessage(ex.Message);
        }

        // Save aggregate
        try
        {
            await _repository.SaveAsync(account);
        }
        catch (RepositorySaveException)
        {
            return new DepositOutput()
                .SetExitCode(ExitCode.ConflictFailure)
                .SetMessage("Concurrent modification detected");
        }

        return new DepositOutput()
            .Succeed()
            .SetId(account.Id.Value)
            .SetMessage("Deposit successful");
    }
}
```

### 3. Repository Implementation

**Java**:
```java
public class SqlRepositoryPeer implements RepositoryPeer<OutboxData<String>, String> {
    private final DataSource dataSource;

    @Override
    @Transactional
    public void save(OutboxData<String> data) {
        try (Connection conn = dataSource.getConnection()) {
            conn.setAutoCommit(false);

            try {
                // Save aggregate state
                String sql = "UPDATE accounts SET owner = ?, balance = ?, version = ? " +
                            "WHERE id = ? AND version = ?";
                try (PreparedStatement stmt = conn.prepareStatement(sql)) {
                    stmt.setString(1, data.getOwner());
                    stmt.setBigDecimal(2, data.getBalance());
                    stmt.setInt(3, data.getOptimisticLockVersion());
                    stmt.setString(4, data.getId());
                    stmt.setInt(5, data.getVersion());

                    int updated = stmt.executeUpdate();
                    if (updated == 0) {
                        throw new RepositoryPeerSaveException(
                            RepositorySaveException.OPTIMISTIC_LOCKING_FAILURE);
                    }
                }

                // Save events to outbox
                String eventSql = "INSERT INTO outbox (event_id, event_type, event_body) " +
                                 "VALUES (?, ?, ?)";
                try (PreparedStatement stmt = conn.prepareStatement(eventSql)) {
                    for (DomainEvent event : data.getEvents()) {
                        DomainEventData eventData = DomainEventMapper.toData(event);
                        stmt.setString(1, eventData.id().toString());
                        stmt.setString(2, eventData.eventType());
                        stmt.setBytes(3, eventData.eventBody());
                        stmt.addBatch();
                    }
                    stmt.executeBatch();
                }

                conn.commit();
            } catch (SQLException e) {
                conn.rollback();
                throw new RepositoryPeerSaveException("Database error", e);
            }
        } catch (SQLException e) {
            throw new RepositoryPeerSaveException("Connection error", e);
        }
    }
}
```

**C#**:
```csharp
public class SqlRepositoryPeer : IRepositoryPeer<OutboxData<AccountId>, AccountId>
{
    private readonly string _connectionString;

    public async Task SaveAsync(OutboxData<AccountId> data)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Save aggregate state
            var sql = @"UPDATE accounts
                       SET owner = @Owner, balance = @Balance, version = @NewVersion
                       WHERE id = @Id AND version = @OldVersion";

            await using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@Owner", data.Owner);
            command.Parameters.AddWithValue("@Balance", data.Balance);
            command.Parameters.AddWithValue("@NewVersion", data.GetOptimisticLockVersion());
            command.Parameters.AddWithValue("@Id", data.Id.Value);
            command.Parameters.AddWithValue("@OldVersion", data.Version);

            int updated = await command.ExecuteNonQueryAsync();
            if (updated == 0)
            {
                throw new RepositoryPeerSaveException(
                    RepositorySaveException.OptimisticLockingFailure);
            }

            // Save events to outbox
            var eventSql = @"INSERT INTO outbox (event_id, event_type, event_body)
                            VALUES (@EventId, @EventType, @EventBody)";

            foreach (var @event in data.Events)
            {
                var eventData = DomainEventMapper.ToData(@event);

                await using var eventCommand = new SqlCommand(eventSql, connection, transaction);
                eventCommand.Parameters.AddWithValue("@EventId", eventData.Id);
                eventCommand.Parameters.AddWithValue("@EventType", eventData.EventType);
                eventCommand.Parameters.AddWithValue("@EventBody", eventData.EventBody);

                await eventCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

### 4. CQRS Command

**Java**:
```java
public class CreateAccountCommand implements Command<CreateAccountInput, CreateAccountOutput> {
    private final Repository<BankAccount, String> repository;

    @Override
    public CreateAccountOutput execute(CreateAccountInput input) {
        // Validate input
        if (input.getOwner() == null || input.getOwner().isEmpty()) {
            return new CreateAccountOutput()
                .setExitCode(ExitCode.VALIDATION_FAILURE)
                .setMessage("Owner name is required");
        }

        if (input.getInitialBalance().compareTo(BigDecimal.ZERO) < 0) {
            return new CreateAccountOutput()
                .setExitCode(ExitCode.VALIDATION_FAILURE)
                .setMessage("Initial balance cannot be negative");
        }

        // Create aggregate
        String accountId = UUID.randomUUID().toString();
        BankAccount account = new BankAccount(
            accountId,
            input.getOwner(),
            input.getInitialBalance()
        );

        // Save aggregate
        try {
            repository.save(account);
        } catch (RepositorySaveException e) {
            return new CreateAccountOutput()
                .setExitCode(ExitCode.DATABASE_FAILURE)
                .setMessage("Failed to create account");
        }

        return new CreateAccountOutput()
            .succeed()
            .setAccountId(accountId)
            .setMessage("Account created successfully");
    }
}
```

**C#**:
```csharp
public class CreateAccountCommand : ICommand<CreateAccountInput, CreateAccountOutput>
{
    private readonly IRepository<BankAccount, AccountId, IInternalDomainEvent> _repository;

    public async Task<CreateAccountOutput> ExecuteAsync(CreateAccountInput input)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(input.Owner))
        {
            return new CreateAccountOutput()
                .SetExitCode(ExitCode.ValidationFailure)
                .SetMessage("Owner name is required");
        }

        if (input.InitialBalance.Amount < 0)
        {
            return new CreateAccountOutput()
                .SetExitCode(ExitCode.ValidationFailure)
                .SetMessage("Initial balance cannot be negative");
        }

        // Create aggregate
        var accountId = new AccountId(Guid.NewGuid().ToString());
        var account = new BankAccount(
            accountId,
            input.Owner,
            input.InitialBalance
        );

        // Save aggregate
        try
        {
            await _repository.SaveAsync(account);
        }
        catch (RepositorySaveException)
        {
            return new CreateAccountOutput()
                .SetExitCode(ExitCode.DatabaseFailure)
                .SetMessage("Failed to create account");
        }

        return new CreateAccountOutput()
            .Succeed()
            .SetAccountId(accountId.Value)
            .SetMessage("Account created successfully");
    }
}
```

### 5. Value Object

**Java**:
```java
public record Money(BigDecimal amount, String currency) implements ValueObject {
    public Money {
        if (amount == null) {
            throw new IllegalArgumentException("Amount cannot be null");
        }
        if (currency == null || currency.isEmpty()) {
            throw new IllegalArgumentException("Currency cannot be null or empty");
        }
    }

    public Money(BigDecimal amount) {
        this(amount, "USD");
    }

    public Money add(Money other) {
        if (!this.currency.equals(other.currency)) {
            throw new IllegalArgumentException("Cannot add different currencies");
        }
        return new Money(this.amount.add(other.amount), this.currency);
    }

    public Money subtract(Money other) {
        if (!this.currency.equals(other.currency)) {
            throw new IllegalArgumentException("Cannot subtract different currencies");
        }
        return new Money(this.amount.subtract(other.amount), this.currency);
    }
}
```

**C#**:
```csharp
public record Money(decimal Amount, string Currency = "USD") : IValueObject
{
    // Compact constructor with validation
    public Money
    {
        if (string.IsNullOrWhiteSpace(Currency))
        {
            throw new ArgumentException("Currency cannot be null or empty", nameof(Currency));
        }
    }

    public Money Add(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Cannot add different currencies: {Currency} and {other.Currency}");
        }
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Cannot subtract different currencies: {Currency} and {other.Currency}");
        }
        return new Money(Amount - other.Amount, Currency);
    }
}
```

---

## Common Gotchas

❌ **Forgetting `await`**
```csharp
// WRONG
var account = repository.FindByIdAsync(id); // Returns Task<Account?>, not Account?

// CORRECT
var account = await repository.FindByIdAsync(id);
```

❌ **Forgetting `Async` suffix**
```csharp
// WRONG
await repository.FindById(id); // Method doesn't exist

// CORRECT
await repository.FindByIdAsync(id);
```

❌ **Using blocking calls in async code**
```csharp
// WRONG - Deadlock risk!
var result = task.Result;
var result = task.Wait();

// CORRECT
var result = await task;
```

❌ **Nullable reference types**
```csharp
// WRONG
Account account = await repository.FindByIdAsync(id); // Compiler warning: may be null

// CORRECT
Account? account = await repository.FindByIdAsync(id);
if (account is null) throw new NotFoundException();
```

❌ **Record equality with mutable fields**
```csharp
// WRONG (mutable - breaks equality)
public record Money(decimal Amount, string Currency)
{
    public decimal Amount { get; set; } = Amount; // Mutable!
}

// CORRECT (immutable)
public record Money(decimal Amount, string Currency);
```

❌ **Pattern matching without `break`**
```csharp
// WRONG
switch (@event)
{
    case AccountCreated e:
        Id = e.AccountId;
        // Missing break - will fall through!
    case MoneyDeposited e:
        _balance += e.Amount;
        break;
}

// CORRECT
switch (@event)
{
    case AccountCreated e:
        Id = e.AccountId;
        break; // Required!
    case MoneyDeposited e:
        _balance += e.Amount;
        break;
}
```

❌ **Transaction boundary at wrong layer**
```csharp
// WRONG - Repository should NOT manage transactions
public class OutboxRepository<T, D, ID> : IRepository<T, ID>
{
    public async Task SaveAsync(T aggregate)
    {
        using var transaction = await _dbContext.BeginTransactionAsync(); // ❌ WRONG!
        // ...
    }
}

// CORRECT - RepositoryPeer manages transactions
public class SqlRepositoryPeer<D, ID> : IRepositoryPeer<D, ID>
{
    public async Task SaveAsync(D data)
    {
        using var transaction = await _dbContext.BeginTransactionAsync(); // ✅ CORRECT!
        // ...
    }
}
```

---

## Migration Checklist

### Pre-Migration
- [ ] Install .NET 8 SDK
- [ ] Setup .NET project structure (solution + projects)
- [ ] Add ezDDD.NET NuGet packages (`ezDDD.Core` or specific modules)
- [ ] Enable nullable reference types (`<Nullable>enable</Nullable>` in .csproj)
- [ ] Setup async/await throughout codebase

### Code Migration
- [ ] Replace `UseCase<I, O>` with `IUseCase<TInput, TOutput>`
- [ ] Change `execute()` to `async Task<TOutput> ExecuteAsync()`
- [ ] Replace `Optional<T>` with `T?` (nullable reference types)
- [ ] Replace `List<T>` with `List<T>` or `IReadOnlyList<T>` for outputs
- [ ] Replace `Map<K, V>` with `Dictionary<TKey, TValue>`
- [ ] Change all repository methods to async (`FindByIdAsync`, `SaveAsync`, `DeleteAsync`)
- [ ] Update event handlers to use pattern matching (switch expressions)
- [ ] Convert Java records to C# records (PascalCase properties)
- [ ] Update DI configuration (Spring → ASP.NET Core)
- [ ] Replace `Optional.empty()` with `null` checks (`is null`)

### Event Sourcing Migration
- [ ] Add event replay constructor: `public Aggregate(IEnumerable<TEvent> events)`
- [ ] Implement `_When(TEvent @event)` with pattern matching
- [ ] Implement `_EnsureInvariant()` for business rule checks
- [ ] Implement `GetCategory()` for stream naming
- [ ] Register event types with `DomainEventTypeMapper`
- [ ] Ensure R1/R2/R3 rules are enforced (construction/command/destruction events)

### Testing Migration
- [ ] Replace JUnit `@Test` with xUnit `[Fact]`
- [ ] Add `async Task` to async tests (not `void`)
- [ ] Add `await` to async method calls
- [ ] Update assertion syntax (JUnit → xUnit)
- [ ] Replace `assertEquals` with `Assert.Equal`
- [ ] Replace `assertNotNull` with `Assert.NotNull`
- [ ] Replace `assertTrue` with `Assert.True`

### Verification
- [ ] All tests passing
- [ ] No compiler warnings (especially nullable warnings)
- [ ] Async/await used correctly (no `.Result` or `.Wait()`)
- [ ] Nullable annotations correct (`T?` for nullable, `T` for non-nullable)
- [ ] Event sourcing R1/R2/R3 rules preserved
- [ ] Transaction boundaries at IRepositoryPeer level (NOT IRepository)
- [ ] Domain events cleared after successful save

---

## Further Reading

- **ROADMAP.md** - Development progress and milestones
- **ADR Index** (docs/adr/README.md) - Architecture Decision Records

---

**Questions or Issues?**

- GitHub Issues: https://github.com/cwouyang/ezDDD.NET/issues
- Java ezddd: https://gitlab.com/TeddyChen/ezddd
- .NET Documentation: https://learn.microsoft.com/en-us/dotnet/

---

**Migration Best Practices**:

1. **Start with Entity module** - Migrate core domain entities first
2. **Add event sourcing gradually** - Start with state sourcing, then add event sourcing
3. **Test thoroughly** - Write integration tests for complete workflows
4. **Use async/await consistently** - Don't mix sync and async code
5. **Enable nullable reference types** - Catch null issues at compile time
6. **Follow .NET naming conventions** - PascalCase for public members, _camelCase for private fields
7. **Leverage pattern matching** - Use switch expressions for event handling
8. **Keep transaction boundaries correct** - IRepositoryPeer level, not IRepository

**Performance Tips**:

1. **Cache ConstructorInfo** - EsRepository uses reflection, cache constructor lookup
2. **Use ValueTask<T>** - For high-performance async code that often completes synchronously
3. **Profile async code** - Use async profilers to find bottlenecks
4. **Consider compiled expressions** - For faster reflection-based instantiation

---

*This migration guide is based on ezDDD.NET v1.0.0-alpha.1 and Java ezddd 2.x*
