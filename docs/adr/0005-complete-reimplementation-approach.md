# ADR-0005: Complete Reimplementation Approach

## Status

**Accepted**

- **Date**: 2025-10-31
- **Deciders**: Project maintainers
- **Status Date**: 2025-10-31

---

## Context

### Problem Statement

When porting Java ezddd to .NET, we must decide between two fundamental approaches:

1. **Complete Reimplementation**: Write all DDD tactical patterns from scratch in C#/.NET
2. **Wrapping Existing Libraries**: Build on top of existing .NET DDD/CQRS libraries (MediatR, EventFlow, Marten, etc.)

This decision affects:
- API design flexibility and alignment with Java ezddd
- Maintenance burden and long-term control
- Dependency footprint and library weight
- Ability to leverage .NET platform features idiomatically
- Feature completeness (event sourcing + state sourcing + CQRS + Clean Architecture)

### Relevant Context

**.NET Ecosystem Existing Solutions**:

1. **MediatR** (CQRS via Mediator Pattern)
   - Focuses on message dispatch (Request/Response)
   - Does NOT provide: DDD tactical patterns (Entity, ValueObject, AggregateRoot)
   - Does NOT provide: Event sourcing, Repository/RepositoryPeer separation
   - Good for: Request/response CQRS commands and queries

2. **EventFlow** (Event Sourcing Framework)
   - Provides: AggregateRoot, event store abstraction, snapshots, read model projections
   - Does NOT provide: State sourcing + Transactional Outbox pattern
   - Does NOT provide: Clean Architecture layers (UseCase, Input/Output)
   - Heavyweight with infrastructure concerns (job scheduling, sagas)

3. **Marten** (Event Store using PostgreSQL)
   - Provides: Complete event sourcing and document database
   - Database-specific (PostgreSQL only)
   - Does NOT provide: DDD tactical pattern abstractions
   - Heavy dependency (requires PostgreSQL)

**Java ezddd Philosophy**:
- Lightweight tactical DDD library
- Simultaneous support for event sourcing AND state sourcing
- Clean Architecture layering (Entities, Use Cases, Interface Adapters)
- CQRS separation (Command, Query, Projection, Projector)
- Bridge pattern for persistence (IRepository / IRepositoryPeer)
- Zero external dependencies (only Java standard library + uContract)

### Constraints

- Must maintain 100% behavioral alignment with Java ezddd
- Must support BOTH event sourcing and state sourcing (with Transactional Outbox)
- Must follow Clean Architecture layers
- Must be lightweight (focused on tactical DDD patterns only)
- Should leverage modern .NET platform features (async/await, records, nullable, pattern matching)

---

## Decision

**We will completely reimplement ezDDD.NET from scratch, without wrapping or depending on existing .NET DDD/CQRS libraries.**

### Details

- **No third-party DDD libraries**: Not using MediatR, EventFlow, Marten, or similar frameworks
- **Full control over API**: Design APIs to exactly match Java ezddd philosophy and semantics
- **Leverage .NET idioms**: Use modern C# features (records, pattern matching, nullable reference types, async/await)
- **Focus on tactical DDD**: Implement only core patterns (Entity, AggregateRoot, Repository, UseCase, CQRS)
- **Minimal dependencies**: Only .NET BCL + TeddySoft ecosystem (uContract.NET)

**What we will implement**:
```
EzDdd.Common   → BiMap, IConverter, JsonUtil (utilities)
EzDdd.Entity   → IEntity, IValueObject, IDomainEvent, AggregateRoot, EsAggregateRoot
EzDdd.UseCase  → IUseCase, IRepository, IRepositoryPeer, IMessageBus, EsRepository, OutboxRepository
EzDdd.Cqrs     → ICommand, IQuery, IProjection, IProjector, IArchive, CqrsOutput
EzDdd.Core     → Documentation and aggregator module
```

---

## Consequences

### Positive Consequences

- ✅ **100% API Control**: Can precisely match Java ezddd API design and semantics
- ✅ **Behavioral Alignment**: No impedance mismatch with existing libraries' design choices
- ✅ **Platform Idiomatic**: Free to use .NET features optimally (records for events, pattern matching for `When()`, nullable reference types)
- ✅ **Lightweight**: Zero unnecessary features or abstractions
- ✅ **Clean Dependencies**: Only .NET BCL + uContract.NET (no third-party library sprawl)
- ✅ **Full Feature Set**: Support BOTH event sourcing AND state sourcing (not available in any single existing library)
- ✅ **Clean Architecture**: Implement complete UseCase/Input/Output layer (missing in existing libraries)
- ✅ **Maintenance Control**: No dependency on external libraries' breaking changes or design decisions
- ✅ **Learning Resource**: Clear, focused implementation serves as DDD reference

