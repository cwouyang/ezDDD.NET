# ADR-0003: Module Architecture and Dependency Chain

## Status

**Accepted**

- **Date**: 2025-10-31
- **Deciders**: Project maintainers
- **Status Date**: 2025-10-31

---

## Context

### Problem Statement

We need to decide how to structure ezDDD.NET into modules (NuGet packages) and define the dependency relationships between them. Key questions:
- How many modules should we have?
- What are the responsibilities of each module?
- What is the dependency chain between modules?
- How do modules map to Clean Architecture layers?
- Should we have an aggregator module for convenience?

This decision affects:
- **User installation experience**: Granular packages vs. monolithic package
- **Dependency management**: Transitive dependencies and version management
- **Separation of concerns**: Clear boundaries between different DDD concepts
- **Maintenance**: Ability to evolve modules independently
- **Semantic alignment**: Mapping to Clean Architecture and Java ezddd structure

### Relevant Context

**Java ezddd Module Structure**:
```
ezddd/
├── ezddd-common/          # Utilities
├── ezddd-entity/          # DDD entities layer
├── ezddd-usecase/         # Use cases layer
├── ezddd-cqrs/            # CQRS patterns
└── ezddd-core/            # Aggregator module
```

Java ezddd uses a 5-module structure with clear layering and a core aggregator module for convenience.

**Clean Architecture Layers** (Robert C. Martin):
1. **Entities** (Enterprise Business Rules) - Domain model, business rules
2. **Use Cases** (Application Business Rules) - Application-specific logic
3. **Interface Adapters** - Controllers, presenters, gateways
4. **Frameworks & Drivers** - UI, database, external services

**Dependency Rule**: Dependencies point inward only (outer layers depend on inner layers, never vice versa).

**Package Naming** (from ADR-0002):
- Package ID: `ezDDD.*` (brand identity)
- Namespace: `EzDdd.*` (PascalCase .NET convention)

### Constraints

- Must maintain semantic alignment with Java ezddd structure
- Must follow Clean Architecture dependency direction
- Must enable users to install only what they need
- Should provide convenience aggregator for common scenarios
- Must avoid circular dependencies between modules

---

## Decision

**We will structure ezDDD.NET as 5 modules with a strict unidirectional dependency chain:**

```
EzDdd.Common
    ↓
EzDdd.Entity
    ↓
EzDdd.UseCase
    ↓
EzDdd.Cqrs
    ↓
EzDdd.Core (aggregator)
```

### Module Definitions

#### 1. **EzDdd.Common** (Foundation)

**Package ID**: `ezDDD.Common`
**Namespace**: `EzDdd.Common`
**Dependencies**: None (only .NET BCL)

**Purpose**: Foundation utilities used across all other modules

**Contents**:
- `BiMap<TKey, TValue>` - Bidirectional map (thread-safe)
- `IConverter<TSource, TTarget>` - Generic type conversion interface
- `JsonUtil` - System.Text.Json utilities for serialization

**Clean Architecture Layer**: Infrastructure utilities (not part of core CA layers)

---

#### 2. **EzDdd.Entity** (Entities Layer)

**Package ID**: `ezDDD.Entity`
**Namespace**: `EzDdd.Entity`
**Dependencies**: `ezDDD.Common`, `uContract`

**Purpose**: Core DDD tactical patterns and domain building blocks

**Contents**:
- `IEntity<TId>` - Entity interface with unique identity
- `IValueObject` - Marker interface for immutable value objects
- `IDomainEvent` - Base domain event interface
  - `InternalDomainEvent` - Events within bounded context
    - `IConstructionEvent` - Marker for aggregate creation
    - `IDestructionEvent` - Marker for aggregate deletion
  - `ExternalDomainEvent` - Events from/to other bounded contexts
- `AggregateRoot<TId, TEvent>` - Base aggregate with event collection
- `EsAggregateRoot<TId, TEvent>` - Event-sourced aggregate with R1/R2/R3 rules
- `DomainEventTypeMapper` - BiMap-based event type to string mapping

**Clean Architecture Layer**: **Entities Layer** (Enterprise Business Rules)

---

#### 3. **EzDdd.UseCase** (Use Cases Layer)

**Package ID**: `ezDDD.UseCase`
**Namespace**: `EzDdd.UseCase`
**Dependencies**: `ezDDD.Entity` (→ `ezDDD.Common`, `uContract`)

**Purpose**: Application business rules, use case abstractions, and repository patterns

