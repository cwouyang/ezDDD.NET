# ADR-0002: Package Naming and Structure

## Status

**Accepted**

- **Date**: 2025-10-28
- **Deciders**: Project maintainers
- **Status Date**: 2025-10-31 (documented as ADR)

---

## Context

### Problem Statement

When porting the Java ezddd library to .NET, we must decide on the naming convention for:
1. **NuGet Package IDs** (what users install via `dotnet add package`)
2. **C# Namespaces** (what users write in `using` statements)
3. **Solution/Project names** (what developers see in the codebase)

The naming decision must balance two competing concerns:
- **Brand identity**: Maintaining recognizable connection to the Java `ezddd` library
- **.NET conventions**: Following established .NET ecosystem naming standards

### Relevant Context

- Java version uses lowercase package names: `com.teddysoft.ezddd.*`
- .NET ecosystem strongly favors PascalCase for namespaces (e.g., `System.Collections`, `Microsoft.Extensions`)
- NuGet package IDs are case-insensitive but typically follow brand naming (e.g., `Newtonsoft.Json`, `AutoMapper`)
- Users interact with Package IDs during installation and Namespaces during coding

### Constraints

- Must maintain recognizable brand identity with Java `ezddd`
- Must not violate .NET community conventions (to avoid appearing unprofessional)
- Must be intuitive for both Java ezddd users migrating to .NET and native .NET developers

---

## Decision

We adopt a **dual naming strategy**:

### Package IDs (NuGet)
Use lowercase brand identifier `ezDDD` with capitalized suffix:
- `ezDDD.Common`
- `ezDDD.Entity`
- `ezDDD.UseCase`
- `ezDDD.Cqrs`
- `ezDDD.Core`

### Namespaces (C# Code)
Use PascalCase following .NET conventions:
- `EzDdd.Common`
- `EzDdd.Entity`
- `EzDdd.UseCase`
- `EzDdd.Cqrs`
- `EzDdd.Core`

### Solution/Project Files
- Solution: `ezDDD.sln`
- Projects: Match Package ID (e.g., `ezDDD.Common.csproj`)

### Details

**User Experience**:
```bash
# Installation: Users see brand name
dotnet add package ezDDD.Common
```

```csharp
// Usage: Developers write idiomatic .NET code
using EzDdd.Common;
using EzDdd.Entity;
```

This approach provides:
- **Visual brand continuity** when browsing NuGet.org or installing packages
- **Idiomatic .NET code** that follows PascalCase namespace conventions
- **Clear separation** between external identity (package) and internal identity (namespace)

---

## Consequences

### Positive Consequences

- ✅ **Brand Recognition**: `ezDDD` package name clearly connects to Java `ezddd` library
- ✅ **Platform Idiomatic**: `EzDdd` namespaces follow .NET PascalCase standard (consistent with BCL, ASP.NET Core, Entity Framework)
- ✅ **Best of Both Worlds**: Balances brand identity preservation with .NET conventions
- ✅ **Familiar to .NET Developers**: Namespace convention matches expectations (similar to `Newtonsoft.Json` package but `Newtonsoft.Json` namespace)
- ✅ **Migration Clarity**: Java users recognize package names; .NET users see proper namespaces

### Negative Consequences

- ❌ **Slight Inconsistency**: Package ID and Namespace differ, requiring documentation
- ❌ **Learning Curve**: Users must understand two different naming schemes (though this is common in .NET ecosystem)

### Neutral Consequences

- ⚖️ **Precedent Exists**: This dual naming is common in .NET (e.g., `Newtonsoft.Json`, `AutoMapper`, `FluentValidation`)
- ⚖️ **Documentation Requirement**: Must clearly document both naming schemes in README and getting started guides

---

## Alternatives Considered

### Alternative 1: Consistent `ezDDD` Everywhere

**Description**: Use `ezDDD.*` for both Package IDs and Namespaces

```csharp
using ezDDD.Common;  // Namespace also uses ezDDD
```

**Pros**:
- Perfect consistency between package and namespace
- Simpler to document (only one naming scheme)
- Exact brand preservation

**Cons**:
- Violates .NET PascalCase namespace convention
- Appears unprofessional to .NET developers (lowercase namespace is unusual)
- Inconsistent with entire .NET ecosystem (BCL, popular libraries)
- Poor developer experience (violates language idioms)

**Why rejected**: Violating .NET namespace conventions would make the library appear low-quality or unmaintained. The .NET community strongly expects PascalCase namespaces.

---

### Alternative 2: Consistent `EzDdd` Everywhere

**Description**: Use `EzDdd.*` for both Package IDs and Namespaces

```bash
dotnet add package EzDdd.Common  # Package ID also uses EzDdd
```

**Pros**:
- Perfect consistency between package and namespace
- Follows .NET conventions everywhere
- Simpler conceptual model

**Cons**:
- Loses brand identity connection to Java `ezddd`
- `EzDdd` is less recognizable than `ezDDD` when browsing NuGet
- Breaks visual continuity with Java ecosystem
- Harder for Java users to discover the .NET port

**Why rejected**: Package ID is the primary discovery mechanism on NuGet.org. Losing the `ezDDD` brand identifier would make it harder for users to find the library and recognize its connection to the Java version.

---

### Alternative 3: Different Name Entirely (e.g., `DddToolkit.NET`)

**Description**: Use a completely different name for the .NET port

**Pros**:
- No confusion about case differences
- Could appeal to pure .NET audience
- Avoids lowercase in package name

**Cons**:
- Complete loss of brand identity
- No connection to Java ezddd
- Confusing for users seeking .NET port of ezddd
- Requires building brand recognition from scratch

**Why rejected**: The primary goal is to port ezddd to .NET, not create a new unrelated library. Maintaining brand continuity is essential for discoverability and cross-platform adoption.

---

## Related Decisions

- **Depends on**: None (foundational decision)
- **Related to**: ADR-0003 (Module Architecture) - Module names depend on this naming decision
- **Influences**: All API naming decisions throughout the project

---

## Implementation Notes

### Project Structure
```
ezDDD.sln                           (Solution file)
├── src/
│   ├── ezDDD.Common/               (Project folder)
│   │   ├── ezDDD.Common.csproj    (Project file - Package ID)
│   │   └── *.cs                    (namespace EzDdd.Common)
│   ├── ezDDD.Entity/
│   │   ├── ezDDD.Entity.csproj
│   │   └── *.cs                    (namespace EzDdd.Entity)
│   └── ...
└── tests/
    └── ...
```

### Package Metadata (.csproj)
```xml
<PropertyGroup>
  <PackageId>ezDDD.Common</PackageId>
  <RootNamespace>EzDdd.Common</RootNamespace>
  <AssemblyName>EzDdd.Common</AssemblyName>
</PropertyGroup>
```

### Documentation Requirements
- README must explain dual naming in "Getting Started" section
- Migration guide must clearly show both package installation and namespace usage
- XML documentation should reference `ezDDD` (package) and `EzDdd` (namespace) appropriately

---

## References

- Internal porting notes (not retained) - 命名決策
- [.NET Naming Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)
- [NuGet Package ID Guidelines](https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package#choose-a-unique-package-identifier-and-version-number)
- [Java ezddd Repository](https://gitlab.com/TeddyChen/ezddd)

---

## Revision History

| Date       | Status   | Notes                                  |
|------------|----------|----------------------------------------|
| 2025-10-28 | Accepted | Decision confirmed during planning     |
| 2025-10-31 | Accepted | Documented as ADR-0002                 |

---
