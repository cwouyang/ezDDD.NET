# CLAUDE.md - ezddd.NET Port

This file provides guidance to Claude Code when working on the **.NET port** of ezddd.

> **Note**: This is for the .NET/C# version. For the Java version, see [../ezddd/CLAUDE.md](../ezddd/CLAUDE.md)

---

## 📌 Project Overview

**ezddd.NET** is a .NET port of the Java ezddd library, providing tactical Domain-Driven Design (DDD) patterns, Command Query Responsibility Segregation (CQRS), and Clean Architecture (CA) support. It supports both **state sourcing** and **event sourcing** for implementing aggregates and repositories.

**Based on**: Java ezddd 2.1.0 (commit `6e94aee`) → **Syncing to Java 4.1.0** (Phase 6)

- **Language**: C# / .NET 8+
- **Build Tool**: dotnet CLI
- **Development Version**: 1.0.0-dev (unreleased)
- **Target Release**: 1.0.0 (based on Java ezddd 4.1.0)
- **Target Framework**: .NET 8.0
- **Main Namespace**: `EzDdd`
- **Package ID Prefix**: `ezDDD`
- **Semantic Parity**: ~98% with Java 2.1.0 (targeting ~99% with Java 4.1.0 after Phase 6)

---

## ⚠️ Project Status

**✅ PHASE 5 COMPLETE - 📋 PHASE 6 IN PLANNING** (2026-01-06)

**Implementation Progress**:
- ✅ **Phase 1-5 Complete** (Based on Java ezddd 2.1.0):
  - ✅ **Phase 1** - EzDdd.Common (69 tests passing)
  - ✅ **Phase 2** - EzDdd.Entity (85 tests passing)
  - ✅ **Phase 3** - EzDdd.UseCase (279 tests passing) - Complete with all fixes
  - ✅ **Phase 4** - EzDdd.Cqrs (68 tests passing) - Complete
  - ✅ **Phase 5** - EzDdd.Core + Documentation (11,212+ lines) - Complete
- 📋 **Phase 6 Planned** (Sync to Java ezddd 4.1.0):
  - 44 commits to sync (+5,167/-2,132 lines)
  - 7 new ADRs to write (ADR-0024 to ADR-0030)
  - Estimated: 44-62 hours (~1-2 weeks)

**ADRs Status**:
- ✅ **Completed**: 23 ADRs (Stage 1-5)
  - Stage 1 (6 ADRs): Core Architecture
  - Stage 2 (5 ADRs): Core DDD Patterns
  - Stage 3 (5 ADRs): Phase 3 Post-Review
  - Stage 4 (3 ADRs): Phase 4 Critical
  - Stage 5 (4 ADRs): Phase 4 Post-Implementation
- 📋 **Planned**: 7 ADRs (Stage 6, Phase 6)
  - ADR-0024 to ADR-0030 (Java 4.1.0 sync)

**Test Coverage**: 516 tests passing (100% pass rate, >90% code coverage) - Updated 2026-01-06 after S1

**Current Status** (2026-01-06):
- ✅ Phase 1-5 complete (based on Java ezddd 2.1.0, commit `6e94aee`)
- 📋 Phase 6 planning (sync to Java ezddd 4.1.0, commit `91fac63`)
- ⏳ Pre-publication synchronization in progress
- 🎯 All work complete before first NuGet publication

**Next Steps**: Complete Phase 6 (Java 4.1.0 synchronization) → Publish 1.0.0 to NuGet

**📊 Complete Roadmap**: See [ROADMAP.md](ROADMAP.md) for detailed workflow, timeline, and progress tracking

---

## 📊 Java Version Correspondence

### Current Implementation (Phase 1-5) ✅

- **Based on**: Java ezddd 2.1.0
- **GitLab Repository**: https://gitlab.com/TeddyChen/ezddd
- **GitLab Commit**: `6e94aee`
- **Commit Message**: "[Refactoring and Deployment and Feature Addition] (1) Revised DomainEventDto to InternalDomainEventDto. (2) Release version 2.1.0. (3) Added readTree to support byte array."
- **Release Date**: 2024-03
- **Semantic Parity**: ~98%
- **Status**: ✅ Complete (501 tests passing, 23 ADRs)