### Negative Consequences

- ❌ **Initial Development Time**: More upfront work than wrapping existing libraries
- ❌ **Bug Discovery**: Must discover and fix bugs ourselves (vs. relying on battle-tested libraries)
- ❌ **Community Adoption**: Smaller ecosystem compared to popular libraries like MediatR
- ❌ **Maintenance Burden**: Responsible for all bug fixes and enhancements

### Neutral Consequences

- ⚖️ **Ecosystem Compatibility**: Can be used alongside MediatR/EventFlow/Marten if users choose (not mutually exclusive)
- ⚖️ **Code Volume**: More code to maintain, but all code is laser-focused on DDD patterns (no feature bloat)
- ⚖️ **Learning Curve**: Users must learn ezDDD.NET API, but API is designed to be intuitive and well-documented

---

## Alternatives Considered

### Alternative 1: Build on MediatR (CQRS Only)

**Description**: Use MediatR for command/query dispatch, implement DDD patterns separately

```csharp
// Hypothetical MediatR-based approach
public class CreateWorkflowCommand : IRequest<CreateWorkflowOutput>
{
    public string Name { get; init; }
}

public class CreateWorkflowHandler : IRequestHandler<CreateWorkflowCommand, CreateWorkflowOutput>
{
    public async Task<CreateWorkflowOutput> Handle(CreateWorkflowCommand request, CancellationToken ct)
    {
        // Implementation
    }
}
```

**Pros**:
- Popular, well-known library in .NET ecosystem
- Good ASP.NET Core integration
- Pipeline behaviors for cross-cutting concerns

**Cons**:
- **API Mismatch**: MediatR's `IRequest<TResponse>` pattern differs from ezddd's `IUseCase<TInput, TOutput>`
- **Missing DDD Patterns**: No Entity, ValueObject, AggregateRoot, DomainEvent abstractions
- **No Event Sourcing**: Must implement event sourcing completely separately
- **No Repository Bridge**: Missing IRepository/IRepositoryPeer separation
- **Extra Dependency**: Adds external dependency for limited benefit
- **Mediator Pattern Leak**: Forces Mediator pattern into all use cases (not required by DDD)

**Why rejected**: MediatR solves a different problem (in-process messaging) than what ezDDD provides (tactical DDD patterns). Using MediatR would force the "mediator pattern" abstraction into a DDD library where it's not conceptually necessary. The API mismatch would create confusion for users migrating from Java ezddd.

---

### Alternative 2: Build on EventFlow (Event Sourcing Framework)

**Description**: Use EventFlow for event sourcing, add additional layers for Clean Architecture

**Pros**:
- Mature event sourcing implementation
- Built-in event store abstraction
- Snapshot support
- Read model projections

**Cons**:
- **No State Sourcing**: EventFlow only supports event sourcing, not state sourcing with Transactional Outbox
- **Heavy Infrastructure**: Includes job scheduling, sagas, and other infrastructure concerns beyond tactical DDD
- **API Mismatch**: EventFlow's aggregate API differs from ezddd's `EsAggregateRoot` design
- **Missing Clean Architecture**: No UseCase/Input/Output layer
- **Heavyweight Dependency**: Pulls in many concepts not needed for tactical DDD
- **Limited Control**: Constrained by EventFlow's design decisions (e.g., aggregate command handler pattern)

**Why rejected**: EventFlow is designed as a full event sourcing framework with infrastructure concerns (job scheduling, sagas), making it too heavyweight for a tactical DDD library. It doesn't support state sourcing, which is a core ezDDD feature. The aggregate API doesn't match ezddd's template method pattern (`Apply()` / `When()` / `EnsureInvariant()`).

---

### Alternative 3: Build on Marten (Event Store)

**Description**: Use Marten as event store backend, build DDD patterns on top

**Pros**:
- Production-ready event sourcing
- Built-in projection engine
- Document database capabilities
- Good performance

**Cons**:
- **Database Lock-in**: Requires PostgreSQL (not database-agnostic)
- **Heavy Dependency**: Large library with many features beyond tactical DDD
- **Missing DDD Abstractions**: No Entity, ValueObject, AggregateRoot abstractions
- **No Clean Architecture**: Missing UseCase layer
- **Storage Dependency**: ezDDD should be storage-agnostic; Marten ties us to PostgreSQL
- **Overkill**: ezDDD focuses on tactical patterns, not providing an event store

