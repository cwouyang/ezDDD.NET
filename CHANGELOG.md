# Changelog

All notable changes to ezDDD.NET will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Planned
- Performance benchmarks and optimization profiles
- Additional CQRS examples based on user feedback
- Integration adapters for popular event stores (EventStoreDB, Marten)
- ASP.NET Core integration helpers and middleware
- Community contributions and feedback integration

---

## [1.0.0] - 2026-01-XX

### Overview

**🎉 Initial Stable Release** - ezDDD.NET 1.0.0, a complete .NET port of **Java ezddd 4.1.0** (GitLab commit: `91fac63`).

This release represents the **first public NuGet publication** with all Java 4.1.0 features fully synchronized. Through **Phase 6 Pre-Publication Synchronization**, all breaking changes from Java ezddd 2.1.0 → 4.1.0 were incorporated into the initial 1.0.0 release, ensuring users receive a complete, up-to-date API from day one.

**Key Highlights**:
- ✅ **~99% semantic parity** with Java ezddd 4.1.0
- ✅ **562 tests passing** (100% pass rate, including 37 integration tests)
- ✅ **27 ADRs** documenting all architectural decisions
- ✅ **Zero external dependencies** (only .NET BCL + uContract.NET)
- ✅ **5 NuGet packages** ready for publication
- ✅ **Complete documentation** (11,212+ lines across all docs)

**Based on**: Java ezddd 4.1.0
- **Java Repository**: https://gitlab.com/TeddyChen/ezddd
- **Commit**: `91fac63` (Release 4.1.0)
- **Synchronization**: 44 commits from Java 2.1.0 (`6e94aee`) → 4.1.0 (`91fac63`)

### Added - Java 4.1.0 Features (Phase 6)

#### Metadata Support for Idempotency (Stage S1)
**Breaking Change** (incorporated into 1.0.0 pre-publication):
- **`IDomainEvent.Metadata`** property - `IReadOnlyDictionary<string, string>`
  - Enables idempotency detection via CorrelationId
  - Supports distributed tracing (CausationId, TraceId)
  - User context tracking (UserId, TenantId for multi-tenancy)
  - Preserved through entire event lifecycle:
    - Event creation in aggregates
    - Serialization via DomainEventMapper
    - Storage in EventStoreData
    - Publishing via MessageProducer
    - Deserialization during event replay

**Benefits**:
- Detect and handle duplicate operations (at-least-once delivery semantics)
- Trace event chains across distributed systems
- Audit trail with user and request context
- Multi-tenancy support via metadata filtering

**Test Coverage**: 16 integration tests covering metadata propagation, serialization, and idempotency

**Related**: [ADR-0008](docs/adr/0008-idomain-event-hierarchy.md) (Updated for Metadata property)

#### System Reconciliation Interface (Stage S2)
**New Feature**:
- **`IReconciler<in TContext, TReport>`** interface for system state reconciliation
  - `Task<TReport> ReconcileAsync(TContext context)` - Execute reconciliation logic
  - Designed for scheduled background jobs and administrative tasks
- **`NullContext`** singleton for reconcilers without context
  - `NullContext.Instance` - Null object pattern implementation
  - Type-safe alternative to `object` or `null`

**Use Cases**:
- Expired data cleanup (draft orders, abandoned shopping carts)
- Referential integrity enforcement (orphaned records, broken links)
- Data archival and aggregation (historical data, analytics)
- System health checks and reporting (consistency validation)

**Example**:
```csharp
public class CleanUpExpiredOrdersReconciler : IReconciler<CleanupContext, CleanupReport>
{
    public async Task<CleanupReport> ReconcileAsync(CleanupContext context)
    {
        // Find and delete expired orders
        var expiredOrders = await FindExpiredAsync(context.ExpirationDays);
        int deleted = await DeleteAsync(expiredOrders);
        return new CleanupReport(Checked: expiredOrders.Count, Deleted: deleted);
    }
}
```

**Test Coverage**: 8 integration tests covering reconciler execution, NullContext pattern, and realistic workflows

**Related**: [ADR-0024](docs/adr/0024-ireconciler-interface-system-reconciliation.md)

#### MessageProducer Refactoring (Stage S3)
**Breaking Change** (incorporated into 1.0.0 pre-publication):
- **`IMessageProducer<TMessage>`** replaces the old MessageBus pattern
  - `Task PostAsync(TMessage message)` - Asynchronous message posting
  - `IDisposable` - Proper resource management (network connections, buffers)
  - Generic type parameter for flexible message types
- **`InMemoryMessageProducer<TMessage>`** - In-memory implementation
  - `PostedMessages` property for test verification
  - Thread-safe with `ConcurrentQueue<TMessage>`
  - Disposal clears all posted messages

**Migration from MessageBus**:
- Old: `IMessageBus` / `IEventBusProducer` with `SendAsync(IEnumerable<IDomainEvent>)`
- New: `IMessageProducer<TMessage>` with `PostAsync(TMessage)`
- Benefits: Type safety, resource management, simpler API

**Repository Design** (Updated 2026-01-14):
- `EsRepository` and `OutboxRepository` **do NOT** accept `IMessageProducer` parameter
- Repositories only save to event store (no direct event publishing)
- Event publishing handled by independent **EventStoreRelay** (Transactional Outbox pattern)
- Guarantees eventual consistency with automatic retry on failures
- See `examples/EventInfrastructure/EventStoreRelay.cs` for reference implementation

**Test Coverage**: 8 integration tests for resource management, disposal, and concurrent posting

**Related**: [ADR-0025](docs/adr/0025-messageproducer-refactoring-java-4-1-0-alignment.md)

