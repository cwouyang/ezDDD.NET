# Phase 7 Semantic Parity Report (P.6 Spot-Check)

> **Date**: 2026-07-04
> **Baseline**: Java ezddd 6.0.1 (commit `3aac0f5`, upstream HEAD 2026-07-03)
> **Previous baseline**: Java ezddd 4.1.0 (commit `91fac63`)
> **Scope**: The five substantive upstream changes between 4.1.0 and 6.0.1, verified by
> reading both codebases side by side (not by comparing planning documents).
> **Plan**: [PHASE7_SYNC_PLAN.md](PHASE7_SYNC_PLAN.md) · **ADRs**: ADR-0028, ADR-0029

---

## Verification Method

For each upstream change, the Java source at `3aac0f5` and the .NET source at the
Phase 7 I4 commit (`c8bdb5b`) were read directly and compared for:
signature/contract equivalence, control-flow equivalence, exception/null behavior,
and test coverage equivalence. Intentional .NET platform idioms (async, NRT,
generic variance) are noted separately and do not reduce parity, consistent with
the methodology used in prior phase verifications.

---

## 1. Reactor Hierarchy: `Notifier` + `Projector<Input>` (Java 5.0.0, `b7a336f`)

| | Java (`3aac0f5`) | .NET (Phase 7 I1, `9683b36`) |
|---|---|---|
| Reactor | `Reactor<Input>` in `usecase.port.in.interactor`, single method `void execute(Input input)` | `IReactor<in TInput>` in `EzDdd.UseCase.Port.In`, single method `Task ExecuteAsync(TInput input)` |
| Projector | `Projector<Input> extends Reactor<Input>` (empty body, `ezcqrs` query package) | `IProjector<in TInput> : IReactor<TInput>` (empty body, `EzDdd.Cqrs.Query`) |
| Notifier | `Notifier<Input> extends Reactor<Input>` (empty body, `ezcqrs` query package) | `INotifier<in TInput> : IReactor<TInput>` (empty body, `EzDdd.Cqrs.Query`) |

**Behavioral comparison**:
- Type hierarchy identical: both `Projector`/`Notifier` are empty specializations of
  `Reactor`, contributing role semantics only. Doc comments ported faithfully
  (Notifier's Clean Architecture cross-layer note, Reactor's idempotency note).
- Module placement identical: Reactor in usecase core, Projector/Notifier in the CQRS
  module. Java's `port.in.interactor` package is flattened into `Port/In/` per the
  project's established package-flattening convention (pre-Phase-6 precedent for this
  exact file location).

**Intentional improvements (no parity impact)**:
- `void execute` → `Task ExecuteAsync` — async per Phase 3 precedent (reactor work is
  I/O: writing read models, dispatching notifications).
- Contravariant `in TInput` — .NET generic variance idiom.

**Parity: 100%**

## 2. `ExternalDomainEventDto` Removal (`dc4b2e1`)

- **Java**: class deleted from `usecase.port.inout.domainevent`; `git ls-files` at
  `3aac0f5` confirms no `ExternalDomainEventDto.java` remains.
- **.NET**: `git ls-files` confirms no `ExternalDomainEventDto.cs` exists — and none ever
  did (the class was deferred during the original port; a contrary "100% parity" claim in
  an earlier internal class-by-class verification note was wrong and has been corrected in
  this iteration).

**Result**: absent on both sides at the current baseline — the deferred-port gap closed
itself upstream. Confirmed no-op in Phase 7 I2.

**Parity: 100%** (vacuous — no artifact on either side)

## 3. `ExternalDomainEventPublisher<E>` (`f440d15`)

| | Java | .NET (Phase 7 I2, `ffb304e`) |
|---|---|---|
| Declaration | `interface ExternalDomainEventPublisher<E extends ExternalDomainEvent>` | `interface IExternalDomainEventPublisher<in TEvent> where TEvent : IExternalDomainEvent` |
| Method | `void publish(E event)` | `Task PublishAsync(TEvent @event)` |
| Location | `usecase.port.inout.messaging` | `EzDdd.UseCase.Port.InOut.Messaging` |

**Behavioral comparison**:
- Generic constraint equivalent: Java `E extends ExternalDomainEvent` (which extends
  `DomainEvent`) ↔ .NET `where TEvent : IExternalDomainEvent` (which extends
  `IDomainEvent`).
- Single-method out-port contract identical; package placement identical.

**Intentional improvements (no parity impact)**:
- `publish` → `PublishAsync` returning `Task` — publishing to brokers/BCs is inherently
  I/O-bound (D2 in the Phase 7 plan).
