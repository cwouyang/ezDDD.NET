# ADR-0004: Zero Third-Party Dependency Principle

## Status

**Accepted**

- **Date**: 2025-10-31
- **Deciders**: Project maintainers
- **Status Date**: 2025-10-31

---

## Context

### Problem Statement

We need to establish a dependency management policy for ezDDD.NET core libraries. Key questions:
- Should we allow external third-party NuGet dependencies?
- What about .NET built-in APIs (BCL)?
- How do we handle dependencies within the TeddySoft ecosystem (e.g., uContract.NET)?
- How do we ensure long-term maintainability and minimize version conflicts?

This decision affects:
- **Package size**: More dependencies = larger package
- **Version conflicts**: Dependencies can conflict with user's dependencies
- **Security**: Each dependency is a potential vulnerability
- **Maintenance burden**: Dependencies require updates and compatibility management
- **Installation simplicity**: Fewer dependencies = easier installation
- **Design freedom**: Dependencies may constrain API design choices

### Relevant Context

**Java ezddd Dependencies**:
- **Zero external dependencies**: Uses only Java Standard Library
- **Single ecosystem dependency**: uContract 2.0.0 (TeddySoft library for Design by Contract)
- Philosophy: Lightweight, focused, minimal dependency footprint

**.NET Built-in APIs (available in .NET 8+)**:
- `System.Text.Json` - JSON serialization for event serialization and deep copy
- `System.Reflection` - Reflection for `EsAggregateRoot` instantiation from events
- `System.Collections.Concurrent` - Thread-safe collections (BiMap, DomainEventTypeMapper)
- `System.Linq` - LINQ for collection operations
- `System.Threading.Tasks` - Async/await support

**TeddySoft Ecosystem**:
- **uContract.NET** (v1.0.0+) - Design by Contract library
  - Provides: `Contract.Require()`, `Contract.Ensure()`, `Contract.Invariant()`, `Contract.Check()`
  - Used by: EsAggregateRoot for invariant checking (R1, R2, R3 rules)
  - Semantic parity: Java ezddd uses uContract 2.0.0
  - Same maintainers: Both are TeddySoft libraries

