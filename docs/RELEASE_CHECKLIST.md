# ezDDD.NET Release Checklist

Complete checklist for releasing ezDDD.NET to NuGet.

> **Current Version**: 1.0.0-alpha.1
> **Last Updated**: 2025-11-22
> **Modules**: 5 (Common, Entity, UseCase, Cqrs, Core)

---

## Table of Contents

- [Pre-Release Checklist](#pre-release-checklist)
- [Version Update](#version-update)
- [Quality Assurance](#quality-assurance)
- [Multi-Module Verification](#multi-module-verification)
- [Documentation Review](#documentation-review)
- [Package Build](#package-build)
- [Package Verification](#package-verification)
- [NuGet Publishing](#nuget-publishing)
- [Post-Release](#post-release)
- [Rollback Procedure](#rollback-procedure)

---

## Pre-Release Checklist

### Code Quality

- [ ] **All tests pass** (`dotnet test`)
  ```bash
  cd path/to/ezDDD.NET
  dotnet test
  # Expected: 501/501 tests passing (100%)
  # EzDdd.Common: 69/69
  # EzDdd.Entity: 85/85
  # EzDdd.UseCase: 279/279
  # EzDdd.Cqrs: 68/68
  ```

- [ ] **No compiler warnings** (`dotnet build`)
  ```bash
  dotnet clean
  dotnet build -c Release
  # Expected: 0 errors, 0 warnings (except SourceLink if configured)
  ```

- [ ] **Code coverage verified** (optional for alpha)
  ```bash
  dotnet test --collect:"XPlat Code Coverage"
  # Target: >90% coverage (achieved)
  ```

- [ ] **No TODO/FIXME comments** in production code
  ```bash
  grep -r "TODO\|FIXME" src/ --exclude-dir=obj --exclude-dir=bin
  # Expected: None or documented in issues
  ```

### Functional Testing

- [ ] **Manual smoke tests - EzDdd.Common**
  - [ ] BiMap put/get/remove operations work
  - [ ] BiMap inverse() returns correct bidirectional view
  - [ ] JsonUtil.DeepCopy works correctly for complex objects
  - [ ] IConverter<TSource, TTarget> implementations work
  - [ ] Thread-safe operations work under concurrency

- [ ] **Manual smoke tests - EzDdd.Entity**
  - [ ] IEntity<TId> identity equality works
  - [ ] AggregateRoot event collection works
  - [ ] AggregateRoot versioning increments correctly
  - [ ] EsAggregateRoot event replay reconstructs state
  - [ ] EsAggregateRoot R1/R2/R3 invariant rules enforced correctly
  - [ ] DomainEventTypeMapper registration and lookup works
  - [ ] InternalDomainEvent.IConstructionEvent must be first event
  - [ ] InternalDomainEvent.IDestructionEvent must be last event

- [ ] **Manual smoke tests - EzDdd.UseCase**
  - [ ] IUseCase<TInput, TOutput>.ExecuteAsync() works
  - [ ] EsRepository loads aggregates from event stream
  - [ ] EsRepository saves events to event store
  - [ ] EsRepository uses reflection cache for constructor lookup
  - [ ] OutboxRepository transactional outbox pattern works
  - [ ] OutboxRepository clears events after successful save
  - [ ] BlockingMessageBus dispatches events to reactors
  - [ ] IMessageBusProducer.Dispose() releases resources
  - [ ] Repository bridge pattern works (IRepository ↔ IRepositoryPeer)
  - [ ] DomainEventMapper converts events to/from DomainEventData

- [ ] **Manual smoke tests - EzDdd.Cqrs**
  - [ ] ICommand<TInput, TOutput> marker interface works
  - [ ] IQuery<TInput, TOutput> marker interface works
  - [ ] IInquiry<TInput, TOutput> validation queries work
  - [ ] IProjection<TInput, TOutput> read model builders work
  - [ ] IProjector background service marker works
  - [ ] IArchive<TData, TId> query database interface works
  - [ ] CqrsOutput<T> fluent API (Success/Failure/NotFound) works
  - [ ] CqrsOutput<T> conversion to IOutput works

### Dependency Check

- [ ] **Zero external dependencies verified** (except uContract.NET)
  ```bash
  dotnet list src/EzDdd.Common/EzDdd.Common.csproj package
  dotnet list src/EzDdd.Entity/EzDdd.Entity.csproj package
  dotnet list src/EzDdd.UseCase/EzDdd.UseCase.csproj package
  dotnet list src/EzDdd.Cqrs/EzDdd.Cqrs.csproj package
  dotnet list src/EzDdd.Core/EzDdd.Core.csproj package
  # Expected: Only project references (no external packages except uContract.NET >= 1.0.0 for Entity)
  ```

- [ ] **Module dependency chain correct**
  ```
  EzDdd.Common (no dependencies)
      ↓
  EzDdd.Entity (depends on: Common + uContract.NET >= 1.0.0)
      ↓
  EzDdd.UseCase (depends on: Common, Entity)
      ↓
  EzDdd.Cqrs (depends on: UseCase → Entity → Common)
      ↓
  EzDdd.Core (depends on: Common, Entity, UseCase, Cqrs)
  ```

- [ ] **uContract.NET version constraint correct**
  ```bash
  grep "uContract" src/EzDdd.Entity/EzDdd.Entity.csproj
  # Expected: <PackageReference Include="uContract" Version="1.0.0" /> (or higher)
  ```

### Security Check

- [ ] **No hardcoded secrets** in code
  ```bash
  grep -ri "password\|secret\|key\|token" src/ --exclude-dir=obj --exclude-dir=bin
  # Expected: Only variable names, no actual secrets
  ```

- [ ] **No sensitive data in tests**
  ```bash
  grep -ri "password\|secret\|key\|token" tests/ --exclude-dir=obj --exclude-dir=bin
  # Expected: Only test data (e.g., "test-password")
  ```

---

## Version Update

### Update Version Numbers

⚠️ **CRITICAL**: All 5 modules MUST have the same version number for consistency.

- [ ] **Update `src/EzDdd.Common/EzDdd.Common.csproj`**
  - [ ] `<Version>1.0.0-alpha.1</Version>`
  - [ ] `<AssemblyVersion>1.0.0.0</AssemblyVersion>`
  - [ ] `<FileVersion>1.0.0.0</FileVersion>`
  - [ ] `<PackageVersion>1.0.0-alpha.1</PackageVersion>` (if present)

- [ ] **Update `src/EzDdd.Entity/EzDdd.Entity.csproj`**
  - [ ] `<Version>1.0.0-alpha.1</Version>`
  - [ ] `<AssemblyVersion>1.0.0.0</AssemblyVersion>`
  - [ ] `<FileVersion>1.0.0.0</FileVersion>`
  - [ ] `<PackageVersion>1.0.0-alpha.1</PackageVersion>` (if present)

- [ ] **Update `src/EzDdd.UseCase/EzDdd.UseCase.csproj`**
  - [ ] `<Version>1.0.0-alpha.1</Version>`
  - [ ] `<AssemblyVersion>1.0.0.0</AssemblyVersion>`
  - [ ] `<FileVersion>1.0.0.0</FileVersion>`
  - [ ] `<PackageVersion>1.0.0-alpha.1</PackageVersion>` (if present)

- [ ] **Update `src/EzDdd.Cqrs/EzDdd.Cqrs.csproj`**
  - [ ] `<Version>1.0.0-alpha.1</Version>`
  - [ ] `<AssemblyVersion>1.0.0.0</AssemblyVersion>`
  - [ ] `<FileVersion>1.0.0.0</FileVersion>`
  - [ ] `<PackageVersion>1.0.0-alpha.1</PackageVersion>` (if present)

- [ ] **Update `src/EzDdd.Core/EzDdd.Core.csproj`**
  - [ ] `<Version>1.0.0-alpha.1</Version>`
  - [ ] `<AssemblyVersion>1.0.0.0</AssemblyVersion>`
  - [ ] `<FileVersion>1.0.0.0</FileVersion>`
  - [ ] `<PackageVersion>1.0.0-alpha.1</PackageVersion>` (if present)

### Verify Version Consistency

- [ ] **Verify version consistency across all .csproj files**
  ```bash
  grep "<Version>" src/*/EzDdd.*.csproj
  # Expected: All show 1.0.0-alpha.1
  ```

- [ ] **Verify assembly version consistency**
  ```bash
  grep "<AssemblyVersion>" src/*/EzDdd.*.csproj
  # Expected: All show 1.0.0.0
  ```

- [ ] **Verify file version consistency**
  ```bash
  grep "<FileVersion>" src/*/EzDdd.*.csproj
  # Expected: All show 1.0.0.0
  ```

### Update Documentation

- [ ] **Update `CHANGELOG.md`**
  - [ ] Move items from `[Unreleased]` to `[1.0.0-alpha.1]`
  - [ ] Add release date (2025-11-XX)
  - [ ] Verify all changes documented
  - [ ] Categorize changes: Added, Changed, Fixed, Deprecated, Removed
  - [ ] Include module-specific changes for all 5 modules

- [ ] **Update `README.md`**
  - [ ] Version badge shows `1.0.0-alpha.1`
  - [ ] Status updated to "Alpha Release"
  - [ ] Test count accurate (501 tests)
  - [ ] Module count accurate (5 modules)
  - [ ] Installation instructions correct
  - [ ] Quick start examples use correct version

- [ ] **Update `ROADMAP.md`**
  - [ ] Project status updated to the released version
  - [ ] Last updated information reflects the release

### Update Module-Specific Documentation

- [ ] **Update `API_REFERENCE.md`**
  - [ ] Version number in header updated
  - [ ] All 44 APIs listed with correct signatures
  - [ ] Module organization reflects 5 modules

- [ ] **Update `USAGE_EXAMPLES.md`**
  - [ ] Version number in header updated
  - [ ] Examples reference correct NuGet package versions

- [ ] **Update `MIGRATION_GUIDE.md`**
  - [ ] Version comparison table updated
  - [ ] Java ezddd version referenced (if applicable)

---

## Quality Assurance

### Build Verification

- [ ] **Clean build all configurations**
  ```bash
  dotnet clean
  dotnet build -c Debug
  dotnet build -c Release
  # Expected: 0 errors, 0 warnings
  ```

- [ ] **Test all modules independently**
  ```bash
  dotnet test tests/EzDdd.Common.Tests/EzDdd.Common.Tests.csproj -c Release
  # Expected: 69/69 tests passing

  dotnet test tests/EzDdd.Entity.Tests/EzDdd.Entity.Tests.csproj -c Release
  # Expected: 85/85 tests passing

  dotnet test tests/EzDdd.UseCase.Tests/EzDdd.UseCase.Tests.csproj -c Release
  # Expected: 279/279 tests passing

  dotnet test tests/EzDdd.Cqrs.Tests/EzDdd.Cqrs.Tests.csproj -c Release
  # Expected: 68/68 tests passing
  ```

- [ ] **Test entire solution**
  ```bash
  dotnet test -c Release
  # Expected: 501/501 tests passing (100%)
  ```

- [ ] **Verify test output**
  ```bash
  dotnet test -c Release --logger "console;verbosity=detailed"
  # Expected: No skipped tests, no warnings
  ```

### Performance Verification

- [ ] **EsRepository reflection cache works**
  - [ ] First load uses reflection
  - [ ] Subsequent loads use cached ConstructorInfo
  - [ ] No performance degradation on repeated loads

- [ ] **BiMap operations are O(1)**
  - [ ] Put, get, remove operations are fast
  - [ ] Thread-safe operations don't cause deadlocks

- [ ] **BlockingMessageBus dispatches events efficiently**
  - [ ] No memory leaks with large event volumes
  - [ ] Snapshot enumeration prevents collection modification exceptions

### Documentation Verification

- [ ] **All documentation links work**
  - [ ] README.md links to API_REFERENCE.md
  - [ ] README.md links to USAGE_EXAMPLES.md
  - [ ] README.md links to MIGRATION_GUIDE.md
  - [ ] README.md links to CHANGELOG.md
  - [ ] API_REFERENCE.md internal links work
  - [ ] USAGE_EXAMPLES.md internal links work
  - [ ] ADRs cross-reference correctly (16 ADRs)

- [ ] **Code examples compile**
  - [ ] README.md examples are syntactically correct
  - [ ] API_REFERENCE.md examples compile (all 44 APIs)
  - [ ] USAGE_EXAMPLES.md examples compile (30+ examples)
  - [ ] MIGRATION_GUIDE.md examples compile (both Java and C#)

- [ ] **Documentation coverage complete**
  - [ ] All public APIs documented (44 total)
  - [ ] All interfaces documented (IEntity, IValueObject, IDomainEvent, etc.)
  - [ ] All base classes documented (AggregateRoot, EsAggregateRoot, etc.)
  - [ ] All utilities documented (BiMap, JsonUtil, IConverter, etc.)

---

## Multi-Module Verification

⚠️ **CRITICAL**: ezDDD.NET has 5 modules. Each must be verified independently AND as a whole.

### Module Build Order

Modules MUST build in dependency order:

```bash
# 1. Common (no dependencies)
dotnet build src/EzDdd.Common/EzDdd.Common.csproj -c Release
# Expected: Build succeeded, 0 errors, 0 warnings

# 2. Entity (depends on Common + uContract.NET)
dotnet build src/EzDdd.Entity/EzDdd.Entity.csproj -c Release
# Expected: Build succeeded, 0 errors, 0 warnings

# 3. UseCase (depends on Entity → Common)
dotnet build src/EzDdd.UseCase/EzDdd.UseCase.csproj -c Release
# Expected: Build succeeded, 0 errors, 0 warnings

# 4. Cqrs (depends on UseCase → Entity → Common)
dotnet build src/EzDdd.Cqrs/EzDdd.Cqrs.csproj -c Release
# Expected: Build succeeded, 0 errors, 0 warnings

# 5. Core (depends on all 4 modules)
dotnet build src/EzDdd.Core/EzDdd.Core.csproj -c Release
# Expected: Build succeeded, 0 errors, 0 warnings
```

- [ ] **All 5 modules build successfully in order**

### Module Dependency Verification

- [ ] **EzDdd.Common has zero dependencies**
  ```bash
  dotnet list src/EzDdd.Common/EzDdd.Common.csproj package
  # Expected: No packages

  dotnet list src/EzDdd.Common/EzDdd.Common.csproj reference
  # Expected: No project references
  ```

- [ ] **EzDdd.Entity depends only on Common + uContract.NET**
  ```bash
  dotnet list src/EzDdd.Entity/EzDdd.Entity.csproj reference
  # Expected: ../EzDdd.Common/EzDdd.Common.csproj

  dotnet list src/EzDdd.Entity/EzDdd.Entity.csproj package
  # Expected: uContract >= 1.0.0
  ```

- [ ] **EzDdd.UseCase depends only on Common + Entity**
  ```bash
  dotnet list src/EzDdd.UseCase/EzDdd.UseCase.csproj reference
  # Expected: ../EzDdd.Common/EzDdd.Common.csproj
  #           ../EzDdd.Entity/EzDdd.Entity.csproj

  dotnet list src/EzDdd.UseCase/EzDdd.UseCase.csproj package
  # Expected: No packages (only project references)
  ```

- [ ] **EzDdd.Cqrs depends only on UseCase (transitive: Entity, Common)**
  ```bash
  dotnet list src/EzDdd.Cqrs/EzDdd.Cqrs.csproj reference
  # Expected: ../EzDdd.UseCase/EzDdd.UseCase.csproj

  dotnet list src/EzDdd.Cqrs/EzDdd.Cqrs.csproj package
  # Expected: No packages (only project references)
  ```

- [ ] **EzDdd.Core depends on all 4 modules**
  ```bash
  dotnet list src/EzDdd.Core/EzDdd.Core.csproj reference
  # Expected: ../EzDdd.Common/EzDdd.Common.csproj
  #           ../EzDdd.Entity/EzDdd.Entity.csproj
  #           ../EzDdd.UseCase/EzDdd.UseCase.csproj
  #           ../EzDdd.Cqrs/EzDdd.Cqrs.csproj

  dotnet list src/EzDdd.Core/EzDdd.Core.csproj package
  # Expected: No packages (only project references)
  ```

### Module Assembly Verification

- [ ] **Verify all assemblies produce correct DLLs**
  ```bash
  ls -lh src/EzDdd.Common/bin/Release/net8.0/EzDdd.Common.dll
  ls -lh src/EzDdd.Entity/bin/Release/net8.0/EzDdd.Entity.dll
  ls -lh src/EzDdd.UseCase/bin/Release/net8.0/EzDdd.UseCase.dll
  ls -lh src/EzDdd.Cqrs/bin/Release/net8.0/EzDdd.Cqrs.dll
  ls -lh src/EzDdd.Core/bin/Release/net8.0/EzDdd.Core.dll
  # Expected: All 5 DLLs present with reasonable sizes (20-100 KB each)
  ```

- [ ] **Verify XML documentation files generated**
  ```bash
  ls -lh src/EzDdd.Common/bin/Release/net8.0/EzDdd.Common.xml
  ls -lh src/EzDdd.Entity/bin/Release/net8.0/EzDdd.Entity.xml
  ls -lh src/EzDdd.UseCase/bin/Release/net8.0/EzDdd.UseCase.xml
  ls -lh src/EzDdd.Cqrs/bin/Release/net8.0/EzDdd.Cqrs.xml
  ls -lh src/EzDdd.Core/bin/Release/net8.0/EzDdd.Core.xml
  # Expected: All 5 XML files present
  ```

### Version Consistency

⚠️ **CRITICAL**: All 5 modules MUST have the same version number.

- [ ] **Verify version consistency across all .csproj files**
  ```bash
  grep "<Version>" src/*/EzDdd.*.csproj
  # Expected: All show 1.0.0-alpha.1
  ```

- [ ] **Verify assembly version consistency**
  ```bash
  grep "<AssemblyVersion>" src/*/EzDdd.*.csproj
  # Expected: All show 1.0.0.0
  ```

- [ ] **Verify package metadata consistency**
  - [ ] All have same `<Authors>TeddySoft</Authors>`
  - [ ] All have same `<Copyright>Copyright (c) 2025 TeddySoft</Copyright>`
  - [ ] All have same `<PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>`
  - [ ] All have same `<RepositoryUrl>`
  - [ ] All have correct `<Description>` for each module

---

## Documentation Review

### README.md

- [ ] Status section accurate ("Alpha Release", "Released 2025-11-XX")
- [ ] Version badge shows `1.0.0-alpha.1`
- [ ] Quick Start examples work and compile
- [ ] Installation instructions correct for all 5 packages
- [ ] Feature list complete (44 APIs across 5 modules)
- [ ] Module diagram correct (Common → Entity → UseCase → Cqrs → Core)
- [ ] API overview accurate (5 modules, zero external dependencies)
- [ ] All links work (internal and external)
- [ ] Test statistics accurate (501 tests, 100% passing)

### API_REFERENCE.md

- [ ] All 44 public APIs documented
  - [ ] EzDdd.Common: 3 APIs (BiMap, JsonUtil, IConverter)
  - [ ] EzDdd.Entity: 12 APIs (IEntity, IValueObject, IDomainEvent, AggregateRoot, EsAggregateRoot, etc.)
  - [ ] EzDdd.UseCase: 18 APIs (IUseCase, IRepository, EsRepository, OutboxRepository, IMessageBus, etc.)
  - [ ] EzDdd.Cqrs: 11 APIs (ICommand, IQuery, IProjection, IProjector, IArchive, CqrsOutput, etc.)
- [ ] Signatures accurate (match actual implementations)
- [ ] Examples compile (copy-paste ready)
- [ ] Cross-references correct (internal links work)
- [ ] Version number in header matches release version

### USAGE_EXAMPLES.md

- [ ] 30+ examples present (covering all major patterns)
- [ ] All examples compile (syntactically correct)
- [ ] Examples cover all 5 modules:
  - [ ] EzDdd.Common examples (BiMap, JsonUtil, IConverter)
  - [ ] EzDdd.Entity examples (Entity, ValueObject, AggregateRoot, EsAggregateRoot)
  - [ ] EzDdd.UseCase examples (UseCase, Repository, Event Sourcing, State Sourcing)
  - [ ] EzDdd.Cqrs examples (Command, Query, Projection, CqrsOutput)
  - [ ] Integration examples (full workflows)
- [ ] Code is copy-paste ready (includes all necessary usings)
- [ ] Examples demonstrate best practices
- [ ] Version number in header matches release version

### MIGRATION_GUIDE.md

- [ ] Java ezddd vs C# ezDDD.NET comparisons accurate
- [ ] API mapping complete for all 44 APIs
- [ ] Examples compile (both Java and C# versions)
- [ ] Common gotchas listed (async/await, nullable, records, etc.)
- [ ] Platform differences explained (.NET vs Java)
- [ ] Version comparison table updated (Java ezddd version vs C# ezDDD.NET version)

### CHANGELOG.md

- [ ] All changes documented since last version
- [ ] Release date set (2025-11-XX)
- [ ] Version number correct (`[1.0.0-alpha.1] - 2025-11-XX`)
- [ ] Changes categorized (Added, Changed, Fixed, Deprecated, Removed)
- [ ] Module-specific changes called out for all 5 modules
- [ ] Future plans listed in `[Unreleased]` section
- [ ] Breaking changes clearly marked (if any)

### ADRs (Architecture Decision Records)

- [ ] All 16 ADRs reviewed and up-to-date
  - [ ] ADR-0001 to ADR-0006 (Core Architecture)
  - [ ] ADR-0007 to ADR-0011 (Core DDD Patterns)
  - [ ] ADR-0012 to ADR-0016 (Phase 3 Post-Review)
- [ ] ADR index in `docs/adr/README.md` accurate
- [ ] ADR status correct ("Accepted" for finalized decisions)
- [ ] Cross-references between ADRs work

---

## Package Build

### Clean and Build All Packages

⚠️ **IMPORTANT**: Build packages in **dependency order** to ensure correct references.

```bash
# Clean everything first
dotnet clean
rm -rf src/*/bin src/*/obj
rm -rf nupkgs
mkdir nupkgs

# Build in dependency order
dotnet pack src/EzDdd.Common/EzDdd.Common.csproj -c Release -o nupkgs
dotnet pack src/EzDdd.Entity/EzDdd.Entity.csproj -c Release -o nupkgs
dotnet pack src/EzDdd.UseCase/EzDdd.UseCase.csproj -c Release -o nupkgs
dotnet pack src/EzDdd.Cqrs/EzDdd.Cqrs.csproj -c Release -o nupkgs
dotnet pack src/EzDdd.Core/EzDdd.Core.csproj -c Release -o nupkgs
```

- [ ] **All pack commands succeed with 0 errors, 0 warnings**

### Verify Package Files

- [ ] **All 5 .nupkg files created**
  ```bash
  ls -lh nupkgs/
  # Expected:
  # ezDDD.Common.1.0.0-alpha.1.nupkg
  # ezDDD.Entity.1.0.0-alpha.1.nupkg
  # ezDDD.UseCase.1.0.0-alpha.1.nupkg
  # ezDDD.Cqrs.1.0.0-alpha.1.nupkg
  # ezDDD.Core.1.0.0-alpha.1.nupkg
  ```

- [ ] **Package sizes reasonable**
  ```bash
  ls -lh nupkgs/*.nupkg
  # Expected: Each package 10-50 KB (small libraries)
  # Common: ~15 KB
  # Entity: ~25 KB
  # UseCase: ~35 KB
  # Cqrs: ~20 KB
  # Core: ~10 KB (aggregator only)
  ```

- [ ] **Package file names match expected format**
  - [ ] `ezDDD.Common.1.0.0-alpha.1.nupkg`
  - [ ] `ezDDD.Entity.1.0.0-alpha.1.nupkg`
  - [ ] `ezDDD.UseCase.1.0.0-alpha.1.nupkg`
  - [ ] `ezDDD.Cqrs.1.0.0-alpha.1.nupkg`
  - [ ] `ezDDD.Core.1.0.0-alpha.1.nupkg`

---

## Package Verification

### Inspect Package Contents

Use NuGet Package Explorer (https://github.com/NuGetPackageExplorer/NuGetPackageExplorer) or command-line tools:

```bash
# Extract and inspect (example for Common)
unzip -l nupkgs/ezDDD.Common.1.0.0-alpha.1.nupkg

# Check for:
# - lib/net8.0/EzDdd.Common.dll
# - lib/net8.0/EzDdd.Common.xml (documentation)
# - ezDDD.Common.nuspec (package metadata)
# - README.md (if included)
# - LICENSE (if included)
```

- [ ] **Inspect ezDDD.Common package contents**
  ```bash
  unzip -l nupkgs/ezDDD.Common.1.0.0-alpha.1.nupkg
  # Expected: lib/net8.0/EzDdd.Common.dll, EzDdd.Common.xml, nuspec
  ```

- [ ] **Inspect ezDDD.Entity package contents**
  ```bash
  unzip -l nupkgs/ezDDD.Entity.1.0.0-alpha.1.nupkg
  # Expected: lib/net8.0/EzDdd.Entity.dll, EzDdd.Entity.xml, nuspec
  ```

- [ ] **Inspect ezDDD.UseCase package contents**
  ```bash
  unzip -l nupkgs/ezDDD.UseCase.1.0.0-alpha.1.nupkg
  # Expected: lib/net8.0/EzDdd.UseCase.dll, EzDdd.UseCase.xml, nuspec
  ```

- [ ] **Inspect ezDDD.Cqrs package contents**
  ```bash
  unzip -l nupkgs/ezDDD.Cqrs.1.0.0-alpha.1.nupkg
  # Expected: lib/net8.0/EzDdd.Cqrs.dll, EzDdd.Cqrs.xml, nuspec
  ```

- [ ] **Inspect ezDDD.Core package contents**
  ```bash
  unzip -l nupkgs/ezDDD.Core.1.0.0-alpha.1.nupkg
  # Expected: lib/net8.0/EzDdd.Core.dll, EzDdd.Core.xml, nuspec
  ```

### Verify Package Metadata

For EACH package, verify metadata using NuGet Package Explorer or:

```bash
unzip -p nupkgs/ezDDD.Common.1.0.0-alpha.1.nupkg ezDDD.Common.nuspec
```

- [ ] **ezDDD.Common metadata**
  - [ ] Package ID: `ezDDD.Common`
  - [ ] Version: `1.0.0-alpha.1`
  - [ ] Authors: `TeddySoft`
  - [ ] Description: "Foundation utilities for ezDDD.NET tactical DDD framework"
  - [ ] Tags: `ddd`, `domain-driven-design`, `tactical-ddd`, `utilities`
  - [ ] License: Apache-2.0
  - [ ] Project URL correct
  - [ ] Repository URL correct
  - [ ] Dependencies: None
  - [ ] Target Framework: net8.0

- [ ] **ezDDD.Entity metadata**
  - [ ] Package ID: `ezDDD.Entity`
  - [ ] Version: `1.0.0-alpha.1`
  - [ ] Authors: `TeddySoft`
  - [ ] Description: "Core DDD entities layer for ezDDD.NET with event sourcing support"
  - [ ] Tags: `ddd`, `domain-driven-design`, `entity`, `aggregate`, `event-sourcing`
  - [ ] License: Apache-2.0
  - [ ] Project URL correct
  - [ ] Repository URL correct
  - [ ] Dependencies: `ezDDD.Common >= 1.0.0-alpha.1`, `uContract >= 1.0.0`
  - [ ] Target Framework: net8.0

- [ ] **ezDDD.UseCase metadata**
  - [ ] Package ID: `ezDDD.UseCase`
  - [ ] Version: `1.0.0-alpha.1`
  - [ ] Authors: `TeddySoft`
  - [ ] Description: "Use cases layer for ezDDD.NET with repository and message bus patterns"
  - [ ] Tags: `ddd`, `domain-driven-design`, `use-case`, `repository`, `message-bus`
  - [ ] License: Apache-2.0
  - [ ] Project URL correct
  - [ ] Repository URL correct
  - [ ] Dependencies: `ezDDD.Common >= 1.0.0-alpha.1`, `ezDDD.Entity >= 1.0.0-alpha.1`
  - [ ] Target Framework: net8.0

- [ ] **ezDDD.Cqrs metadata**
  - [ ] Package ID: `ezDDD.Cqrs`
  - [ ] Version: `1.0.0-alpha.1`
  - [ ] Authors: `TeddySoft`
  - [ ] Description: "CQRS patterns for ezDDD.NET with command/query separation and projections"
  - [ ] Tags: `ddd`, `domain-driven-design`, `cqrs`, `command`, `query`, `projection`
  - [ ] License: Apache-2.0
  - [ ] Project URL correct
  - [ ] Repository URL correct
  - [ ] Dependencies: `ezDDD.UseCase >= 1.0.0-alpha.1`
  - [ ] Target Framework: net8.0

- [ ] **ezDDD.Core metadata**
  - [ ] Package ID: `ezDDD.Core`
  - [ ] Version: `1.0.0-alpha.1`
  - [ ] Authors: `TeddySoft`
  - [ ] Description: "Complete ezDDD.NET tactical DDD framework (aggregator package)"
  - [ ] Tags: `ddd`, `domain-driven-design`, `tactical-ddd`, `event-sourcing`, `cqrs`
  - [ ] License: Apache-2.0
  - [ ] Project URL correct
  - [ ] Repository URL correct
  - [ ] Dependencies: `ezDDD.Common >= 1.0.0-alpha.1`, `ezDDD.Entity >= 1.0.0-alpha.1`, `ezDDD.UseCase >= 1.0.0-alpha.1`, `ezDDD.Cqrs >= 1.0.0-alpha.1`
  - [ ] Target Framework: net8.0

### Test Package Installation

Create a test project and install packages to verify they work correctly:

```bash
mkdir test-installation
cd test-installation
dotnet new console -n TestInstall
cd TestInstall

# Test installing Core (should pull all dependencies)
dotnet add package ezDDD.Core --version 1.0.0-alpha.1 --source "../../nupkgs"
dotnet build
# Expected: Build succeeds, all 5 packages restored

# Verify all packages installed
dotnet list package
# Expected: ezDDD.Common 1.0.0-alpha.1
#           ezDDD.Entity 1.0.0-alpha.1
#           ezDDD.UseCase 1.0.0-alpha.1
#           ezDDD.Cqrs 1.0.0-alpha.1
#           ezDDD.Core 1.0.0-alpha.1
#           uContract >= 1.0.0
```

- [ ] **Test Core package installation** (pulls all dependencies)
  - [ ] `dotnet add package ezDDD.Core` succeeds
  - [ ] All 5 ezDDD packages restored
  - [ ] uContract.NET restored as transitive dependency
  - [ ] `dotnet build` succeeds

- [ ] **Test individual package installation**
  ```bash
  # Create another test project
  cd ..
  dotnet new console -n TestIndividual
  cd TestIndividual

  # Test Common
  dotnet add package ezDDD.Common --version 1.0.0-alpha.1 --source "../../../nupkgs"
  dotnet build
  # Expected: Build succeeds

  # Test Entity
  dotnet add package ezDDD.Entity --version 1.0.0-alpha.1 --source "../../../nupkgs"
  dotnet build
  # Expected: Build succeeds, Common auto-restored

  # Test UseCase
  dotnet add package ezDDD.UseCase --version 1.0.0-alpha.1 --source "../../../nupkgs"
  dotnet build
  # Expected: Build succeeds, Entity and Common auto-restored
  ```

- [ ] **Test package uninstallation** (verify clean removal)
  ```bash
  dotnet remove package ezDDD.Core
  dotnet build
  # Expected: Build fails (missing references) - confirms package was used
  ```

### Test Package Usage

- [ ] **Write test code using ezDDD.NET APIs**

  Edit `Program.cs`:
  ```csharp
  using EzDdd.Common;
  using EzDdd.Entity;
  using EzDdd.UseCase;
  using EzDdd.Cqrs;

  // Test BiMap (Common)
  var biMap = new BiMap<string, int>();
  biMap.Put("one", 1);
  Console.WriteLine($"BiMap test: {biMap.Get("one")}");

  // Test DomainEventTypeMapper (Entity)
  DomainEventTypeMapper.Register<TestEvent>("TestEvent");
  Console.WriteLine($"Event type registered: {DomainEventTypeMapper.GetTypeName<TestEvent>()}");

  // Test CqrsOutput (Cqrs)
  var output = CqrsOutput<string>.Success("Test passed!");
  Console.WriteLine($"CQRS output: {output.GetValueOrThrow()}");

  // Simple test event
  public record TestEvent : InternalDomainEvent
  {
      public Guid Id { get; init; }
      public DateTime OccurredOn { get; init; }
      public string Source { get; init; } = string.Empty;
      public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
  }

  Console.WriteLine("All ezDDD.NET APIs accessible!");
  ```

- [ ] **Build test project**
  ```bash
  dotnet build
  # Expected: Compiles successfully with 0 errors, 0 warnings
  ```

- [ ] **Run test project**
  ```bash
  dotnet run
  # Expected: Output shows "All ezDDD.NET APIs accessible!"
  ```

- [ ] **Verify IntelliSense works** (in VS Code or Visual Studio)
  - [ ] Type `EzDdd.` and verify autocomplete shows all 5 namespaces
  - [ ] Type `new BiMap<` and verify generic parameters show
  - [ ] Hover over APIs and verify XML documentation appears

---

## NuGet Publishing

⚠️ **CRITICAL**: Publish packages in **dependency order**. If you publish out of order, dependent packages will fail to install because NuGet cannot resolve dependencies.

**Publishing Order**: Common → Entity → UseCase → Cqrs → Core

**Estimated Time**: 1-2 hours (including NuGet indexing waits of ~5-10 minutes per package)

### Generate API Key

- [ ] Go to https://www.nuget.org/account/apikeys
- [ ] Create new API key: `ezDDD.NET 1.0.0-alpha.1 Release`
- [ ] Set glob pattern: `ezDDD.*`
- [ ] Set expiration: 90 days (recommended for security)
- [ ] Copy API key to secure location (password manager, environment variable)
- [ ] **NEVER commit API key to source control**

### Publish Packages (IN ORDER!)

⚠️ **MUST publish in this exact order. Wait for NuGet indexing after each publish before proceeding to the next package.**

#### 1. Publish Common (no dependencies)

- [ ] **Publish ezDDD.Common**
  ```bash
  dotnet nuget push nupkgs/ezDDD.Common.1.0.0-alpha.1.nupkg \
    --api-key YOUR_API_KEY \
    --source https://api.nuget.org/v3/index.json
  # Expected: "Your package was pushed."
  ```

- [ ] **Wait for NuGet indexing** (~5-10 minutes)
  - [ ] Check https://www.nuget.org/packages/ezDDD.Common/
  - [ ] Verify version `1.0.0-alpha.1` appears
  - [ ] Verify "Install Package" command shows correct version

- [ ] **Verify ezDDD.Common is publicly available**
  ```bash
  dotnet new console -n TestCommon
  cd TestCommon
  dotnet add package ezDDD.Common --version 1.0.0-alpha.1
  dotnet build
  # Expected: Package restores from NuGet.org, build succeeds
  cd ..
  rm -rf TestCommon
  ```

#### 2. Publish Entity (depends on Common)

- [ ] **Publish ezDDD.Entity**
  ```bash
  dotnet nuget push nupkgs/ezDDD.Entity.1.0.0-alpha.1.nupkg \
    --api-key YOUR_API_KEY \
    --source https://api.nuget.org/v3/index.json
  # Expected: "Your package was pushed."
  ```

- [ ] **Wait for NuGet indexing** (~5-10 minutes)
  - [ ] Check https://www.nuget.org/packages/ezDDD.Entity/
  - [ ] Verify version `1.0.0-alpha.1` appears
  - [ ] Verify Dependencies section shows `ezDDD.Common >= 1.0.0-alpha.1` and `uContract >= 1.0.0`

- [ ] **Verify ezDDD.Entity is publicly available**
  ```bash
  dotnet new console -n TestEntity
  cd TestEntity
  dotnet add package ezDDD.Entity --version 1.0.0-alpha.1
  dotnet build
  # Expected: Package restores from NuGet.org, Common auto-restored, build succeeds
  cd ..
  rm -rf TestEntity
  ```

#### 3. Publish UseCase (depends on Common, Entity)

- [ ] **Publish ezDDD.UseCase**
  ```bash
  dotnet nuget push nupkgs/ezDDD.UseCase.1.0.0-alpha.1.nupkg \
    --api-key YOUR_API_KEY \
    --source https://api.nuget.org/v3/index.json
  # Expected: "Your package was pushed."
  ```

- [ ] **Wait for NuGet indexing** (~5-10 minutes)
  - [ ] Check https://www.nuget.org/packages/ezDDD.UseCase/
  - [ ] Verify version `1.0.0-alpha.1` appears
  - [ ] Verify Dependencies section shows `ezDDD.Common >= 1.0.0-alpha.1` and `ezDDD.Entity >= 1.0.0-alpha.1`

- [ ] **Verify ezDDD.UseCase is publicly available**
  ```bash
  dotnet new console -n TestUseCase
  cd TestUseCase
  dotnet add package ezDDD.UseCase --version 1.0.0-alpha.1
  dotnet build
  # Expected: Package restores from NuGet.org, Entity and Common auto-restored, build succeeds
  cd ..
  rm -rf TestUseCase
  ```

#### 4. Publish Cqrs (depends on UseCase)

- [ ] **Publish ezDDD.Cqrs**
  ```bash
  dotnet nuget push nupkgs/ezDDD.Cqrs.1.0.0-alpha.1.nupkg \
    --api-key YOUR_API_KEY \
    --source https://api.nuget.org/v3/index.json
  # Expected: "Your package was pushed."
  ```

- [ ] **Wait for NuGet indexing** (~5-10 minutes)
  - [ ] Check https://www.nuget.org/packages/ezDDD.Cqrs/
  - [ ] Verify version `1.0.0-alpha.1` appears
  - [ ] Verify Dependencies section shows `ezDDD.UseCase >= 1.0.0-alpha.1`

- [ ] **Verify ezDDD.Cqrs is publicly available**
  ```bash
  dotnet new console -n TestCqrs
  cd TestCqrs
  dotnet add package ezDDD.Cqrs --version 1.0.0-alpha.1
  dotnet build
  # Expected: Package restores from NuGet.org, UseCase/Entity/Common auto-restored, build succeeds
  cd ..
  rm -rf TestCqrs
  ```

#### 5. Publish Core (depends on all 4 modules)

- [ ] **Publish ezDDD.Core**
  ```bash
  dotnet nuget push nupkgs/ezDDD.Core.1.0.0-alpha.1.nupkg \
    --api-key YOUR_API_KEY \
    --source https://api.nuget.org/v3/index.json
  # Expected: "Your package was pushed."
  ```

- [ ] **Wait for NuGet indexing** (~5-10 minutes)
  - [ ] Check https://www.nuget.org/packages/ezDDD.Core/
  - [ ] Verify version `1.0.0-alpha.1` appears
  - [ ] Verify Dependencies section shows all 4 modules (`ezDDD.Common`, `ezDDD.Entity`, `ezDDD.UseCase`, `ezDDD.Cqrs` >= 1.0.0-alpha.1)

- [ ] **Verify ezDDD.Core is publicly available**
  ```bash
  dotnet new console -n TestCore
  cd TestCore
  dotnet add package ezDDD.Core --version 1.0.0-alpha.1
  dotnet build
  # Expected: Package restores from NuGet.org, all 4 modules + uContract auto-restored, build succeeds
  cd ..
  rm -rf TestCore
  ```

### Verify NuGet.org

For each package, verify the following on NuGet.org:

- [ ] **ezDDD.Common on NuGet.org**
  - [ ] Package visible at https://www.nuget.org/packages/ezDDD.Common/
  - [ ] Version `1.0.0-alpha.1` listed
  - [ ] Package description correct
  - [ ] README displays correctly
  - [ ] Dependencies show: None
  - [ ] Download stats initialized (0 downloads initially)
  - [ ] License shows Apache-2.0
  - [ ] Project URL correct
  - [ ] Tags correct: `ddd`, `domain-driven-design`, `tactical-ddd`, `utilities`

- [ ] **ezDDD.Entity on NuGet.org**
  - [ ] Package visible at https://www.nuget.org/packages/ezDDD.Entity/
  - [ ] Version `1.0.0-alpha.1` listed
  - [ ] Dependencies show: `ezDDD.Common >= 1.0.0-alpha.1`, `uContract >= 1.0.0`
  - [ ] All metadata correct

- [ ] **ezDDD.UseCase on NuGet.org**
  - [ ] Package visible at https://www.nuget.org/packages/ezDDD.UseCase/
  - [ ] Version `1.0.0-alpha.1` listed
  - [ ] Dependencies show: `ezDDD.Common >= 1.0.0-alpha.1`, `ezDDD.Entity >= 1.0.0-alpha.1`
  - [ ] All metadata correct

- [ ] **ezDDD.Cqrs on NuGet.org**
  - [ ] Package visible at https://www.nuget.org/packages/ezDDD.Cqrs/
  - [ ] Version `1.0.0-alpha.1` listed
  - [ ] Dependencies show: `ezDDD.UseCase >= 1.0.0-alpha.1`
  - [ ] All metadata correct

- [ ] **ezDDD.Core on NuGet.org**
  - [ ] Package visible at https://www.nuget.org/packages/ezDDD.Core/
  - [ ] Version `1.0.0-alpha.1` listed
  - [ ] Dependencies show: All 4 modules >= 1.0.0-alpha.1
  - [ ] All metadata correct

### End-to-End Installation Test

After all 5 packages are published and indexed, perform a final end-to-end test:

- [ ] **Create clean test project**
  ```bash
  mkdir final-test
  cd final-test
  dotnet new console -n FinalTest
  cd FinalTest
  ```

- [ ] **Install ezDDD.Core from NuGet.org**
  ```bash
  dotnet add package ezDDD.Core --version 1.0.0-alpha.1
  dotnet restore
  # Expected: All 5 packages + uContract restored from NuGet.org
  ```

- [ ] **Verify all packages restored**
  ```bash
  dotnet list package
  # Expected:
  # ezDDD.Common 1.0.0-alpha.1
  # ezDDD.Entity 1.0.0-alpha.1
  # ezDDD.UseCase 1.0.0-alpha.1
  # ezDDD.Cqrs 1.0.0-alpha.1
  # ezDDD.Core 1.0.0-alpha.1
  # uContract >= 1.0.0
  ```

- [ ] **Write test code using all modules**

  Edit `Program.cs`:
  ```csharp
  using EzDdd.Common;
  using EzDdd.Entity;
  using EzDdd.UseCase;
  using EzDdd.Cqrs;

  // Test Common
  var biMap = new BiMap<string, int>();
  biMap.Put("one", 1);
  Console.WriteLine($"BiMap test: {biMap.Get("one")}");

  // Test Entity
  DomainEventTypeMapper.Register<TestEvent>("TestEvent");
  Console.WriteLine($"Event type registered: {DomainEventTypeMapper.GetTypeName<TestEvent>()}");

  // Test UseCase (marker interface - no runtime test needed)
  Console.WriteLine("IUseCase interface available");

  // Test Cqrs
  var output = CqrsOutput<string>.Success("Test passed!");
  Console.WriteLine($"CQRS output: {output.GetValueOrThrow()}");

  // Test event
  public record TestEvent : InternalDomainEvent
  {
      public Guid Id { get; init; }
      public DateTime OccurredOn { get; init; }
      public string Source { get; init; } = string.Empty;
      public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
  }

  Console.WriteLine("All ezDDD.NET modules work from NuGet.org!");
  ```

- [ ] **Build and run final test**
  ```bash
  dotnet build
  dotnet run
  # Expected: Compiles and runs successfully
  # Output: "All ezDDD.NET modules work from NuGet.org!"
  ```

- [ ] **Clean up test project**
  ```bash
  cd ../..
  rm -rf final-test
  ```

---

## Post-Release

### Git Tagging

- [ ] **Commit version changes** (if not already committed)
  ```bash
  git add .
  git commit -m "chore: Release 1.0.0-alpha.1"
  git push origin master
  ```

- [ ] **Tag the release**
  ```bash
  git tag -a v1.0.0-alpha.1 -m "Release 1.0.0-alpha.1 - Alpha release with 5 modules, 44 APIs, 501 tests"
  git push origin v1.0.0-alpha.1
  ```

- [ ] **Verify tag pushed**
  ```bash
  git tag -l
  # Expected: v1.0.0-alpha.1 listed
  ```

### Create GitHub Release (if using GitHub)

- [ ] Go to https://github.com/YOUR_ORG/ezDDD.NET/releases/new
- [ ] Select tag: `v1.0.0-alpha.1`
- [ ] Release title: `v1.0.0-alpha.1 - Alpha Release`
- [ ] Description: Copy relevant sections from CHANGELOG.md
  - [ ] Overview
  - [ ] What's New
  - [ ] Installation instructions
  - [ ] Breaking changes (if any)
- [ ] Check "This is a pre-release" (for alpha versions)
- [ ] Attach .nupkg files (optional - they're on NuGet.org)
- [ ] Publish release

### Update Documentation

- [ ] **Update ROADMAP.md**
  - [ ] Mark Phase 5 complete ✅
  - [ ] Update overall status to "Released 1.0.0-alpha.1"
  - [ ] Record release date (2025-11-XX)
  - [ ] Update progress to 100%

- [ ] **Update README.md**
  - [ ] Remove "not yet published" notes (if any)
  - [ ] Update installation instructions with actual NuGet commands
  - [ ] Update status badge to show "Alpha Release"
  - [ ] Add "Quick Start" section with real NuGet install commands

- [ ] **Update project website** (if applicable)
  - [ ] Post release announcement
  - [ ] Update documentation links
  - [ ] Update download/installation page

### Announce Release

- [ ] **Post announcement on relevant channels**
  - [ ] Twitter/X (if applicable)
  - [ ] Reddit (r/dotnet, r/programming, etc.)
  - [ ] Dev.to or Medium blog post
  - [ ] LinkedIn
  - [ ] Company/project blog

- [ ] **Notify early adopters/testers**
  - [ ] Email testers who provided feedback
  - [ ] Post in project Discord/Slack (if applicable)
  - [ ] Thank contributors

- [ ] **Update package manager listings**
  - [ ] Verify NuGet.org listings are complete
  - [ ] Update any third-party package directories

### Monitor

- [ ] **Watch for download stats on NuGet.org**
  - [ ] Check https://www.nuget.org/stats/packages/ezDDD.Core
  - [ ] Monitor daily for first week
  - [ ] Track adoption trends

- [ ] **Monitor GitHub issues for bug reports**
  - [ ] Watch for new issues
  - [ ] Respond to bug reports within 24-48 hours
  - [ ] Triage and prioritize issues

- [ ] **Check NuGet package health**
  - [ ] Verify packages install correctly (user feedback)
  - [ ] Monitor for dependency issues
  - [ ] Check for security vulnerabilities (Dependabot, etc.)

- [ ] **Monitor community feedback**
  - [ ] Stack Overflow questions
  - [ ] GitHub Discussions
  - [ ] Social media mentions
  - [ ] Blog post comments

### Post-Release Tasks

- [ ] **Update CHANGELOG.md for next version**
  - [ ] Add `[Unreleased]` section
  - [ ] Move any future plans to unreleased

- [ ] **Plan next release**
  - [ ] Create milestone for 1.0.0-alpha.2 (or 1.0.0-beta.1)
  - [ ] Prioritize issues and features
  - [ ] Update ROADMAP.md with next steps

- [ ] **Rotate API key** (security best practice)
  - [ ] After 90 days, generate new API key
  - [ ] Revoke old API key

---

## Rollback Procedure

⚠️ **WARNING**: NuGet packages **cannot be deleted** once published (only unlisted). Plan carefully before publishing.

### If Critical Bug Found BEFORE Publishing

If you discover a critical bug during this checklist (before running `dotnet nuget push`):

- [ ] **Do NOT publish any packages**
- [ ] **Fix bug immediately**
  - [ ] Write failing test reproducing the bug
  - [ ] Fix the bug
  - [ ] Verify test passes
- [ ] **Re-run full verification**
  - [ ] `dotnet clean`
  - [ ] `dotnet build -c Release`
  - [ ] `dotnet test -c Release`
  - [ ] Re-run all smoke tests
- [ ] **Increment version if needed**
  - [ ] If version was already tagged: increment to 1.0.0-alpha.2
  - [ ] Update all 5 .csproj files
  - [ ] Update documentation
- [ ] **Re-run entire checklist from beginning**

### If Critical Bug Found AFTER Publishing

⚠️ **Cannot delete from NuGet** - must publish new version:

#### Step 1: Unlist Broken Packages

- [ ] **Unlist on NuGet.org web interface**
  - [ ] Go to https://www.nuget.org/packages/ezDDD.Common/1.0.0-alpha.1/Delete
  - [ ] Click "Unlist" (NOT "Delete" - that's permanent)
  - [ ] Repeat for all 5 packages (Common, Entity, UseCase, Cqrs, Core)
  - [ ] Unlisting makes packages invisible to new installs but doesn't break existing projects

#### Step 2: Fix Bug Immediately

- [ ] **Reproduce bug**
  - [ ] Create failing test
  - [ ] Document reproduction steps

- [ ] **Fix bug**
  - [ ] Implement fix
  - [ ] Verify test passes
  - [ ] Run full test suite

- [ ] **Document fix**
  - [ ] Update CHANGELOG.md
  - [ ] Add bug fix to `[1.0.0-alpha.2] - 2025-11-XX` section
  - [ ] Clearly describe what was broken and how it's fixed

#### Step 3: Publish Patch Version

- [ ] **Increment version to 1.0.0-alpha.2**
  - [ ] Update all 5 .csproj files
  - [ ] Update CHANGELOG.md
  - [ ] Update README.md
  - [ ] Update ROADMAP.md

- [ ] **Follow full release checklist again**
  - [ ] Run all tests
  - [ ] Build all packages
  - [ ] Verify all packages
  - [ ] Publish in dependency order (Common → Entity → UseCase → Cqrs → Core)

- [ ] **Tag new version**
  ```bash
  git tag -a v1.0.0-alpha.2 -m "Release 1.0.0-alpha.2 - Fix critical bug in [module]"
  git push origin v1.0.0-alpha.2
  ```

#### Step 4: Notify Users

- [ ] **Post on GitHub issues**
  - [ ] Create issue: "Critical bug in 1.0.0-alpha.1: [description]"
  - [ ] Explain what was broken
  - [ ] Link to fix commit
  - [ ] Recommend upgrade to 1.0.0-alpha.2

- [ ] **Update documentation**
  - [ ] Add warning banner to README.md: "⚠️ Version 1.0.0-alpha.1 has critical bug. Please upgrade to 1.0.0-alpha.2."
  - [ ] Update installation instructions to recommend 1.0.0-alpha.2

- [ ] **Announce on social media**
  - [ ] Post on Twitter/X: "ezDDD.NET 1.0.0-alpha.2 released - fixes critical bug in [module]. Please upgrade."
  - [ ] Email early adopters

### If Multiple Packages Affected

If only some packages have bugs:

- [ ] **Unlist only affected packages**
- [ ] **Fix bugs in affected modules**
- [ ] **Increment version for ALL 5 packages** (maintain version consistency)
- [ ] **Publish all 5 packages again** (even unchanged ones)

⚠️ **Rationale**: All ezDDD.NET packages should have the same version number for simplicity. Even if only EzDdd.UseCase has a bug, publish 1.0.0-alpha.2 for all 5 modules.

---

## Emergency Contacts

In case of issues during release:

- **NuGet.org Support**: https://www.nuget.org/policies/Contact
- **GitHub Support**: https://support.github.com/
- **Project Maintainer**: [Your contact info]
- **Backup Maintainer**: [Backup contact info]

---

## Notes

### Time Estimates

- **First Release**: 3-4 hours
  - Pre-release checks: 30 minutes
  - Version updates: 15 minutes
  - Quality assurance: 30 minutes
  - Multi-module verification: 30 minutes
  - Package build: 15 minutes
  - Package verification: 30 minutes
  - NuGet publishing: 1-1.5 hours (including waits)
  - Post-release: 30 minutes

- **Subsequent Releases**: 1-2 hours (familiar with process)

### Multi-Module Complexity

ezDDD.NET requires extra care due to 5 modules:

1. **Dependency Order**: MUST publish Common → Entity → UseCase → Cqrs → Core
2. **Version Consistency**: ALL 5 modules MUST have same version
3. **Indexing Waits**: ~5-10 minutes per package (50 minutes total for 5 packages)
4. **Verification**: Test each module individually AND as a whole

### Critical Success Factors

1. ✅ Publish in dependency order (Common → Entity → UseCase → Cqrs → Core)
2. ✅ Wait for NuGet indexing between each publish
3. ✅ Verify version consistency across all 5 modules
4. ✅ Test package installation before publishing
5. ✅ Test end-to-end after all 5 packages published
6. ✅ Monitor NuGet.org for first 24-48 hours after release

### Common Pitfalls

- ❌ Publishing out of order → Dependency resolution fails
- ❌ Not waiting for indexing → Dependent packages fail
- ❌ Mismatched versions → Confusing for users
- ❌ Skipping end-to-end test → Missing integration issues
- ❌ Forgetting to unlist broken packages → Users install broken version

---

**Last Updated**: 2025-11-22
**Version**: 1.0.0-alpha.1
**Modules**: 5 (Common, Entity, UseCase, Cqrs, Core)
**Maintainer**: TeddySoft