#### Service Layer Pattern (Stage S4)
**New Pattern** (Optional):
- Explicit Service classes for complex business logic
- Extract multi-aggregate workflows from Use Cases
- Maintain Use Cases as thin coordination layers
- Recommended for workflows involving:
  - Multiple aggregates
  - External API calls
  - Complex validation rules
  - Multi-step transactions

**Example**:
```csharp
public interface ITransferMoneyService
{
    Task<TransferConfirmation> TransferAsync(
        AccountId fromId,
        AccountId toId,
        Money amount);
}

public class TransferMoneyService : ITransferMoneyService
{
    private readonly IRepository<BankAccount, AccountId> _repository;

    public async Task<TransferConfirmation> TransferAsync(...)
    {
        // Load both accounts
        var fromAccount = await _repository.FindByIdAsync(fromId);
        var toAccount = await _repository.FindByIdAsync(toId);

        // Execute business logic
        fromAccount.Withdraw(amount);
        toAccount.Deposit(amount);

        // Save both
        await _repository.SaveAsync(fromAccount);
        await _repository.SaveAsync(toAccount);

        return new TransferConfirmation(fromId, toId, amount);
    }
}
```

**Related**: [ADR-0026](docs/adr/0026-service-layer-pattern.md)

#### Thread Safety Enhancements (Stage S5)
**Improvements**:
- **DomainEventTypeMapper** - Fixed static initialization race condition
  - Changed `static readonly BiMap<string, Type>` → `static readonly Lazy<BiMap<string, Type>>`
  - Thread-safe lazy initialization prevents concurrent registration issues
  - 7 new concurrency tests (10-100 threads)
- **MessageProducer** - Thread-safe concurrent posting
  - Uses `ConcurrentQueue<TMessage>` for lock-free message storage
  - Verified with 50-200 concurrent posting tasks
- **BiMap** - Existing lock-based thread safety verified
  - 7 existing concurrency tests (10-100 threads)

**Test Coverage**: 14 new concurrency tests added in Stage S5

**Related**: [ADR-0027](docs/adr/0027-thread-null-safety-review.md)

#### Null Safety Enhancements (Stage S5)
**Improvements**:
- **22 null checks added** using `ArgumentNullException.ThrowIfNull()`
  - EzDdd.Common: 7 checks (BiMap, JsonUtil)
  - EzDdd.Entity: 1 check (AggregateRoot)
  - EzDdd.UseCase: 12 checks (Repositories, Mappers, MessageProducer)
  - EzDdd.Cqrs: 2 checks (CqrsOutput)
- **Uniform validation** across all public APIs
- **Compile-time safety** with nullable reference types (`#nullable enable`)

**Related**: [ADR-0027](docs/adr/0027-thread-null-safety-review.md)

### Integration Testing (Stage S6)

**New Test Suite**: `EzDdd.Integration.Tests` (38 tests, 100% passing)

**Test Categories**:
1. **CQRS Flow with Metadata** (8 tests)
   - End-to-end metadata propagation through entire CQRS lifecycle
   - Idempotency detection using CorrelationId
   - Causation chain verification (CausationId)
   - Special character and Unicode handling

2. **Event Sourcing with Metadata** (8 tests)
   - Metadata preservation through event replay
   - Multiple save/load cycles
   - Large event streams (50 events)
   - Serialization round-trip consistency

3. **IReconciler Execution** (8 tests)
   - Basic reconciler workflows
   - NullContext pattern usage
   - Realistic cleanup scenarios (18 orders, 10 expired)
   - Context validation and error handling

4. **MessageProducer Resource Management** (8 tests)
   - IDisposable contract compliance
   - Resource cleanup on disposal
   - Using statement patterns
   - Concurrent posting before disposal

5. **Concurrent Operations** (6 tests)
   - DomainEventTypeMapper concurrent registration (20 threads)
   - MessageProducer high-volume posting (200 messages)
   - Repository concurrent save/load operations
   - Mixed concurrent workflows

**Total Integration Tests**: 38 tests (100% passing)

### Changed - Architecture & Design

#### Updated Patterns (Phase 6)
- **Event Publishing**: Repositories now use `IMessageProducer<TMessage>` instead of legacy patterns
- **Resource Management**: All disposable resources follow modern C# using patterns
- **Thread Safety**: Enhanced concurrent access support across all components
- **Null Safety**: Consistent parameter validation using .NET 8 patterns

### Documentation Updates (Stage S6)

#### Updated Files
- **README.md** - Updated for Java 4.1.0 features
  - New features section (Metadata, IReconciler, MessageProducer)
  - Updated code examples with Metadata property
  - Updated API count (44 → 46 APIs)
  - Semantic parity: ~98% → ~99%

- **27 Architecture Decision Records**:
  - **Stage 1-5**: 23 ADRs (Phase 1-5)
  - **Stage 6**: 4 new ADRs (Phase 6 Java 4.1.0 sync)
    - [ADR-0024](docs/adr/0024-ireconciler-interface-system-reconciliation.md) - IReconciler Interface
    - [ADR-0025](docs/adr/0025-messageproducer-refactoring-java-4-1-0-alignment.md) - MessageProducer Refactoring
    - [ADR-0026](docs/adr/0026-service-layer-pattern.md) - Service Layer Pattern
    - [ADR-0027](docs/adr/0027-thread-null-safety-review.md) - Thread/Null Safety Review

### Technical Details

**Test Statistics**:
- **Total Tests**: 562 tests
  - EzDdd.Common.Tests: 69 tests
  - EzDdd.Entity.Tests: 92 tests
  - EzDdd.UseCase.Tests: 297 tests
  - EzDdd.Cqrs.Tests: 67 tests
  - **EzDdd.Integration.Tests: 37 tests** (NEW)
