# ADR Planning Document for ezDDD.NET

**Created**: 2025-10-31
**Last Updated**: 2026-01-06
**Status**: Active (Phase 6 Planning)
**Total ADRs Planned**: 31 (23 completed, 8 planned)

---

## Purpose

This document provides a **roadmap for all Architecture Decision Records (ADRs)** for the ezDDD.NET project. It coordinates ADR writing across implementation phases and manages dependencies between architectural decisions.

**ezDDD.NET Context**:
- .NET port of Java ezddd library
- 6 implementation phases (Phase 1-5 complete, Phase 6 planned)
- Currently based on Java ezddd 2.1.0, targeting Java ezddd 4.1.0
- Pre-publication synchronization (1.0.0 will be based on Java 4.1.0)

---

## Planned ADRs by Stage

### 🎯 Stage 1: Foundation (Phase 1 - EzDdd.Common)
**Status**: ✅ Complete (6 ADRs written)
**Phase**: Phase 1 - EzDdd.Common module
**Timeline**: 2025-10-31

#### ADR-0001: Target Framework Selection (.NET 8.0+)
- **Topic**: Which .NET version to target for ezDDD.NET
- **Key Points**:
  - LTS support timeline
  - Modern C# features (nullable reference types, records, pattern matching)
  - Async/await maturity
- **Status**: ✅ Completed
- **Planning Docs Section**: DOTNET_PORT.md - "Target Framework"
- **Dependencies**: None (foundational)

#### ADR-0002: Package Naming and Structure
- **Topic**: NuGet package ID vs. C# namespace naming convention
- **Key Points**:
  - Brand consistency (ezDDD) vs. .NET convention (PascalCase)
  - Package IDs: ezDDD.Common, ezDDD.Entity, etc.
  - Namespaces: EzDdd.Common, EzDdd.Entity, etc.
- **Status**: ✅ Completed
- **Planning Docs Section**: DOTNET_PORT.md - "Naming Convention"
- **Dependencies**: None

#### ADR-0003: Module Architecture and Dependency Chain
- **Topic**: Strict module dependency hierarchy
- **Key Points**:
  - Layered architecture: Common → Entity → UseCase → Cqrs → Core
  - No circular dependencies
  - Clear separation of concerns
- **Status**: ✅ Completed
- **Planning Docs Section**: DOTNET_PORT.md - "Module Architecture"
- **Dependencies**: None (foundational)

#### ADR-0004: Zero Third-Party Dependency Principle
- **Topic**: Minimize external dependencies for core library
- **Key Points**:
  - Only .NET BCL and uContract.NET allowed
  - System.Text.Json for serialization
  - No MediatR, no AutoMapper, no Newtonsoft.Json
- **Status**: ✅ Completed
- **Planning Docs Section**: DOTNET_PORT.md - "Dependencies"
- **Dependencies**: ADR-0001

#### ADR-0005: Complete Reimplementation Approach
- **Topic**: Port strategy (line-by-line vs. reimplementation)
- **Key Points**:
  - Reimplementation approach chosen for .NET idioms
  - Maintain semantic parity (~95-99%)
  - Leverage .NET strengths (async/await, nullable types)
- **Status**: ✅ Completed
- **Planning Docs Section**: DOTNET_PORT.md - "Porting Strategy"
- **Dependencies**: ADR-0001

#### ADR-0006: uContract.NET Integration for Design by Contract
- **Topic**: Design by Contract support via uContract.NET dependency
- **Key Points**:
  - Semantic parity with Java ezddd's use of uContract 2.0.0
  - Essential for EsAggregateRoot invariant checking
  - Part of TeddySoft ecosystem (not third-party)
- **Status**: ✅ Completed
- **Planning Docs Section**: DOTNET_PORT.md - "Dependencies"
- **Dependencies**: ADR-0004
- **Related**: ADR-0010, ADR-0011 (use uContract for invariants)

---

