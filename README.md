# ezDDD.NET

> **Tactical Domain-Driven Design patterns library for .NET 8+**
>
> Based on [Java ezddd 6.0.1](https://gitlab.com/TeddyChen/ezddd)

A modern tactical DDD library for .NET, specifically designed for Domain-Driven Design with event sourcing, state sourcing, and CQRS patterns. This is a faithful .NET port of the **Java ezddd 6.0.1** library (GitLab commit: `3aac0f5`) with **~99% semantic parity** and .NET-specific improvements.

[![Build and Test](https://github.com/cwouyang/ezDDD.NET/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/cwouyang/ezDDD.NET/actions/workflows/build-and-test.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Status](https://img.shields.io/badge/status-alpha-orange.svg)](#status)

---

## Status

✅ **First public release in preparation** - **Based on Java ezddd 6.0.1**

- ✅ **Phase 1-5**: Core Implementation
- ✅ **Phase 6**: Java 4.1.0 Synchronization
- ✅ **Phase 7**: Java 6.0.1 Synchronization
- ✅ **543 tests passing (100%)**
- ✅ **29 ADRs documented**
- ✅ **Zero external dependencies** (only .NET BCL + uContract.NET)
- ✅ **~99% semantic parity** with Java ezddd 6.0.1

**Included in the first release (Java 4.1.0 → 6.0.1 features)**:
- ✅ **IDomainEvent.Metadata** - Idempotency and distributed tracing support
- ✅ **IReconciler** - System state reconciliation interface
- ✅ **IReactor / IProjector&lt;TInput&gt; / INotifier** - Unified reactor hierarchy for event reaction (Java 5.0.0)
- ✅ **IExternalDomainEventPublisher** - Out-port for publishing integration events (Java 6.0.x)
- ✅ **Transactional Outbox + Relay** - Repositories persist events; a separate Relay publishes them (matches Java architecture)
- ✅ **Thread Safety** - Enhanced concurrent operation support
- ✅ **Null Safety** - Comprehensive parameter validation

**Status**: first public release in preparation (not yet published to NuGet)

---

## Table of Contents

- [Quick Start](#quick-start)
- [Features](#features)
- [Modules](#modules)
- [API Reference](#api-reference)
- [Architecture](#architecture)
- [Examples](#examples)
- [Differences from Java Version](#differences-from-java-version)
- [Documentation](#documentation)
- [Requirements](#requirements)
- [License](#license)

---

## Quick Start

### Installation

```bash
# Install the core package (includes all modules)
dotnet add package ezDDD.Core

# Or install specific modules
dotnet add package ezDDD.Entity      # Core DDD patterns
dotnet add package ezDDD.UseCase     # Use cases & repositories
dotnet add package ezDDD.Cqrs        # CQRS patterns
```

*(Note: NuGet packages will be published after final testing)*

### Basic Example: Event-Sourced Aggregate

```csharp
using EzDdd.Entity;
using EzDdd.UseCase;

// Define domain events as records with Metadata support
public record AccountCreated(
    Guid Id,
    DateTimeOffset OccurredOn,
    AccountId AccountId,
    string Owner,
    Money InitialBalance
) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent
{
    public string Source => AccountId.Value;

    // Metadata for idempotency and distributed tracing
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
}

public record MoneyDeposited(
    Guid Id,
    DateTimeOffset OccurredOn,
    AccountId AccountId,
    Money Amount
) : IInternalDomainEvent
{
    public string Source => AccountId.Value;

    // Metadata for correlation and causation tracking
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
}

// Define event-sourced aggregate with R1/R2/R3 rules
public sealed class BankAccount : EsAggregateRoot<AccountId, IInternalDomainEvent>
{
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
        Apply(@event); // R1: Construction event
    }

    // Constructor for event replay
    public BankAccount(IEnumerable<IInternalDomainEvent> events) : base(events) { }

    public string Owner { get; private set; } = string.Empty;
    public Money Balance { get; private set; } = new(0);

    public void Deposit(Money amount)
    {
        if (amount.Amount <= 0)
            throw new InvalidOperationException("Amount must be positive");

        var @event = new MoneyDeposited(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Id,
            amount
        );
        Apply(@event); // R2: Command event
    }

    // Pattern matching for event handling
    protected override void _When(IInternalDomainEvent @event)
    {
        switch (@event)
        {
            case AccountCreated created:
                Id = created.AccountId;
                Owner = created.Owner;
                Balance = created.InitialBalance;
                break;

            case MoneyDeposited deposited:
                Balance = Balance.Add(deposited.Amount);
                break;
        }
    }

    // Business invariants
    protected override void _EnsureInvariant()
    {
        if (Balance.Amount < 0)
            throw new InvalidOperationException("Balance cannot be negative");

        if (string.IsNullOrWhiteSpace(Owner))
            throw new InvalidOperationException("Owner cannot be empty");
    }

    // Stream naming: "account-{id}"
    public override string GetCategory() => "account";
}

// Use with EsRepository
var repository = new EsRepository<BankAccount, AccountId, IInternalDomainEvent>(
    new InMemoryEventStorePeer()
);

// Create and save
var account = new BankAccount(
    new AccountId(Guid.NewGuid()),
    "John Doe",
    new Money(100)
);
await repository.SaveAsync(account);

// Load and use
var loaded = await repository.FindByIdAsync(account.Id);
loaded?.Deposit(new Money(50));
await repository.SaveAsync(loaded!);
```

### Basic Example: CQRS Flow

```csharp
using EzDdd.Cqrs;
using EzDdd.UseCase;

// Command (write operation)
public record CreateAccountInput(string Owner, decimal InitialBalance) : IInput;

public class CreateAccountCommand : ICommand<CreateAccountInput, CqrsOutput<AccountId>>
{
    private readonly IRepository<BankAccount, AccountId> _repository;

    public CreateAccountCommand(IRepository<BankAccount, AccountId> repository)
    {
        _repository = repository;
    }

    public async Task<CqrsOutput<AccountId>> ExecuteAsync(CreateAccountInput input)
    {
        try
        {
            var accountId = new AccountId(Guid.NewGuid());
            var account = new BankAccount(
                accountId,
                input.Owner,
                new Money(input.InitialBalance)
            );

            await _repository.SaveAsync(account);

            return CqrsOutput<AccountId>.Success(accountId)
                .WithMessage("Account created successfully");
        }
        catch (Exception ex)
        {
            return CqrsOutput<AccountId>.Failure(
                $"Failed to create account: {ex.Message}"
            );
        }
    }
}

// Query (read operation)
public record GetAccountBalanceInput(AccountId AccountId) : IInput;

public record AccountBalanceView(string Owner, decimal Balance);

public class GetAccountBalanceQuery : IQuery<GetAccountBalanceInput, CqrsOutput<AccountBalanceView>>
{
    private readonly IArchive<AccountData, AccountId> _archive;

    public GetAccountBalanceQuery(IArchive<AccountData, AccountId> archive)
    {
        _archive = archive;
    }

    public async Task<CqrsOutput<AccountBalanceView>> ExecuteAsync(GetAccountBalanceInput input)
    {
        var data = await _archive.FindByIdAsync(input.AccountId);

        if (data == null)
            return CqrsOutput<AccountBalanceView>.Failure("Account not found");

        var view = new AccountBalanceView(data.Owner, data.Balance);
        return CqrsOutput<AccountBalanceView>.Success(view);
    }
}

// Projection (read model builder)
public class AccountBalanceProjection : IProjection<GetAccountBalanceInput, AccountBalanceView>
{
    private readonly IArchive<AccountData, AccountId> _archive;

    public async Task<AccountBalanceView> QueryAsync(GetAccountBalanceInput input)
    {
        var data = await _archive.FindByIdAsync(input.AccountId);
        return new AccountBalanceView(data!.Owner, data.Balance);
    }
}
```

---

## Features

### Core DDD Tactical Patterns
- ✅ **Entities** - `IEntity<TId>` with unique identity and covariant type parameter
- ✅ **Value Objects** - `IValueObject` marker for immutable types
- ✅ **Aggregate Roots** - `AggregateRoot<TId, TEvent>` for state sourcing with event collection
- ✅ **Domain Events** - `IDomainEvent` with Id, OccurredOn, Source, **Metadata** (Java 4.1.0)
  - `IInternalDomainEvent` - Events within bounded context
  - `IConstructionEvent` - Marker for aggregate creation (R1 rule)
  - `IDestructionEvent` - Marker for aggregate deletion (R3 rule)
  - **Metadata Property** - `IReadOnlyDictionary<string, string>` for idempotency and distributed tracing
    - Supports CorrelationId, CausationId, UserId, TraceId
    - Preserved through entire event lifecycle (serialization, replay, publishing)
- ✅ **Event Sourcing** - `EsAggregateRoot<TId, TEvent>` with R1/R2/R3 correctness rules
- ✅ **Event Type Mapping** - `DomainEventTypeMapper` for serialization with thread-safe Lazy<BiMap>

### Event Sourcing Support
- ✅ **R1/R2/R3 Correctness Rules** - Template method pattern enforces invariants
  - **R1 (Construction)**: `{pre₀} fun₀ {post₀ & INV}` - First event establishes invariants
  - **R2 (Command)**: `{preₜ & INV} funₜ {postₜ & INV}` - Commands maintain invariants
  - **R3 (Destruction)**: `{preᵤ & INV} funᵤ {postᵤ}` - Final event may break invariants
- ✅ **Event Replay** - Reconstruct aggregate state from event history
- ✅ **EsRepository** - Generic event store repository with reflection optimization
- ✅ **Constructor Caching** - ConcurrentDictionary for performance (near-zero reflection overhead)
- ✅ **Stream Naming** - `{category}-{id}` convention

### State Sourcing with Transactional Outbox
- ✅ **OutboxRepository** - Atomic persistence of state + events
- ✅ **Faster Reads** - Query current state without event replay
- ✅ **Transaction Boundary** - Enforced at IRepositoryPeer layer (not IRepository)
- ✅ **Event Publishing** - Reliable event distribution pattern via outbox
- ✅ **Dual-Write Safety** - Database transaction ensures atomicity

### CQRS Patterns
- ✅ **Commands** - `ICommand<TInput, TOutput>` for write operations (extends IUseCase)
- ✅ **Queries** - `IQuery<TInput, TOutput>` for read operations (extends IUseCase)
- ✅ **Inquiries** - `IInquiry<TInput, TOutput>` for validation queries (independent)
- ✅ **Projections** - `IProjection<TInput, TOutput>` for read model builders (independent)
- ✅ **Projectors** - `IProjector<TInput>` reactors that maintain read models (extends `IReactor<TInput>`)
- ✅ **Notifiers** - `INotifier<TInput>` reactors that convert internal events to external (integration) events
- ✅ **Archive** - `IArchive<TData, TId>` for query database (read-side counterpart to IRepository)
- ✅ **CqrsOutput** - Fluent API for unified output with `Success()` and `Failure()` factory methods

### System Reconciliation (Java 4.1.0)
- ✅ **IReconciler<TContext, TReport>** - Interface for system state reconciliation
  - Maintenance tasks (cleanup, consistency checks, periodic jobs)
  - Context → Reconciler → Report workflow
  - **NullContext** - Singleton pattern for reconcilers without context
- ✅ **Use Cases**:
  - Expired data cleanup (draft orders, abandoned carts)
  - Referential integrity enforcement
  - Data archival and aggregation
  - System health checks and reporting

### Clean Architecture
- ✅ **Use Cases Layer** - `IUseCase<TInput, TOutput>` command pattern with async/await
- ✅ **Bridge Pattern** - `IRepository` (abstraction) ↔ `IRepositoryPeer` (implementor) separation
- ✅ **Ports & Adapters** - Hexagonal architecture support with clear layer boundaries
- ✅ **Dependency Direction** - Unidirectional: Common → Entity → UseCase → Cqrs → Core

### Event Reaction & External Publishing (Java 5.0.0 / 6.0.x)
- ✅ **IReactor<TInput>** - In-port for reacting to messages with side effects (idempotent handling)
  - `Task ExecuteAsync(TInput)` - Asynchronous message handling
  - Base of the reactor hierarchy: `IProjector<TInput>` and `INotifier<TInput>` both extend it
- ✅ **INotifier<TInput>** - Reactor that converts internal domain events into external
  (integration) events and dispatches them outward, upholding Clean Architecture layer boundaries
- ✅ **IExternalDomainEventPublisher<TEvent>** - Out-port for publishing `IExternalDomainEvent`
  to message brokers (e.g., Kafka), downstream bounded contexts, or front-ends
  - `Task PublishAsync(TEvent)` - Asynchronous publishing
- ✅ **Relay Pattern** - Repositories do NOT publish events; a separate Relay reads the
  event store/outbox and publishes reliably (see `examples/EventInfrastructure/EventStoreRelay.cs`)
  - Note: Java 6.0.0 moved `MessageProducer` out of core into the separate ezddd-gateway
    artifact; ezDDD.NET core follows suit (a .NET Gateway package is deferred post-1.0)

### Design Philosophy
- 🚀 **Async/await throughout** - All I/O operations are async (`Task<T>`, never blocking)
- 🎯 **Nullable reference types** - Compile-time null safety (`#nullable enable`)
- 📦 **Zero external dependencies** - Only .NET BCL + uContract.NET (ecosystem dependency)
- 🔒 **Thread-safe** - Concurrent collections, Lazy<T>, and snapshot patterns for safe concurrent access
- 💎 **Strongly typed** - Generic constraints and covariance (`in TInput`, `out TOutput`)
- 🧪 **Highly tested** - 543 tests (including integration tests), >90% coverage across all modules

### .NET Platform Improvements
- ✅ **Async/await** - Non-blocking I/O throughout (vs Java's blocking `execute()` methods)
- ✅ **Nullable reference types** - Compile-time null safety (`T?` vs Java's `Optional<T>`)
- ✅ **Record types** - Immutable domain events and value objects with primary constructors
- ✅ **Pattern matching** - Cleaner event handlers (vs Java's `instanceof` chains)
- ✅ **Modern C# idioms** - File-scoped namespaces, target-typed new, init-only properties
- ✅ **Generic variance** - Covariant/contravariant interfaces for flexible composition
- ✅ **Default interface methods** - `IStoreData.GetOptimisticLockVersion()`

---

## Modules

ezDDD.NET is organized into **5 modules** with unidirectional dependency chain:

```
EzDdd.Common (utilities)
    ↓
EzDdd.Entity (core DDD)
    ↓
EzDdd.UseCase (use cases)
    ↓
EzDdd.Cqrs (CQRS)
    ↓
EzDdd.Core (aggregator)
```

### Module Breakdown

#### EzDdd.Common - Foundation Utilities
Foundation utilities for the entire framework:
- **`Converter<TSource, TTarget>`** - Type conversion delegate (semantic mapping to `Func<TSource, TTarget>`)
- **`JsonUtil`** - System.Text.Json utilities
  - `DeepCopy<T>(T)` - Deep copy via JSON serialization
- **`BiMap<TKey, TValue>`** - Thread-safe bidirectional map
  - `Put(key, value)` - Add or update bidirectional mapping
  - `GetValue(key)` / `GetKey(value)` - Bidirectional lookup
  - `ContainsKey(key)` / `ContainsValue(value)` - Existence checks
  - Lock-based synchronization for thread safety

**Dependencies**: None
**Tests**: 69 passing (100% coverage)

#### EzDdd.Entity - Core DDD Building Blocks
Core DDD building blocks (entities layer):
- **`IEntity<out TId>`** - Covariant interface for entities with unique identity
- **`IValueObject`** - Marker interface for immutable value objects
- **`IDomainEvent`** - Base domain event interface (Java 4.1.0 with Metadata)
  - Properties: `Id` (Guid), `OccurredOn` (DateTimeOffset), `Source` (string)
  - **`Metadata`** (IReadOnlyDictionary<string, string>) - **New in Java 4.1.0**
    - Idempotency support (CorrelationId for duplicate detection)
    - Distributed tracing (CausationId, TraceId for event chains)
    - User context (UserId, TenantId for multi-tenancy)
- **`IInternalDomainEvent`** - Internal events within bounded context
  - `IConstructionEvent` - Marker for aggregate creation (R1 rule)
  - `IDestructionEvent` - Marker for aggregate deletion (R3 rule)
- **`AggregateRoot<TId, TEvent>`** - State sourcing aggregate root
  - `RaiseDomainEvent(TEvent)` - Add event to internal collection
  - `GetDomainEvents()` - Get read-only list of raised events
  - `ClearDomainEvents()` - Clear events after successful persistence
  - `Version` property for optimistic locking
- **`EsAggregateRoot<TId, TEvent>`** - Event-sourced aggregate root
  - Template method pattern for R1/R2/R3 enforcement
  - `_When(TEvent)` - Abstract event handler (override with pattern matching)
  - `_EnsureInvariant()` - Abstract invariant checker (override with business rules)
  - `GetCategory()` - Abstract category for stream naming (`{category}-{id}`)
  - Event replay from history via constructor
- **`DomainEventTypeMapper`** - BiMap-based event type mapping
  - `Register<TEvent>(typeName)` - Register event type with string name
  - Thread-safe registration for serialization/deserialization

**Dependencies**: EzDdd.Common, uContract.NET
**Tests**: 92 passing (>90% coverage)

#### EzDdd.UseCase - Use Cases and Repositories
Use cases layer with persistence abstractions:

**Foundation Interfaces**:
- **`IInput`** / **`IOutput`** - Marker interfaces for use case inputs/outputs
- **`IVersionedInput`** - Input with version field for optimistic locking
- **`ExitCode`** - Enumeration (`SUCCESS = 0`, `FAILURE = 1`)
- **`IReactor<in TInput>`** - Reactor in-port for async, idempotent message handling (`ExecuteAsync(TInput)`); base interface of `IProjector<TInput>` and `INotifier<TInput>`

**Use Case Pattern**:
- **`IUseCase<in TInput, out TOutput>`** - Contravariant/covariant interface
  - `ExecuteAsync(TInput)` - Main use case execution method (async)
- **`UseCaseFailureException`** - Use case failure exception

**System Reconciliation (Java 4.1.0)**:
- **`IReconciler<in TContext, TReport>`** - Interface for system state reconciliation
  - `Task<TReport> ReconcileAsync(TContext context)` - Execute reconciliation logic
  - Typically invoked by scheduled background jobs or admin tools
- **`NullContext`** - Singleton for reconcilers without context (`NullContext.Instance`)

**Repository Pattern - Bridge Pattern**:
- **`IStoreData`** - Base interface for persistence DTOs
  - `Id` property (object type), `Version` property (long for optimistic locking)
- **`IRepository<TAggregate, TId>`** - Domain abstraction (use cases layer)
  - `FindByIdAsync(TId)` - Load aggregate (nullable return)
  - `SaveAsync(TAggregate)` - Persist aggregate
  - `DeleteAsync(TId)` - Delete aggregate by ID
- **`IRepositoryPeer<TData, TId>`** - Persistence SPI (adapters layer)
  - `LoadAsync(TId)` / `SaveAsync(TData)` / `DeleteAsync(TId)`
  - **Transaction boundary at this level!**
- **`RepositorySaveException`** / **`RepositoryPeerSaveException`** - Layer-specific exceptions

**Event Infrastructure**:
- **`IExternalDomainEvent`** - External events for cross-context integration
- **`DomainEventData`** - Serialized event DTO (record type)
- **`DomainEventMapper`** - Bidirectional event conversion (domain ↔ DTO)
- **`InternalDomainEventDto`** - Internal event DTO structure for frontend communication

**Event Sourcing Repository**:
- **`EventStoreData`** - Event store DTO (implements IStoreData)
- **`EventStoreMapper`** - Aggregate ↔ EventStoreData mapping
- **`EsRepository<TAggregate, TId, TEvent>`** - Generic event sourcing repository
  - Reflection-based aggregate instantiation with constructor caching
  - ConcurrentDictionary for ConstructorInfo caching (performance)
  - Captures events BEFORE SaveAsync(), clears AFTER successful save

**State Sourcing Repository - Transactional Outbox**:
- **`IOutboxData`** - Outbox DTO interface (extends IStoreData)
- **`OutboxMapper<TAggregate, TId, TEvent>`** - Abstract outbox mapper
- **`OutboxRepository<TAggregate, TId, TEvent>`** - Generic outbox repository
  - Atomic persistence: aggregate state + events in single transaction
  - Transaction boundary at IRepositoryPeer implementation level

**External Event Publishing (Java 6.0.x)**:
- **`IExternalDomainEventPublisher<TEvent>`** - Out-port for publishing external domain events (`PublishAsync(TEvent)`)
- **`PostEventFailureException`** - Exception for event publishing failures
- Note: `MessageProducer` lives outside the core (Java: ezddd-gateway artifact); see `examples/EventInfrastructure/` for the Relay pattern

**Dependencies**: EzDdd.Entity → EzDdd.Common
**Tests**: 283 passing (>90% coverage)

#### EzDdd.Cqrs - CQRS Patterns
CQRS pattern separation:

**Command Side**:
- **`ICommand<in TInput, out TOutput>`** - Marker for write operations (extends IUseCase)
- **`IInquiry<in TInput, out TOutput>`** - Validation query usable within commands
  - Independent of IUseCase with dedicated `QueryAsync()` method
- **`IInquiryInput`** - Marker for inquiry inputs

**Query Side**:
- **`IQuery<in TInput, out TOutput>`** - Marker for read operations (extends IUseCase)
- **`IProjection<in TInput, out TOutput>`** - Read model builder
  - Independent of IUseCase with dedicated `QueryAsync()` method
- **`IProjectionInput`** - Marker for projection inputs
- **`IProjector<TInput>`** - Reactor that maintains read models (extends `IReactor<TInput>`); typically hosted as a background service
- **`INotifier<TInput>`** - Reactor that converts internal domain events into external (integration) events and dispatches them via `IExternalDomainEventPublisher<TEvent>`
- **`IArchive<TData, TId>`** - Query database interface
  - Read-side counterpart to IRepository
  - `FindByIdAsync(TId)` - Load read model data asynchronously

**Unified Output**:
- **`CqrsOutput<T>`** - Unified output class with fluent API
  - Static factory methods: `Success(T data)`, `Failure(string message)`
  - Fluent methods: `WithData(T)`, `WithMessage(string)`, `WithCode(ExitCode)`
  - Properties: `IsSuccess`, `Data`, `Message`, `ExitCode`

**Dependencies**: EzDdd.UseCase → EzDdd.Entity → EzDdd.Common
**Tests**: 71 passing (>90% coverage)

#### EzDdd.Core - Aggregator Package
Aggregator package for convenient installation:
- No additional code (pure aggregator)
- Package references to all 4 core modules
- **Install this for all features**

**Dependencies**: All 4 core modules

### Package IDs

| NuGet Package | Namespace | Purpose |
|---------------|-----------|---------|
| `ezDDD.Common` | `EzDdd.Common` | Foundation utilities |
| `ezDDD.Entity` | `EzDdd.Entity` | Core DDD patterns |
| `ezDDD.UseCase` | `EzDdd.UseCase` | Use cases & repositories |
| `ezDDD.Cqrs` | `EzDdd.Cqrs` | CQRS patterns |
| `ezDDD.Core` | (all above) | **All-in-one package** ⭐ |

---

## API Reference

> 📖 **Complete Documentation**: [API_REFERENCE.md](docs/examples/API_REFERENCE.md) (3,674 lines with detailed signatures, parameters, exceptions, and examples)

**Quick Overview** - Public APIs across 4 modules:

| Module | Key Types |
|--------|-----------|
| **Common** | `Converter<TSource, TTarget>`, `JsonUtil`, `BiMap<TKey, TValue>` |
| **Entity** | `IEntity<TId>`, `IValueObject`, `IDomainEvent`, `IInternalDomainEvent`, `AggregateRoot<TId, TEvent>`, `EsAggregateRoot<TId, TEvent>`, `DomainEventTypeMapper` |
| **UseCase** | `IUseCase<TInput, TOutput>`, **`IReactor<TInput>`**, **`IReconciler<TContext, TReport>`**, **`NullContext`**, `IRepository<TAggregate, TId>`, `IRepositoryPeer<TData, TId>`, `EsRepository<TAggregate, TId>`, `OutboxRepository<TAggregate, TData, TId>`, **`IExternalDomainEventPublisher<TEvent>`**, `DomainEventMapper`, etc. |
| **Cqrs** | `ICommand<TInput, TOutput>`, `IQuery<TInput, TOutput>`, `IInquiry<TInput, TOutput>`, `IProjection<TInput, TOutput>`, `IProjector<TInput>`, `INotifier<TInput>`, `IArchive<TData, TId>`, `CqrsOutput<T>` |

### Common Module APIs

**`BiMap<TKey, TValue>`** - Thread-safe bidirectional map
- `Put(TKey key, TValue value)` - Add or update mapping
- `GetValue(TKey key)` - Forward lookup (key → value)
- `GetKey(TValue value)` - Reverse lookup (value → key)
- `ContainsKey(TKey key)` - Check key existence
- `ContainsValue(TValue value)` - Check value existence
- `Count` - Number of mappings

**`JsonUtil`** - System.Text.Json utilities
- `DeepCopy<T>(T source)` - Deep copy via JSON serialization

### Entity Module APIs

**`IEntity<out TId>`** - Covariant entity interface
- `TId Id { get; }` - Unique identity

**`IDomainEvent`** - Base domain event interface (Java 4.1.0)
- `Guid Id { get; }` - Event unique identifier
- `DateTimeOffset OccurredOn { get; }` - Event timestamp
- `string Source { get; }` - Aggregate identifier
- **`IReadOnlyDictionary<string, string> Metadata { get; }`** - Event metadata (Java 4.1.0)

**`AggregateRoot<TId, TEvent>`** - State sourcing aggregate root
- `RaiseDomainEvent(TEvent @event)` - Add event to collection
- `GetDomainEvents()` - Get read-only list of events
- `ClearDomainEvents()` - Clear events after persistence
- `Version` - Optimistic locking version

**`EsAggregateRoot<TId, TEvent>`** - Event-sourced aggregate root
- `Apply(TEvent @event)` - Apply event with R1/R2/R3 rules
- `_When(TEvent @event)` - Abstract event handler (override)
- `_EnsureInvariant()` - Abstract invariant checker (override)
- `GetCategory()` - Abstract stream category

**`DomainEventTypeMapper`** - Event type mapping
- `Register<TEvent>(string typeName)` - Register event type
- `GetTypeName(Type type)` - Get type name for serialization
- `GetType(string typeName)` - Get Type from name

### UseCase Module APIs

**`IUseCase<in TInput, out TOutput>`** - Use case pattern
- `Task<TOutput> ExecuteAsync(TInput input)` - Execute use case

**`IReconciler<in TContext, TReport>`** - System reconciliation interface (Java 4.1.0)
- `Task<TReport> ReconcileAsync(TContext context)` - Execute reconciliation
- `NullContext.Instance` - Use for reconcilers without context

**`IRepository<TAggregate, TId>`** - Domain repository abstraction
- `Task<TAggregate?> FindByIdAsync(TId id)` - Load aggregate
- `Task SaveAsync(TAggregate aggregate)` - Persist aggregate
- `Task DeleteAsync(TId id)` - Delete aggregate

**`EsRepository<TAggregate, TId, TEvent>`** - Event sourcing repository
- Constructor with `IRepositoryPeer<EventStoreData, TId>` parameter
- Implements IRepository interface
- Automatic event replay and constructor caching

**`OutboxRepository<TAggregate, TId, TEvent>`** - State sourcing repository
- Constructor with `IRepositoryPeer<IOutboxData, TId>` and `OutboxMapper<TAggregate, TId, TEvent>`
- Implements IRepository interface
- Transactional Outbox pattern for reliable event publishing

**`IReactor<in TInput>`** - Reactor in-port (base of `IProjector<TInput>` / `INotifier<TInput>`)
- `Task ExecuteAsync(TInput input)` - Handle a message idempotently

**`IExternalDomainEventPublisher<in TEvent>`** - External event publishing out-port
- `Task PublishAsync(TEvent @event)` - Publish an `IExternalDomainEvent` to external systems
- Typically invoked by an `INotifier<TInput>` implementation

### Cqrs Module APIs

**`ICommand<in TInput, out TOutput>`** - Command marker (extends IUseCase)

**`IQuery<in TInput, out TOutput>`** - Query marker (extends IUseCase)

**`IInquiry<in TInput, out TOutput>`** - Validation query
- `Task<TOutput> QueryAsync(TInput input)` - Execute inquiry

**`IProjection<in TInput, out TOutput>`** - Read model builder
- `Task<TOutput> QueryAsync(TInput input)` - Build projection

**`IArchive<TData, TId>`** - Query database interface
- `Task<TData?> FindByIdAsync(TId id)` - Load read model data

**`CqrsOutput<T>`** - Unified output class
- `static CqrsOutput<T> Success(T data)` - Create success result
- `static CqrsOutput<T> Failure(string message, ExitCode? code = null)` - Create failure result
- `CqrsOutput<T> WithData(T data)` - Fluent API: set data
- `CqrsOutput<T> WithMessage(string message)` - Fluent API: set message
- `CqrsOutput<T> WithCode(ExitCode code)` - Fluent API: set exit code
- `bool IsSuccess` - Success/failure indicator
- `T? Data` - Result data (nullable)
- `string? Message` - Result message (nullable)
- `ExitCode ExitCode` - Exit code (SUCCESS or FAILURE)

---

## Architecture

### Clean Architecture Layers

```
┌──────────────────────────────────────────────────┐
│  Frameworks & Drivers                            │  ← Your code
│  (ASP.NET Core, EF Core, EventStoreDB, etc.)    │
├──────────────────────────────────────────────────┤
│  Interface Adapters                              │  ← Your code
│  (IRepositoryPeer implementations, Controllers)  │
├──────────────────────────────────────────────────┤
│  Use Cases                                       │  ← ezDDD.UseCase
│  (IUseCase, IRepository, ICommand, IQuery)      │     ezDDD.Cqrs
├──────────────────────────────────────────────────┤
│  Entities                                        │  ← ezDDD.Entity
│  (AggregateRoot, DomainEvent, ValueObject)      │
├──────────────────────────────────────────────────┤
│  Common                                          │  ← ezDDD.Common
│  (BiMap, JsonUtil, Converter)                   │
└──────────────────────────────────────────────────┘
```

**Dependency Direction**: Always inward (outer layers depend on inner layers)

### Event Sourcing Correctness Rules (R1/R2/R3)

ezDDD.NET enforces three invariant rules for event-sourced aggregates via template method pattern:

#### R1 (Construction Rule)
```
{pre₀} fun₀ {post₀ & INV}
```

- **First event** of an aggregate (must implement `IConstructionEvent`)
- **No precondition check** (aggregate doesn't exist yet)
- **Postcondition check** (invariant must hold after event)
- Establishes initial invariants

**Example:**
```csharp
public BankAccount(AccountId id, string owner, Money initialBalance)
{
    Id = id;
    var @event = new AccountCreated(id, owner, initialBalance);
    Apply(@event); // R1: No precondition check, invariant checked after
}
```

#### R2 (Command Rule)
```
{preₜ & INV} funₜ {postₜ & INV}
```

- **Most common case** for business logic
- **Precondition check** (invariant must hold before event)
- **Postcondition check** (invariant must hold after event)
- Maintains invariants throughout aggregate lifetime

**Example:**
```csharp
public void Deposit(Money amount)
{
    // Template method checks invariant BEFORE this event
    var @event = new MoneyDeposited(Id, amount);
    Apply(@event); // R2: Invariant checked before and after
    // Template method checks invariant AFTER this event
}
```

#### R3 (Destruction Rule)
```
{preᵤ & INV} funᵤ {postᵤ}
```

- **Final event** of an aggregate (must implement `IDestructionEvent`)
- **Precondition check** (invariant must hold before deletion)
- **No postcondition check** (aggregate is being deleted, invariant may be broken)

**Example:**
```csharp
public void Close(string reason)
{
    // Template method checks invariant BEFORE this event
    var @event = new AccountClosed(Id, reason);
    Apply(@event); // R3: Invariant checked before, NOT after
    // No postcondition check (aggregate being deleted)
}
```

### Bridge Pattern (Repository Abstraction)

```
Use Cases Layer (Domain)          Interface Adapters Layer (Infrastructure)
┌────────────────────────┐        ┌──────────────────────────────────┐
│  IRepository           │        │  IRepositoryPeer                 │
│  ==================     │        │  =====================           │
│  + FindByIdAsync()     │        │  + LoadAsync()                   │
│  + SaveAsync()         │◄───────│  + SaveAsync()  ← Transaction!  │
│  + DeleteAsync()       │        │  + DeleteAsync()                 │
└────────────────────────┘        └──────────────────────────────────┘
         ▲                                     ▲
         │                                     │
         │                          ┌──────────┴──────────┐
         │                          │                     │
   EsRepository                EventStorePeer      OutboxRepositoryPeer
   OutboxRepository           (EF Core, Marten)   (SQL Server, PostgreSQL)
```

**Key Design Points**:
- **IRepository**: Domain abstraction (use cases layer) - defines WHAT operations are needed
- **IRepositoryPeer**: Persistence SPI (adapters layer) - defines HOW to persist data
- **Transaction Boundary**: MUST be at IRepositoryPeer level (NOT IRepository level)
- **Bridge Pattern**: Separates abstraction from implementation, enabling Clean Architecture

**Why Bridge Pattern?**
- Aggregates in entities layer don't leak to adapters layer
- Domain logic independent of persistence technology
- Swap persistence implementations without changing domain code

### CQRS Architecture

```
Command Side (Write Model)              Query Side (Read Model)
┌─────────────────────────┐            ┌──────────────────────────┐
│  ICommand               │            │  IQuery                  │
│  ─────────              │            │  ───────                 │
│  + ExecuteAsync()       │            │  + ExecuteAsync()        │
│         ↓               │            │         ↓                │
│  IRepository            │            │  IProjection             │
│  ────────────           │            │  ────────────            │
│  + SaveAsync()          │            │  + QueryAsync()          │
│         ↓               │            │         ↓                │
│  Write Database         │            │  IArchive                │
│  (Event Store or        │            │  ─────────               │
│   Current State)        │            │  + FindByIdAsync()       │
└─────────────────────────┘            │         ↓                │
                                       │  Query Database          │
                                       │  (Denormalized views)    │
                                       └──────────────────────────┘
             │                                    ▲
             │       Domain Events                │
             └────────────────────────────────────┘
                      IProjector
                   (Background Service)
```

**Key Components**:
- **Write Model**: Commands use IRepository to persist aggregates
- **Read Model**: Queries use IProjection/IArchive for optimized reads
- **Projectors**: Background services listen to events and update read models
- **Eventual Consistency**: Read models eventually consistent with write model
- **Separation**: Write and read models can use different databases and schemas

**Why CQRS?**
- **Scalability**: Scale reads and writes independently
- **Performance**: Optimize read models for specific queries (denormalization)
- **Flexibility**: Different persistence strategies for commands and queries
- **Complexity Trade-off**: Eventual consistency, but better scalability

---

## Examples

> 📚 **30+ Complete Examples**: [USAGE_EXAMPLES.md](docs/examples/USAGE_EXAMPLES.md) (3,416 lines with real-world scenarios)

### Event Sourcing Example

```csharp
// Define value object
public record Money(decimal Amount) : IValueObject
{
    public Money Add(Money other) => new(Amount + other.Amount);
    public Money Subtract(Money other) => new(Amount - other.Amount);
}

// Define domain events as records with Metadata
public record AccountCreated(
    Guid Id,
    DateTimeOffset OccurredOn,
    AccountId AccountId,
    string Owner,
    Money InitialBalance
) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent
{
    public string Source => AccountId.Value;

    // Java 4.1.0: Metadata for idempotency and distributed tracing
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>();
}

// Event-sourced aggregate
public sealed class BankAccount : EsAggregateRoot<AccountId, IInternalDomainEvent>
{
    public BankAccount(AccountId id, string owner, Money initialBalance)
    {
        Id = id;
        Apply(new AccountCreated(Guid.NewGuid(), DateTimeOffset.UtcNow, id, owner, initialBalance));
    }

    public BankAccount(IEnumerable<IInternalDomainEvent> events) : base(events) { }

    public string Owner { get; private set; } = string.Empty;
    public Money Balance { get; private set; } = new(0);

    protected override void _When(IInternalDomainEvent @event)
    {
        switch (@event)
        {
            case AccountCreated created:
                Id = created.AccountId;
                Owner = created.Owner;
                Balance = created.InitialBalance;
                break;
            // ... more cases
        }
    }

    protected override void _EnsureInvariant()
    {
        if (Balance.Amount < 0)
            throw new InvalidOperationException("Balance cannot be negative");
    }

    public override string GetCategory() => "account";
}
```

### CQRS Command Example

```csharp
public record CreateAccountInput(string Owner, decimal InitialBalance) : IInput;

public class CreateAccountCommand : ICommand<CreateAccountInput, CqrsOutput<AccountId>>
{
    private readonly IRepository<BankAccount, AccountId> _repository;

    public CreateAccountCommand(IRepository<BankAccount, AccountId> repository)
    {
        _repository = repository;
    }

    public async Task<CqrsOutput<AccountId>> ExecuteAsync(CreateAccountInput input)
    {
        try
        {
            var accountId = new AccountId(Guid.NewGuid());
            var account = new BankAccount(accountId, input.Owner, new Money(input.InitialBalance));
            await _repository.SaveAsync(account);

            return CqrsOutput<AccountId>.Success(accountId)
                .WithMessage("Account created successfully");
        }
        catch (Exception ex)
        {
            return CqrsOutput<AccountId>.Failure($"Failed: {ex.Message}");
        }
    }
}
```

### CQRS Query Example

```csharp
public record GetAccountInput(AccountId AccountId) : IInput;

public record AccountView(string Owner, decimal Balance);

public class GetAccountQuery : IQuery<GetAccountInput, CqrsOutput<AccountView>>
{
    private readonly IArchive<AccountData, AccountId> _archive;

    public GetAccountQuery(IArchive<AccountData, AccountId> archive)
    {
        _archive = archive;
    }

    public async Task<CqrsOutput<AccountView>> ExecuteAsync(GetAccountInput input)
    {
        var data = await _archive.FindByIdAsync(input.AccountId);
        if (data == null)
            return CqrsOutput<AccountView>.Failure("Account not found");

        var view = new AccountView(data.Owner, data.Balance);
        return CqrsOutput<AccountView>.Success(view);
    }
}
```

### Event Reaction Example (Reactor + Relay)

```csharp
// Define reactor (IProjector<TInput> and INotifier<TInput> extend IReactor<TInput>)
public class AccountEventReactor : IReactor<IDomainEvent>
{
    public async Task ExecuteAsync(IDomainEvent @event)
    {
        switch (@event)
        {
            case AccountCreated created:
                Console.WriteLine($"Account {created.AccountId} created for {created.Owner}");
                break;
            case MoneyDeposited deposited:
                Console.WriteLine($"${deposited.Amount.Amount} deposited to {deposited.AccountId}");
                break;
        }
        await Task.CompletedTask;
    }
}

// Setup repository (NO MessageProducer parameter)
var repository = new EsRepository<BankAccount, AccountId>(peer);

// Save aggregate (Repository does NOT publish events)
var account = new BankAccount(accountId, "John Doe", new Money(100));
await repository.SaveAsync(account); // Only saves to event store

// Event publishing handled by EventStoreRelay (background service)
// See examples/EventInfrastructure/EventStoreRelay.cs for Relay pattern implementation
```

### System Reconciliation Example

```csharp
// Simple reconciler for cleaning up expired orders
public class CleanUpOrdersReconciler : IReconciler<CleanupContext, CleanupReport>
{
    public async Task<CleanupReport> ReconcileAsync(CleanupContext context)
    {
        // Find and clean up expired orders
        var expiredOrders = await FindExpiredOrdersAsync(context.ExpirationDays);
        int deletedCount = await DeleteOrdersAsync(expiredOrders);

        return new CleanupReport(Processed: expiredOrders.Count, Deleted: deletedCount);
    }
}

// For reconcilers without context, use NullContext
public class GlobalCleanupReconciler : IReconciler<NullContext, CleanupReport>
{
    public async Task<CleanupReport> ReconcileAsync(NullContext context)
    {
        // Perform global system cleanup
        int cleaned = await PerformGlobalCleanupAsync();
        return new CleanupReport(Processed: 0, Deleted: cleaned);
    }
}

// Usage
var reconciler = new CleanUpOrdersReconciler();
var report = await reconciler.ReconcileAsync(new CleanupContext(ExpirationDays: 7));
```

### More Examples

See [USAGE_EXAMPLES.md](docs/examples/USAGE_EXAMPLES.md) for:
- **Event Sourcing Workflows** (10+ examples)
- **State Sourcing with Outbox** (8+ examples)
- **CQRS Patterns** (12+ examples)
- **System Reconciliation** (4+ examples with scheduling patterns)
- **Event Publishing & Relay Integration** (5+ examples)
- **Real-World Scenarios**: Banking, E-commerce, Inventory, Order Management

---

## Differences from Java Version

### Syntax Differences

| Aspect | Java ezddd | C# ezDDD.NET |
|--------|------------|--------------|
| **Method Naming** | `execute(input)` | `ExecuteAsync(input)` (PascalCase + async) |
| **Field Naming** | `_balance` (camelCase) | `_balance` (_camelCase private fields) |
| **Generics** | `<ID, E>` | `<TId, TEvent>` (T prefix convention) |
| **Null Safety** | `Optional<T>`, `@Nullable` | `T?` (nullable reference types) |
| **Async** | Synchronous (`execute()`) | Async/await (`ExecuteAsync()` returns `Task<T>`) |
| **Collections** | `List<T>`, `Map<K,V>` | `List<T>`, `Dictionary<TKey,TValue>` |
| **Lambda** | `() -> x > 0` | `() => x > 0` |
| **Event Handling** | `instanceof` chains | Pattern matching with `switch` expressions |

### API Mapping

| Java ezddd | C# ezDDD.NET | Notes |
|------------|--------------|-------|
| `execute(I input)` | `ExecuteAsync(TInput input)` | **Async** with Task<T> return |
| `Optional<T> findById(ID)` | `Task<T?> FindByIdAsync(TId)` | Nullable + Async |
| `void when(E event)` | `void _When(TEvent @event)` | Pattern matching instead of instanceof |
| `raiseDomainEvent(E)` | `RaiseDomainEvent(TEvent)` | PascalCase |
| `getDomainEvents()` | `GetDomainEvents()` | Returns IReadOnlyList<T> |
| `clearDomainEvents()` | `ClearDomainEvents()` | PascalCase |
| `getCategory()` | `GetCategory()` | PascalCase |
| `ensureInvariant()` | `_EnsureInvariant()` | Protected method with underscore prefix |

### Platform Differences

| Feature | Java ezddd | C# ezDDD.NET |
|---------|------------|--------------|
| **Immutability** | `final` fields, getters | `record` types with primary constructors, `init` properties |
| **Null Safety** | `@Nullable`, `Optional<T>` | Nullable reference types (`T?`) with compiler enforcement |
| **Async/Await** | CompletableFuture, blocking | Native `async/await` with `Task<T>`, non-blocking |
| **Variance** | Limited covariance | Full covariance/contravariance (`in TInput`, `out TOutput`) |
| **Pattern Matching** | `instanceof` + cast | Native pattern matching with `switch` expressions |
| **Serialization** | Jackson | System.Text.Json (built-in) |
| **Reflection** | `Class.getDeclaredConstructor()` | `Type.GetConstructor()` with ConstructorInfo caching |

### Semantic Parity

- ✅ **~99% semantic parity** achieved with Java ezddd 6.0.1
- ✅ **Core patterns preserved**: Entity, AggregateRoot, Repository, CQRS identical
- ✅ **R1/R2/R3 event sourcing rules**: Identical enforcement via template method
- ✅ **Bridge pattern**: IRepository ↔ IRepositoryPeer separation identical
- ✅ **Transactional Outbox**: Same dual-write pattern for reliability
- ✅ **Stream naming**: `{category}-{id}` convention identical
- ✅ **CQRS separation**: Command/Query/Inquiry/Projection identical
- ✅ **Metadata support**: IDomainEvent.Metadata for idempotency (Java 4.1.0)
- ✅ **Reconciler pattern**: IReconciler for system maintenance (Java 4.1.0)
- ✅ **Reactor hierarchy**: IReactor / IProjector&lt;TInput&gt; / INotifier&lt;TInput&gt; (Java 5.0.0)
- ✅ **External event publishing**: IExternalDomainEventPublisher out-port (Java 6.0.x)
- ✅ **Core boundary**: MessageProducer excluded from core, matching Java's ezddd-gateway split (Java 6.0.0)

### Example Comparison

**Java:**
```java
public class BankAccount extends EsAggregateRoot<AccountId, InternalDomainEvent> {
    private Money balance;

    public void deposit(Money amount) {
        var event = new MoneyDeposited(UUID.randomUUID(), Instant.now(), id, amount);
        apply(event);
    }

    @Override
    protected void when(InternalDomainEvent event) {
        if (event instanceof MoneyDeposited deposited) {
            this.balance = balance.add(deposited.amount());
        }
    }
}
```

**C#:**
```csharp
public sealed class BankAccount : EsAggregateRoot<AccountId, IInternalDomainEvent>
{
    private Money _balance = new(0);

    public void Deposit(Money amount)
    {
        var @event = new MoneyDeposited(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, amount);
        Apply(@event);
    }

    protected override void _When(IInternalDomainEvent @event)
    {
        switch (@event)
        {
            case MoneyDeposited deposited:
                _balance = _balance.Add(deposited.Amount);
                break;
        }
    }
}
```

> 🔄 **Complete Migration Guide**: [MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md) (1,437 lines with side-by-side Java/C# examples for all patterns)

---

## Documentation

### User Documentation
- 📖 **[API_REFERENCE.md](docs/examples/API_REFERENCE.md)** - Complete API documentation (3,674 lines)
  - All 44 methods with signatures, parameters, exceptions, examples
  - Detailed usage patterns for every API
  - Technical details (lazy evaluation, caching, thread safety)

- 📚 **[USAGE_EXAMPLES.md](docs/examples/USAGE_EXAMPLES.md)** - Real-world examples (3,416 lines)
  - 30+ practical scenarios (banking, e-commerce, inventory, order management)
  - Event sourcing workflows (creation, commands, replay, deletion)
  - State sourcing with Transactional Outbox
  - CQRS patterns (commands, queries, projections, projectors)
  - Message bus integration examples

- 🔄 **[MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md)** - Java → .NET migration (1,437 lines)
  - Side-by-side code comparisons for all patterns
  - Syntax mapping tables
  - API equivalence guide
  - Platform differences explained

- ✅ **[RELEASE_CHECKLIST.md](docs/RELEASE_CHECKLIST.md)** - Publishing guide (915 lines)
  - NuGet package publishing workflow
  - Pre-release verification steps
  - Post-release tasks

- 📝 **[CHANGELOG.md](CHANGELOG.md)** - Release history
  - Consolidated [Unreleased] entry covering the full first-release scope
  - Follows Keep a Changelog / Semantic Versioning

### Developer Documentation
- 👨‍💻 **[AGENTS.md](AGENTS.md)** - Development standards and workflow
  - Build/test commands and module architecture
  - TDD, Tidy First, and ADR conventions
  - Key rules and gotchas

- 🗺️ **[ROADMAP.md](ROADMAP.md)** - Project roadmap
  - Current status and release target
  - Future considerations and upstream tracking

- 📋 **[Architecture Decision Records](docs/adr/)** - 27 ADRs documenting design decisions
  - **Stage 1**: Core Architecture (ADR-0001 to ADR-0006)
  - **Stage 2**: Core DDD Patterns (ADR-0007 to ADR-0011)
  - **Stage 3**: Phase 3 Post-Review (ADR-0012 to ADR-0016)
  - **Stage 4**: Phase 4 Critical (ADR-0017 to ADR-0019)
  - **Stage 5**: Phase 4 Post-Implementation (ADR-0020 to ADR-0023)
  - **Stage 6**: Phase 6 Java 4.1.0 Sync (ADR-0024 to ADR-0027)

### Phase Documentation
- **[ADR_PLANNING.md](docs/adr/ADR_PLANNING.md)** - Complete ADR roadmap (28 planned ADRs)

### Related Projects
- **[Java ezddd](https://gitlab.com/TeddyChen/ezddd)** - Original Java library by TeddyChen
- **[uContract.NET](https://github.com/cwouyang/uContract.NET)** - Design by Contract for .NET (dependency)

---

## Requirements

### Runtime Requirements
- **.NET 8.0 or later** (LTS until November 2026)
- **C# 12** (nullable reference types enabled)

### Dependencies
- **uContract.NET 1.0.0+** - Design by Contract support (TeddySoft ecosystem)
  - Provides `Contract.Require()`, `Contract.Ensure()`, `Contract.Invariant()`, `Contract.Check()`
  - Essential for EsAggregateRoot invariant checking (R1, R2, R3 rules)
  - Part of TeddySoft ecosystem, not considered third-party dependency
- **Zero external dependencies** - Only .NET built-in APIs for production code
  - `System.Text.Json` for event serialization and deep copy
  - `System.Reflection` for EsAggregateRoot reflection instantiation
  - `System.Collections.Concurrent` for thread-safe collections

### Testing Requirements
- **xUnit 2.4.2+** - Testing framework
- **No mocking libraries** - Keep tests simple and clear

### Build Requirements
```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Create NuGet packages
dotnet pack -c Release
```

---

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines.

**Before contributing**:
1. Read the [Architecture Decision Records](docs/adr/) to understand design rationale
2. Review the [ADR_PLANNING.md](docs/adr/ADR_PLANNING.md) for planned decisions
3. Follow the coding standards in [AGENTS.md](AGENTS.md)
4. Write tests BEFORE implementation (TDD)
5. Maintain >90% unit test coverage
6. Update ADRs and documentation when making architectural changes

**Development Workflow**:
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Write tests for your changes
4. Implement your changes
5. Run tests (`dotnet test`)
6. Commit with descriptive message (`git commit -m 'Add amazing feature'`)
7. Push to branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

---

## License

**MIT License** — Copyright (c) 2025-2026 ezDDD.NET Contributors. See [LICENSE](LICENSE) for details.

This project is a derivative work of the [Java ezddd library](https://gitlab.com/TeddyChen/ezddd)
by Teddy Chen and contributors, which is licensed under the **Apache License 2.0**.
See [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt) for the required attribution and license text.

---

## Acknowledgments

- **Original Java ezddd 6.0.1**: [Teddy Chen](https://gitlab.com/TeddyChen) (TeddySoft)
  - Repository: https://gitlab.com/TeddyChen/ezddd
  - Commit: `3aac0f5` (6.0.1)
  - History: `6e94aee` (2.1.0) → `91fac63` (4.1.0) synchronized in Phase 6 → `3aac0f5` (6.0.1) synchronized in Phase 7
- **Inspiration**: Domain-Driven Design by Eric Evans - Tactical DDD patterns
- **Architecture**: Clean Architecture by Robert C. Martin - Layered architecture design
- **Event Sourcing**: Martin Fowler's Event Sourcing pattern - Event sourcing concepts
- **CQRS**: Greg Young's CQRS pattern - Command/Query separation
- **Design by Contract**: Bertrand Meyer - Contract-based programming concepts
- **Community Contributors** - Thank you for feedback and contributions

---

## Support

- 📖 **Documentation**: See [docs/](docs/) directory for comprehensive guides
- 🐛 **Bug Reports**: [GitHub Issues](https://github.com/cwouyang/ezDDD.NET/issues)
- 💡 **Feature Requests**: [GitHub Issues](https://github.com/cwouyang/ezDDD.NET/issues)
- 💬 **Questions**: [Stack Overflow](https://stackoverflow.com/questions/tagged/ezddd-dotnet) (tag: `ezddd-dotnet`)

---

## Links

- **Java ezddd (original) 6.0.1**: https://gitlab.com/TeddyChen/ezddd (commit: `3aac0f5`)
- **uContract.NET**: https://github.com/cwouyang/uContract.NET
- **NuGet Packages**: (will be published soon)
- **API Documentation**: [docs/examples/API_REFERENCE.md](docs/examples/API_REFERENCE.md)
- **Usage Examples**: [docs/examples/USAGE_EXAMPLES.md](docs/examples/USAGE_EXAMPLES.md)
- **Migration Guide**: [docs/MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md)

---

**ezDDD.NET** - Tactical Domain-Driven Design for .NET 8+

*Last updated: 2026-07-04 (Java 6.0.1 synchronization complete)*