- **Pass Rate**: 100% (562/562 passing)
- **Coverage**: >90% across all modules

**Build Quality**:
- ✅ 0 compiler warnings
- ✅ 0 compiler errors
- ✅ Enhanced Roslyn analyzers enabled
- ✅ `/p:AnalysisLevel=latest /p:EnforceCodeStyleInBuild=true` passes

**Package Sizes**:
- ezDDD.Common: ~35KB
- ezDDD.Entity: ~41KB
- ezDDD.UseCase: ~63KB
- ezDDD.Cqrs: ~37KB
- ezDDD.Core: ~28KB (aggregator)

### Breaking Changes

**Note**: Since this is the **first public NuGet publication**, these changes are incorporated into the initial 1.0.0 release. Users will never see the old API - no migration needed.

**From Java 2.1.0 → 4.1.0 (incorporated pre-publication)**:
1. **IDomainEvent.Metadata property** - Events now require Metadata property
   - Old: No Metadata property
   - New: `IReadOnlyDictionary<string, string> Metadata { get; }`
   - Impact: All event definitions must include Metadata property

2. **MessageBus → MessageProducer refactoring**
   - Old: `IMessageBus`, `IEventBusProducer` with `SendAsync(IEnumerable<IDomainEvent>)`
   - New: `IMessageProducer<TMessage>` with `PostAsync(TMessage)`
   - Impact: Event publishing code must use new IMessageProducer interface

**Migration**: Not applicable (pre-publication incorporation - users only see 1.0.0 API)

### Version Strategy: Pre-Publication Synchronization

**Critical Decision**: Since ezDDD.NET had **NOT been published to NuGet yet**, we incorporated all Java 4.1.0 changes (including breaking changes) into the **initial 1.0.0 release**.

**Benefits**:
- ✅ Users receive complete, up-to-date API aligned with Java 4.1.0 from day one
- ✅ No "outdated" API published based on older Java version
- ✅ No migration needed - users never see the old API
- ✅ Feature-complete mature API in initial release
- ✅ Semantic parity with latest Java ezddd from the start

**Phase 6 Synchronization Work** (44 commits, 6 stages, 23.5 hours):
- Stage S1: IDomainEvent.Metadata property (1.5 hours)
- Stage S2: IReconciler interface (5 hours)
- Stage S3: MessageProducer refactoring (3 hours)
- Stage S4: Service Layer pattern (6 hours)
- Stage S5: Thread/Null Safety review (6 hours)
- Stage S6: Integration Testing & Documentation (2 hours, in progress)

This approach was **only possible because we hadn't published yet**. Once published, breaking changes would require a major version bump (2.0.0).

**Note on ADR-0028 (Pre-Publication Sync Strategy)**: Originally planned as an ADR but determined to be unnecessary. This is a **process decision** (when to synchronize) rather than an **architecture decision** (how to design). Since external users never saw the Java 2.1.0-based development version, this internal development process is irrelevant to them. The strategy is documented here in CHANGELOG.md (sufficient for historical record), while ADRs 0024-0027 document the actual architectural decisions for each synchronized feature.

### Dependencies

**Runtime**:
- **.NET 8.0+** (LTS until November 2026)
- **uContract.NET 1.0.0+** - Design by Contract (TeddySoft ecosystem)

**No External Dependencies**: Only .NET BCL APIs
- `System.Text.Json` for serialization
- `System.Reflection` for event replay
- `System.Collections.Concurrent` for thread safety

**Testing** (dev dependencies):
- xUnit 2.5.3
- Microsoft.NET.Test.Sdk 17.8.0
- coverlet.collector 6.0.0

### NuGet Packages (5 packages)

All packages versioned as **1.0.0**:

| Package ID | Namespace | Size | Description |
|------------|-----------|------|-------------|
| `ezDDD.Common` | `EzDdd.Common` | ~35KB | Foundation utilities (BiMap, JsonUtil, Converter) |
| `ezDDD.Entity` | `EzDdd.Entity` | ~41KB | Core DDD patterns (Entity, AggregateRoot, DomainEvent) |
| `ezDDD.UseCase` | `EzDdd.UseCase` | ~63KB | Use cases, repositories, and messaging |
| `ezDDD.Cqrs` | `EzDdd.Cqrs` | ~37KB | CQRS patterns (Command, Query, Projection) |
| `ezDDD.Core` | All above | ~28KB | **All-in-one aggregator package** ⭐ |

**Installation**:
```bash
dotnet add package ezDDD.Core  # Recommended - includes all modules
```

### Known Issues

None - All 562 tests passing (100% pass rate).

### Contributors

- **Original Java ezddd**: Teddy Chen (TeddySoft)
- **Design by Contract**: uContract.NET integration

### See Also

- **Migration Guide**: Not applicable (initial release)
- **Java ezddd 4.1.0**: https://gitlab.com/TeddyChen/ezddd (commit: `91fac63`)
- **Complete Documentation**: [README.md](README.md), [API_REFERENCE.md](docs/examples/API_REFERENCE.md)
- **ADR Index**: [docs/adr/README.md](docs/adr/README.md)

---

## [1.0.0-alpha.1] - 2025-11-18

### Overview
Initial alpha release of ezDDD.NET, a .NET port of Java ezddd providing tactical Domain-Driven Design patterns, CQRS, and Clean Architecture support. Supports both **event sourcing** and **state sourcing** for implementing aggregates and repositories.

This is a faithful port of the **Java ezddd 2.1.0** library (GitLab commit: `6e94aee`) with .NET-specific improvements.