**Why rejected**: Marten is an event store implementation, not a tactical DDD pattern library. ezDDD should be database-agnostic—users should be able to implement `IRepositoryPeer` for any database (SQL Server, PostgreSQL, MongoDB, CosmosDB, etc.). Depending on Marten would force PostgreSQL on all users and make the library heavyweight.

---

### Alternative 4: Combination Approach (Multiple Libraries)

**Description**: Combine MediatR (CQRS) + EventFlow (event sourcing) + custom implementations for other patterns

**Pros**:
- Leverage existing solutions where available
- Reduce some implementation work

**Cons**:
- **Multiple Dependencies**: Pulls in multiple third-party libraries
- **API Fragmentation**: Different patterns use different library APIs (inconsistent user experience)
- **Impedance Mismatch**: Glue code needed to make libraries work together
- **Heavyweight**: Combined footprint is large
- **Version Conflicts**: Risk of dependency version conflicts
- **Limited Control**: Design constrained by multiple libraries' opinions
- **Still Missing Features**: State sourcing, Clean Architecture layers still need custom implementation

**Why rejected**: Combining multiple libraries creates a Frankenstein architecture with inconsistent APIs, heavy dependency footprint, and maintenance complexity. We'd still need to implement significant portions ourselves (state sourcing, Clean Architecture), so the benefit doesn't justify the cost.

---

### Alternative 5: Minimal Wrapper (Facade Pattern)

**Description**: Create thin facades over existing libraries to provide ezddd-like API

**Pros**:
- Less initial implementation work
- Leverage battle-tested libraries

**Cons**:
- **Leaky Abstraction**: Underlying library assumptions leak through the facade
- **Limited Control**: Can't fix underlying library issues
- **API Compromises**: Forced to compromise API design to fit underlying library
- **Incomplete Feature Set**: Still need custom implementations for missing features
- **Future Breaking Changes**: Vulnerable to breaking changes in underlying libraries
- **Confusing Errors**: Error messages reference underlying library concepts, not ezDDD concepts

**Why rejected**: Facade pattern works when wrapping 80% of functionality, but here we'd be wrapping <40% (DDD tactical patterns are unique enough that existing libraries don't cover them well). The leaky abstraction would create a confusing user experience and limit our ability to provide a clean, focused API.

---

## Related Decisions

- **Related to**: ADR-0004 (Zero Third-Party Dependency Principle) - Reimplementation enables zero-dependency goal
- **Related to**: ADR-0001 (Target Framework) - Reimplementation allows full use of .NET 8 features
- **Influences**: All subsequent implementation ADRs (design is not constrained by existing libraries)

---

## Implementation Notes

### Development Approach

1. **Phase 1**: Implement utilities (BiMap, IConverter, JsonUtil)
2. **Phase 2**: Implement core DDD patterns (Entity, ValueObject, DomainEvent, AggregateRoot)
3. **Phase 3**: Implement event sourcing (EsAggregateRoot with R1/R2/R3 rules)
4. **Phase 4**: Implement repository abstraction (IRepository / IRepositoryPeer bridge)
5. **Phase 5**: Implement Use Case layer (IUseCase, Input/Output)
6. **Phase 6**: Implement CQRS layer (ICommand, IQuery, IProjection, IProjector)

### Reference Implementation

- Java ezddd is the source of truth for behavior
- Port tests from Java to ensure behavioral equivalence
- Cross-reference Java implementation for semantic correctness

### Quality Standards

- >90% unit test coverage
- Integration tests for complete DDD scenarios
- Clear XML documentation for all public APIs
- Usage examples in documentation

---

## References

- [Java ezddd Repository](https://gitlab.com/TeddyChen/ezddd) - Source of truth for behavior
- [DOTNET_PORT.md - 完全重新實作](../../DOTNET_PORT.md#1-完全重新實作)
- [DOTNET_PORT.md - .NET 生態系統現況分析](../../DOTNET_PORT.md#net-生態系統現況分析)
- [MediatR GitHub](https://github.com/jbogard/MediatR)
- [EventFlow GitHub](https://github.com/eventflow/EventFlow)
- [Marten GitHub](https://github.com/JasperFx/marten)

---

## Revision History

| Date       | Status   | Notes                                  |
|------------|----------|----------------------------------------|
| 2025-10-31 | Accepted | Decision finalized and documented      |

---