**Contents**:
- **Port.In/** (Input ports):
  - `IUseCase<TInput, TOutput>` - Use case interface with `ExecuteAsync()`
  - `IInput` - Marker interface for use case inputs
  - `IOutput` - Marker interface for use case outputs
  - `IReactor` - Event reactor interface

- **Port.Out/** (Output ports):
  - `IRepository<TAggregate, TId>` - Domain repository abstraction
  - `IRepositoryPeer<TData, TId>` - SPI for persistence implementation (Bridge pattern)

- **Port.InOut/** (Bidirectional ports):
  - `DomainEventMapper` - Maps domain events to/from `DomainEventData`
  - `DomainEventData` - Serializable event record
  - `IMessageBus` - Intra-process event distribution

- **Implementation/**:
  - `EsRepository<TAggregate, TId, TEvent>` - Generic event sourcing repository
  - `OutboxRepository<TAggregate, TId, TEvent>` - State sourcing with Transactional Outbox
  - `BlockingMessageBus` - Simple in-process message bus implementation

**Clean Architecture Layer**: **Use Cases Layer** (Application Business Rules)

---

#### 4. **EzDdd.Cqrs** (CQRS Layer)

**Package ID**: `ezDDD.Cqrs`
**Namespace**: `EzDdd.Cqrs`
**Dependencies**: `ezDDD.UseCase` (→ `ezDDD.Entity` → `ezDDD.Common`, `uContract`)

**Purpose**: Command Query Responsibility Segregation patterns

**Contents**:
- **Command/** (Write side):
  - `ICommand<TInput, TOutput>` - Marker interface extending `IUseCase` for write operations
  - `IInquiry<TInput, TOutput>` - Validation queries usable within commands

- **Query/** (Read side):
  - `IQuery<TInput, TOutput>` - Marker interface extending `IUseCase` for read operations
  - `IProjection<TInput, TOutput>` - Read model builder from query database
  - `IProjector` - Background service for maintaining read models
  - `IArchive<TData, TId>` - Query database interface (read-side repository)

- **CqrsOutput<T>** - Unified output with success/failure states and builder pattern

**Clean Architecture Layer**: **Use Cases Layer** (Application Business Rules, CQRS specialization)

---

#### 5. **EzDdd.Core** (Aggregator)

**Package ID**: `ezDDD.Core`
**Namespace**: `EzDdd.Core` (no additional types, pure aggregator)
**Dependencies**: All other ezDDD packages

**Purpose**: Convenience aggregator package for users who want all functionality

**Contents**:
- No additional code
- Depends on: `ezDDD.Common`, `ezDDD.Entity`, `ezDDD.UseCase`, `ezDDD.Cqrs`
- Provides documentation and consolidated README

**Clean Architecture Layer**: Aggregator (not part of CA layers)

**User Experience**:
```bash
# Install everything
dotnet add package ezDDD.Core

# Or install granularly
dotnet add package ezDDD.Entity  # Just entities
dotnet add package ezDDD.UseCase # Entities + use cases
dotnet add package ezDDD.Cqrs    # Entities + use cases + CQRS
```

---

## Dependency Chain Rationale

### Why This Order?

```
Common → Entity → UseCase → Cqrs → Core
```

**Common First**:
- Utilities (BiMap, IConverter, JsonUtil) have no domain knowledge
- Can be used independently by other modules
- Zero dependencies (only .NET BCL)

**Entity Depends on Common**:
- `DomainEventTypeMapper` uses `BiMap<Type, string>`
- Event sourcing utilities may use `JsonUtil` for serialization
- Entities are foundation of domain model

**UseCase Depends on Entity**:
- `IUseCase` operates on entities and aggregates
- `IRepository<TAggregate, TId>` stores aggregates (defined in Entity layer)
- Use cases execute business logic on domain model

**Cqrs Depends on UseCase**:
- `ICommand` and `IQuery` are specializations of `IUseCase`
- CQRS is architectural pattern layered on top of use cases
- Read models are projections derived from domain events

**Core Depends on All**:
- Aggregator for convenience
- Users get all functionality with one package

### Clean Architecture Mapping

| ezDDD.NET Module | Clean Architecture Layer | Direction |
|------------------|--------------------------|-----------|
| `EzDdd.Common` | Infrastructure (utilities) | → |
| `EzDdd.Entity` | **Entities** (Enterprise Rules) | → |
| `EzDdd.UseCase` | **Use Cases** (Application Rules) | → |
| `EzDdd.Cqrs` | **Use Cases** (CQRS variant) | → |
| Interface Adapters | User implements `IRepositoryPeer` | ← |
| Frameworks/Drivers | ASP.NET, EF Core, etc. | ← |

Dependencies flow **inward** (Cqrs → UseCase → Entity → Common), following Clean Architecture dependency rule.

---

## Consequences

### Positive Consequences

- ✅ **Clear Separation of Concerns**: Each module has well-defined responsibilities
- ✅ **Semantic Parity**: Matches Java ezddd 5-module structure
- ✅ **Clean Architecture Compliance**: Dependencies flow inward only
- ✅ **Granular Installation**: Users can install only what they need
- ✅ **Transitive Dependencies**: Installing `ezDDD.Cqrs` automatically pulls in dependencies
- ✅ **Convenience Aggregator**: `ezDDD.Core` for users who want everything
- ✅ **Independent Evolution**: Modules can evolve independently (within SemVer constraints)
- ✅ **Clear Namespace Organization**: Module names map to namespaces (`EzDdd.Entity`, etc.)

### Negative Consequences

- ❌ **Multiple Packages**: Users must understand module structure
- ❌ **Transitive Dependency Chain**: Installing `ezDDD.Cqrs` pulls 4 dependencies (Common, Entity, UseCase, Cqrs, uContract)
- ❌ **Version Coordination**: All modules must be versioned together for compatibility

### Neutral Consequences

- ⚖️ **5 Packages to Maintain**: More packages than single monolithic library, but manageable
- ⚖️ **Documentation Requirement**: Must clearly explain module structure in README

---

## Alternatives Considered

### Alternative 1: Monolithic Single Package

**Description**: Single package `ezDDD` containing all functionality

**Pros**:
- Simplest installation (`dotnet add package ezDDD`)
- No dependency management
- Single version number

**Cons**:
- **All-or-nothing**: Users must install everything even if they only need entities
- **Larger package size**: Full package even for minimal usage
- **Less clear structure**: No physical separation enforcing architectural boundaries
- **Semantic mismatch**: Java ezddd uses 5-module structure

**Why rejected**: Violates modularity principle and doesn't match Java ezddd structure. Users needing only entity patterns shouldn't pull in CQRS, use case, and repository code.

---

### Alternative 2: Flat Structure (No Dependency Chain)

**Description**: 4 independent modules with no dependencies between them

```
EzDdd.Common (independent)
EzDdd.Entity (independent)
EzDdd.UseCase (independent)
EzDdd.Cqrs (independent)
```

**Pros**:
- Maximum flexibility
- Users can pick and choose freely

**Cons**:
- **Violates layering**: Use cases require entities, but wouldn't depend on them
- **Code duplication**: Each module would need to define shared concepts
- **Semantic mismatch**: Java ezddd has clear layering
- **Breaks Clean Architecture**: No enforcement of dependency direction

**Why rejected**: Violates fundamental Clean Architecture dependency rule. Use cases inherently depend on entities; CQRS depends on use cases. A flat structure would require code duplication or break layering principles.

---

### Alternative 3: Granular Modules (10+ Packages)

**Description**: Split further into many small packages

```
EzDdd.Common
EzDdd.Entity.Core
EzDdd.Entity.Events
EzDdd.Entity.Aggregates
EzDdd.UseCase.Core
EzDdd.UseCase.Repository
EzDdd.Cqrs.Command
EzDdd.Cqrs.Query
EzDdd.Cqrs.Projection
EzDdd.Core
```

**Pros**:
- Maximum granularity
- Users install only precise features

**Cons**:
- **Over-engineering**: Too many packages for library size
- **Maintenance burden**: 10+ packages to version and coordinate
- **Complex dependency graph**: Users must understand many relationships
- **Semantic mismatch**: Java ezddd uses 5 modules, not 10+
- **Discovery problem**: Which packages do I need?

**Why rejected**: Excessive granularity creates more problems than it solves. ezDDD.NET is a tactical DDD library, not a sprawling framework. 5 modules provide good balance between modularity and simplicity.

---

### Alternative 4: 3-Module Structure (Coarser)

**Description**: Combine modules into fewer packages

```
EzDdd.Common
EzDdd.Domain (Entity + UseCase)
EzDdd.Cqrs
```

**Pros**:
- Fewer packages to manage
- Simpler dependency graph

**Cons**:
- **Semantic mismatch**: Java ezddd separates Entity and UseCase
- **Mixed responsibilities**: Domain combines two Clean Architecture layers
- **Less granular control**: Can't install just entities without use cases
- **Violates Single Responsibility**: Entity and UseCase are different concerns

**Why rejected**: Combining Entity and UseCase modules violates Clean Architecture layer separation and doesn't match Java ezddd structure. Users should be able to use entity patterns (IEntity, IValueObject, AggregateRoot) without pulling in use case and repository abstractions.

---

## Related Decisions

- **Depends on**: ADR-0002 (Package Naming) - Module names use ezDDD/EzDdd convention
- **Enables**: Clear module boundaries for implementation
- **Influences**: All subsequent implementation ADRs (which module contains what)

---

## Implementation Notes

### Project Structure

```
ezDDD.NET/
├── src/
│   ├── ezDDD.Common/
│   │   └── ezDDD.Common.csproj
│   ├── ezDDD.Entity/
│   │   └── ezDDD.Entity.csproj (depends on Common)
│   ├── ezDDD.UseCase/
│   │   └── ezDDD.UseCase.csproj (depends on Entity)
│   ├── ezDDD.Cqrs/
│   │   └── ezDDD.Cqrs.csproj (depends on UseCase)
│   └── ezDDD.Core/
│       └── ezDDD.Core.csproj (depends on all)
│
└── tests/
    ├── EzDdd.Common.Tests/
    ├── EzDdd.Entity.Tests/
    ├── EzDdd.UseCase.Tests/
    └── EzDdd.Cqrs.Tests/
```

### Project References

**ezDDD.Entity.csproj**:
```xml
<ItemGroup>
  <ProjectReference Include="..\ezDDD.Common\ezDDD.Common.csproj" />
  <PackageReference Include="uContract" Version="1.0.0" />
</ItemGroup>
```

**ezDDD.UseCase.csproj**:
```xml
<ItemGroup>
  <ProjectReference Include="..\ezDDD.Entity\ezDDD.Entity.csproj" />
  <!-- Transitively gets Common and uContract -->
</ItemGroup>
```

**ezDDD.Cqrs.csproj**:
```xml
<ItemGroup>
  <ProjectReference Include="..\ezDDD.UseCase\ezDDD.UseCase.csproj" />
  <!-- Transitively gets Entity, Common, uContract -->
</ItemGroup>
```

**ezDDD.Core.csproj** (Aggregator):
```xml
<ItemGroup>
  <ProjectReference Include="..\ezDDD.Common\ezDDD.Common.csproj" />
  <ProjectReference Include="..\ezDDD.Entity\ezDDD.Entity.csproj" />
  <ProjectReference Include="..\ezDDD.UseCase\ezDDD.UseCase.csproj" />
  <ProjectReference Include="..\ezDDD.Cqrs\ezDDD.Cqrs.csproj" />
</ItemGroup>
```

### Versioning Strategy

All modules should be versioned together:
- **v1.0.0**: Initial release of all 5 modules
- **v1.1.0**: Minor version bump applies to all modules
- **v2.0.0**: Major version bump applies to all modules

This ensures compatibility across all ezDDD.NET packages.

### Documentation Requirements

**README.md** should explain module structure:

````markdown
## Package Structure

ezDDD.NET consists of 5 modules:

| Package | Purpose | Install When |
|---------|---------|--------------|
| `ezDDD.Common` | Foundation utilities | You need BiMap, IConverter utilities |
| `ezDDD.Entity` | DDD entities, aggregates, events | You're implementing domain model |
| `ezDDD.UseCase` | Use cases, repositories | You need application business rules |
| `ezDDD.Cqrs` | CQRS patterns | You're implementing CQRS architecture |
| `ezDDD.Core` | All of the above | You want complete ezDDD functionality |

### Installation

**Full installation**:
```bash
dotnet add package ezDDD.Core
```

**Minimal installation** (just entities):
```bash
dotnet add package ezDDD.Entity
```

**CQRS application**:
```bash
dotnet add package ezDDD.Cqrs  # Includes UseCase, Entity, Common
```
````

---

## Long-Term Considerations

### Future Module Additions

If new modules are needed (e.g., `ezDDD.EventStore`, `ezDDD.Projections`):
- Should extend dependency chain (depend on appropriate layer)
- Should not create circular dependencies
- Consider if they belong in core or as optional extensions

### Breaking Changes

Breaking changes in lower modules affect all higher modules:
- Breaking change in `Common` → Affects all modules
- Breaking change in `Entity` → Affects UseCase, Cqrs, Core
- Breaking change in `Cqrs` → Only affects Core

Minimize breaking changes in foundational modules (Common, Entity).

---

## References

- [Clean Architecture (Robert C. Martin)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Java ezddd Module Structure](https://gitlab.com/TeddyChen/ezddd)
- [DOTNET_PORT.md - 模組架構](../../DOTNET_PORT.md#模組架構)
- [ADR-0002: Package Naming and Structure](0002-package-naming-and-structure.md)
- [.NET Library Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/)

---

## Revision History

| Date       | Status   | Notes                                  |
|------------|----------|----------------------------------------|
| 2025-10-31 | Accepted | Decision finalized and documented      |

---