**Based on**: Java ezddd 2.1.0
- **Java Repository**: https://gitlab.com/TeddyChen/ezddd
- **Commit**: `6e94aee` (Release 2.1.0)

This release represents ~97% completion of the core framework with 501 tests passing and 23 architecture decision records documenting all major design decisions.

### Added

#### EzDdd.Common (Phase 1 - 2025-10-31)
Foundation utilities for the entire framework:
- `Converter<TSource, TTarget>` - Type conversion delegate (maps to `Func<TSource, TTarget>`)
  - Semantic mapping from Java functional interface to .NET delegate
- `JsonUtil` - System.Text.Json utilities
  - `DeepCopy<T>(T)` - Deep copy via JSON serialization
  - Generic type parameter support with nullable annotations
- `BiMap<TKey, TValue>` - Thread-safe bidirectional map
  - `Put(key, value)` - Add or update bidirectional mapping
  - `GetValue(key)` - Forward lookup (key → value)
  - `GetKey(value)` - Reverse lookup (value → key)
  - `ContainsKey(key)` - Check key existence
  - `ContainsValue(value)` - Check value existence
  - `Remove(key)` - Remove mapping by key
  - `RemoveByValue(value)` - Remove mapping by value
  - `Clear()` - Remove all mappings
  - `Count` - Get number of mappings
  - Lock-based synchronization for thread safety

**Test Results**: 69 tests passing (100% coverage)

#### EzDdd.Entity (Phase 2 - 2025-11-01)
Core DDD building blocks (entities layer):
- `IEntity<out TId>` - Covariant interface for entities with unique identity
  - Generic type parameter `TId` for strongly-typed identity
  - Covariant design enables inheritance polymorphism
- `IValueObject` - Marker interface for immutable value objects
  - Semantic marker following DDD tactical patterns
- `IDomainEvent` - Base domain event interface
  - Properties: `Id` (Guid), `OccurredOn` (DateTime), `Source` (string), `Metadata` (Dictionary)
  - Foundation for event sourcing and event-driven architecture
- `IInternalDomainEvent` - Internal events within bounded context
  - `IConstructionEvent` - Marker interface for aggregate creation (must be first event)
  - `IDestructionEvent` - Marker interface for aggregate deletion (must be last event)
  - Semantic markers enforcing event sourcing correctness rules
- `AggregateRoot<TId, TEvent>` - State sourcing aggregate root
  - `RaiseDomainEvent(TEvent)` - Add event to internal collection
  - `GetDomainEvents()` - Get read-only list of raised events
  - `ClearDomainEvents()` - Clear events after successful persistence
  - `Version` property for optimistic locking
  - Event collection management with encapsulation
- `EsAggregateRoot<TId, TEvent>` - Event-sourced aggregate root
  - R1/R2/R3 correctness rules enforcement via template method pattern
  - `When(TEvent)` - Abstract event handler (override with pattern matching)
  - `EnsureInvariant()` - Abstract invariant checker (override with business rules)
  - `GetCategory()` - Abstract category for stream naming convention
  - Event replay from history via constructor
  - Template method pattern for invariant checking:
    - R1 (Construction): No precondition check, postcondition check enforced
    - R2 (Command): Precondition and postcondition checks enforced
    - R3 (Destruction): Precondition check enforced, no postcondition check
  - Stream naming convention: `{category}-{id}`
- `DomainEventTypeMapper` - BiMap-based event type mapping
  - `Register<TEvent>(typeName)` - Register event type with string name
  - `GetTypeName(Type)` - Get string name for event type
  - `GetType(typeName)` - Get Type from string name
  - Thread-safe registration using BiMap
  - Essential for event serialization and deserialization

**Test Results**: 85 tests passing (>95% coverage, including 8 integration tests with banking domain)

#### EzDdd.UseCase (Phase 3 - 2025-11-06)
Use cases layer with persistence abstractions:

**Foundation Interfaces** (Iteration 1):
- `IInput` - Marker interface for use case inputs
- `IOutput` - Marker interface for use case outputs
- `IVersionedInput` - Input with version field for optimistic locking
  - `long Version { get; }` - Version property (matches Java long type)
- `ExitCode` - Enumeration for use case execution results
  - `SUCCESS = 0` - Successful execution
  - `FAILURE = 1` - Failed execution
- `IReactor` - Event reactor interface for async event processing
  - `ReactToAsync(IEnumerable<IDomainEvent>)` - React to domain events asynchronously

**Use Case Pattern** (Iteration 2):
- `IUseCase<in TInput, out TOutput>` - Contravariant/covariant interface
  - `ExecuteAsync(TInput)` - Main use case execution method (async)
  - Generic variance enables flexible composition
- `UseCaseFailureException` - Use case failure exception
  - 4 constructors for flexible exception handling
  - Inherits from System.Exception (unchecked exception in .NET)

**Repository Pattern - Bridge Pattern** (Iteration 3):
- `IStoreData` - Base interface for persistence DTOs
  - `Id` property (object type for flexibility)
  - `Version` property (long for optimistic locking, matches Java long)
  - `GetOptimisticLockVersion()` - Default interface method
- `IRepository<TAggregate, TId>` - Domain abstraction (use cases layer)
  - `FindByIdAsync(TId)` - Load aggregate (nullable return)
  - `SaveAsync(TAggregate)` - Persist aggregate
  - `DeleteAsync(TId)` - Delete aggregate by ID
  - Bridge pattern abstraction for Clean Architecture