### Target Implementation (Phase 6) 📋

- **Target**: Java ezddd 4.1.0
- **GitLab Commit**: `91fac63` (HEAD, master)
- **Changes Since 2.1.0**: 44 commits (+5,167 lines, -2,132 lines)
- **Major Version Jump**: 2.1.0 → 3.0.0/3.0.1 → 4.0.0 → 4.1.0
- **Target Semantic Parity**: ~99%

**Major Changes in Java 4.1.0**:
- ⚠️ **BREAKING**: `IDomainEvent.Metadata` property - Add metadata for idempotency support
- ⚠️ **BREAKING**: `MessageBus` → `MessageProducer<T>` refactoring - New messaging pattern
- ✨ **NEW**: `IReconciler<Context, Report>` interface - System state reconciliation
- 🔄 **REFACTOR**: Service Layer pattern - Explicit service classes
- 🐛 **FIX**: Thread safety improvements (CopyOnWriteArrayList, AtomicReference)
- 🐛 **FIX**: Null safety - Comprehensive null validation
- 🐛 **FIX**: Equals/HashCode contract compliance

### Version Strategy - Pre-Publication Synchronization

**⚠️ Critical Decision**: Since ezDDD.NET has **NOT been published to NuGet yet**, we have a unique opportunity to:

- ✅ Incorporate all Java 4.1.0 changes (including breaking changes) into the **initial 1.0.0 release**
- ✅ Avoid publishing an "outdated" API based on older Java version
- ✅ Deliver a complete, up-to-date API aligned with Java ezddd 4.1.0 from day one
- ✅ Users receive a mature, feature-complete 1.0.0 from the start
- ✅ No migration needed - users never see the old API

**Approach**:
1. Complete Phase 6 synchronization work (44-62 hours)
2. Update all internal references from "based on Java 2.1.0" to "based on Java 4.1.0"
3. Publish initial version as **1.0.0** to NuGet
4. Users get the full Java 4.1.0 feature set from the start

**This is only possible because we haven't published yet**. Once published, breaking changes would require a major version bump (2.0.0).

**Complete Sync Plan**: See [DOTNET_PORT.md](DOTNET_PORT.md) "Java 4.1.0 Synchronization Plan" section (lines 45-1040) for detailed implementation plan.

---

## 📖 Planning Documents

### Core Planning
- **[ROADMAP.md](ROADMAP.md)** - 🎯 Development roadmap and progress tracking
- **[DOTNET_PORT.md](DOTNET_PORT.md)** - Technical planning and API design
- **[CLAUDE.md](CLAUDE.md)** - This file (development guidance)

### Phase 3 Documentation (Reference)
- **[PHASE3_IMPLEMENTATION_PLAN.md](docs/PHASE3_IMPLEMENTATION_PLAN.md)** - Implementation plan (8 iterations)
- **[PHASE3_JAVA_ANALYSIS.md](docs/PHASE3_JAVA_ANALYSIS.md)** - Java source analysis (2,172 lines)

### Architecture Decision Records
- **[ADR_PLANNING.md](docs/adr/ADR_PLANNING.md)** - 28 planned ADRs roadmap
- **[README.md](docs/adr/README.md)** - ADR index and workflow
- **[ADR.template.md](docs/adr/ADR.template.md)** - Standard template

---

## 🎯 Project Goals

Create a .NET library that:

1. **Maintains Java ezddd's Design Philosophy**:
   - Same tactical DDD patterns (Entity, ValueObject, AggregateRoot, EsAggregateRoot)
   - Same Clean Architecture layers (Entity, UseCase, Interface Adapters)
   - Same CQRS patterns (Command, Query, Projection, Projector)
   - Same event sourcing correctness rules (R1, R2, R3)

2. **Leverages .NET Platform Strengths**:
   - Async/await throughout
   - Nullable reference types for compile-time safety
   - Record types for immutable domain events and value objects
   - Pattern matching for event handling
   - Modern C# idioms

3. **Maintains Minimal Dependencies**:
   - Zero third-party external dependencies for core libraries
   - Only .NET built-in APIs (System.Text.Json, System.Reflection, System.Collections.Concurrent)
   - Only TeddySoft ecosystem dependencies (uContract.NET for Design by Contract)