### 🏗️ Stage 2: Core DDD Patterns (Phase 2 - EzDdd.Entity)
**Status**: ✅ Complete (5 ADRs written)
**Phase**: Phase 2 - EzDdd.Entity module
**Timeline**: 2025-11-01

#### ADR-0007: IEntity and IValueObject Design
- **Topic**: Interface design for core DDD building blocks
- **Key Points**:
  - IEntity<TId> with Id property
  - IValueObject as marker interface
  - Immutability enforcement strategies
- **Status**: ✅ Completed
- **Planning Docs Section**: DOTNET_PORT.md - "Entity Layer"
- **Dependencies**: ADR-0003

#### ADR-0008: IDomainEvent Hierarchy
- **Topic**: Domain event interface design and hierarchy
- **Key Points**:
  - IDomainEvent base: Id, OccurredOn, Source properties
  - InternalDomainEvent vs. ExternalDomainEvent
  - IConstructionEvent and IDestructionEvent markers
- **Status**: ✅ Completed
- **Planning Docs Section**: DOTNET_PORT.md - "Domain Events"
- **Dependencies**: ADR-0007

#### ADR-0009: AggregateRoot Base Class Design
- **Topic**: AggregateRoot<TId, TEvent> implementation for state sourcing
- **Key Points**:
  - Event collection management
  - Version tracking
  - Event clearing semantics
- **Status**: ✅ Completed
- **Planning Docs Section**: DOTNET_PORT.md - "Aggregate Patterns"
- **Dependencies**: ADR-0007, ADR-0008

#### ADR-0010: EsAggregateRoot Event Sourcing Implementation
- **Topic**: EsAggregateRoot<TId, TEvent> for event-sourced aggregates
- **Key Points**:
  - Event replay via reflection
  - Template method pattern (Apply calls When)
  - ConstructorInfo caching for performance
- **Status**: ✅ Completed
- **Planning Docs Section**: DOTNET_PORT.md - "Event Sourcing"
- **Dependencies**: ADR-0006, ADR-0009

#### ADR-0011: Event Replay and Invariant Checking (R1, R2, R3)
- **Topic**: Correctness rules for event sourcing
- **Key Points**:
  - R1 (Construction): No pre-invariant check
  - R2 (Command): Check invariants before and after
  - R3 (Destruction): No post-invariant check
- **Status**: ✅ Completed
- **Planning Docs Section**: DOTNET_PORT.md - "Event Sourcing Rules"
- **Dependencies**: ADR-0010

---

### 📋 Stage 3: Post-Phase 3 Review (EzDdd.UseCase)
**Status**: ✅ Complete (5 ADRs written)
**Phase**: Phase 3 - EzDdd.UseCase module
**Timeline**: 2025-11-06

#### ADR-0012: Resource Management for Event Bus Producers
- **Topic**: IDisposable pattern for IEventBusProducer and IMessageBus
- **Key Points**:
  - Event bus producers implement IDisposable
  - Proper cleanup of external resources
  - Async disposal considerations
- **Status**: ✅ Completed
- **Planning Docs Section**: Phase 3 implementation plan (internal working note) - "Messaging"
- **Dependencies**: ADR-0003

#### ADR-0013: Transaction Boundaries in Repository Pattern
- **Topic**: Where transaction boundaries belong (IRepositoryPeer, not IRepository)
- **Key Points**:
  - IRepository is domain abstraction (no transactions)
  - IRepositoryPeer has transaction responsibility
  - Ensures aggregates don't leak to adapters
- **Status**: ✅ Completed
- **Planning Docs Section**: Phase 3 implementation plan (internal working note) - "Repository"
- **Dependencies**: ADR-0003

#### ADR-0014: DomainEventData Equality Semantics
- **Topic**: Should DomainEventData equality include all fields or just Id?
- **Key Points**:
  - Record types provide structural equality (all fields)
  - Ensures event history integrity
  - No equals/hashCode contract violations