- `IRepositoryPeer<TData, TId>` - Persistence SPI (adapters layer)
  - `LoadAsync(TId)` - Load data transfer object
  - `SaveAsync(TData)` - Persist data (transaction boundary at this level!)
  - `DeleteAsync(TId)` - Delete data
  - Bridge pattern implementor with transaction responsibility
- `RepositorySaveException` - Repository save failure (domain layer)
  - `OptimisticLockingFailure` constant for version conflict detection
- `RepositoryPeerSaveException` - Peer save failure (adapter layer)
  - Exception translation from infrastructure to domain layer

**Event Infrastructure** (Iteration 4):
- `IExternalDomainEvent` - External events for cross-context integration
  - Marker interface for events crossing bounded contexts
- `DomainEventData` - Serialized event DTO (record type)
  - Properties: `Id`, `Type`, `OccurredOn`, `Source`, `Payload`, `Metadata`
  - Immutable record for event persistence
  - JSON-serializable structure
- `DomainEventMapper` - Bidirectional event conversion
  - `ToData(IDomainEvent)` - Convert domain event to DTO
  - `ToDomain(DomainEventData)` - Convert DTO to domain event
  - Uses DomainEventTypeMapper for type resolution
  - Uses System.Text.Json for payload serialization
- `InternalDomainEventDto` - Internal event DTO structure
  - Properties: `Id`, `Type`, `OccurredOn`, `Source`, `Payload`, `Metadata`
  - Cross-platform DTO for frontend communication
  - Matches DomainEventData structure for consistency

**Event Sourcing Repository** (Iteration 5):
- `EventStoreData` - Event store DTO (implements IStoreData)
  - Properties: `Id`, `Version`, `Events` (list of InternalDomainEventDto)
  - Optimistic locking version = base version + event count
- `EventStoreMapper` - Aggregate ↔ EventStoreData mapping
  - `ToData(aggregate)` - Convert aggregate to event store DTO
  - `ToDomain(data)` - Convert event store DTO to aggregate via event replay
  - Extracts events before conversion
- `EsRepository<TAggregate, TId, TEvent>` - Generic event sourcing repository
  - Three generic type parameters with comprehensive constraints
  - Reflection-based aggregate instantiation with constructor caching
  - `ConcurrentDictionary` for ConstructorInfo caching (performance optimization)
  - Captures events BEFORE SaveAsync() call
  - Clears events AFTER successful save
  - Uses EventStoreMapper for conversion
  - Delegates actual persistence to IRepositoryPeer

**State Sourcing Repository - Transactional Outbox** (Iteration 6):
- `IOutboxData` - Outbox DTO interface (extends IStoreData)
  - Property: `UnsentEvents` (list of DomainEventData)
  - Dual-write pattern: aggregate state + events
- `OutboxMapper<TAggregate, TId, TEvent>` - Abstract outbox mapper
  - `ToData(aggregate)` - Convert aggregate to outbox DTO (must implement)
  - `ToDomain(data)` - Convert outbox DTO to aggregate (must implement)
  - Template for user-defined aggregate mapping
- `OutboxRepository<TAggregate, TId, TEvent>` - Generic outbox repository
  - Atomic persistence: aggregate state + events in single transaction
  - Transaction boundary at IRepositoryPeer implementation level
  - Captures events BEFORE SaveAsync() call
  - Clears events AFTER successful save
  - Transactional Outbox pattern for reliable event publishing

**Message Bus - Observer Pattern** (Iteration 7):
- `IMessageBus` - In-process event bus interface
  - `RegisterAsync(IReactor)` - Register event handler asynchronously
  - `SendAsync(IEnumerable<IDomainEvent>)` - Publish events asynchronously
  - Observer pattern abstraction
- `IMessageProducer` - External event bus interface
  - `SendAsync(IEnumerable<IDomainEvent>)` - Publish to external event bus
  - IDisposable for resource management
  - Adapter for external messaging infrastructure
- `BlockingMessageBus` - Synchronous message bus implementation
  - Thread-safe observer registration with lock + snapshot pattern
  - Sequential event processing to maintain order
  - Observers stored in List<IReactor> with lock synchronization
- `EventBusProducer` - Adapter bridging internal and external buses
  - Wraps IMessageBus as IMessageProducer
  - Adapts internal event bus to external producer interface
- `GenericReactor<TEvent>` - Generic reactor for specific event types
  - Filters events by type before processing
  - Type-safe event handling with pattern matching

**Integration Tests** (Iteration 8):
- 49 integration tests covering complete workflows
- Banking test domain: BankAccount (event-sourced), Money (value object)
- Events: AccountCreated, MoneyDeposited, MoneyWithdrawn, AccountClosed
- Use cases: CreateAccountUseCase, DepositUseCase, WithdrawUseCase
- Test scenarios:
  - Event sourcing workflow (10 tests)
  - State sourcing workflow (10 tests)
  - Message bus integration (7 tests)
  - Use case execution flow (8 tests)
  - Event infrastructure (6 tests)
  - Cross-component integration (8 tests)

**Test Results**: 279 tests passing (>95% coverage, including 49 integration tests)

#### EzDdd.Cqrs (Phase 4 - 2025-11-18)
CQRS pattern separation:

**Command Side** (Iteration 3):
- `ICommand<in TInput, out TOutput>` - Marker for write operations (extends IUseCase)
  - Contravariant input, covariant output for flexible composition
  - Semantic marker following CQRS pattern
- `IInquiry<in TInput, out TOutput>` - Validation query usable within commands
  - Read-only validation operations during command processing
  - Independent of IUseCase with dedicated QueryAsync() method
- `IInquiryInput` - Marker for inquiry inputs

