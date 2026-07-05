# ADR-0028: Reactor Type Hierarchy and Projector/Notifier Genericization

## Status

**Accepted**

- **Date**: 2026-07-04
- **Deciders**: Development Team
- **Status Date**: 2026-07-04

---

## Context

### Problem Statement

Java ezddd 5.0.0 (commit `b7a336f`) introduced a reactor type hierarchy on the query side:
`Notifier<Input> extends Reactor<Input>` was added, and `Projector` — previously a pure
marker interface — became `Projector<Input> extends Reactor<Input>`. How should ezDDD.NET
align with this hierarchy, given that the .NET side no longer has an `IReactor` interface?

### Relevant Context

- **Upstream change**: Java `b7a336f` (5.0.0) made `Reactor<Input>` the common supertype for
  event-handling in-ports. `Projector<Input>` writes read models; `Notifier<Input>` converts
  internal domain events into external domain events (integration events) and dispatches them
  outward. Upstream also reworded the `Reactor` javadoc, removing "updating an aggregate"
  from the list of side effects.
- **.NET over-removal**: During Phase 6 Stage S3 (commit `2f9235b`, IMessageBus removal),
  the .NET side removed `IReactor` together with the message bus infrastructure. Upstream,
  however, kept `Reactor` at HEAD (`3aac0f5`) — it is an in-port abstraction independent of
  the removed messaging infrastructure. `IReactor` must therefore be re-added.
- **ADR-0020** decided `IProjector` is a pure marker interface with zero methods, with
  implementations separately implementing `IReactor` (Phase 3 era) and `BackgroundService`.
  The upstream genericization invalidates the "pure marker" part of that decision.
- **Pre-1.0 status**: ezDDD.NET has not been published to NuGet, so this breaking change
  can be absorbed into the initial 1.0.0 release (same rationale as Phase 6).

### Constraints

- Semantic parity with Java ezddd HEAD (`3aac0f5`) — Phase 7 targets ≥98%
- Async/await for all I/O operations (ADR-0016)
- Zero third-party dependencies (ADR-0004); interfaces only, no infrastructure coupling
- Naming/idiom rules of prior phases: `I` prefix, `TInput` type parameter, contravariant `in`

---

## Decision

**Re-add `IReactor<in TInput>` with `Task ExecuteAsync(TInput input)` in EzDdd.UseCase, and
make both query-side reactors extend it: `IProjector<in TInput> : IReactor<TInput>` (breaking:
was a non-generic marker) and the new `INotifier<in TInput> : IReactor<TInput>`.**

### Details

#### IReactor (EzDdd.UseCase.Port.In, file at `Port/In/IReactor.cs`)

```csharp
public interface IReactor<in TInput>
{
    Task ExecuteAsync(TInput input);
}
```

- **D1 — async `ExecuteAsync`**: Java's `Reactor.execute(Input)` is synchronous (`void`).
  The .NET port uses `Task ExecuteAsync(TInput)` per the Phase 3 precedent (the original
  `IReactor` was already async) and ADR-0016: reactor side effects are I/O-bound — projectors
  write read models to a query database, notifiers dispatch events to external systems.
- **Contravariant `in TInput`**: a reactor of a base message type can serve wherever a
  reactor of a derived message type is expected (ADR-0021 variance rules).
- The file lives at `Port/In/IReactor.cs` — its original pre-removal location.
  The .NET port flattens the upstream `port/in/interactor` package into `Port/In/`,
  consistent with the sibling in-port types (`IUseCase`, `IReconciler`, `IInput`, etc.)
  that map to the same Java package, keeping namespace and folder aligned.

#### IProjector (EzDdd.Cqrs.Query) — BREAKING

```csharp
public interface IProjector<in TInput> : IReactor<TInput>
{
}
```

- Mirrors upstream `Projector<Input> extends Reactor<Input>`.
- Implementors now must provide `ExecuteAsync(TInput)`; the compiler enforces the event
  handling contract that ADR-0020 previously left as a documentation-only convention.
- Lifecycle management guidance from ADR-0020 is unchanged: implementations still pair
  with `BackgroundService`/`IHostedService` in the infrastructure layer; `IProjector<TInput>`
  itself stays free of hosting concerns.

#### INotifier (EzDdd.Cqrs.Query) — NEW

```csharp
public interface INotifier<in TInput> : IReactor<TInput>
{
}
```

- Mirrors upstream `Notifier<Input>` (since 5.0.0): receives internal domain events,
  converts them into external domain events (integration events), and dispatches them
  through an out-port to front-ends, downstream bounded contexts, or external systems
  (such as Kafka).
- Upholds the Clean Architecture cross-layer principle: entities-layer objects must not
  leave the use cases layer and travel outward directly.

---

## Consequences

### Positive Consequences

- ✅ **Semantic parity restored**: matches Java HEAD `3aac0f5` reactor hierarchy (Projector,
  Notifier, Reactor) one-to-one
- ✅ **Compile-time contract**: projector/notifier implementations must implement
  `ExecuteAsync`, instead of relying on documented convention
- ✅ **Corrects Phase 6 over-removal**: `IReactor` returns as an in-port abstraction,
  decoupled from the removed message bus infrastructure
