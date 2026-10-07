# ADR-0031: Alignment with Java ezddd 9.0.1

## Status

**Accepted**

- **Date**: 2026-10-06
- **Deciders**: Development Team
- **Status Date**: 2026-10-06

---

## Context

### Problem Statement

ezDDD.NET 2.0.0 is a port of Java ezddd 6.0.1 (commit `3aac0f5`). Upstream has since released 7.0.0, 7.1.0, 8.0.0,
9.0.0 and 9.0.1 (commit `aa7a99c`). These releases change the shape of the use-case layer and add a framework-level
answer to a gap that ezDDD.NET users also have: under at-least-once delivery, nothing stops an event from being
projected twice. `IArchive` upsert idempotency ([ADR-0023](0023-archive-idempotency-requirements.md)) only makes
repeated writes converge; it does not stop an accumulating projection (a balance, a counter) from counting an event
twice. How should ezDDD.NET adopt the upstream changes, and where must it deviate?

### Relevant Context

Upstream changes from 6.0.1 to 9.0.1, with the rationale given in the upstream commit messages:

- **7.0.0** — `CqrsOutput` is renamed `DefaultOutput` and moved from the ezcqrs module to the use-case module.
  `Reactor` becomes a `UseCase` that returns a `DefaultOutput`, and `Notifier` and the use-case `Projector` follow the
  same shape instead of the former `void execute(Input)` contract. `Command` and `Query` bound their output on
  `DefaultOutput`. The release also moves the Java compiler to 21. A follow-up commit uses the raw `DefaultOutput` type
  in the use-case interfaces.
- **7.1.0** — the `IGNORE` and `REJECT` exit codes, with `ignore()` and `reject()` on `Output` and `DefaultOutput`, "so
  use cases can report that a request was ignored or rejected instead of merely failing". Javadoc now states that
  `Reactor`, `Notifier` and `Projector` are siblings, not subtypes of `Reactor`.