- **Status**: ✅ Completed
- **Planning Docs Section**: Phase 3 implementation plan (internal working note) - "Event Mapping"
- **Dependencies**: ADR-0008

#### ADR-0015: Cross-Platform DTO Structure
- **Topic**: Serialization format for DomainEventData and StoreData
- **Key Points**:
  - byte[] for event body (supports any serializer)
  - JSON default via System.Text.Json
  - Content-Type metadata for format detection
- **Status**: ✅ Completed
- **Planning Docs Section**: Phase 3 implementation plan (internal working note) - "Data Transfer"
- **Dependencies**: ADR-0004

#### ADR-0016: Async/Await Throughout
- **Topic**: All I/O operations must be async
- **Key Points**:
  - IRepository: FindByIdAsync, SaveAsync, DeleteAsync
  - IUseCase: ExecuteAsync
  - In-memory operations remain sync
- **Status**: ✅ Completed
- **Planning Docs Section**: DOTNET_PORT.md - "Async/Await"
- **Dependencies**: ADR-0001
- **Related**: All UseCase and Repository ADRs

---

### 🎯 Stage 4: Phase 4 Critical ADRs (EzDdd.Cqrs - Planning)
**Status**: ✅ Complete (3 ADRs written)
**Phase**: Phase 4 - EzDdd.Cqrs module (Planning)
**Timeline**: 2025-11-10

#### ADR-0017: CqrsOutput Implementation Strategy
- **Topic**: Fluent API design with builder pattern for CQRS output
- **Key Points**:
  - Mutable builder with SetXxx() methods
  - Success/failure state management
  - Type-safe output construction
- **Status**: ✅ Completed
- **Planning Docs Section**: Phase 4 CQRS kickoff plan (internal working note) - "CqrsOutput Design"
- **Dependencies**: ADR-0003

#### ADR-0018: IArchive Async Method Design
- **Topic**: All IArchive operations must be async
- **Key Points**:
  - FindByIdAsync, SaveAsync, DeleteAsync (async throughout)
  - Consistent with IRepository pattern
  - Supports async I/O to query databases
- **Status**: ✅ Completed
- **Planning Docs Section**: Phase 4 CQRS kickoff plan (internal working note) - "IArchive Design"
- **Dependencies**: ADR-0016

#### ADR-0019: IInquiry and IProjection Independence
- **Topic**: IInquiry and IProjection are independent patterns
- **Key Points**:
  - IInquiry: Validation queries (usable within commands)
  - IProjection: Read model builders (generate views)
  - Both extend IUseCase independently
- **Status**: ✅ Completed
- **Planning Docs Section**: Phase 4 CQRS kickoff plan (internal working note) - "Inquiry vs Projection"
- **Dependencies**: ADR-0003

---

### ⚙️ Stage 5: Phase 4 Post-Implementation (EzDdd.Cqrs)
**Status**: ✅ Complete (4 ADRs written)
**Phase**: Phase 4 - EzDdd.Cqrs module (Implementation)
**Timeline**: 2025-11-18

#### ADR-0020: IProjector Lifecycle Management
- **Topic**: IProjector as pure marker interface
- **Key Points**:
  - No Run() method (lifecycle managed separately)
  - BackgroundService handles execution
  - Clear separation of concerns
- **Status**: ✅ Completed
- **Planning Docs Section**: Phase 4 CQRS kickoff plan (internal working note) - "Projector Pattern"
- **Dependencies**: ADR-0019

#### ADR-0021: Generic Variance Annotations
- **Topic**: Contravariant input, covariant output in CQRS interfaces
- **Key Points**:
  - ICommand<in TInput, TOutput>
  - IArchive<TData, in TId>
  - Type safety and flexibility
- **Status**: ✅ Completed
- **Planning Docs Section**: Phase 4 CQRS kickoff plan (internal working note) - "Generic Variance"
- **Dependencies**: ADR-0017, ADR-0018, ADR-0019