- ✅ **Explicit integration-event role**: `INotifier<TInput>` names the previously implicit
  "publish outward" responsibility

### Negative Consequences

- ❌ **Breaking change**: `IProjector` (non-generic marker) → `IProjector<TInput>` with a
  required method; all existing implementors need updating (acceptable pre-1.0, absorbed
  into the initial 1.0.0 release)
- ❌ **Signature divergence from Java**: `execute` vs `ExecuteAsync` (deliberate, per
  ADR-0016; same divergence as `IUseCase`)

### Neutral Consequences

- ⚖️ Marker-interface simplicity is traded for an enforced contract — type checks like
  `x is IProjector<T>` now require the input type argument
- ⚖️ `INotifier`/`IProjector` remain behaviorally identical at the interface level; the
  distinction is semantic (read-model writing vs outward event dispatch)

---

## Alternatives Considered

### Alternative 1: Synchronous `void Execute(TInput input)` (literal Java parity)

**Description**: Port `Reactor.execute` verbatim as a synchronous method.

**Pros**:
- Signature-level parity with Java
- No `Task` allocation for trivially synchronous reactors

**Cons**:
- Reactor side effects (read-model writes, external dispatch) are I/O-bound in .NET
- Violates ADR-0016 (async/await throughout) and the Phase 3 `IReactor` precedent
- Forces sync-over-async in real implementations

**Why rejected**: The .NET port consistently makes I/O-bound ports async; the original
Phase 3 `IReactor` had already made this exact adaptation (D1).

---

### Alternative 2: Keep `IProjector` as a non-generic marker

**Description**: Ignore the upstream genericization; keep the ADR-0020 pure-marker design.

**Pros**:
- No breaking change
- Marker simplicity

**Cons**:
- Diverges from upstream 5.0.0+ API shape (fails the Phase 7 parity target)
- Event handling contract stays convention-only, not compiler-enforced
- `INotifier` would have no common supertype with `IProjector`

**Why rejected**: Phase 7's purpose is HEAD parity before the 1.0.0 release; pre-1.0 is
the only time this breaking change is free.

---

### Alternative 3: Reactor hierarchy without re-adding `IReactor`

**Description**: Give `IProjector<TInput>` and `INotifier<TInput>` each their own
`ExecuteAsync` without a shared base interface.

**Pros**:
- One fewer public type

**Cons**:
- Loses the upstream `Reactor` abstraction that unifies event-handling in-ports
- Duplicate method declarations; no polymorphic handling of reactors
- Would repeat the Phase 6 over-removal mistake

**Why rejected**: Upstream keeps `Reactor` at HEAD as the common in-port supertype;
removing it was an over-removal, not a design decision.

---

## Related Decisions

- **Supersedes**: [ADR-0020](0020-iprojector-lifecycle-management.md) - IProjector Lifecycle
  Management Integration — the "pure marker interface" decision is replaced by the generic
  reactor contract; ADR-0020's lifecycle-separation guidance (pair with
  `BackgroundService`/`IHostedService` in infrastructure, keep hosting out of the domain
  interface) remains applicable and is carried forward
- **Related to**: [ADR-0025](0025-messageproducer-refactoring-java-4-1-0-alignment.md) -
  MessageProducer Refactoring — the Phase 6 S3 commit (`2f9235b`) implementing that decision
  also removed `IReactor`, which this ADR re-adds
- **Related to**: [ADR-0016](0016-async-await-throughout.md) - Async/Await Throughout —
  basis for D1 (`ExecuteAsync` instead of `execute`)
- **Related to**: [ADR-0021](0021-generic-variance-annotations.md) - Generic Variance
  Annotations — basis for contravariant `in TInput`

---

## Implementation Notes

- `src/EzDdd.UseCase/Port/In/IReactor.cs` — re-added at its original pre-removal
  location (namespace `EzDdd.UseCase.Port.In`)
- `src/EzDdd.Cqrs/Query/IProjector.cs` — genericized; XML docs revised (marker/lifecycle
  passages updated, example rewritten to the `ExecuteAsync` contract)
- `src/EzDdd.Cqrs/Query/INotifier.cs` — new, javadoc translated from upstream `Notifier.java`
- Test implementors updated: `AccountProjector : IProjector<DomainEventData>` (its existing
  `ExecuteAsync(DomainEventData)` now implements the interface method);
  `ProjectorTests`/`NotifierTests` lock the type hierarchy and contravariance at compile time
- No behavioral tests added (upstream `b7a336f` added none)

---

## References

- **Java ezddd commit `b7a336f`** (5.0.0): "Add Notifier... revise Projector to extend Reactor"
- **Java ezddd HEAD `3aac0f5`**: `ezddd-usecase/.../port/in/interactor/Reactor.java`,
  `ezcqrs/.../cqrs/usecase/query/Projector.java`, `ezcqrs/.../cqrs/usecase/query/Notifier.java`
- **ezDDD.NET commit `2f9235b`** (Phase 6 S3): IMessageBus removal that also removed `IReactor`
- Phase 7 synchronization plan (internal working note, not retained) — P.2 decision D1, iteration I1

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2026-07-04 | Accepted    | Initial decision (Phase 7 I1)  |

---