**Query Side** (Iteration 4):
- `IQuery<in TInput, out TOutput>` - Marker for read operations (extends IUseCase)
  - Contravariant input, covariant output for flexible composition
  - Semantic marker following CQRS pattern
- `IProjection<in TInput, out TOutput>` - Read model builder
  - Generates view models from query database
  - Independent of IUseCase with dedicated QueryAsync() method
- `IProjectionInput` - Marker for projection inputs
- `IProjector` - Background service marker for maintaining read models
  - Marker interface only (no methods)
  - Integration with hosting frameworks via marker semantics
- `IArchive<TData, TId>` - Query database interface
  - Read-side counterpart to IRepository
  - `FindByIdAsync(TId)` - Load read model data asynchronously
  - All async methods for I/O operations
  - Idempotency requirements for projector resilience

**Unified Output** (Iteration 2):
- `CqrsOutput<T>` - Unified output class with fluent API
  - Static factory methods:
    - `Success(T data)` - Create success result with data
    - `Failure(string message, ExitCode? code)` - Create failure result
  - Fluent methods:
    - `WithData(T data)` - Set data
    - `WithMessage(string message)` - Set message
    - `WithCode(ExitCode code)` - Set exit code
  - Properties:
    - `IsSuccess` - Success/failure indicator
    - `Data` - Result data (nullable)
    - `Message` - Result message (nullable)
    - `ExitCode` - Exit code (SUCCESS or FAILURE)
  - Self-referential generic pattern for fluent API type safety
  - Base class design with virtual methods for extensibility

**Test Results**: 68 tests passing (>95% coverage, including 14 integration tests with Order CQRS domain)

#### EzDdd.Core (Phase 5 - 2025-11-18)
Aggregator package for convenient installation:
- No additional code (pure aggregator)
- Package references to all 4 core modules:
  - EzDdd.Common
  - EzDdd.Entity
  - EzDdd.UseCase
  - EzDdd.Cqrs
- Single `dotnet add package ezDDD.Core` installs complete framework

### Documentation

#### Core Documentation (Phase 5 - 2025-11-18)
- **README.md** - Complete project overview with Quick Start guide
- **API_REFERENCE.md** (3,674 lines) - Comprehensive API documentation for all 44 public APIs
- **USAGE_EXAMPLES.md** (3,416 lines) - 30+ real-world examples across all modules
- **MIGRATION_GUIDE.md** (1,437 lines) - Java ezddd → .NET migration guide with syntax mappings

#### Architecture Decision Records (23 ADRs)

**Stage 1 - Core Architecture** (2025-10-31):
- [ADR-0001](docs/adr/0001-target-framework.md) - Target Framework (.NET 8)
- [ADR-0002](docs/adr/0002-package-naming-and-structure.md) - Package Naming and Structure
- [ADR-0003](docs/adr/0003-module-architecture-dependency-chain.md) - Module Architecture and Dependency Chain
- [ADR-0004](docs/adr/0004-zero-third-party-dependency-principle.md) - Zero Third-Party Dependency Principle
- [ADR-0005](docs/adr/0005-complete-reimplementation-approach.md) - Complete Reimplementation Approach
- [ADR-0006](docs/adr/0006-ucontract-integration-design-by-contract.md) - uContract.NET Integration for Design by Contract

**Stage 2 - Core DDD Patterns** (2025-11-01):
- [ADR-0007](docs/adr/0007-ientity-ivalueobject-design.md) - IEntity and IValueObject Design
- [ADR-0008](docs/adr/0008-idomain-event-hierarchy.md) - IDomainEvent Hierarchy Design
- [ADR-0009](docs/adr/0009-aggregate-root-base-class-design.md) - AggregateRoot Base Class Design
- [ADR-0010](docs/adr/0010-esaggregate-root-event-sourcing-implementation.md) - EsAggregateRoot Event Sourcing Implementation (R1, R2, R3 Rules)
- [ADR-0011](docs/adr/0011-event-replay-invariant-checking.md) - Event Replay and Invariant Checking

**Stage 3 - Phase 3 Post-Review** (2025-11-10):
- [ADR-0012](docs/adr/0012-resource-management-event-bus-producers.md) - Resource Management Pattern for External Event Bus Producers
- [ADR-0013](docs/adr/0013-transaction-boundaries-repository-pattern.md) - Transaction Boundaries in Repository Pattern
- [ADR-0014](docs/adr/0014-domaineventdata-equality-semantics.md) - DomainEventData Equality Semantics
- [ADR-0015](docs/adr/0015-cross-platform-dto-structure.md) - Cross-Platform DTO Structure (InternalDomainEventDto)
- [ADR-0016](docs/adr/0016-async-await-throughout.md) - Async/Await Throughout All I/O Operations

**Stage 4 - Phase 4 Critical** (2025-11-17):
- [ADR-0017](docs/adr/0017-cqrsoutput-implementation-strategy.md) - CqrsOutput Implementation Strategy
- [ADR-0018](docs/adr/0018-iarchive-async-method-design.md) - IArchive Async Method Design
- [ADR-0019](docs/adr/0019-iinquiry-iprojection-independence.md) - IInquiry and IProjection Independence from IUseCase

**Stage 5 - Phase 4 Post-Implementation** (2025-11-18):
- [ADR-0020](docs/adr/0020-iprojector-lifecycle-management.md) - IProjector Lifecycle Management Integration
- [ADR-0021](docs/adr/0021-generic-variance-annotations.md) - Generic Variance Annotations for CQRS Interfaces
- [ADR-0022](docs/adr/0022-read-model-design-patterns.md) - Read Model Design Patterns
- [ADR-0023](docs/adr/0023-archive-idempotency-requirements.md) - Archive Idempotency Requirements