#### ADR-0022: Read Model Design Patterns
- **Topic**: C# record types for read models
- **Key Points**:
  - Immutable records with positional parameters
  - Clear separation from write model
  - Optimized for query performance
- **Status**: ✅ Completed
- **Planning Docs Section**: Phase 4 CQRS kickoff plan (internal working note) - "Read Model Design"
- **Dependencies**: ADR-0001

#### ADR-0023: Archive Idempotency Requirements
- **Topic**: IArchive operations must be idempotent
- **Key Points**:
  - SaveAsync has upsert semantics
  - Reliable event replay
  - Eventual consistency guarantees
- **Status**: ✅ Completed
- **Planning Docs Section**: Phase 4 CQRS kickoff plan (internal working note) - "Archive Operations"
- **Dependencies**: ADR-0018

---

### 🚀 Stage 6: Phase 6 Java 4.1.0 Synchronization (NEW - Planned)
**Status**: ⏳ Planned (7 ADRs to write)
**Phase**: Phase 6 - Java 4.1.0 Synchronization
**Timeline**: 2026-01-06 to TBD (~40-58 hours / 1-2 weeks)


#### ADR-0024: IReconciler Interface for System State Reconciliation
- **Topic**: Add IReconciler<TContext, TReport> pattern for periodic state consistency checks
- **Key Points**:
  - NEW FEATURE: No breaking changes
  - Pattern for cleanup tasks (e.g., orphaned records)
  - NullContext for context-less reconcilers
  - Java 4.1.0 feature addition
- **Status**: ⏳ Planned (S2)
- **Priority**: 🟢 NEW FEATURE
- **Planning Docs Section**: DOTNET_PORT.md - "Java 4.1.0 Synchronization Plan, Stage 3"
- **Dependencies**: ADR-0003 (UseCase layer pattern)
- **Related**: ADR-0020 (similar to IProjector lifecycle)
- **Estimated Effort**: 4-6 hours
- **Java Commits**: da156c6, f377dcf, d4ed869

#### ADR-0025: MessageBus to MessageProducer Pattern Refactoring
- **Topic**: Replace IMessageBus<T> with simplified IMessageProducer<T> pattern
- **Key Points**:
  - BREAKING CHANGE: Removes subscription from producer
  - IMessageProducer<in T> : IDisposable with PostAsync() only
  - Subscription management moves to application layer
  - Cleaner separation of concerns
  - Java 4.1.0 architectural change
- **Status**: ✅ Completed (S3, 2026-01-07, 3 hours)
- **ADR Document**: [ADR-0025](0025-messageproducer-refactoring-java-4-1-0-alignment.md)
- **Priority**: 🔴 BREAKING (Critical)
- **Planning Docs Section**: DOTNET_PORT.md - "Java 4.1.0 Synchronization Plan, Stage 4"
- **Dependencies**: ADR-0012 (extends resource management), ADR-0024 (metadata in messages)
- **Related**: ADR-0013 (messaging patterns)
- **Estimated Effort**: 10-14 hours (most complex)
- **Java Commits**: 676e0e0, 4acead6

#### ADR-0026: Service Layer Pattern for Complex Business Logic
- **Topic**: Document and recommend Service layer pattern for extracting reusable business logic
- **Key Points**:
  - OPTIONAL PATTERN: Not required, recommended
  - Extract logic >20 lines from UseCases
  - Improves testability and reusability
  - Java 4.1.0 pattern documentation
- **Status**: ✅ Completed (S4, 2026-01-08, 6 hours)
- **ADR Document**: [ADR-0026](0026-service-layer-pattern.md)
- **Priority**: 🟡 OPTIONAL (Guidance)
- **Planning Docs Section**: DOTNET_PORT.md - "Java 4.1.0 Synchronization Plan, Stage 5"
- **Dependencies**: ADR-0003 (UseCase layer)
- **Related**: ADR-0017 (UseCase patterns)
- **Estimated Effort**: 6-8 hours
- **Actual Effort**: 6 hours (3 iterations: Pattern doc → Example impl → ADR + integration)
- **Java Commits**: Multiple refactorings

