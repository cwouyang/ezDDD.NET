# ADR-0029: MessageProducer Removal from Core & Gateway Package Deferral

## Status

**Accepted**

- **Date**: 2026-07-04
- **Deciders**: Development Team
- **Status Date**: 2026-07-04

---

## Context

### Problem Statement

Java ezddd 6.0.0 (commit `67686ac`, "[Refactoring] Move MessageProducer interface to
ezddd-gateway") removed the `MessageProducer` interface from the core library and relocated
it to the external `ezddd-gateway` artifact. The upstream core no longer defines a message
producer abstraction at HEAD (`3aac0f5`). ezDDD.NET, however, still carries
`IMessageProducer<TMessage>` and `InMemoryMessageProducer<TMessage>` in
`EzDdd.UseCase.Port.InOut.Messaging` — a module-boundary divergence from upstream 6.0.

How should ezDDD.NET align with this boundary, given that no .NET counterpart of
`ezddd-gateway` exists and the 1.0.0 release scope is already fixed?

### Relevant Context

- **Upstream change**: `67686ac` (6.0.0) deletes `MessageProducer.java` from
  `ezddd-usecase/.../port/inout/messaging/` — a pure move to `ezddd-gateway`; the interface
  shape is unchanged, only its home artifact changes.
- **ADR-0025** (Phase 6 S3) established `IMessageProducer` as the primary messaging port in
  core, replacing the MessageBus pattern, together with `InMemoryMessageProducer` for testing.
  The upstream 6.0 move partially invalidates that placement decision (the producer-only
  pattern itself remains upstream's design — just outside core).
- **Repository decoupling already done**: `EsRepository`/`OutboxRepository` have no
  `IMessageProducer` dependency (Relay pattern; see ADR-0025). Core production code has
  zero usages of the interface — only tests and the `examples/EventInfrastructure/` relay
  example consume it.
- **No .NET gateway exists**: `ezddd-gateway` has no .NET port, and creating one now would
  expand the 1.0.0 scope (HANDOFF P0-3: release 1.0.0 at upstream HEAD parity).
- **Pre-1.0 status**: ezDDD.NET has not been published to NuGet; deleting a public type is
  free of migration cost for external users (same rationale as ADR-0025/ADR-0028).

### Constraints

- Semantic parity with Java ezddd HEAD (`3aac0f5`) — Phase 7 targets ≥98%
- 1.0.0 release scope must not grow (no new packages)
- Examples must stay conceptually correct without being compiled into the solution

---

## Decision

**Delete `IMessageProducer<TMessage>` and `InMemoryMessageProducer<TMessage>` from the core
library (EzDdd.UseCase), defer the ezDDD.Gateway package (the .NET counterpart of
`ezddd-gateway`) to post-1.0, and let the `examples/EventInfrastructure/` relay example carry
its own minimal producer abstraction in the meantime.**

### Details

- **Removed from core** (with their tests):
  - `src/EzDdd.UseCase/Port/InOut/Messaging/IMessageProducer.cs`
  - `src/EzDdd.UseCase/Port/InOut/Messaging/InMemoryMessageProducer.cs`
  - `tests/EzDdd.UseCase.Tests/Port/InOut/Messaging/MessageProducerTests.cs`
  - `tests/EzDdd.UseCase.Tests/Port/InOut/Messaging/InMemoryMessageProducerTests.cs`
  - `tests/EzDdd.Integration.Tests/MessageProducerResourceTests.cs`
- **`EzDdd.UseCase.Port.InOut.Messaging` namespace survives**: it still hosts
  `IExternalDomainEventPublisher<TEvent>` (ported from upstream `f440d15` in Phase 7 I2),
  matching upstream, which likewise keeps its `port/inout/messaging` package.
- **ezDDD.Gateway deferred**: when upstream's `ezddd-gateway` contract stabilizes and demand
  exists, a separate `ezDDD.Gateway` NuGet package will provide the official
  `IMessageProducer<TMessage>` (post-1.0; new minor release, non-breaking).
- **Example self-sufficiency**: `examples/EventInfrastructure/IMessageProducer.cs` declares a
  minimal `IMessageProducer<in TMessage> : IDisposable` with `Task PostAsync(TMessage)`,
  annotated to state that core no longer ships this abstraction and that ezDDD.Gateway will.
  `EventStoreRelay` references this local declaration.
- Integration tests that used `InMemoryMessageProducer` merely as an in-memory event sink
  were rewritten to collect published events in a plain `List<DomainEventData>`; tests whose
  subject was the producer implementation itself were deleted with it.

---

## Consequences

### Positive Consequences

- ✅ **Module-boundary parity with upstream 6.0**: core contains exactly the ports upstream
  core contains; the gateway concern lives outside, as in Java
- ✅ **Smaller core API surface**: one less abstraction (plus its in-memory implementation)
  to document, version, and support in 1.0.0
- ✅ **1.0.0 scope protected**: no new package added under release pressure
- ✅ **Free breaking change**: pre-NuGet, no external consumers to migrate

### Negative Consequences

- ❌ **BREAKING**: public types `IMessageProducer<TMessage>` and
  `InMemoryMessageProducer<TMessage>` are deleted from EzDdd.UseCase. Any code written
  against pre-release builds must either (a) declare its own minimal producer abstraction
  (as the EventStoreRelay example now does — a 4-line interface) or (b) wait for the
  ezDDD.Gateway package post-1.0
- ❌ **No official producer port at 1.0.0**: applications integrating message brokers define
  the port themselves until ezDDD.Gateway ships; abstractions may diverge slightly between
  applications in the interim

### Neutral Consequences

- ⚖️ The producer-only pattern established by ADR-0025 remains the recommended design; only
  its packaging home changed (core → application/gateway layer)
- ⚖️ `InMemoryMessageProducer`'s testing convenience is trivially reproducible with a
  `List<T>`-backed fake, as the rewritten integration tests demonstrate

---

## Alternatives Considered

### Alternative 1: Keep `IMessageProducer` in core

**Description**: Retain the interface and in-memory implementation in EzDdd.UseCase,
ignoring the upstream move.

**Pros**:
- No breaking change for pre-release users
- Testing convenience of `InMemoryMessageProducer` preserved

**Cons**:
- Diverges from the upstream 6.0 module boundary at the exact release meant to be
  HEAD-parity 1.0.0
- Core production code has zero usages — the type would be a dangling port
- Removing it after 1.0.0 would require a major version bump

**Why rejected**: Phase 7's purpose is HEAD parity before 1.0.0; keeping a port that
upstream deliberately evicted from core would freeze the divergence into the public API.

---

### Alternative 2: Create the ezDDD.Gateway package now

**Description**: Immediately port `ezddd-gateway` as a sixth NuGet package containing
`IMessageProducer<TMessage>`.

**Pros**:
- Full artifact-level parity with upstream, not just core parity
- Users get the official producer port at 1.0.0

**Cons**:
- Expands the 1.0.0 scope (new package: metadata, docs, CI, versioning, parity tracking)
- Upstream `ezddd-gateway` is young; porting now risks chasing an unstable contract
- No current .NET consumer demands it (core itself does not use the port)

**Why rejected**: HANDOFF P0-3 fixes the 1.0.0 scope at core-parity. A one-interface package
adds release overhead without present value; it can ship post-1.0 as a non-breaking addition.

---

## Related Decisions

- **Amends**: [ADR-0025](0025-messageproducer-refactoring-java-4-1-0-alignment.md) -
  MessageProducer Refactoring — the producer-only pattern and Relay guidance stand, but the
  "IMessageProducer/InMemoryMessageProducer live in core" placement is replaced by this
  decision (upstream 6.0 moved the interface to ezddd-gateway)
- **Related to**: [ADR-0028](0028-reactor-hierarchy-projector-notifier-genericization.md) -
  Reactor Type Hierarchy — the sibling Phase 7 alignment decision; together they bring the
  messaging/query in-out ports to HEAD (`3aac0f5`) shape
- **Related to**: [ADR-0012](0012-resource-management-event-bus-producers.md) - Resource
  Management for Event Bus Producers — the IDisposable guidance carries over to the
  example-local abstraction and to the future ezDDD.Gateway interface

---

## Implementation Notes

- `src/EzDdd.Cqrs/Command/ICommand.cs` — XML docs no longer reference
  `IMessageProducer{TMessage}`; the extensibility list now describes relay-based event
  publication (Transactional Outbox)
- `src/EzDdd.UseCase/EzDdd.UseCase.csproj` — stale Features lines (`BlockingMessageBus`,
  `EventBusProducer`, both removed in Phase 6 S3) and the `message-bus` package tag cleaned
- Integration test rewrites: `CqrsFlowWithMetadataTests`, `EventSourcingMetadataTests`
  (producer → `List<DomainEventData>` sink), `CompleteCqrsFlowTests` (events flow directly
  to the projector), `ConcurrentOperationsTests` (2 producer-specific tests removed, unused
  producer instances dropped)
- `examples/EventInfrastructure/` — new `IMessageProducer.cs`; `EventStoreRelay.cs` and
  `README.md` updated to reference the example-local abstraction and this ADR

---

## References

- **Java ezddd commit `67686ac`** (6.0.0): "[Refactoring] Move MessageProducer interface to
  ezddd-gateway" — deletes `ezddd-usecase/.../port/inout/messaging/MessageProducer.java`
- **Java ezddd HEAD `3aac0f5`**: core contains no message producer abstraction
- Phase 7 synchronization plan (internal working note, not retained) — P.2 decision D3, iteration I3
- Prior decoupling of repositories from the producer port was recorded in an internal
  session handoff note (superseded by this ADR)

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2026-07-04 | Accepted    | Initial decision (Phase 7 I3)  |

---