- **8.0.0** — `UseCaseDecorator` (a base class that implements the in-port it decorates, so that a cross-cutting
  concern can be added without touching use cases) and `DomainEventDataInput` (one shared input carrying a
  `DomainEventData`, replacing the nested `DefaultInput` of each message-driven use case). `IdempotentDecorator` turns
  "recognize events already handled" into a concern of its own: an `IdempotentIdParser` extracts the data id and an
  `Inquiry` answers from an `IdempotentInquiryInput`. The entities layer of the query side gains `ReadModel`,
  `ReadValue`, and a pure-function `Projector` ("loading and saving stay in the use cases layer, so the projection
  logic itself can be unit-tested without any infrastructure"); the use-case `Projector` is removed, because once the
  projection logic moved out the in-port "has nothing left of its own" — the use case that receives the event is
  written as a `Reactor`.
- **9.0.0** — `ReadModel` becomes a class that carries an event deduplication record, so the record "is written and
  read back together with the model, and therefore always describes the state it accompanies". `IdempotentIdParser`
  returns an `Optional`, so a use case can report events it does not handle; `occurredOn` is removed from
  `IdempotentInquiryInput` because "whether an event has already been applied is answered by the recorded event ids".
- **9.0.1** — a no-argument `ReadModel` constructor, and the default capacity raised from 10 to 50 to cover more than
  two of ezddd-gateway's default 20-event poll batches.

Other context:

- ezDDD.NET 2.0.0 is published on NuGet. Every breaking change needs an upgrade note and lands in a major version.
  The changes ship under `[Unreleased]`; the version number is set when the release is prepared.
- [ADR-0022](0022-read-model-design-patterns.md) made positional records the read-model pattern. Upstream's
  `ReadModel` is an abstract class with reference equality.
- [ADR-0015](0015-cross-platform-dto-structure.md) keeps serialized shapes compatible with Java where they cross
  platforms.
- ezDDD.NET has no gateway package ([ADR-0029](0029-messageproducer-removal-gateway-deferral.md)), so upstream's
  capacity sizing does not transfer directly.

### Constraints

- Semantic parity with Java ezddd at the new baseline (`aa7a99c`), with deviations only where C# or the .NET
  conventions of earlier ADRs require them.
- Zero third-party dependencies ([ADR-0004](0004-zero-third-party-dependency-principle.md)); serialization uses
  System.Text.Json.
- Async for I/O ([ADR-0016](0016-async-await-throughout.md)).
- Level Z analyzers with warnings as errors: no public nested types (CA1034), no visible protected fields (CA1051).
- PublicAPI baselines must record every added and removed member.

---

## Decision

**Align ezDDD.NET with Java ezddd 9.0.1 in one breaking change set: rename and move the output type, make reactors and
notifiers sibling use cases, remove the use-case projector, and add the decorator, idempotency, and read-model types —
with the eleven decisions below and the deviations listed in Details. Concurrency and atomicity limits inherited from
upstream are documented, not guarded.**

### Details

#### Decisions

| # | Decision |
|---|----------|
| D1 | Java's raw `DefaultOutput` maps to a **non-generic** `DefaultOutput : DefaultOutput<DefaultOutput>`; event-driven in-ports use it as their output type. |
| D2 | `ReadModel` is an **`abstract record`**, so ADR-0022's positional-record read models keep working. The base record hand-writes a deep-copying copy constructor and an equality that excludes the deduplication record. |
| D3 | `CqrsOutput<T>` is renamed outright; no `[Obsolete]` alias. |
| D4 | Java's nested `Reactor.DefaultInput` / `Notifier.DefaultInput` are **not** ported; `DomainEventDataInput` replaces them. |
| D5 | The deduplication record's JSON shape matches Java (`eventDeduplicationRecord`, `processedEventIds`, `MAX_EVENT_CAPACITY`), continuing ADR-0015. Only the record's shape is pinned; cross-platform compatibility of a whole read-model document is not a goal. |
| D6 | The id parser is an interface (`IIdempotentIdParser<TInput>`) **and** `IdempotentDecorator` has a constructor overload taking a `Func<IInternalDomainEvent, string?>`. |
| D7 | Capacity < 1 is rejected by constructors (`ArgumentOutOfRangeException`). On JSON read, a stored capacity that is missing, `null`, or < 1 becomes 50, and a `null`/missing id list or a `null` id is treated as absent — a malformed record value never stops a read model from loading (JSON that is syntactically wrong or of the wrong type still throws). |
| D8 | Event ids: the public API takes `Guid`; the record stores lowercase `"D"`-format strings, exactly what Java's `UUID.toString()` stores. On read, a stored string that parses as a GUID (any case or format) is normalized to lowercase `"D"`, so it matches; other strings are kept as they are and never match. |
| D9 | `DomainEventDataInput` is a **non-sealed record**, so record inputs (the de facto input style of this repository) can derive from it. |
| D10 | `IOutput.Ignore()` / `Reject()` are ordinary interface members (upstream parity) — a breaking change for classes that implement `IOutput` directly. |
| D11 | Concurrency and atomicity limits inherited from upstream are documented (see Known limitations), not guarded. |

#### Public contract

Namespaces follow the existing mapping: Java `…ezddd.usecase.port.in.interactor` → `EzDdd.UseCase.Port.In`; Java
`…cqrs.usecase[.query|.command]` → `EzDdd.Cqrs[.Query|.Command]`; Java `…cqrs.entity.query` (new) →
`EzDdd.Cqrs.Entity.Query`.

`EzDdd.UseCase` (package `ezDDD.UseCase`):

- `ExitCode` gains `Ignore = 2` and `Reject = 3`; `ExitCodeExtensions.Code()` returns 2 and 3 for them.
- `IOutput` gains `IOutput Ignore()` and `IOutput Reject()` (D10).
- `DefaultOutput<T> : IOutput where T : DefaultOutput<T>, new()` is the former `EzDdd.Cqrs.CqrsOutput<T>` with
  unchanged behavior (defaults `Id = ""`, `Message = ""`, `ExitCode = Success`; fluent setters return `T`;
  `SetId`/`SetMessage` reject `null`; static `Create()`), plus `T Ignore()` and `T Reject()`. The self-referential
  design of [ADR-0017](0017-cqrsoutput-implementation-strategy.md) carries over unchanged.
- `DefaultOutput : DefaultOutput<DefaultOutput>` (D1) is not sealed. A subclass can be returned, but its `Create()` and
  fluent setters still return `DefaultOutput`, as with Java's raw type.
- `IReactor<in TInput> : IUseCase<TInput, DefaultOutput> where TInput : IInput` replaces
  `IReactor<in TInput> { Task ExecuteAsync(TInput) }`.
- `UseCaseDecorator<TInput, TOutput>` (abstract) exposes the decorated use case as the protected get-only property
  `DecoratedUseCase` and rejects `null` with `ArgumentNullException`; `ExecuteAsync` stays abstract.
- `DomainEventDataInput : IInput` (D9) is a non-sealed record with one `required` init-only property `Event`.

`EzDdd.Cqrs` (package `ezDDD.Cqrs`), use-case side:

- `ICommand` / `IQuery`: `where TOutput : DefaultOutput<TOutput>, new()`.
- `INotifier<in TInput> : IUseCase<TInput, DefaultOutput> where TInput : IInput` — no longer derives from `IReactor`.
- Removed: `EzDdd.Cqrs.CqrsOutput<T>` (moved) and `EzDdd.Cqrs.Query.IProjector<TInput>` (the use-case projector).
- `IIdempotentIdParser<in TInput>` with `string? Parse(TInput input)`: `null` means the message names no data this use
  case handles (Java's empty `Optional`); an empty string is a valid id.
- `IdempotentInquiryInput : IInquiryInput` with settable `string DataId` (default `""`, matching `DefaultOutput.Id`)
  and `Guid EventId`.
- `IdempotentDecorator<TInput, TOutput> : UseCaseDecorator<TInput, TOutput>` (`where TInput : DomainEventDataInput`,
  `where TOutput : IOutput`). Like upstream it is not an `IReactor`/`INotifier`, so a wrapped reactor is held as
  `IUseCase<TInput, DefaultOutput>`. Constructors take the use case, an `IInquiry<IdempotentInquiryInput, bool>`, a
  `Func<TOutput>` output factory (a new instance per call), and the parser as `IIdempotentIdParser<IInternalDomainEvent>`
  or `Func<IInternalDomainEvent, string?>` (D6). `ExecuteAsync`: reject a `null` input or `input.Event`; convert the
  event with `DomainEventMapper.ToDomain<IInternalDomainEvent>` (a failure propagates before the parser or inquiry is
  called); parser `null` → a fresh `Ignore` output, nothing else is called; inquiry `true` → a fresh `Ignore` output;
  otherwise execute the decorated use case with the same input instance and return its output unchanged. The inquiry
  answers `true` only when the event has already been applied to the data, and `false` when the data does not exist.

`EzDdd.Cqrs.Entity.Query` (package `ezDDD.Cqrs`), entities side:

- `IProjector<in TInput, out TOutput>` with `TOutput Project(TInput input)` — synchronous on purpose: it performs no
  I/O, so ADR-0016 does not apply.
- `IReadValue` — marker for value-semantic components of a read model.
- `EventDeduplicationRecord` (top-level `sealed class`) — `DefaultMaxEventCapacity = 50`; `IsEventHandled(Guid)`,
  `SetEventId(Guid)`; ids kept in first-insertion order, re-recording does not move an id (Java `LinkedHashSet`
  semantics), the eldest are dropped while the count exceeds the capacity; no public properties; JSON
  `processedEventIds` then `MAX_EVENT_CAPACITY`, read under D7 and D8; a stored capacity is kept as stored and wins
  over the constructor's; stored ids beyond the capacity are kept until the next `SetEventId`.
- `abstract record ReadModel` (D2) — `protected` constructors with the default or a given capacity;
  `EventDeduplicationRecord EventDeduplicationRecord { get; private set; }` serialized as `eventDeduplicationRecord`,
  whose private setter ignores `null` (a document without the record keeps the constructor's empty record);
  `UpdateEventDeduplicationRecord(Guid)`; a `protected` copy constructor that deep-copies the record, so a `with`
  copy at any depth of derivation owns its own record; `Equals(ReadModel?)` is
  `ReferenceEquals(this, other) || (other is not null && EqualityContract == other.EqualityContract)` and
  `GetHashCode()` is `EqualityContract.GetHashCode()`, so the record never takes part in equality, equality stays
  symmetric across derivation levels, and read models of different runtime types are never equal.

#### Hosting event-driven use cases (carried forward from ADR-0020 and ADR-0028)

The lifecycle guidance of [ADR-0020](0020-iprojector-lifecycle-management.md), carried forward by ADR-0028, now
applies to the `IReactor`-driven flow: the interfaces carry no start/stop or subscription members. An application
hosts the loop that receives events (for example a `BackgroundService`/`IHostedService` that polls an event store
relay or a broker) in its infrastructure layer, and for each event executes the reactor — usually wrapped in
`IdempotentDecorator` — with a `DomainEventDataInput`. The reactor loads the read model from an `IArchive`, calls an
entities-layer `IProjector`, records the event id, and saves once.

#### Deviations from upstream

| Upstream | ezDDD.NET | Why |
|----------|-----------|-----|
| raw `DefaultOutput` | non-generic `DefaultOutput` | C# has no raw types (D1) |
| `Command`/`Query` bound on raw `DefaultOutput` | `where TOutput : DefaultOutput<TOutput>, new()` | carried over from the `CqrsOutput<T>` constraint (ADR-0017) |
| `ReadModel` abstract class, reference equality | `abstract record`, equality by runtime type and derived members, record excluded | keeps ADR-0022 records (D2) |
| `DomainEventDataInput` class with a public field | non-sealed record with a `required` init property | D9 |
| nested `DefaultInput` | not ported | D4 |
| `create()` / `create(Class)` factories on inputs and `DefaultOutput.create(Class)` | not ported; object initializers / `new` | C# idiom; `DefaultOutput<T>.Create()` is kept |
| `IdempotentInquiryInput` plain class with public fields; `eventId` `UUID` (null until set) | implements `IInquiryInput`; settable properties; `EventId` is `Guid` (default `Guid.Empty`); `DataId` defaults to `""` | the .NET marker for inquiry inputs; C# idiom |
| — | extra `IdempotentDecorator` constructor taking `Func<IInternalDomainEvent, string?>` | lambdas cannot target a C# interface (D6) |
| protected field `useCase` | protected get-only property `DecoratedUseCase` | CA1051 |
| `DefaultOutput` id `null` by default; setters accept `null` | `Id = ""` by default; `SetId`/`SetMessage` throw `ArgumentNullException` | carried over from `CqrsOutput<T>` (ADR-0017) |
| nested `ReadModel.EventDeduplicationRecord` | top-level type | CA1034 |
| record API takes `String`; stored strings compared as-is | record API takes `Guid`, stores the same lowercase strings; stored GUID strings in other cases or formats normalized on read | type safety; identical JSON for canonical ids; non-canonical GUIDs still match (D8) |
| constructors and `execute` accept `null` (NPE later) | `ArgumentNullException` up front (constructors, `ExecuteAsync` input and `input.Event`) | fail fast, consistent with existing .NET guards |
| capacity not validated; a stored 0 empties the record on every write | constructors reject < 1; JSON read maps missing / `null` / < 1 to 50 | D7 |
| `eventDeduplicationRecord: null` sets the field to null (NPE later); `processedEventIds: null` likewise | `null` record keeps the constructor's empty record; `null` id list → no ids | D7 — tolerate 2.x and malformed documents |
| `Supplier<O>` / `Optional<String>` | `Func<TOutput>` / `string?` | language idiom |
| `Projector.project` / decorator `execute` | `Project` (sync) / `ExecuteAsync` (async) | ADR-0016 |
| Java 21 compiler upgrade | — | not applicable |

#### Known limitations (documented, not guarded — D11)

These are stated in the XML documentation of `IdempotentDecorator`, `ReadModel` and `EventDeduplicationRecord`, and
in the changelog.

- **Best-effort filter, not mutual exclusion.** Concurrent deliveries of the same event for the same data id can both
  pass the inquiry. Deliver all events for one data id serially (e.g., partition by data id), or — when the only effect
  of the decorated use case is a save — use an archive with optimistic concurrency. Optimistic concurrency does not
  help a use case with side effects before its save (notifications, outbound calls): those can still happen twice.
- **The inquiry's answer is advisory.** The decorated use case loads the data again, and another writer may change it
  in between.
- **One atomic write.** Call `UpdateEventDeduplicationRecord` before the single `SaveAsync` of the read model — also
  on the first event, which creates the model — so the model and its record are written together. An `IArchive`
  implementation must not drop `eventDeduplicationRecord`. A use case that decides an event is irrelevant and skips
  the save records nothing, which is harmless.
- **Archives return copies.** `IArchive.FindByIdAsync` must return an instance the caller can change without changing
  stored state. If it hands out the stored instance, a failure between `UpdateEventDeduplicationRecord` and `SaveAsync`
  leaves the event marked as handled although nothing was persisted, and the redelivery is ignored — the event is
  lost.
- **Equality excludes the record.** An application or archive that skips a save when the new model equals the old one
  also skips a save whose only change is a newly recorded event id. Compare on something other than read-model
  equality, or always save after `UpdateEventDeduplicationRecord`.
- **Deletes drop the record.** When a read model is deleted (for example on an `AccountClosed` event), its record goes
  with it. A later redelivery of an earlier event (for example `AccountCreated`) is projected again and can recreate
  the model. This is inherent to storing the record inside the model; an application that needs protection keeps a
  tombstone or its own record.
- **Capacity is persisted per document.** A stored capacity wins over the constructor's, so raising it in code only
  affects new documents; existing documents need an explicit migration.
- **Window size.** A redelivery older than the capacity is no longer recognized and is projected again. Upstream's
  default of 50 is sized for ezddd-gateway's poll size of 20; ezDDD.NET has no gateway, so size the capacity above the
  number of events that can be redelivered for one read model. Each remembered id adds about 39 bytes to the document.
- **Rollback.** Rolling back to ezDDD.NET 2.x and re-saving a read model drops `eventDeduplicationRecord`; after
  upgrading again, redeliveries still within the window are projected again.
- **Aggregates.** Only `ReadModel` carries a deduplication record. Using `IdempotentDecorator` for a reactor that
  changes an aggregate requires an application-supplied inquiry backed by its own record of handled events.
- **Reflection-based System.Text.Json only.** The JSON shape lives in private `[JsonInclude]` members
  (`EventDeduplicationRecord`) and a private setter (`ReadModel`). A source-generated `JsonSerializerContext` in a
  consumer assembly cannot see them: it silently writes `{}` for the record and reads nothing back, without a SYSLIB
  warning. Source generation is therefore unsupported for read models.
- **Not thread-safe.** `EventDeduplicationRecord` and `ReadModel` instances are not safe for concurrent mutation.

---

## Consequences

### Positive Consequences

- ✅ **Framework-level deduplication**: under at-least-once delivery, a reactor wrapped in `IdempotentDecorator` skips
  an event already projected into a read model, which upsert idempotency (ADR-0023) alone could not do.
- ✅ **Pure, testable projection logic**: `IProjector<TInput, TOutput>` has no I/O; loading and saving live in the
  reactor.
- ✅ **One use-case shape**: reactors, notifiers, commands and queries are all `IUseCase` implementations returning an
  `IOutput`, so decorators such as `IdempotentDecorator` apply to any of them.
- ✅ **Cross-platform record shape**: the deduplication record serializes exactly as Java writes it, so canonical
  documents are interchangeable at the record level.
- ✅ **ADR-0022 read models keep working**: positional records derive from `ReadModel` without changing their
  declaration style.
- ✅ **Parity restored**: the baseline moves to Java ezddd 9.0.1 (`aa7a99c`).

### Negative Consequences

- ❌ **BREAKING for every consumer of `CqrsOutput<T>`**: there is no `[Obsolete]` alias (D3), so every output class
  needs a rename and a `using` change.
- ❌ **BREAKING for reactors and notifiers**: `ExecuteAsync` returns `Task<DefaultOutput>` and inputs must implement
  `IInput`; `IReactor<string>` or `IReactor<DomainEventData>` no longer compile.
- ❌ **Silent breaks**: code that maps `ExitCode` with `switch` or `==` meets the new values 2 and 3, and dependency
  injection that registers or resolves notifiers as `IReactor<T>` no longer finds them. The compiler reports neither.
- ❌ **Direct `IOutput` implementers must add two members** (D10).
- ❌ **Correctness depends on documented rules the library cannot enforce**: serial delivery per data id, archives
  that return copies, one atomic write, never skipping a save on equality. A violation silently loses or duplicates a
  projection.
- ❌ **No source-generated JSON** for read models; applications that rely on `JsonSerializerContext` (for example for
  trimming or Native AOT) cannot use `ReadModel` as is.
- ❌ **Equality semantics differ from upstream**: Java's `ReadModel` uses reference equality; ezDDD.NET compares
  runtime type and derived members.

### Neutral Consequences

- ⚖️ `EzDdd.Cqrs.Entity.Query.IProjector<TInput, TOutput>` reuses the simple name of the removed
  `EzDdd.Cqrs.Query.IProjector<TInput>`; the namespace and arity tell them apart.
- ⚖️ Stored read-model documents gain an `eventDeduplicationRecord` property when the model derives from `ReadModel`.
  System.Text.Json readers ignore it by default; strict readers (for example Java with `FAIL_ON_UNKNOWN_PROPERTIES`)
  must tolerate it.
- ⚖️ `IdempotentDecorator` is not an `IReactor`, so composition roots hold a wrapped reactor as
  `IUseCase<TInput, DefaultOutput>`.
- ⚖️ Each remembered event id adds about 39 bytes to a document; at the default capacity of 50 that is about 2 KB.

---

## Alternatives Considered

### Alternative 1: Keep `CqrsOutput<T>` as an `[Obsolete]` alias

**Description**: Add `DefaultOutput<T>` in `EzDdd.UseCase` and keep `EzDdd.Cqrs.CqrsOutput<T>` as an obsolete
subclass or duplicate for one major version.

**Pros**:
- Existing output classes compile with a warning instead of an error
- Consumers can migrate gradually

**Cons**:
- A self-referential generic cannot be aliased transparently: `class CqrsOutput<T> : DefaultOutput<T> where T :
  CqrsOutput<T>` changes the constraint, and `ICommand`/`IQuery` constrain on `DefaultOutput<TOutput>` anyway
- The release is a major version with other compile-breaking changes (`IReactor`, `INotifier`, `IOutput`), so the
  alias would not make the upgrade non-breaking
- Two names for one concept on NuGet, with a later removal that breaks again

**Why rejected**: The alias saves one rename in an upgrade that already requires source changes, and it would itself
need a second breaking removal (D3).

---

### Alternative 2: Port `ReadModel` as an abstract class with reference equality (literal parity)

**Description**: Mirror upstream exactly: `public abstract class ReadModel` with a mutable record field and default
reference equality.

**Pros**:
- Exact upstream semantics, including equality
- No hand-written copy constructor or equality

**Cons**:
- Records cannot derive from a class that is not a record, so every ADR-0022 positional-record read model would have
  to be rewritten as a class
- `with` expressions, which ADR-0022 uses for projector updates, would no longer be available
- Value equality of read models, which tests and applications rely on, would be lost

**Why rejected**: It breaks the read-model pattern the .NET port already standardized on, for no behavioral gain (D2).

---

### Alternative 3: Port Java's nested `Reactor.DefaultInput` / `Notifier.DefaultInput`

**Description**: Keep upstream's nested input types alongside `DomainEventDataInput`.

**Pros**:
- Name-level parity with upstream 7.0.0 code

**Cons**:
- Public nested types trip CA1034 under the Level Z analyzers
- Upstream itself replaced them with `DomainEventDataInput` in 8.0.0
- Two input types with the same role

**Why rejected**: Upstream already superseded them; porting them adds a type with no remaining purpose (D4).

---

### Alternative 4: Guard against concurrent duplicate delivery in the library

**Description**: Make `IdempotentDecorator` serialize executions per data id (a keyed lock), or require archives with
optimistic concurrency.

**Pros**:
- Closes the race in which two deliveries of one event both pass the inquiry

**Cons**:
- An in-process lock does not help across processes, which is the usual deployment for message consumers
- Requiring optimistic concurrency changes the `IArchive` contract and does not prevent side effects before the save
- Upstream does not guard either; the guard would be a .NET-only behavior

**Why rejected**: No in-library guard is correct across processes; the remedies belong to the delivery topology
(partitioning by data id) and are documented instead (D11).

---

### Alternative 5: A type-level `JsonConverter` for the deduplication record

**Description**: Serialize `EventDeduplicationRecord` (and the `ReadModel` property) through a custom
`JsonConverter` instead of private `[JsonInclude]` members, so that source-generated contexts keep the record.

**Pros**:
- Source-generated `JsonSerializerContext` and Native AOT consumers would keep the record
- The Java shape could be pinned against consumer options such as `NumberHandling.WriteAsString` or
  `ReferenceHandler.Preserve`

**Cons**:
- Read models are positional records deriving from `ReadModel` with a private-setter base property, which is
  reflection-only anyway; a converter on the record type alone would not make read models source-generation
  compatible
- More code to keep in step with the tolerant read rules (D7, D8)

**Why rejected for now**: It does not solve the whole problem on its own. Source generation is documented as
unsupported, and the converter is recorded as a follow-up candidate (see Implementation Notes).

---

## Related Decisions

- **Supersedes**: [ADR-0017](0017-cqrsoutput-implementation-strategy.md) - CqrsOutput Implementation Strategy — the
  type is renamed `DefaultOutput<T>` and moved to `EzDdd.UseCase`; its self-referential design, defaults and `null`
  guards carry over unchanged, and `Ignore()`/`Reject()` are added.
- **Supersedes**: [ADR-0028](0028-reactor-hierarchy-projector-notifier-genericization.md) - Reactor Type Hierarchy
  and Projector/Notifier Genericization — the reactor hierarchy is replaced by sibling use cases returning
  `DefaultOutput`, and the use-case `IProjector` is removed.
- **Extends**: [ADR-0020](0020-iprojector-lifecycle-management.md) - IProjector Lifecycle Management Integration
  (already superseded by ADR-0028) — its lifecycle-separation guidance is restated above for the `IReactor`-driven flow.
- **Amends**: [ADR-0022](0022-read-model-design-patterns.md) - Read Model Design Patterns — read models that need
  event deduplication derive from `ReadModel`; their equality excludes the record.
- **Amends**: [ADR-0023](0023-archive-idempotency-requirements.md) - Archive Idempotency Requirements — upsert
  idempotency is complemented by event deduplication, and archives must return copies and persist the record.
- **Related to**: [ADR-0015](0015-cross-platform-dto-structure.md) - Cross-Platform DTO Structure — the record's JSON
  shape follows Java (D5).
- **Related to**: [ADR-0016](0016-async-await-throughout.md) - Async/Await Throughout — `ExecuteAsync` is async;
  `IProjector.Project` is synchronous because it performs no I/O.
- **Related to**: [ADR-0019](0019-iinquiry-iprojection-independence.md) - IInquiry and IProjection Independence — the
  idempotency check is an `IInquiry`, outside the use-case hierarchy.
- **Related to**: [ADR-0021](0021-generic-variance-annotations.md) - Generic Variance Annotations — `IReactor`,
  `INotifier` and `IIdempotentIdParser` keep a contravariant input; `IProjector<in TInput, out TOutput>` adds a
  covariant output.
- **Related to**: [ADR-0029](0029-messageproducer-removal-gateway-deferral.md) - MessageProducer Removal & Gateway
  Deferral — without a gateway, the default capacity is not tied to a known poll size.

ADR-0017 and ADR-0028 carry a "Superseded by ADR-0031" status; ADR-0020 extends its superseded pointer; ADR-0022 and
ADR-0023 carry an amendment note. Other accepted ADRs that show the old type names carry a dated status note.

---

## Implementation Notes

- **Order of the change set**: the rename moved `CqrsOutput<T>` first as a structural change; the exit codes, the
  reactor/notifier redefinition, `UseCaseDecorator`, `EventDeduplicationRecord` and its JSON shape, `ReadModel` with
  `IReadValue` and the entities-layer `IProjector`, and `IdempotentDecorator` followed as separate behavioral changes,
  each test-first.
- **JSON mechanism**: a private `[JsonConstructor]` was tried first and failed System.Text.Json's parameter name and
  type matching. The shape lives in private `[JsonInclude]` properties with `[JsonPropertyName]`, whose setters are
  the tolerant read path of D7 and D8; the public constructor still rejects a capacity below 1.
- **`Populate` object-creation handling**: the id-list property returns a copy, so a caller whose options set
  `PreferredObjectCreationHandling = Populate` silently lost every stored id — the serializer filled the throwaway
  copy and skipped the setter (confirmed three times by independent probes). The member pins
  `[JsonObjectCreationHandling(JsonObjectCreationHandling.Replace)]`; a test reads ids under `Populate`.
- **Source-generation probe**: an executed two-assembly probe showed that a consumer's source-generated
  `JsonSerializerContext` writes `{}` for the record and reads nothing back, with no SYSLIB warning. This led to the
  "reflection-based only" limitation rather than a converter (Alternative 5).
- **`IdempotentInquiryInput.DataId`** defaults to `""` — a non-nullable string, matching `DefaultOutput.Id`.
- **Test infrastructure**: the integration test drives an `AccountProjector` reactor through `IdempotentDecorator`
  against a test-only `JsonCopyArchive<TData, TId>` that copies through JSON on both find and save. It pins that a
  redelivery returns `Ignore` and leaves the stored JSON unchanged, that a save that fails once followed by a
  redelivery loses no event, and that with capacity 3 a redelivery older than the window is projected again. The
  reference-sharing `InMemoryArchive` remains for other suites.
- **Follow-up candidates**:
  - A type-level `JsonConverter` for `EventDeduplicationRecord` / `ReadModel` to support source generation and to pin
    the Java shape against consumer `NumberHandling.WriteAsString` / `ReferenceHandler.Preserve` options.
  - Reading a very large stored id list is quadratic (duplicate detection uses a list); negligible at realistic
    capacities.
  - `[AllowNull]` on `ReadModel.EventDeduplicationRecord`, so that System.Text.Json's `RespectNullableAnnotations`
    accepts a stored `null` record.

---

## References

- **Java ezddd** (upstream): releases 7.0.0, 7.1.0, 8.0.0, 9.0.0 and 9.0.1; baseline commit `aa7a99c` (9.0.1),
  previous baseline `3aac0f5` (6.0.1). Relevant sources at 9.0.1:
  `ezddd-usecase/.../port/in/interactor/{UseCaseDecorator,DomainEventDataInput}.java`,
  `ezcqrs/.../cqrs/usecase/{IdempotentDecorator,IdempotentIdParser,IdempotentInquiryInput}.java`,
  `ezcqrs/.../cqrs/entity/query/{Projector,ReadModel,ReadValue}.java`
- [CHANGELOG](../../CHANGELOG.md) — `[Unreleased]` lists every change and the upgrade notes
- [API Reference](../examples/API_REFERENCE.md) — `DefaultOutput`, `IdempotentDecorator`, `ReadModel` and related types
- [Java ezddd repository](https://gitlab.com/TeddyChen/ezddd)

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2026-10-06 | Accepted    | Initial decision, written after the implementation |

---