#### ADR-0027: Thread Safety and Null Safety Standards
- **Topic**: Comprehensive thread safety and null validation standards
- **Key Points**:
  - FIX: DomainEventTypeMapper initialization (Lazy<T> or lock)
  - FIX: BlockingMessageBus concurrent access (if retained)
  - ENHANCE: ArgumentNullException.ThrowIfNull() in all public APIs
  - Java 4.1.0 quality improvements
- **Status**: ⏳ Planned (S5)
- **Priority**: 🔵 QUALITY
- **Planning Docs Section**: DOTNET_PORT.md - "Java 4.1.0 Synchronization Plan, Stage 6"
- **Dependencies**: All core implementation ADRs
- **Related**: ADR-0004 (BCL usage), ADR-0014 (equals/hashCode)
- **Estimated Effort**: 6-8 hours
- **Java Commits**: a18512a, d2353cd, bfbe7ca, c345a10, 5088058, 3b862c6, 9decbba

#### ADR-0028: Version 1.0.0 Pre-Publication Synchronization Strategy
- **Topic**: Decision to incorporate all Java 4.1.0 changes into initial 1.0.0 release before NuGet publication
- **Status**: ❌ **NOT NEEDED** - Process decision, not architecture decision
- **Rationale for Not Creating ADR**:
  - **ADR Purpose**: Architecture Decision Records document "why we designed it this way" (architecture)
  - **This Topic**: Documents "when we synchronized" (development process)
  - **External User Value**: Zero - users only see 1.0.0, internal development process is irrelevant
  - **Already Documented**: CHANGELOG.md "Version Strategy" section (lines 303-324) explains the decision
  - **Conclusion**: This is internal development process documentation, not an architectural decision
- **Where Documented Instead**:
  - CHANGELOG.md section "Version Strategy: Pre-Publication Synchronization"
  - CLAUDE.md "Version Strategy - Pre-Publication Synchronization" section
  - ADRs 0024-0027 document the actual architectural decisions (what features, how designed)
- **Priority**: 🟠 PROCESS (not architecture)
- ~~**Estimated Effort**: Not applicable~~

#### ADR-0029: Phase 6 Post-Implementation Review
- **Topic**: Summary and retrospective of Java 4.1.0 synchronization effort
- **Status**: ❌ **NOT NEEDED** - Project retrospective, not architecture decision
- **Rationale for Not Creating ADR**:
  - **ADR Purpose**: Document architectural decisions (design choices)
  - **This Topic**: Project retrospective and lessons learned (development process)
  - **External User Value**: Zero - internal development reflection has no value to library users
  - **Conclusion**: This is internal project management documentation, not an architectural decision
- **Where Documented Instead**:
  - Session handoff notes (S0-S6) contained detailed session summaries (internal working notes, since removed from the repository)
  - CHANGELOG.md documents all implementation work
  - Future retrospectives can be internal documents (not ADRs)
- **Priority**: 📊 REVIEW (not architecture)
- ~~**Estimated Effort**: Not applicable~~

#### ADR-0030: Java 4.1.0 Feature Parity Verification
- **Topic**: Detailed comparison matrix of Java ezddd 4.1.0 vs. ezDDD.NET 1.0.0
- **Status**: ❌ **NOT NEEDED** - Verification report, not architecture decision
- **Rationale for Not Creating ADR**:
  - **ADR Purpose**: Document architectural decisions (design choices)
  - **This Topic**: Verification report and comparison matrix (quality assurance)
  - **External User Value**: Minimal - Java migration users need "Differences" guide, not parity report
  - **Conclusion**: This is QA documentation, not an architectural decision