- Contravariant `in TEvent`.

**Parity: 100%**

## 4. `MessageProducer` Moved Out of Core (Java 6.0.0, `67686ac`)

- **Java**: `67686ac` deletes `usecase/port/inout/messaging/MessageProducer.java`
  (16 lines) from the core; the interface now lives in the separate **ezddd-gateway**
  artifact (root pom at `3aac0f5` pins `ezddd-gateway.version = 3.0.2`).
  `PostEventFailureException` **remains** in the Java usecase core.
- **.NET** (Phase 7 I3, `1b9b1e4`): `IMessageProducer`, `InMemoryMessageProducer`, and
  their tests removed from `EzDdd.UseCase`; `PostEventFailureException` **retained** in
  core (matches Java). `examples/EventInfrastructure/IMessageProducer.cs` carries a
  minimal local producer abstraction for the EventStoreRelay example — analogous to
  Java's sample modules (`ezddd-core-sample`, `ezcqrs-sample`) carrying their own
  `InMemoryMessageProducer`; examples are not compiled into or packed with the core.

**Core public API surface now matches**: no `MessageProducer` type in either core.

**Documented deviation (distribution, not semantics)**: Java users can depend on the
published ezddd-gateway artifact; a .NET **ezDDD.Gateway** package is deferred until
after 1.0 (ADR-0029). This affects ecosystem availability, not core behavior.

**Parity: 100%** (core semantics; gateway deferral recorded in ADR-0029)

## 5. `OutboxRepository.findById` Soft-Delete Filter (Java 6.0.1, `3aac0f5`)

| Step | Java `findById(ID id)` | .NET `FindByIdAsync(TId id)` (Phase 7 I4, `c8bdb5b`) |
|---|---|---|
| Null guard | `requireNotNull("id", id)` | — (relies on NRT; see deviation note) |
| Load | `repositoryPeer.findById(id.toString())` | `await _peer.FindByIdAsync(id)` |
| Missing | `Optional.empty()` | `null` |
| Map | `mapper.toDomain(data.get())` | `_mapper.ToDomain(data)` |
| **Deleted filter** | `if (aggregate.isDeleted()) return Optional.empty();` | `if (aggregate.IsDeleted) return null;` |
| Found | `Optional.of(aggregate)` | `return aggregate;` |

**Behavioral comparison**:
- The bug-fix control flow is line-for-line equivalent: load → empty check → map →
  **IsDeleted check → empty** → return. `Optional<T>`/`T?` mapping is the project-wide
  convention.
- **Test parity**: Java `OutboxRepositoryFindByIdTest` has two behavioral cases
  (deleted → empty; not deleted → present). .NET `OutboxRepositoryFindByIdTests` ports
  both (`FindByIdAsync_WhenAggregateIsDeleted_ReturnsNull`,
  `FindByIdAsync_WhenAggregateIsNotDeleted_ReturnsAggregate`) with equivalent in-memory
  peer/mapper/aggregate test fixtures. XML doc on the .NET test cites the source commit.
- Peer key typing differs by prior design: Java peers are keyed by `String`
  (`id.toString()`); .NET peers are keyed by `TId` (typed, pre-existing intentional
  improvement — unchanged by this fix).

**Minor deviation (noted)**: Java's `findById` has a runtime `requireNotNull` on `id`;
the .NET method relies on nullable reference types instead of a runtime throw (the .NET
class does use `ArgumentNullException.ThrowIfNull` in the constructor, `SaveAsync`, and
`DeleteAsync`). Behavior differs only for a null id passed in defiance of NRT
annotations. Candidate one-line hardening for a future tidy commit; does not affect the
ported bug-fix semantics.

**Parity: 98%**

---

## Conclusion

| # | Upstream change | Parity |
|---|---|---|
| 1 | Reactor hierarchy (`Projector<Input>`, `Notifier<Input>`) | 100% |
| 2 | `ExternalDomainEventDto` removal | 100% (no-op) |
| 3 | `ExternalDomainEventPublisher<E>` | 100% |
| 4 | `MessageProducer` out of core | 100% (gateway package deferred, ADR-0029) |
| 5 | `OutboxRepository.findById` deleted-aggregate filter | 98% |

**Overall spot-check parity: ~99.6% — target of ≥98% met.** ✅

Verification environment: `dotnet build ezDDD.sln --no-incremental` — 0 errors /
0 warnings; `dotnet test` — 543/543 passing (Common 69 / Entity 92 / UseCase 283 /
Cqrs 71 / Integration 28), 2026-07-04.