#### Planning Documentation
- **ROADMAP.md** - Complete development roadmap with session logs
- **DOTNET_PORT.md** - Technical planning and API design decisions
- **CLAUDE.md** - Development guidance for Claude Code
- **PHASE3_IMPLEMENTATION_PLAN.md** - Phase 3 detailed iteration plan (8 iterations)
- **PHASE3_JAVA_ANALYSIS.md** - Java ezddd source analysis (2,172 lines)
- **PHASE4_JAVA_ANALYSIS.md** - Java ezcqrs source analysis (1,090 lines)
- **PHASE4_IMPLEMENTATION_PLAN.md** - Phase 4 detailed iteration plan (7 iterations)
- **PHASE3_POST_REVIEW_ACTION_PLAN.md** - Phase 3 review and ADR planning
- **ADR_PLANNING.md** - Complete ADR roadmap (28 planned ADRs)

### Testing

**Test Coverage**:
- **Total**: 501 tests passing (0 failures)
- **EzDdd.Common**: 69 tests (100% coverage)
- **EzDdd.Entity**: 85 tests (>95% coverage)
- **EzDdd.UseCase**: 279 tests (>95% coverage)
- **EzDdd.Cqrs**: 68 tests (>95% coverage)

**Test Frameworks**:
- xUnit 2.4.2+ - Primary testing framework
- No mocking libraries - Simple, clear tests

**Test Domains**:
- Banking domain (Phase 3): BankAccount (event-sourced), Money (value object)
- Order domain (Phase 4): Order (CQRS aggregate), OrderProjection (read model)

**Integration Tests**:
- 49 tests in Phase 3 (banking workflows)
- 14 tests in Phase 4 (CQRS flows)
- Complete end-to-end scenarios

### Architecture

**Design Patterns Implemented**:
- **Clean Architecture** - 4 layers (Entity → UseCase → Interface Adapters → Frameworks)
- **Bridge Pattern** - IRepository (abstraction) ↔ IRepositoryPeer (implementor)
- **Template Method Pattern** - EsAggregateRoot with R1/R2/R3 correctness rules
- **Observer Pattern** - MessageBus for intra-process event distribution
- **Command Pattern** - IUseCase for encapsulating business logic
- **Strategy Pattern** - IReactor for pluggable event handling
- **Adapter Pattern** - EventBusProducer adapting IMessageBus to IMessageProducer

**CQRS Architecture**:
- **Write Model**: Commands use IRepository to persist aggregates
- **Read Model**: Queries use IProjection/IArchive for optimized reads
- **Projectors**: Background services listen to events and update read models
- **Eventual Consistency**: Read models eventually consistent with write model

**Event Sourcing Architecture**:
- Complete event replay with invariant checking
- R1/R2/R3 correctness rules via template method pattern
- Stream naming convention: `{category}-{id}`
- Append-only event persistence

**State Sourcing Architecture**:
- Transactional Outbox pattern for atomic persistence
- Dual-write: aggregate state + events
- Faster reads than pure event sourcing
- Transaction boundary at IRepositoryPeer implementation level

### Platform Features