- **Where Documented Instead**:
  - README.md section "Differences from Java Version" (comprehensive comparison)
  - README.md section "Semantic Parity" (parity metrics already stated: ~99%)
  - MIGRATION_GUIDE.md (1,437 lines) - Complete Java → .NET migration guide
  - CHANGELOG.md documents all Java 4.1.0 features implemented
- **Priority**: ✅ VERIFICATION (not architecture)
- ~~**Estimated Effort**: Not applicable~~

---

## Progress Tracking

| Stage | ADR Range | Description | Total | Completed | Progress |
|-------|-----------|-------------|-------|-----------|----------|
| Stage 1 | 0001-0006 | Foundation (Phase 1) | 6 | 6 | 100% ✅ |
| Stage 2 | 0007-0011 | Core DDD Patterns (Phase 2) | 5 | 5 | 100% ✅ |
| Stage 3 | 0012-0016 | Post-Phase 3 Review | 5 | 5 | 100% ✅ |
| Stage 4 | 0017-0019 | Phase 4 Critical ADRs | 3 | 3 | 100% ✅ |
| Stage 5 | 0020-0023 | Phase 4 Post-Implementation | 4 | 4 | 100% ✅ |
| Stage 6 | 0024-0030 | Java 4.1.0 Synchronization | 7 | 3 | 43% 🚀 |
| **TOTAL** | **0001-0030** | | **30** | **26** | **87%** |

### Phase 6 (Stage 6) Detailed Progress

| ADR | Title | Priority | Status | Iteration | Hours |
|-----|-------|----------|--------|-----------|-------|
| ~~N/A~~ | ~~IDomainEvent Metadata~~ | ~~N/A~~ | ✅ Covered by ADR-0008 | S1 | ~~N/A~~ |
| 0024 | IReconciler Interface | 🟢 NEW | ✅ Complete | S2 | 4-6 (5) |
| 0025 | MessageProducer Refactoring | 🔴 BREAKING | ✅ Complete | S3 | 10-14 (3) |
| 0026 | Service Layer Pattern | 🟡 OPTIONAL | ✅ Complete | S4 | 6-8 (6) |
| 0027 | Thread/Null Safety | 🔵 QUALITY | ⏳ Planned | S5 | 6-8 |
| 0028 | Pre-Publication Sync | 🟠 PROCESS | ⏳ Planned | S6 | 6-8 |
| 0029 | Post-Implementation Review | 📊 REVIEW | ⏳ Planned | S7 | 2-4 |
| 0030 | Feature Parity Verification | ✅ VERIFY | ⏳ Planned | S7 | 2-4 |

**Total Stage 6 Effort**: 40-58 hours (~6-8 working days, 1-2 calendar weeks across 3-4 sessions)

---

## Suggested Writing Order by Implementation Stage

### ✅ Stage 1: Before Phase 1 (Foundation)
**Timeline**: 2025-10-31
**Status**: Complete

Write ADRs **0001-0006** to establish technical foundation:
- ADR-0001: Target Framework
- ADR-0002: Package Naming
- ADR-0003: Module Architecture
- ADR-0004: Zero Dependencies
- ADR-0005: Reimplementation Approach
- ADR-0006: uContract Integration

---

### ✅ Stage 2: During Phase 2 (Core DDD Patterns)
**Timeline**: 2025-11-01
**Status**: Complete

Write ADRs **0007-0011** for core Entity layer:
- ADR-0007: IEntity/IValueObject
- ADR-0008: IDomainEvent Hierarchy
- ADR-0009: AggregateRoot Design
- ADR-0010: EsAggregateRoot Implementation
- ADR-0011: R1/R2/R3 Rules

---

### ✅ Stage 3: After Phase 3 (UseCase Review)
**Timeline**: 2025-11-06
**Status**: Complete

Write ADRs **0012-0016** post-Phase 3 implementation:
- ADR-0012: Resource Management
- ADR-0013: Transaction Boundaries
- ADR-0014: DomainEventData Equality
- ADR-0015: Cross-Platform DTOs
- ADR-0016: Async/Await Throughout

