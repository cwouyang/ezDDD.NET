# ADR-0030: uContract 2.0.0 — Precondition Contracts Enforced by Default

## Status

**Accepted**

- **Date**: 2026-10-05
- **Deciders**: Development Team
- **Status Date**: 2026-10-05

---

## Context

### Problem Statement

[ADR-0006](0006-ucontract-integration-design-by-contract.md) chose uContract.NET for Design by
Contract and rejected `Debug.Assert` because "Domain invariants must be enforced in all builds."
The five contracts in ezDDD.NET 1.0.0 are argument preconditions rather than the invariants that
ADR-0006 discusses, but they rest on the same principle, and they were not enforced in a default
configuration. The uContract 1.0.0 package evaluated no contract unless the application set the
environment variable `DBC=on`: its default depended on the build configuration of uContract itself,
and the package is built in Release
([uContract.NET issue #32](https://github.com/cwouyang/uContract.NET/issues/32)). Every
`Contract.Require` call in ezDDD.NET 1.0.0 was therefore skipped for a consumer who set nothing.

uContract 2.0.0 enables contracts by default in every build configuration. Which uContract version
should ezDDD.NET depend on, which exceptions should its checks raise once they run, and what
version number does the result need?

### Relevant Context

- **Where ezDDD.NET uses uContract**: only through `Contract.Require`, in five places — four in
  `DomainEventTypeMapper` (`Register`, `GetTypeName(Type)`, `GetTypeName(IInternalDomainEvent)`,
  `GetType`) and one in the `EsAggregateRoot` replay constructor. The R1/R2/R3 invariant checking
  that `EsAggregateRoot` performs itself (`Apply` / `_EnsureInvariant`, see
  [ADR-0010](0010-esaggregate-root-event-sourcing-implementation.md)) is written without uContract,
  so ezDDD.NET's own code there is not affected. An application's override of `_EnsureInvariant`
  may call uContract; see Consequences.
- **Behaviour on invalid input**, measured on 2026-10-05 by running the 13 test cases added with
  this decision against each uContract version, with no `DBC*` variable set. The results were the
  same in Debug and Release:

  | Call | With uContract 1.0.0 | With uContract 2.0.0 |
  |------|----------------------|----------------------|
  | `DomainEventTypeMapper.Register<T>(null)` | `ArgumentNullException` | `PreconditionViolationException` |
  | `Register<T>("")`, `Register<T>("  ")` | No exception; the blank name was registered | `PreconditionViolationException` |
  | `GetTypeName((Type)null)` | `ArgumentNullException` | `PreconditionViolationException` |
  | `GetTypeName((IInternalDomainEvent)null)` | `NullReferenceException` | `PreconditionViolationException` |
  | `GetType(null)` | `ArgumentNullException` | `PreconditionViolationException` |
  | `GetType("")`, `GetType("  ")` | `InvalidOperationException` (not registered), if no blank name had been registered | `PreconditionViolationException` |
  | Replay constructor with `null` events | `NullReferenceException` | `PreconditionViolationException` |
  | `DomainEventMapper.ToDomain<T>` with a blank `EventType` | `InvalidOperationException` | `PreconditionViolationException` |

  `DomainEventMapper.ToDomain<T>` (ezDDD.UseCase) is affected because it calls
  `DomainEventTypeMapper.GetType`. For a null `EventType` it behaves as `GetType(null)`; that row is
  read from the code, not run.
- **The 1.0.0 documentation was not accurate.** The XML documentation promised
  `ArgumentNullException` for these inputs. That held only in the three cases where the underlying
  map lookup happened to raise it. It never held for blank names, a null event, or null replay
  events. With uContract 1.0.0, the exceptions in the middle column came from code that ran after
  the skipped contract, not from an argument check.
- **Exception type**: `uContract.Exceptions.PreconditionViolationException` derives from
  `ContractViolationException`, which derives from `System.Exception`. It is not an
  `ArgumentException`.
- **Test evidence** (as of 2026-10-05): the 543 tests that existed before this decision pass with
  uContract 1.0.0 and with 2.0.0, in both configurations — none of them observed these contracts.
  Thirteen test cases now cover them (11 in `EzDdd.Entity.Tests`, 2 in `EzDdd.UseCase.Tests`).
  With the reference put back to 1.0.0, exactly those 13 fail.
- **Why it went unnoticed**: uContract 1.0.0 skipped the contracts in every build configuration,
  and no test supplied invalid input to these members.
- **Upstream**: the replay constructor of `EsAggregateRoot` in Java ezddd 6.0.1 states the same
  precondition with uContract alone (`requireNotNull("Domain events", events)`), with no
  separate argument check.
- **How uContract 2.0.0 switches contracts** (read from its source and changelog):
  - It reads its switches from environment variables once per process. For preconditions the order
    is `DBC_PRE`, then `DBC`, then the default (enabled). An empty or unrecognised value is ignored.
  - The switches are process-wide. They also switch the contracts that the application declares.
  - There is no programmatic switch
    ([uContract.NET issue #35](https://github.com/cwouyang/uContract.NET/issues/35)).
  - A contract that is reached while another contract's condition is being evaluated is skipped
    (re-entrancy guard).

### Constraints

- Invariants must be enforced in production builds (ADR-0006).
- uContract is ezDDD.NET's only runtime dependency outside the BCL
  ([ADR-0004](0004-zero-third-party-dependency-principle.md)); the decision must not add another.
- ezDDD.NET 1.0.0 is published on NuGet, so a change that callers can observe must follow Semantic
  Versioning.

---

## Decision

**ezDDD.NET depends on uContract 2.0.0 or later. Its five precondition checks are enforced by
default and throw `uContract.Exceptions.PreconditionViolationException`. The release that contains
this change is a new major version, ezDDD.NET 2.0.0.**

### Details

- **Dependency**: `src/EzDdd.Entity/EzDdd.Entity.csproj` declares
  `<PackageReference Include="uContract" Version="2.0.0" />`. This is a minimum version with no
  upper bound. ezDDD.UseCase, ezDDD.Cqrs and ezDDD.Core receive uContract
  transitively through ezDDD.Entity; ezDDD.Common does not depend on it.
- **No additional argument guards**: no `ArgumentNullException` check is added in front of the
  contracts. Each precondition is stated once, as a contract.
- **Major version**: callers see different exception types, and blank type names that
  `Register` accepted are now rejected. Both are breaking changes.
- **Disabling the checks**: `DBC_PRE=off`, or `DBC=off` when `DBC_PRE` is not set, disables the
  five checks. Nothing in ezDDD.NET overrides these switches.
- The [changelog](../../CHANGELOG.md) lists the changed members and the upgrade steps. This ADR does
  not repeat them.

---

## Consequences

### Positive Consequences

- ✅ **ADR-0006's requirement is met**: the preconditions run in Debug and Release without any
  configuration.
- ✅ **Blank type names can no longer enter the registry**: `Register<T>("")` and
  `Register<T>("  ")` used to succeed.
- ✅ **One exception type for one kind of error**: all five checks report a violated precondition
  in the same way. Before, the type depended on which line failed first after the skipped contract.
- ✅ **Documentation matches behaviour**: the XML documentation, the README, the API reference and
  the changelog now name the exception that is thrown.
- ✅ **Both build configurations are tested**: CI runs the tests in Release, the configuration the
  packages are published in, as well as Debug.

### Negative Consequences

- ❌ **BREAKING — exception types**: code that catches `ArgumentNullException`,
  `ArgumentException`, `NullReferenceException` or `InvalidOperationException` for the inputs in the
  table no longer catches them. This includes callers of `DomainEventMapper.ToDomain`.
- ❌ **BREAKING — blank names**: an application that registered an empty or whitespace type name now
  fails at registration.
- ❌ **The application's own contracts become enforced**: uContract 2.0.0 arrives transitively, so
  contracts that the application declares are enforced in Release too. For example,
  `Contract.Invariant` calls inside an aggregate's `_EnsureInvariant` now run during event replay
  ([ADR-0011](0011-event-replay-invariant-checking.md)).
  For an application that declares its own contracts, this effect can be larger than the five
  checks. When an aggregate is loaded through `EsRepository`, such a violation during replay
  surfaces as an `InvalidOperationException` whose inner exception chain (through the
  `TargetInvocationException` from the reflective constructor call) carries the contract violation.
  A project that references uContract 1.0.0 directly must move to 2.0.0.
- ❌ **The only way to disable the checks is process-wide**: `DBC_PRE` and `DBC` also switch the
  application's own contracts, and `DBC=off` also disables postconditions, invariants and checks,
  unless their own switch (`DBC_POST`, `DBC_INV`, `DBC_CHECK`) is set.
  The checks cannot be disabled for ezDDD.NET alone.
- ❌ **The tests depend on the process environment**: the 13 test cases fail in a process where
  preconditions are disabled, and a test cannot enable them from code. Contributors must run the
  tests with no `DBC*` variable set (see CONTRIBUTING).

### Neutral Consequences

- ⚖️ `EsRepository` reconstructs aggregates through the replay constructor by reflection and always
  passes a non-null list, so it never triggers the replay-constructor precondition.
- ⚖️ The five preconditions are skipped when they are reached inside the condition of another
  uContract contract, because of uContract's re-entrancy guard. The XML documentation says so.
- ⚖️ `DomainEventTypeMapper.Contains` returns `false` for a blank name without reading the registry.
  It cannot show whether a blank name was registered; `GetAllMappings()` can.
- ⚖️ An application that set `DBC=on` only to make these checks run can remove the setting.
- ⚖️ A later uContract major version can be adopted by a consumer without a new ezDDD.NET release,
  because the reference has no upper bound. ezDDD.NET does not test against such a version in
  advance.

---

## Alternatives Considered

### Alternative 1: Add `ArgumentNullException` guards in front of the contracts

**Description**: Keep the exception type that the 1.0.0 documentation named by adding
`ArgumentNullException.ThrowIfNull` (and an equivalent blank-name check) before each
`Contract.Require`.

**Pros**:
- `catch (ArgumentNullException)` blocks keep working for the three cases that raised it before
- Familiar .NET convention for argument validation

**Cons**:
- Each condition is checked twice, and the contract behind the guard can never fail
- Departs from ADR-0006 (contracts state the preconditions) and from Java ezddd, which states the
  replay precondition with uContract only
- It is still a breaking change: blank names, a null event and null replay events never produced
  `ArgumentNullException`

**Why rejected**: The documented type never held for all inputs, so this alternative preserves
nothing complete. It adds duplicate checks and still changes behaviour.

---

### Alternative 2: Release as a minor or patch version

**Description**: Treat the change as a fix that makes the documented checks run, and release it as
1.0.1 or 1.1.0.

**Pros**:
- Consumers receive the correction through a routine update
- No major-version migration to announce

**Cons**:
- Callers that catch `ArgumentNullException` or `InvalidOperationException` break without warning
- Applications that registered blank names fail at startup after a patch update
- The application's own contracts start to throw in Release after a patch update

**Why rejected**: Callers can observe the change, and it can stop a working application. Semantic
Versioning requires a major version for that.

---

### Alternative 3: Bounded version range `[2.0.0,3.0.0)`

**Description**: Declare an upper bound so that ezDDD.NET is used only with the uContract major
version it was tested with.

**Pros**:
- A future uContract 3.0.0 cannot change ezDDD.NET's behaviour unnoticed

**Cons**:
- A consumer who needs a later uContract major version cannot adopt it until ezDDD.NET releases
- ezDDD.NET must publish a release for every uContract major version, even when nothing it uses
  changed

**Why rejected**: ezDDD.NET uses one uContract method (`Contract.Require`). Blocking consumers
costs more than the protection is worth.

---

### Alternative 4: Stay on uContract 1.0.0 and document `DBC=on`

**Description**: Keep the dependency and tell users to set `DBC=on`.

**Pros**:
- No breaking change and no new release of the dependency chain

**Cons**:
- The default stays unsafe: a consumer who reads nothing gets no checks
- Contradicts ADR-0006, which requires enforcement in all builds
- The 1.0.0 documentation stays wrong for the default configuration

**Why rejected**: A safety check that needs an opt-in does not meet ADR-0006.

---

## Related Decisions

- **Amends**: [ADR-0006](0006-ucontract-integration-design-by-contract.md) - uContract.NET
  Integration for Design by Contract — the choice of uContract.NET stands. The dependency version
  (1.0.0) and the statement that ezDDD.NET 1.x depends on uContract.NET 1.x are replaced by this
  decision. ADR-0006 already expected that a breaking change in uContract.NET could require a major
  version of ezDDD.NET.
- **Related to**: [ADR-0004](0004-zero-third-party-dependency-principle.md) - Zero Third-Party
  Dependency Principle — uContract remains the single ecosystem dependency; only its version
  changes.
- **Related to**: [ADR-0010](0010-esaggregate-root-event-sourcing-implementation.md) -
  EsAggregateRoot Event Sourcing Implementation — defines the replay constructor whose
  precondition is now enforced, and the `_EnsureInvariant` hook in which applications declare
  contracts.
- **Related to**: [ADR-0011](0011-event-replay-invariant-checking.md) - Event Replay and Invariant
  Checking — application contracts inside `_EnsureInvariant` now run during replay in every build
  configuration.

ADR-0004, ADR-0010 and ADR-0011 are accepted and immutable, so they carry no reference back to this
ADR.

---

## Implementation Notes

- **Tests**: `DomainEventTypeMapperTests` and `EsAggregateRootTests` (`EzDdd.Entity.Tests`) and
  `DomainEventMapperTests` (`EzDdd.UseCase.Tests`) assert `PreconditionViolationException` for the
  inputs in the table. The blank-name registration test reads `GetAllMappings()`.
- **CI**: `.github/workflows/build-and-test.yml` builds and tests in Release after Debug.
- **Contributor guidance**: CONTRIBUTING tells contributors to run the tests with no `DBC*`
  environment variable set.
- **Version number**: the package version is set when the release is prepared. This ADR fixes
  only that the number is 2.0.0.
- **If uContract gains a programmatic switch** (issue #35), the tests can enable preconditions
  themselves, and the dependency on the process environment can be removed.

---

## References

- [CHANGELOG](../../CHANGELOG.md) — the entry for this change (released as 2.0.0) lists every changed
  member and the upgrade notes
- [uContract.NET 2.0.0 changelog](https://github.com/cwouyang/uContract.NET/blob/v2.0.0/CHANGELOG.md)
- [uContract.NET ADR-0020: Contracts enabled by default](https://github.com/cwouyang/uContract.NET/blob/v2.0.0/docs/adr/0020-contracts-enabled-by-default.md)
- [uContract.NET issue #32](https://github.com/cwouyang/uContract.NET/issues/32) — contracts
  disabled by default for all package consumers
- [uContract.NET issue #35](https://github.com/cwouyang/uContract.NET/issues/35) — programmatic
  switch for contract configuration
- [uContract.NET README, environment variables](https://github.com/cwouyang/uContract.NET#environment-variables)
- [Semantic Versioning 2.0.0](https://semver.org/spec/v2.0.0.html)

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2026-10-05 | Accepted    | Initial decision               |

---