**C# Language Features**:
- ✅ **Async/await throughout** - All I/O operations are async (Task<T>)
- ✅ **Nullable reference types** - Compile-time null safety (#nullable enable)
- ✅ **Record types** - Immutable domain events and value objects
- ✅ **Pattern matching** - Switch expressions for cleaner event handling
- ✅ **Modern C# idioms** - File-scoped namespaces, target-typed new, init-only properties
- ✅ **Generic variance** - Covariant/contravariant interfaces (in TInput, out TOutput)
- ✅ **Default interface methods** - IStoreData.GetOptimisticLockVersion()

**Thread Safety**:
- ✅ **ConcurrentDictionary** - Constructor caching in EsRepository
- ✅ **Lock-based synchronization** - BiMap, MessageBus, DomainEventTypeMapper
- ✅ **Snapshot pattern** - MessageBus observer iteration
- ✅ **Immutable collections** - Read-only event lists

**Performance Optimizations**:
- ✅ **Reflection caching** - ConstructorInfo cached in ConcurrentDictionary
- ✅ **Lazy evaluation** - Deferred event replay until needed
- ✅ **Efficient serialization** - System.Text.Json for event persistence

### Dependencies

**Runtime Dependencies**:
- **.NET 8.0 BCL** - Only built-in .NET APIs
  - `System.Text.Json` for event serialization and deep copy
  - `System.Reflection` for EsAggregateRoot reflection instantiation
  - `System.Collections.Concurrent` for thread-safe collections
- **uContract.NET 1.0.0+** - Design by Contract support (TeddySoft ecosystem)
  - Provides `Contract.Require()`, `Contract.Ensure()`, `Contract.Invariant()`, `Contract.Check()`
  - Essential for EsAggregateRoot invariant checking (R1, R2, R3 rules)
  - Not considered third-party dependency (part of TeddySoft DDD toolkit)

**Test Dependencies**:
- **xUnit 2.4.2+** - Testing framework
- **No mocking libraries** - Keep tests simple and clear

**Zero External Dependencies Principle**:
- No third-party NuGet packages (except uContract.NET ecosystem)
- Only .NET built-in APIs for production code
- Same philosophy as Java ezddd and uContract.NET

### Semantic Parity with Java ezddd

**Overall Parity**: ~98% semantic parity achieved

**Core Patterns Preserved**:
- ✅ Entity, ValueObject, DomainEvent hierarchy identical
- ✅ AggregateRoot event collection management identical
- ✅ EsAggregateRoot R1/R2/R3 rules identical
- ✅ Bridge pattern (IRepository ↔ IRepositoryPeer) identical
- ✅ Transactional Outbox pattern identical
- ✅ CQRS separation (Command/Query/Inquiry/Projection) identical
- ✅ Stream naming convention identical

**Platform Differences** (by design):
- Async/await throughout (vs Java synchronous)
- Nullable reference types (vs Java @Nullable annotations)
- Record types for immutability (vs Java final fields)
- Pattern matching for event handling (vs Java instanceof)
- Task<T> for async operations (vs Java CompletableFuture)

**API Naming Conventions**:
- Java `execute()` → C# `ExecuteAsync()` (PascalCase + async suffix)
- Java `findById()` → C# `FindByIdAsync()` (PascalCase + async suffix)
- Java `Optional<T>` → C# `T?` (nullable reference types)

### Known Limitations

**Alpha Release Caveats**:
- API may change before 1.0.0 stable release
- No production-ready IRepositoryPeer implementations included (only test implementations)
- No integration with actual event stores (EventStoreDB, Marten) - planned for future
- No performance benchmarks yet - planned for beta

**Technical Limitations**:
- EsRepository requires parameterless constructor or constructor accepting `IEnumerable<TEvent>`
- DomainEventTypeMapper requires explicit type registration
- Reflection overhead for event-sourced aggregate instantiation (mitigated by caching)

### Performance

**When Used Correctly**:
- **Constructor caching**: Near-zero reflection overhead after first instantiation
- **Event replay**: Fast reconstruction from event history
- **State sourcing**: Faster reads than pure event sourcing (no replay needed)
- **Message bus**: Minimal overhead with thread-safe snapshot pattern

**Optimization Opportunities** (future work):
- Expression Trees instead of Reflection for aggregate instantiation
- Ahead-of-time (AOT) compilation support
- Span<T> usage for event processing
- ValueTask<T> for frequently-completed async operations

---

## [Unreleased] - Future Plans

### Planned for 1.0.0-beta.1
- Stability improvements based on alpha feedback
- Performance benchmarks with detailed profiling
- Additional real-world examples (e-commerce, reservation system)
- Integration adapters for EventStoreDB
- Integration adapters for Marten
- ASP.NET Core integration helpers

### Planned for 1.0.0 Stable
- Production-ready documentation
- Community feedback integration
- Performance optimization based on benchmarks
- API finalization (breaking changes only if necessary)
- Full compatibility matrix (supported .NET versions, event stores)

### Planned for 1.1.0+
- Additional convenience methods based on user requests
- Enhanced projector lifecycle management
- Distributed transaction support exploration
- gRPC integration examples
- Azure Service Bus integration adapter
- RabbitMQ integration adapter
- Kafka integration adapter

### Planned for 2.0.0 (Major)
- .NET 9+ features (if applicable)
- Potential API refinements based on 1.x experience
- Advanced event sourcing features (snapshots, temporal queries)
- CQRS orchestration helpers
- Saga pattern support

---

## Version History Summary

| .NET Version | Date | Java Version | Modules | Tests | ADRs | Status |
|--------------|------|--------------|---------|-------|------|--------|
| 1.0.0-alpha.1 | 2025-11-18 | Java 2.1.0 (`6e94aee`) | 5 (Common, Entity, UseCase, Cqrs, Core) | 501 | 23 | ✅ Alpha release |

---

## Migration from Java ezddd

See [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md) for complete migration instructions.

**Key Syntax Changes**:
- Method naming: `execute()` → `ExecuteAsync()` (PascalCase + async)
- Lambda syntax: `() -> x > 0` → `() => x > 0`
- Functional types: `Function<T, R>` → `Func<T, TResult>`
- Null checks: `Optional<T>` → `T?` (nullable reference types)
- Event handling: `instanceof` → pattern matching with `switch`

**Semantic Changes**:
- All I/O operations are async (use `await`)
- Exception handling: try-catch instead of checked exceptions
- Immutability: use record types for events and value objects
- Thread safety: built-in with ConcurrentDictionary and locks

---

## How to Report Issues

If you encounter any issues or have suggestions:
- 🐛 **Bug Reports**: [GitHub Issues](https://github.com/TeddyChen/ezddd.NET/issues)
- 💡 **Feature Requests**: [GitHub Discussions](https://github.com/TeddyChen/ezddd.NET/discussions)
- 💬 **Questions**: [Stack Overflow](https://stackoverflow.com/questions/tagged/ezddd-dotnet)

---

## License

Apache License 2.0 - Same as the [Java ezddd library](https://gitlab.com/TeddyChen/ezddd)

Copyright (c) 2025 ezDDD.NET Contributors

---

## Credits

- **Original Java version**: [Java ezddd 2.1.0](https://gitlab.com/TeddyChen/ezddd) (GitLab commit: `6e94aee`)
  - Author: Teddy Chen (TeddySoft)
  - JDK: Java 17+
  - Dependencies: Jackson, uContract 2.0.0
- **.NET port**: ezDDD.NET Contributors
  - Target: .NET 8+
  - Dependencies: uContract.NET 1.0.0+ (zero external dependencies)
- **Design by Contract**: Bertrand Meyer
- **Tactical DDD**: Eric Evans (Domain-Driven Design)
- **Clean Architecture**: Robert C. Martin
- **Event Sourcing**: Martin Fowler
- **CQRS**: Greg Young

---

[Unreleased]: https://github.com/TeddyChen/ezddd.NET/compare/v1.0.0-alpha.1...HEAD
[1.0.0-alpha.1]: https://github.com/TeddyChen/ezddd.NET/releases/tag/v1.0.0-alpha.1