---

### ✅ Stage 4: Before Phase 4 (CQRS Planning)
**Timeline**: 2025-11-10
**Status**: Complete

Write ADRs **0017-0019** before Phase 4 implementation:
- ADR-0017: CqrsOutput Strategy
- ADR-0018: IArchive Async Design
- ADR-0019: IInquiry/IProjection Independence

---

### ✅ Stage 5: After Phase 4 (CQRS Post-Implementation)
**Timeline**: 2025-11-18
**Status**: Complete

Write ADRs **0020-0023** after Phase 4 implementation:
- ADR-0020: IProjector Lifecycle
- ADR-0021: Generic Variance
- ADR-0022: Read Model Design
- ADR-0023: Archive Idempotency

---

### ⏳ Stage 6: During Phase 6 (Java 4.1.0 Sync)
**Timeline**: 2026-01-06 to TBD (~1-2 weeks)
**Status**: In Planning

Write ADRs **0024-0031** during Java 4.1.0 synchronization:

**S1 (6-8 hours)**: No ADR required
**S2 (4-6 hours)**: ADR-0024 - IReconciler Interface
**S3 (10-14 hours)**: ADR-0025 - MessageProducer Refactoring
**S4 (6-8 hours)**: ADR-0026 - Service Layer Pattern
**S5 (6-8 hours)**: ADR-0027 - Thread/Null Safety
**S6 (6-8 hours)**: ADR-0028 - Pre-Publication Sync Strategy
**S7 (2-4 hours)**: ADR-0029 - Post-Implementation Review
**S7 (2-4 hours)**: ADR-0030 - Feature Parity Verification

**Total**: 44-62 hours

---

## Quality Checklist

Before marking any ADR as "Accepted", verify it meets quality standards:

**→ See [ADR_MAINTENANCE_CHECKLIST.md](ADR_MAINTENANCE_CHECKLIST.md) for the complete checklist.**

**Quick reference**:
- ✅ Context clearly explains the problem
- ✅ Decision is stated unambiguously
- ✅ At least 2 alternatives documented
- ✅ Consequences analyzed (positive/negative/neutral)
- ✅ Related ADRs cross-referenced
- ✅ All three locations updated: ADR file → DOTNET_PORT.md → CLAUDE.md

---

## Changelog

**2025-10-31**: Initial planning, 6 ADRs planned (Stage 1)
**2025-10-31**: Completed ADR-0001 through ADR-0006 (Stage 1)
**2025-11-01**: Added 5 ADRs for Stage 2 (Phase 2), completed ADR-0007 through ADR-0011
**2025-11-06**: Added 5 ADRs for Stage 3 (Phase 3 review), completed ADR-0012 through ADR-0016
**2025-11-10**: Added 3 ADRs for Stage 4 (Phase 4 planning), completed ADR-0017 through ADR-0019
**2025-11-18**: Added 4 ADRs for Stage 5 (Phase 4 post-implementation), completed ADR-0020 through ADR-0023
**2026-01-06**: Added 7 ADRs for Stage 6 (Phase 6 - Java 4.1.0 synchronization, ADR-0024 to ADR-0030), total now 30 ADRs.

---

## Maintenance

### When to Update This Document
- **New ADRs identified**: Add entries with ⏳ status
- **Priorities change**: Reorganize priority groups
- **ADRs merged or split**: Update numbering and entries
- **Writing progress**: Update status (⏳ → 🔄 → ✅)
- **Dependencies clarified**: Update "Dependencies" and "Related" fields

### Next Review
- After completing Stage 6 ADRs (update progress to 100%)
- Before Phase 7 (if any future phases planned)
- When Java ezddd releases version 4.2+ (assess need for additional ADRs)

---

**Last Updated**: 2026-01-06 (Stage 6 planning added)

_This ADR Planning document tracks all architectural decisions for the ezDDD.NET project. For ADR workflow and methodology, see [README.md](README.md)._
