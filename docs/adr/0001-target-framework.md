# ADR-0001: Target Framework - .NET 8

## Status

**Accepted**

- **Date**: 2025-10-31
- **Deciders**: Project maintainers
- **Status Date**: 2025-10-31

---

## Context

### Problem Statement

We need to decide which .NET version(s) to target for ezDDD.NET. This decision affects:
- Available C# language features and runtime APIs
- User adoption (compatibility with existing projects)
- Maintenance cost (supporting multiple versions requires more testing)
- Performance characteristics and modern optimizations
- Alignment with DDD tactical pattern requirements (event sourcing, CQRS)

### Relevant Context

- The Java ezddd uses Java 17+ with modern features
- .NET 8 is the latest Long-Term Support (LTS) release (supported until November 2026)
- .NET 6 is the previous LTS release (support ends November 2024)
- .NET Standard 2.0 offers maximum compatibility but severely limits available APIs
- System.Text.Json is built-in from .NET Core 3.0+ (required for event serialization)
- Nullable reference types require .NET 5+ for full IDE and compiler support
- Records (C# 9+) require .NET 5+ for optimal syntax and runtime support

### Constraints

- Must support `System.Text.Json` for event serialization and deep copy functionality
- Must support modern C# features essential for DDD patterns:
  - **Nullable reference types**: Type-safe aggregate identity and domain modeling
  - **Records**: Immutable domain events and value objects
  - **Pattern matching**: Event handling in `EsAggregateRoot.When()` method
  - **Init-only properties**: Immutable event construction
  - **Async/await**: Repository and use case asynchronous I/O
- Should align with the modern, type-safe philosophy of the Java ezddd
- Must support zero third-party dependencies (built-in APIs only, plus TeddySoft ecosystem)

---

## Decision

**We will target .NET 8 as the minimum supported framework.**

### Details

- **Target Framework Moniker (TFM)**: `net8.0`
- **C# Language Version**: C# 12 (default for .NET 8)
- **No multi-targeting** for the initial release (v1.0.0)
- Future versions may add .NET 9+ support as new LTS versions are released
- All projects in the solution use `<TargetFramework>net8.0</TargetFramework>`

---

## Consequences

### Positive Consequences

- ✅ **Modern C# features**: Full access to C# 12 language features (primary constructors, collection expressions, etc.)
- ✅ **Best performance**: .NET 8 includes significant performance improvements over earlier versions
- ✅ **Built-in APIs**: System.Text.Json with all features available without additional dependencies
- ✅ **Long-term support**: .NET 8 LTS is supported until November 2026 (3 years from release)
- ✅ **Nullable reference types**: Full compiler and IDE support for null safety
- ✅ **Records**: First-class support for immutable domain events and value objects
- ✅ **Pattern matching**: Modern switch expressions for event handling (`When()` method)
- ✅ **Init-only properties**: Immutable object construction without verbose constructors
- ✅ **Simplified maintenance**: Single target framework reduces testing matrix
- ✅ **Async/await enhancements**: Task-based async patterns fully matured
- ✅ **Zero-dependency alignment**: System.Text.Json built-in supports zero-dependency principle

### Negative Consequences

- ❌ **Limited adoption**: Projects on .NET 6 or .NET Framework cannot use this library without upgrading
- ❌ **Migration barrier**: Users must upgrade to .NET 8 to adopt ezDDD.NET
- ❌ **No .NET Framework support**: Cannot be used in legacy .NET Framework projects
- ❌ **Enterprise adoption delay**: Some enterprises may still be on .NET 6 due to slow upgrade cycles

### Neutral Consequences

- ⚖️ **Modern-only approach**: Explicitly targeting modern .NET aligns with the library's philosophy (complete reimplementation, not legacy support)
- ⚖️ **Clear upgrade path**: Users know they need .NET 8+; no confusion about supported versions
- ⚖️ **Forward compatibility**: .NET 8 code will work on .NET 9, 10, etc. (forward compatible)

---

## Alternatives Considered

### Alternative 1: .NET 6 (Previous LTS)

**Description**: Target .NET 6 as the minimum version

**Pros**:
- Wider current adoption (many projects still on .NET 6 as of 2025)
- Has all necessary APIs (System.Text.Json, nullable reference types, records, pattern matching)
- Lower migration barrier for existing .NET 6 projects

**Cons**:
- LTS support ends November 2024 (already expired or expiring soon)
- Less performance optimizations compared to .NET 8
- Missing some C# 11/12 features (though not critical for DDD patterns)
- Starting a new library on an expiring LTS version would force an early upgrade

**Why rejected**: .NET 6 LTS support ends in November 2024, making it a poor foundation for a new library intended for long-term use. Projects should upgrade to .NET 8 LTS for continued support. Starting with .NET 8 provides better long-term stability and avoids immediate obsolescence.

---

### Alternative 2: .NET Standard 2.0

**Description**: Target .NET Standard 2.0 for maximum compatibility

**Pros**:
- Maximum compatibility (includes .NET Framework 4.6.1+, .NET Core 2.0+, Xamarin, Unity)
- Largest potential user base
- Supports legacy enterprise applications

**Cons**:
- **No built-in System.Text.Json**: Requires NuGet dependency (Newtonsoft.Json or System.Text.Json NuGet), breaking zero-dependency principle
- **No nullable reference types**: Missing compile-time null safety, essential for aggregate identity guarantees
- **No records**: Cannot use immutable record types for domain events and value objects (must use classes with manual equality)
- **No pattern matching**: Must use verbose if-else or switch-case with type checking for event handling
- **No init-only properties**: Verbose constructors for immutable objects
- **No async/await enhancements**: Limited Task-based patterns
- **Performance limitations**: Missing runtime optimizations
- **Increased code complexity**: Workarounds for missing APIs increase maintenance burden

**Why rejected**: Would fundamentally compromise ezDDD.NET's design philosophy. Modern C# features (nullable reference types, records, pattern matching) are essential for expressing DDD patterns idiomatically. Adding external dependencies contradicts the zero-dependency principle. The compatibility gain does not justify the significant loss in code quality, type safety, and maintainability.

---

### Alternative 3: Multi-targeting (net6.0;net8.0)

**Description**: Support both .NET 6 and .NET 8 simultaneously

**Pros**:
- Wider compatibility (covers both .NET 6 and .NET 8 users)
- Users can choose based on their constraints
- Gradual migration path for users

**Cons**:
- **Increased testing burden**: Must test both TFMs for every change
- **Increased maintenance complexity**: Conditional compilation (`#if NET6_0`) increases code complexity
- **Conditional compilation noise**: Pollutes codebase with platform-specific workarounds
- **.NET 6 LTS ending soon**: .NET 6 support ends November 2024, making multi-targeting a short-lived benefit
- **No real API differences**: Both .NET 6 and .NET 8 have the same core APIs we need (System.Text.Json, nullable, records)
- **Limited return on investment**: Added complexity does not provide significant value

**Why rejected**: Added complexity does not justify the benefit. .NET 6 LTS support ends in November 2024, so multi-targeting would only provide value for a short period. Since ezDDD.NET uses only APIs available in both frameworks, there's no compelling technical reason to support .NET 6. Better to start with .NET 8 from the beginning and encourage users to upgrade.

---

### Alternative 4: Multi-targeting (netstandard2.0;net8.0)

**Description**: Support both .NET Standard 2.0 and .NET 8 for maximum reach

**Pros**:
- Maximum compatibility (legacy + modern)
- Single package works everywhere

**Cons**:
- **Lowest common denominator API**: Code must work on .NET Standard 2.0, limiting to its restricted API surface
- **Cannot use modern C# features**: Nullable, records, pattern matching unavailable on netstandard2.0 build
- **Conditional compilation nightmare**: Extensive `#if` directives to handle API differences
- **Two completely different implementations**: netstandard2.0 build would be fundamentally different from net8.0 build
- **Testing complexity**: Must test both implementations thoroughly
- **Maintenance burden**: Every change must be carefully tested on both platforms

**Why rejected**: This approach would require maintaining two fundamentally different implementations. The netstandard2.0 build would lack the modern C# features that make ezDDD.NET idiomatic and type-safe. The complexity and maintenance burden far outweigh the compatibility benefit.

---

## Related Decisions

- **Related to**: ADR-0004 (Zero Third-Party Dependency Principle) - .NET 8 has System.Text.Json built-in
- **Related to**: ADR-0012 (Nullable Reference Types Strategy) - .NET 8 ensures full nullable reference type support
- **Related to**: ADR-0013 (Record Types for Immutability) - .NET 8 has first-class record support
- **Related to**: ADR-0014 (Pattern Matching for Event Handling) - .NET 8 supports modern pattern matching syntax
- **Influences**: All subsequent implementation decisions depend on .NET 8 API availability

---

## Implementation Notes

### Project Configuration

All `.csproj` files specify:

```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <LangVersion>12</LangVersion>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

### CI/CD Pipeline

- Use .NET 8 SDK for build and test
- Verify package metadata specifies minimum framework version
- Run tests on .NET 8 runtime only (no multi-targeting needed)

### Documentation Requirements

- README.md must clearly state "Requires .NET 8 or later"
- NuGet package description must include ".NET 8+" requirement
- Migration guide for Java ezddd users should mention .NET 8 requirement
- Getting started guide should verify .NET 8 SDK installation

### SDK Installation Command

Users must have .NET 8 SDK installed:

```bash
# Verify .NET 8 is installed
dotnet --list-sdks

# If not installed, download from:
# https://dotnet.microsoft.com/download/dotnet/8.0
```

---

## References

- [.NET Release Schedule](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [.NET 8 Announcement](https://devblogs.microsoft.com/dotnet/announcing-dotnet-8/)
- [C# 12 Language Features](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12)
- [System.Text.Json Overview](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview)
- [Nullable Reference Types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references)
- [Records in C#](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
- [DOTNET_PORT.md](../../DOTNET_PORT.md)
- [uContract.NET ADR-0001](../../../uContract.NET/docs/adr/0001-target-framework.md) - Similar decision for ecosystem consistency

---

## Revision History

| Date       | Status   | Notes                          |
|------------|----------|--------------------------------|
| 2025-10-31 | Accepted | Decision finalized and documented |

---