---

## 📦 Naming Convention

**Confirmed Decision** (2025-10-28):

### Package IDs (NuGet)
- `ezDDD.Common`
- `ezDDD.Entity`
- `ezDDD.UseCase`
- `ezDDD.Cqrs`
- `ezDDD.Core`

### Namespaces (C# Code)
- `EzDdd.Common`
- `EzDdd.Entity`
- `EzDdd.UseCase`
- `EzDdd.Cqrs`
- `EzDdd.Core`

**Rationale**:
- **Package ID** preserves brand identity (`ezDDD` corresponds to Java's `ezddd`)
- **Namespace** follows .NET PascalCase convention
- Balances brand recognition with .NET ecosystem standards

---

## 📂 Module Architecture

The project consists of 5 modules with clear dependency hierarchy:

```
EzDdd.Common (utilities: BiMap, IConverter, JsonUtil)
    ↓
EzDdd.Entity (core DDD: IEntity, IValueObject, IDomainEvent, AggregateRoot, EsAggregateRoot)
    ↓
EzDdd.UseCase (use cases: IUseCase, IRepository, IRepositoryPeer, IMessageBus, mappers)
    ↓
EzDdd.Cqrs (CQRS: ICommand, IQuery, IProjection, IProjector, IArchive, IInquiry)
    ↓
EzDdd.Core (aggregator module + documentation)
```

### EzDdd.Common
Foundation utilities for the entire framework:
- **BiMap<TKey, TValue>**: Bidirectional mapping (thread-safe version of Java BiMap)
- **IConverter<TSource, TTarget>**: Generic type conversion interface
- **JsonUtil**: System.Text.Json utilities

### EzDdd.Entity
Core DDD building blocks (entities layer):
- **IEntity<TId>**: Interface for entities with unique identity
- **IValueObject**: Marker interface for immutable value objects
- **IDomainEvent**: Base interface with Id, OccurredOn, Source, Metadata
  - **InternalDomainEvent**: Events within a bounded context
    - **IConstructionEvent**: Marker for aggregate creation (must be first event)
    - **IDestructionEvent**: Marker for aggregate deletion (must be last event)
- **AggregateRoot<TId, TEvent>**: Base class for aggregates with event collection and versioning
- **EsAggregateRoot<TId, TEvent>**: Event-sourced aggregate root
  - Reconstructs state from event history via replay
  - Template method `Apply()` enforces invariant checking before/after calling abstract `When()`
  - Stream naming convention: `{category}-{id}`

> **Note**: `ExternalDomainEvent` deferred to Phase 3 (EzDdd.UseCase module) - belongs to integration layer

### EzDdd.UseCase
Use cases layer with persistence abstractions:
- **IUseCase<TInput, TOutput>**: Command pattern interface with `ExecuteAsync(input)` method
- **IInput/IOutput**: Base interfaces for use case inputs and outputs
- **IRepository<TAggregate, TId>**: Aggregate persistence interface (FindByIdAsync, SaveAsync, DeleteAsync)
- **IRepositoryPeer<TData, TId>**: SPI for actual persistence implementation (interface adapters layer)
- **EsRepository**: Generic event sourcing repository implementation
- **OutboxRepository**: Generic state sourcing repository with Transactional Outbox pattern
- **DomainEventMapper**: Converts domain events to/from DomainEventData records
- **IMessageBus/IReactor**: Intra-process event distribution
- **IEventBusProducer**: External event bus integration adapter

### EzDdd.Cqrs
CQRS pattern separation:
- **ICommand<TInput, TOutput>**: Marker extending IUseCase for write operations
- **IQuery<TInput, TOutput>**: Marker extending IUseCase for read operations
- **IInquiry<TInput, TOutput>**: Validation queries usable within commands
- **IProjection<TInput, TOutput>**: Read model builder that generates view models from query database
- **IProjector**: Background service marker for building and maintaining read models
- **IArchive<TData, TId>**: Query database interface (query-side counterpart to IRepository)
- **CqrsOutput<T>**: Unified output class with builder pattern

---

## 🔑 Key Architectural Patterns

### Clean Architecture Layers
- **Entities Layer**: IEntity, IValueObject, IDomainEvent, AggregateRoot
- **Use Cases Layer**: IUseCase, IInput, IOutput, IRepository
- **Interface Adapters Layer**: IRepositoryPeer implementations, Mappers
- **Frameworks/Drivers Layer**: ASP.NET Core, databases (implementation-specific)

### Event Sourcing
1. All aggregate state is reconstructed from events
2. `EsAggregateRoot` constructor accepts `IEnumerable<TEvent>` for replay
3. Events persisted in append-only event store
4. `DomainEventTypeMapper` maps event types to strings for serialization
5. Invariants enforced before/after each event via `EnsureInvariant()`

### State Sourcing with Transactional Outbox
1. `OutboxRepository` stores current aggregate state AND events
2. Ensures atomic persistence of both via database transactions
3. Transaction boundary MUST be at `IRepositoryPeer` implementation level (not IRepository level)
4. Faster reads than pure event sourcing at cost of dual write

### CQRS
- **Write Model**: Commands use IRepository to persist aggregates
- **Read Model**: Queries use IProjection/IArchive for optimized reads
- **Projectors**: Background services listen to events and update read models
- **Eventual Consistency**: Read models eventually consistent with write model

### Bridge Pattern for Persistence
- **IRepository** (abstraction) defines domain-level persistence contract
- **IRepositoryPeer** (implementor) provides actual database implementation
- Ensures aggregates in entities layer don't leak to adapters layer

---

## ⚡ Important Implementation Rules

### Aggregate Correctness Rules
EsAggregateRoot enforces three invariant rules (same as Java):

- **R1 (Construction)**: `{pre₀} fun₀ {post₀ & INV}`
  - Construction events establish initial invariants
  - No precondition invariant check

- **R2 (Command)**: `{preₜ & INV} funₜ {postₜ & INV}`
  - Command events must maintain invariants before and after
  - Invariant checked both before and after `When()`

- **R3 (Destruction)**: `{preᵤ & INV} funᵤ {postᵤ}`
  - Destruction events may break invariants as final operation
  - No postcondition invariant check

### Event Sourcing Implementation
When creating event-sourced aggregates:

1. First event MUST implement `InternalDomainEvent.IConstructionEvent`
2. Last event (deletion) MUST implement `InternalDomainEvent.IDestructionEvent`
3. Override `EnsureInvariant()` to check business rules
4. Override `When(TEvent @event)` to handle state changes (use pattern matching)
5. Override `GetCategory()` to return category string for stream naming
6. Provide constructor accepting `IEnumerable<TEvent>` for replay

**Pattern**:
```csharp
public class Aggregate : EsAggregateRoot<Id, InternalDomainEvent>
{
    public Aggregate(IEnumerable<InternalDomainEvent> events) : base(events) { }
    protected override void EnsureInvariant() { /* check rules */ }
    protected override void When(InternalDomainEvent @event) { /* pattern match */ }
    public override string GetCategory() => "category";
}
```

### Transaction Management
Production `IRepositoryPeer` implementations MUST:

- Use `async/await` for all I/O operations
- Use `@Transactional` equivalent (e.g., `TransactionScope` or EF Core transactions) on `SaveAsync()` method
- Ensure atomic persistence of both aggregate state and events
- Rollback completely if either operation fails
- Transaction boundary ONLY at RepositoryPeer level, NOT Repository level

### Event Type Mapping
Register all domain event types with `DomainEventTypeMapper` for serialization.

---

## 🆚 Differences from Java Version

### Expected Syntax Differences

**Naming Conventions**:
- Java uses concrete classes: `Entity`, `AggregateRoot`, `UseCase`
- C# uses interfaces: `IEntity`, `IAggregateRoot`, `IUseCase`
- C# uses PascalCase: `ExecuteAsync()`, `FindByIdAsync()`

**Generic Parameters**:
- Java: `<ID, E>`
- C#: `<TId, TEvent>`

**Async/Await**:
```java
// Java: Synchronous
O execute(I input) throws UseCaseFailureException;
Optional<T> findById(ID id);

// C#: Asynchronous
Task<TOutput> ExecuteAsync(TInput input);
Task<TAggregate?> FindByIdAsync(TId id);
```

**Collections**:
```java
// Java
List<DomainEvent> getDomainEvents();
Optional<T> findById(ID id);

// C#
IReadOnlyList<DomainEvent> GetDomainEvents();
Task<T?> FindByIdAsync(TId id);  // Nullable reference
```

### .NET Platform Improvements

1. **Pattern Matching**: Use switch expressions with type patterns (more concise than instanceof)
2. **Record Types**: Immutable events with primary constructors and init-only properties
3. **Nullable Reference Types**: Compile-time null safety with `?` annotations
4. **Async/Await**: Modern asynchronous programming throughout all I/O operations

---

## 🏗️ Build Commands

```bash
dotnet build                          # Build the solution
dotnet test                           # Run all tests
dotnet test --filter "ClassName~Foo"  # Run specific tests
dotnet pack                           # Create NuGet packages
dotnet build -c Release               # Release build
```

---

## 📦 Dependencies

### Runtime

**Zero third-party external dependencies** (same philosophy as uContract.NET)

**Built-in Dependencies (.NET BCL)**:
- `System.Text.Json` for event serialization and deep copy
- `System.Reflection` for EsAggregateRoot reflection instantiation
- `System.Collections.Concurrent` for thread-safe collections

**Ecosystem Dependencies (TeddySoft Libraries)**:
- **uContract.NET** (v1.0.0+) - Design by Contract support
  - Provides `Contract.Require()`, `Contract.Ensure()`, `Contract.Invariant()`, `Contract.Check()`
  - Maintains semantic parity with Java ezddd's use of uContract 2.0.0
  - Essential for EsAggregateRoot invariant checking (R1, R2, R3 rules)
  - Part of TeddySoft ecosystem, not considered third-party dependency

**Rationale for uContract.NET dependency**:
- **Semantic Parity**: Java ezddd depends on uContract 2.0.0 for Design by Contract
- **Correctness**: Event sourcing invariant rules require robust contract checking
- **Ecosystem Consistency**: Both libraries are part of TeddySoft DDD toolkit
- **Avoid Duplication**: Reuse existing, tested DbC implementation

### Test
- **xUnit** - Testing framework (same as uContract.NET)
- **No mocking libraries** - Keep tests simple and clear

---

## 📚 References

### Original Java Version (2.1.0)

**This .NET port is based on Java ezddd 2.1.0** (GitLab commit: `6e94aee`)

- **Java ezddd Repository**: https://gitlab.com/TeddyChen/ezddd
- **Commit**: `6e94aee` (Release 2.1.0, 2024)
- **Java ezddd CLAUDE.md**: [../ezddd/CLAUDE.md](../ezddd/CLAUDE.md)
- **Module Documentation**: See Java version modules

### Related .NET Port
- **uContract.NET Repository**: [../uContract.NET](../uContract.NET)
- **uContract.NET DOTNET_PORT.md**: Porting experience reference
- **uContract.NET CLAUDE.md**: Development guidance reference

### .NET Resources
- **Async/Await Best Practices**: https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/
- **Nullable Reference Types**: https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references
- **Records**: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record
- **System.Text.Json**: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview
- **Pattern Matching**: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/patterns

### DDD & Event Sourcing
- **Event Sourcing**: https://martinfowler.com/eaaDev/EventSourcing.html
- **CQRS**: https://martinfowler.com/bliki/CQRS.html
- **DDD Tactical Patterns**: Eric Evans - Domain-Driven Design
- **Clean Architecture**: Robert C. Martin - Clean Architecture

---

## 🚨 Important Reminders for Claude Code

### Before Any Development Task

1. **Context Awareness**: If user doesn't specify "Java" or ".NET", **ASK FIRST**
   - User might be working on either Java ezddd or .NET ezddd.NET
   - Always clarify which version before proceeding

2. **Check Project Status**:
   - ✅ **Phase 1 完成** - EzDdd.Common (69 tests)
   - ✅ **Phase 2 完成** - EzDdd.Entity (85 tests)
   - ✅ **Phase 3 完成** - EzDdd.UseCase (279 tests) - All fixes and ADRs complete
   - ✅ **Phase 4 完成** - EzDdd.Cqrs (67 tests) - All iterations and ADRs complete 🎉
   - 🎯 **下一步**: Phase 5 (EzDdd.Core aggregator module)
   - See [PHASE4_SESSION_STATE.md](docs/PHASE4_SESSION_STATE.md) for Phase 4 completion details

3. **Consult Planning Documents**:
   - Check [ADR_PLANNING.md](docs/adr/ADR_PLANNING.md) for ADR priorities and dependencies
   - Check [DOTNET_PORT.md](DOTNET_PORT.md) for technical decisions (references completed ADRs)
   - Use [ADR.template.md](docs/adr/ADR.template.md) when writing new ADRs
   - Follow ADR maintenance workflow in [docs/adr/README.md](docs/adr/README.md)

### When Implementing

1. **Follow .NET Conventions**:
   - Use PascalCase for public members
   - Use `_camelCase` for private fields
   - Use `async/await` for all I/O operations
   - Enable nullable reference types (`#nullable enable`)
   - Use `record` for immutable types (events, value objects, DTOs)

2. **Maintain Java Parity**:
   - Keep same design philosophy and patterns
   - Preserve same behavior (especially event sourcing rules R1, R2, R3)
   - Cross-reference Java implementation when uncertain

3. **Testing Requirements**:
   - Write tests BEFORE implementation (TDD)
   - Use xUnit with AAA pattern (Arrange, Act, Assert)
   - >90% unit test coverage
   - Integration tests for complete scenarios

4. **Documentation**:
   - XML documentation comments for all public APIs
   - Clear examples in documentation
   - Update DOTNET_PORT.md when making architectural changes
   - Write ADRs as decisions are confirmed (following ADR workflow below)

### ADR Workflow

**Standard Workflow for Each ADR**:
1. **Write ADR** using [ADR.template.md](docs/adr/ADR.template.md)
   - Reference [ADR_PLANNING.md](docs/adr/ADR_PLANNING.md) for key points and dependencies
   - Set Status to "Accepted" for confirmed decisions
   - Fill in all sections: Context, Decision, Consequences, Alternatives, Related, References
2. **Update [docs/adr/README.md](docs/adr/README.md)** (add to ADR Index)
3. **Update [DOTNET_PORT.md](DOTNET_PORT.md)** (add ADR link `[ADR-NNNN](docs/adr/NNNN-title.md)`)
4. **Update this CLAUDE.md** (mark decision as ✅ with ADR reference)

---

## 🎯 Current Phase: Phase 6 - Java 4.1.0 Synchronization 🚀

**Phase 6 Overview** (2026-01-06, estimated 44-62 hours / ~1-2 weeks):

### 📋 Objective

Synchronize ezDDD.NET from Java ezddd 2.1.0 (commit `6e94aee`) to Java ezddd 4.1.0 (commit `91fac63`) **before first NuGet publication**.

This ensures users receive a complete, up-to-date API aligned with Java 4.1.0 from day one, with all breaking changes incorporated into the initial 1.0.0 release.

### 🔥 Major Changes (Breaking)

1. **IDomainEvent.Metadata Property** - Add `IDictionary<string, object> Metadata { get; }` for idempotency support
2. **MessageBus → MessageProducer Refactoring** - Replace Observer pattern with new `IMessageProducer<T>` abstraction
3. **IReconciler Interface** - New `IReconciler<TContext, TReport>` for system state reconciliation
4. **Service Layer Pattern** - Extract complex business logic to explicit Service classes (optional pattern)
5. **Thread Safety Improvements** - Fix concurrency issues (DomainEventTypeMapper, BlockingMessageBus, etc.)
6. **Null Safety Enhancements** - Comprehensive null validation across all public APIs
7. **Equals/HashCode Compliance** - Fix contract violations in domain event data classes

### 📊 Work Breakdown (8 Stages)

| Stage | Description | Est. Hours | Status | ADRs |
|-------|-------------|------------|--------|------|
| **S0** | Planning & Preparation | 4-6 | ✅ Complete (2026-01-06) | - |
| **S1** | IDomainEvent.Metadata | 6-8 | ✅ Complete (2026-01-06) | ADR-0008 (existing) |
| **S2** | IReconciler Interface | 4-6 | ✅ Complete (2026-01-07) | [ADR-0024](docs/adr/0024-ireconciler-interface-system-reconciliation.md) |
| **S3** | MessageProducer Refactoring | 10-14 | ⏳ Pending | ADR-0025 |
| **S4** | Service Layer Pattern | 6-8 | ⏳ Pending | ADR-0026 |
| **S5** | Thread/Null Safety Review | 6-8 | ⏳ Pending | ADR-0027 |
| **S6** | Integration Testing & Docs | 6-8 | ⏳ Pending | ADR-0028 |
| **S7** | Final Review & Completion | 2-4 | ⏳ Pending | ADR-0029, ADR-0030 |
| **TOTAL** | | **44-62 hours** | **3/8 complete (38%)** | **7 ADRs** |

### 🎯 Success Criteria

**Phase 6 Complete When**:
- [ ] All 7 ADRs written and reviewed (ADR-0024 to ADR-0030) - **1/7 complete** ✅ ADR-0024
- [ ] All Java 4.1.0 features implemented and tested - **1/6 complete** ✅ IReconciler
- [ ] 528+ tests passing (>95% coverage, up from current 516) - **Current: 528 tests** ✅
- [ ] Zero compiler warnings - **Current: 0 warnings** ✅
- [ ] Zero static analysis errors
- [ ] All "Java 2.1.0" references updated to "Java 4.1.0" across all files
- [ ] Feature parity with Java 4.1.0 verified (~99%)
- [ ] Documentation updated (README, CHANGELOG, API docs)
- [ ] NuGet packages ready for **1.0.0** release (5 packages)
- [ ] ROADMAP.md and CLAUDE.md reflect Java 4.1.0 base

### 📚 Complete Implementation Plan

See [DOTNET_PORT.md](DOTNET_PORT.md) "Java 4.1.0 Synchronization Plan" section (lines 45-1040) for:
- Detailed stage-by-stage implementation guide
- Code examples and patterns
- Testing strategy
- Risk analysis
- Reference commits from Java ezddd

**Current Progress**: Stage S1 Complete - 2/8 stages complete (25%), 0/7 new ADRs written (ADR-0008 existing covers S1)

---

## 📅 Key Milestones

### Phase 1: EzDdd.Common (2025-10-31) ✅
- 3 components: IConverter, JsonUtil, BiMap
- 69 tests passing
- Thread-safe BiMap with lock-based synchronization

### Phase 2: EzDdd.Entity (2025-11-01) ✅
- 7 components: IEntity, IValueObject, IDomainEvent, AggregateRoot, EsAggregateRoot, DomainEventTypeMapper
- 85 tests passing
- R1/R2/R3 event sourcing correctness rules enforced
- ~95% semantic parity with Java ezddd

### Phase 3: EzDdd.UseCase (2025-11-05 to 2025-11-06) ✅

**Accomplishments**:
- ✅ 8 iterations (Foundation → Use Case → Repository → Events → ES/SS → Message Bus → Integration)
- ✅ 25 core components + 6 integration test suites
- ✅ 254 tests passing, >95% coverage
- ✅ ~98% semantic parity with Java ezddd

**Key Technical Achievements**:
1. Bridge Pattern (IRepository ↔ IRepositoryPeer) - Clean Architecture layer separation
2. Event Sourcing (EsRepository) - Reflection + ConstructorInfo caching
3. State Sourcing (OutboxRepository) - Transactional Outbox pattern
4. Message Bus (BlockingMessageBus) - Observer pattern with thread-safe snapshot
5. Generic variance (IUseCase<in TInput, out TOutput>) - Covariant/contravariant correct

**Critical Design Decisions**:
- ConstructorInfo caching with ConcurrentDictionary (performance)
- IStoreData.Version type: int → long (Java parity)
- Event clearing: only after successful save (NOT on failure)
- IReactor async update: Execute() → ExecuteAsync()
- Events captured BEFORE SaveAsync() (repository clears after save)

**Banking Test Domain**: BankAccount (event-sourced), Money (value object), 3 Use Cases

### Phase 4: EzDdd.Cqrs (2025-11-10 to 2025-11-18) ✅

**Planning Complete** ✅ (Phase P, ~8 hours):
- ✅ Java CQRS analysis, C# API design, implementation plan, ADR planning, kickoff document
- ✅ 3 Critical ADRs written: ADR-0017 (CqrsOutput), ADR-0018 (IArchive), ADR-0019 (IInquiry/IProjection)

**Implementation Complete** ✅ (All 7 iterations, ~13 hours):
- ✅ 9 core components: IInquiryInput, IProjectionInput, CqrsOutput, ICommand, IInquiry, IQuery, IProjection, IProjector, IArchive
- ✅ 67 tests passing (8 + 15 + 9 + 14 + 7 + 14), >90% coverage
- ✅ Complete CQRS flow integration tests (Command → Event → Projector → Archive → Query)
- ✅ 4 additional ADRs written: ADR-0020 (Projector), ADR-0021 (Variance), ADR-0022 (ReadModel), ADR-0023 (Idempotency)
- ✅ 0 compiler warnings (fixed all XML documentation warnings)
- ✅ 42% ahead of schedule (~13/19 hours)

**Key Technical Achievements**:
1. Fluent API CqrsOutput with builder pattern (SetXxx() methods)
2. IArchive async methods throughout (FindByIdAsync, SaveAsync, DeleteAsync)
3. IInquiry/IProjection independence (both extend IUseCase, usable independently or within commands)
4. Complete event projection pattern (AccountProjector updating AccountSummaryReadModel)
5. Marker interfaces for compile-time type safety (IInquiryInput, IProjectionInput)
6. Generic variance annotations (contravariant input, covariant output)
7. Idempotent archive operations (upsert semantics for SaveAsync)
8. Read model design with C# record types

**Critical Design Decisions**:
- IProjector as pure marker interface (lifecycle managed separately via BackgroundService)
- IArchive operations MUST be idempotent for reliable event replay
- Read models use C# record types with positional parameters
- Generic variance: `ICommand<in TInput, TOutput>`, `IArchive<TData, in TId>`
- IInquiry and IProjection are independent from each other (separate use cases)

### Phase 5: EzDdd.Core & Documentation (2025-11-22) ✅
- 4 iterations (Documentation → Release Prep → README → Verification)
- 11,212+ lines of documentation
- 5 NuGet packages created (Common 35K, Entity 41K, UseCase 63K, Cqrs 37K, Core 28K)
- Version 1.0.0-alpha.1 (internal, not published)
- Complete documentation ecosystem
- Based on Java ezddd 2.1.0

### Phase 6: Java 4.1.0 Synchronization (2026-01-06 to TBD) 📋 Planned

**Objective**: Sync from Java ezddd 2.1.0 → 4.1.0 before first NuGet publication

**Scope**:
- 44 commits to synchronize (+5,167/-2,132 lines)
- 8 implementation stages (S0-S7)
- 7 new ADRs (ADR-0024 to ADR-0030)
- Estimated: 44-62 hours (~1-2 weeks across multiple sessions)

**Major Changes**:
- ⚠️ BREAKING: IDomainEvent.Metadata property (idempotency support)
- ⚠️ BREAKING: MessageBus → MessageProducer refactoring
- ✨ NEW: IReconciler interface (system reconciliation)
- 🔄 REFACTOR: Service Layer pattern (optional)
- 🐛 FIX: Thread safety (DomainEventTypeMapper, BlockingMessageBus)
- 🐛 FIX: Null safety (comprehensive validation)
- 🐛 FIX: Equals/HashCode contract compliance

**Target Outcomes**:
- 528+ tests passing (>95% coverage) - **Current: 528 tests** ✅
- ~99% semantic parity with Java 4.1.0
- Feature-complete 1.0.0 ready for NuGet
- All breaking changes in initial release (no migration needed)

**Current Progress**:
- **Stage S2 Complete** (2026-01-07)
- **3/8 stages complete (38%)**
- **1/7 ADRs complete** ([ADR-0024](docs/adr/0024-ireconciler-interface-system-reconciliation.md))
- **528 tests passing** (516→528, +12 new reconciler tests)

---

*Last updated: 2026-01-07 (Phase 6 Stage S2 Complete - IReconciler Interface)*