**Popular .NET Libraries We Could Use** (but won't):
- Newtonsoft.Json - JSON serialization (alternative to System.Text.Json)
- AutoMapper - Object-to-object mapping
- FluentValidation - Validation library
- MediatR - Mediator pattern for CQRS

### Constraints

- Must provide all core DDD tactical pattern functionality
- Must support event sourcing with invariant checking (requires DbC)
- Must maintain semantic parity with Java ezddd (which uses uContract)
- Should minimize potential for version conflicts with user projects
- Must maintain long-term without dependency update churn

---

## Decision

**We will maintain a strict zero third-party dependency policy: ezDDD.NET core libraries will have NO external third-party NuGet package dependencies. All functionality will use only .NET built-in APIs and TeddySoft ecosystem libraries (uContract.NET).**

### Details

**Allowed Dependencies**:

1. ✅ **.NET BCL (Base Class Library)** — Built-in APIs included in .NET 8 runtime:
   - `System.Text.Json` - Event serialization, DomainEventMapper deep copy
   - `System.Reflection` - EsAggregateRoot instantiation via reflection
   - `System.Collections.Concurrent` - Thread-safe BiMap, DomainEventTypeMapper
   - `System.Linq` - Collection operations
   - `System.Threading.Tasks` - Async/await patterns
   - All `System.*` namespaces

2. ✅ **TeddySoft Ecosystem Libraries** — Libraries maintained by the same organization:
   - `uContract.NET` (v1.0.0+) - Design by Contract support
     - **Rationale**: Essential for EsAggregateRoot invariant checking (R1, R2, R3 rules)
     - **Ecosystem member**: Same maintainers, shared design philosophy
     - **Semantic parity**: Java ezddd depends on uContract 2.0.0
     - **Not third-party**: Part of coordinated TeddySoft DDD toolkit

**Not Allowed Dependencies**:

❌ **Third-Party External Libraries** (libraries maintained by other organizations):
- Newtonsoft.Json (JSON serialization) - System.Text.Json is sufficient
- AutoMapper (object mapping) - Hand-written mappers are more explicit
- FluentValidation (validation) - Use manual validation or DbC
- MediatR (CQRS mediator) - Not needed, conflicts with ezDDD design
- EventFlow (event sourcing) - Conflicts with complete reimplementation approach
- Marten (event store) - Database-specific, too heavyweight
- Any other external NuGet packages

**Test Dependencies** (allowed, not distributed):
- ✅ xUnit - Testing framework (dev-only)
- ✅ BenchmarkDotNet - Performance testing (dev-only)
- ✅ Coverlet - Code coverage (dev-only)

**Package Metadata Example**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <PackageId>ezDDD.Entity</PackageId>
  </PropertyGroup>

  <ItemGroup>
    <!-- Ecosystem dependency: OK -->
    <PackageReference Include="uContract" Version="1.0.0" />

    <!-- NO third-party external dependencies -->
  </ItemGroup>
</Project>
```

---

## Consequences

### Positive Consequences

- ✅ **Minimal version conflicts**: Only one ecosystem dependency (uContract.NET) that users are unlikely to have
- ✅ **Small package size**: Only ezDDD code + uContract (~100-200KB per package estimated)
- ✅ **Simple installation**: `dotnet add package ezDDD.Core` — minimal transitive dependencies
- ✅ **Long-term stability**: No dependency update churn from third-party libraries
- ✅ **Security**: Small attack surface, only trusted TeddySoft libraries
- ✅ **Design freedom**: Not constrained by third-party library opinions
- ✅ **Semantic parity**: Matches Java ezddd dependency philosophy (only uContract)
- ✅ **Ecosystem consistency**: All TeddySoft libraries follow similar principles
- ✅ **Corporate-friendly**: Smaller dependency chain to audit and approve

### Negative Consequences

- ❌ **Limited to .NET + TeddySoft features**: Cannot use advanced third-party libraries
- ❌ **Must implement everything**: No shortcuts via popular libraries (AutoMapper, etc.)
- ❌ **Initial development effort**: More code to write vs. using existing libraries
- ❌ **Potential optimizations missed**: Third-party libs might be more optimized
- ❌ **One ecosystem dependency**: Not truly "zero dependency" (depends on uContract.NET)

### Neutral Consequences

- ⚖️ **Ecosystem dependency acceptable**: uContract.NET is well-maintained, stable, and semantically required
- ⚖️ **More code to maintain**: Implement features vs. using libraries, but code is focused and controllable
- ⚖️ **Different from pure zero-dependency**: Unlike uContract.NET (which has zero dependencies), ezDDD.NET has one ecosystem dependency

---

## Alternatives Considered

### Alternative 1: Truly Zero Dependencies (No uContract.NET)

**Description**: Implement Design by Contract inline without depending on uContract.NET

```csharp
// Inline contract checks instead of uContract.NET
protected override void EnsureInvariant()
{
    if (_state == null)
        throw new InvariantViolationException("Workflow state cannot be null");

    if (IsDeleted)
        throw new InvariantViolationException("Workflow is deleted");
}
```

**Pros**:
- Truly zero dependencies (matching uContract.NET's approach)
- One less package to install

**Cons**:
- **Semantic parity loss**: Java ezddd uses uContract 2.0.0 for DbC
- **Code duplication**: Reimplementing contract checking across ezDDD.NET and uContract.NET
- **Inconsistent ecosystem**: uContract.NET exists for DbC; not using it is inconsistent
- **Less expressive**: Inline checks less declarative than `Contract.Invariant()`
- **Missing features**: No contract violation metadata, custom messages, or ESC/Java-style checking

**Why rejected**: Java ezddd depends on uContract 2.0.0 for Design by Contract. To maintain semantic parity, ezDDD.NET should depend on uContract.NET. Reimplementing DbC would duplicate effort and create ecosystem inconsistency.

---

### Alternative 2: Allow Newtonsoft.Json (Better JSON Serialization)

**Description**: Depend on Newtonsoft.Json for event serialization instead of System.Text.Json

**Pros**:
- More mature JSON library (longer track record)
- Better handling of edge cases (circular references, polymorphism)
- Richer features (custom converters, contract resolvers)

**Cons**:
- **Adds third-party dependency**: Goes against minimalism principle
- **Version conflicts**: Newtonsoft.Json is extremely common (high conflict risk)
- **Not necessary**: System.Text.Json handles domain event serialization well
- **Performance**: System.Text.Json is faster for typical DDD scenarios

**Why rejected**: `System.Text.Json` (built-in) is sufficient for domain event serialization. Domain events are simple DTOs (records with primitive types), not complex object graphs. The marginal benefits don't justify adding a third-party dependency.

---

### Alternative 3: Allow AutoMapper (Object Mapping)

**Description**: Use AutoMapper for DomainEventMapper and other mappers

**Pros**:
- Reduces boilerplate mapping code
- Popular, well-tested library

**Cons**:
- **Adds third-party dependency**: Increases dependency footprint
- **Implicit magic**: Convention-based mapping is less explicit
- **Debugging difficulty**: Mapping errors harder to diagnose
- **Overkill**: Domain event mapping is simple (DTOs to/from domain events)
- **Performance overhead**: Reflection-based mapping slower than hand-written code

**Why rejected**: Domain event mapping is simple and explicit hand-written code is more maintainable. AutoMapper's complexity doesn't justify the dependency for simple mappings like `DomainEventData` to/from `IDomainEvent`.

---

### Alternative 4: Pluggable Dependencies (Adapter Pattern)

**Description**: Make serialization pluggable, let users provide implementations

```csharp
public interface IEventSerializer
{
    string Serialize(IDomainEvent @event);
    IDomainEvent Deserialize(string json, Type eventType);
}

// Users can plug in their preferred serializer
DomainEventMapper.Configure(config =>
{
    config.Serializer = new NewtonsoftEventSerializer();
});
```

**Pros**:
- Ultimate flexibility for users
- Can use preferred serialization libraries
- Testable with mocks

**Cons**:
- **Complexity**: Requires abstraction layer, configuration, interfaces
- **Breaks zero-setup**: Users must configure before using
- **Over-engineering**: YAGNI — 99% of users don't need custom serialization
- **Testing burden**: Must test multiple serializer implementations
- **Not DDD-focused**: Serialization is infrastructure concern, not tactical DDD

**Why rejected**: Adds significant complexity for marginal benefit. Simple, opinionated defaults (System.Text.Json) work for the vast majority of DDD use cases. Users who need custom serialization can implement `IRepositoryPeer` with their preferred storage format.

---

### Alternative 5: Separate Packages for Extensions

**Description**: Core packages have minimal dependencies, extension packages add features

```
ezDDD.Core (depends on: .NET BCL + uContract.NET)
ezDDD.Extensions.Newtonsoft (depends on: ezDDD.Core + Newtonsoft.Json)
ezDDD.Extensions.AutoMapper (depends on: ezDDD.Core + AutoMapper)
```

**Pros**:
- Core packages stay lightweight
- Power users can opt into extensions
- Modular architecture

**Cons**:
- **Maintenance burden**: Multiple packages to maintain and version
- **Confusing**: Users must understand which packages to use
- **Version sync complexity**: Must keep packages in sync
- **Not needed**: Core packages provide complete functionality
- **Splits ecosystem**: Fragments user base across core vs. extensions

**Why rejected**: Core ezDDD.NET provides complete DDD tactical pattern functionality using built-in APIs. Extensions would be unnecessary complexity. Users needing custom behavior can implement `IRepositoryPeer` or other SPIs.

---

## Related Decisions

- **Depends on**: ADR-0001 (Target Framework .NET 8) - .NET 8 has System.Text.Json built-in
- **Related to**: ADR-0005 (Complete Reimplementation) - Zero third-party dependencies enables full control
- **Enables**: ADR-0006 (uContract.NET Integration) - Establishes that ecosystem dependencies are acceptable
- **Influences**: All implementation ADRs - Must use .NET BCL + TeddySoft ecosystem only

---

## Implementation Notes

### Dependency Verification in CI/CD

Add CI/CD check to ensure no third-party dependencies sneak in:

```yaml
# GitHub Actions example
- name: Verify Zero Third-Party Dependencies
  run: |
    dotnet pack -c Release
    # Check that packages only depend on uContract.NET
    # (no other third-party dependencies)
    for pkg in bin/Release/ezDDD.*.nupkg; do
      unzip -p $pkg *.nuspec | grep -E "<dependency.*id=\"(?!uContract)" && exit 1
    done
```

### Code Review Checklist

When reviewing PRs, ensure:
- ✅ No third-party `<PackageReference>` added (except uContract.NET)
- ✅ Only `System.*` and `uContract.*` namespaces imported
- ✅ No use of Newtonsoft.Json, AutoMapper, or other external libraries
- ✅ Test projects can have dev dependencies (xUnit, BenchmarkDotNet)

### Documentation Requirements

Clearly advertise minimal dependencies as a feature:

```markdown
# ezDDD.NET

✨ **Features**:
- 🎯 Zero third-party dependencies — uses only .NET built-in APIs + uContract.NET
- 📦 Minimal package size (~150KB per package)
- ⚡ Minimal version conflicts (only uContract.NET dependency)
- 🔒 Small security surface
- 🏛️ TeddySoft ecosystem consistency
```

### Exception: Test Projects

Test projects (`*.Tests`) ARE allowed dev dependencies:
```xml
<ItemGroup>
  <!-- OK: Test dependencies, not distributed in runtime package -->
  <PackageReference Include="xunit" Version="2.6.0" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.0" />
  <PackageReference Include="coverlet.collector" Version="6.0.0" />
  <PackageReference Include="BenchmarkDotNet" Version="0.13.10" />
</ItemGroup>
```

---

## Ecosystem Dependency Justification

### Why uContract.NET is NOT Third-Party

**Definition of "Third-Party"**: Libraries maintained by external organizations with independent release cycles, design philosophies, and governance.

**Why uContract.NET Doesn't Qualify**:

1. **Same Organization**: Both ezDDD.NET and uContract.NET are TeddySoft libraries
2. **Coordinated Releases**: Both follow semantic versioning with compatible version schemes
3. **Shared Philosophy**: Both emphasize lightweight, zero-external-dependency design
4. **Semantic Parity**: Java ezddd depends on uContract 2.0.0; ezDDD.NET depends on uContract.NET
5. **Essential Functionality**: EsAggregateRoot requires DbC for correctness (R1, R2, R3 rules)
6. **Ecosystem Member**: Part of coordinated TeddySoft DDD toolkit, not standalone library

**Analogy**: Microsoft.Extensions.* packages are not "third-party" to ASP.NET Core because they're part of the same coordinated ecosystem.

---

## Long-Term Considerations

### If .NET Standard 2.0 Support is Needed

If we later decide to support .NET Standard 2.0:
- `System.Text.Json` would become a NuGet dependency (not built-in)
- **Decision**: Re-evaluate zero-third-party principle vs. compatibility
- **Recommendation**: Maintain .NET 8+ focus to preserve built-in APIs

### If Advanced Features are Requested

If users request features requiring third-party libraries:
- **Option 1**: Implement using .NET BCL (slower but no dependency)
- **Option 2**: Document how users can implement themselves via SPIs (`IRepositoryPeer`, `IMessageBus`)
- **Option 3**: Create optional extension package (separate from core, clearly marked)

**Recommended**: Option 1 or 2. Keep core packages third-party-free.

---

## References

- [NuGet Package Dependencies](https://learn.microsoft.com/en-us/nuget/consume-packages/dependency-resolution)
- [.NET API Browser](https://learn.microsoft.com/en-us/dotnet/api/)
- [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview)
- [DOTNET_PORT.md - 最小依賴原則](../../DOTNET_PORT.md#2-最小依賴原則)
- [uContract.NET ADR-0011: Zero-Dependency Principle](../../../uContract.NET/docs/adr/0011-zero-dependency-principle.md)
- [Java ezddd Dependencies](https://gitlab.com/TeddyChen/ezddd)

---

## Revision History

| Date       | Status   | Notes                                  |
|------------|----------|----------------------------------------|
| 2025-10-31 | Accepted | Decision finalized and documented      |

---
