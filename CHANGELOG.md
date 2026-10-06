# Changelog

All notable changes to ezDDD.NET will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

Aligns ezDDD.NET with [Java ezddd 9.0.1](https://gitlab.com/TeddyChen/ezddd) (commit `aa7a99c`; upstream releases
7.0.0, 7.1.0, 8.0.0, 9.0.0 and 9.0.1). The decisions and every deviation from upstream are recorded in
[ADR-0031](docs/adr/0031-align-with-java-ezddd-9-0-1.md).

### Added

- `ezDDD.UseCase`: `ExitCode.Ignore` (2) and `ExitCode.Reject` (3), with `Ignore()` and `Reject()` on `IOutput` and
  `DefaultOutput<T>`.
- `ezDDD.UseCase`: non-generic `DefaultOutput` (`DefaultOutput : DefaultOutput<DefaultOutput>`), the output of
  reactors and notifiers.
- `ezDDD.UseCase`: `DomainEventDataInput`, a non-sealed record `IInput` carrying one `DomainEventData` in its
  `required` `Event` property; record inputs that carry more than the event can derive from it.
- `ezDDD.UseCase`: `UseCaseDecorator<TInput, TOutput>`, the base class for decorators of a use case; the decorated use
  case is available to subclasses as `DecoratedUseCase`.
- `ezDDD.Cqrs`: `IdempotentDecorator<TInput, TOutput>`, `IIdempotentIdParser<TInput>` and `IdempotentInquiryInput`.
  The decorator parses the id of the data an event targets, asks an `IInquiry<IdempotentInquiryInput, bool>` whether
  the event was already applied, and returns an `Ignore` output instead of running the decorated use case when it was
  (or when the parser returns `null`). The parser can also be a `Func<IInternalDomainEvent, string?>`.
- `ezDDD.Cqrs`, namespace `EzDdd.Cqrs.Entity.Query` (the entities layer of the query side):
  - `IProjector<TInput, TOutput>` — a synchronous, pure projection function.
  - `ReadModel` — an `abstract record` that positional-record read models can derive from. It carries an
    `EventDeduplicationRecord` (`UpdateEventDeduplicationRecord(Guid)`) that is serialized with the model as
    `eventDeduplicationRecord`, in the same JSON shape as Java ezddd. Copies made with `with` get their own record;
    equality ignores the record. JSON support is reflection-based System.Text.Json only: a source-generated
    `JsonSerializerContext` silently drops the record.
  - `EventDeduplicationRecord` — remembers the most recent event ids (`DefaultMaxEventCapacity` = 50).
  - `IReadValue` — marker for value-semantic components of a read model.
  - Limitations, documented rather than guarded: the decorator is a best-effort filter, not mutual exclusion; an
    archive's `FindByIdAsync` must return a copy; record the event id before the single save; deleting a read model
    deletes its record; a redelivery older than the capacity is projected again. See ADR-0031.

### Changed

- **BREAKING:** `EzDdd.Cqrs.CqrsOutput<T>` is renamed `DefaultOutput<T>` and moved to `EzDdd.UseCase.Port.In`
  (package `ezDDD.UseCase`). Its behavior is unchanged apart from the new `Ignore()` and `Reject()`.
- **BREAKING:** `IOutput` gains `Ignore()` and `Reject()`.
- **BREAKING:** `ExitCode` gains `Ignore` and `Reject`.
- **BREAKING:** `IReactor<TInput>` is now `IReactor<in TInput> : IUseCase<TInput, DefaultOutput> where TInput : IInput`:
  `ExecuteAsync` returns `Task<DefaultOutput>` instead of `Task`, and the input must implement `IInput`.
- **BREAKING:** `INotifier<TInput>` is now `INotifier<in TInput> : IUseCase<TInput, DefaultOutput> where TInput :
  IInput`. It no longer derives from `IReactor<TInput>`; the two are siblings.
- **BREAKING:** `ICommand` and `IQuery` constrain their output with `where TOutput : DefaultOutput<TOutput>, new()`
  (previously `CqrsOutput<TOutput>`).
- **Upgrade notes** (before → after):
  - `CqrsOutput<T>` → `DefaultOutput<T>` (compile error): rename, and add `using EzDdd.UseCase.Port.In;` (package
    `ezDDD.UseCase`, already a dependency of `ezDDD.Cqrs`).
  - `IOutput` gains `Ignore()` / `Reject()` (compile error for classes that implement `IOutput` directly): add both
    members, or derive from `DefaultOutput<T>`.
  - `ExitCode` gains `Ignore` / `Reject` (silent): `switch` statements, `==` comparisons and success-versus-failure
    mappings now meet the values 2 and 3. Handle them or add a default arm. They are produced by `IdempotentDecorator`
    or by explicit calls.
  - `IReactor` returns `Task<DefaultOutput>` and its input must be an `IInput` (compile error): return an output, and
    make the input type implement `IInput` — a record deriving from `DomainEventDataInput` if it carries an event.
    `IReactor<string>` / `IReactor<DomainEventData>` become `IReactor<MyInput>` (for example
    `IReactor<DomainEventDataInput>`).
  - `INotifier` returns `Task<DefaultOutput>`, its input must be an `IInput`, and it is no longer an `IReactor`
    (compile error for implementations and for static uses such as assigning a notifier to `IReactor<T>`; **silent**
    for dependency-injection registrations and lookups by `IReactor<T>`): change it as for `IReactor`, and register and
    resolve notifiers as `INotifier<T>`.
  - `EzDdd.Cqrs.Query.IProjector<TInput>` removed (compile error): write the use case as an `IReactor` that loads the
    read model, calls a projector, and saves it; move the projection logic into
    `EzDdd.Cqrs.Entity.Query.IProjector<TInput, TOutput>` (a different type with the same simple name).
  - `ICommand` / `IQuery` constraint now on `DefaultOutput<TOutput>` (compile error only through the rename): rename;
    add `using EzDdd.UseCase.Port.In;`.
  - Read models (opt-in): derive from `ReadModel` to get event deduplication. Stored documents then gain an
    `eventDeduplicationRecord` property. System.Text.Json readers ignore unknown properties by default; strict readers
    (for example Java with `FAIL_ON_UNKNOWN_PROPERTIES`) must tolerate it. A reader on ezDDD.NET 2.x that re-saves a
    document drops the property, so redeliveries within the window are projected again after upgrading once more.
    Documents without the property load with an empty record.

### Removed

- **BREAKING:** `EzDdd.Cqrs.CqrsOutput<T>` (moved; see Changed).
- **BREAKING:** `EzDdd.Cqrs.Query.IProjector<TInput>`, the use-case projector (removed upstream in Java ezddd 8.0.0).
  Its role is taken by an `IReactor` that drives an `EzDdd.Cqrs.Entity.Query.IProjector<TInput, TOutput>`.

## [2.0.0] - 2026-10-05

### Changed

- **BREAKING:** Moved the `uContract` dependency to 2.0.0. Five existing precondition checks (four in
  `DomainEventTypeMapper`, one in the `EsAggregateRoot` replay constructor) now run by default, in every build
  configuration, and throw `uContract.Exceptions.PreconditionViolationException`. It derives from
  `uContract.Exceptions.ContractViolationException`, which derives from `System.Exception` — it is not an
  `ArgumentException`, so existing `catch (ArgumentNullException)` or `catch (ArgumentException)` blocks no longer
  catch these cases. What was thrown before, and what is thrown now:
  - `DomainEventTypeMapper.Register<T>(null)`: `ArgumentNullException`, now `PreconditionViolationException`.
  - `DomainEventTypeMapper.Register<T>("")` and `Register<T>("  ")`: the blank name was registered without error,
    now `PreconditionViolationException`.
  - `DomainEventTypeMapper.GetTypeName((Type)null)`: `ArgumentNullException`, now `PreconditionViolationException`.
  - `DomainEventTypeMapper.GetTypeName((IInternalDomainEvent)null)`: `NullReferenceException`, now
    `PreconditionViolationException`.
  - `DomainEventTypeMapper.GetType(null)`: `ArgumentNullException`, now `PreconditionViolationException`.
  - `DomainEventTypeMapper.GetType("")` and `GetType("  ")`: previously `InvalidOperationException` (or the registered
    type, if a blank name had been registered), now `PreconditionViolationException`. Stored events whose event type is
    blank can no longer be converted by `DomainEventMapper.ToDomain`.
  - The replay constructor of an `EsAggregateRoot` subclass with `null` events: `NullReferenceException`, now
    `PreconditionViolationException`.
  - `DomainEventMapper.ToDomain<T>(data)` (`ezDDD.UseCase`; single and batch overloads) when `data.EventType` is null
    or blank: it calls `DomainEventTypeMapper.GetType`, so it now throws `PreconditionViolationException` as above
    (previously `ArgumentNullException` for a null event type and `InvalidOperationException` for a blank one).
- **Upgrade notes:**
  - Catch `PreconditionViolationException` or its base `ContractViolationException` (both in `uContract.Exceptions`)
    where you handled the exceptions listed above.
  - The checks are switched by environment variables that uContract reads once per process. Set `DBC_PRE=off`, or
    `DBC=off` when `DBC_PRE` is not set, to disable them. `DBC_PRE` is read first, then `DBC`, then the default
    (enabled). The accepted values are `true/1/yes/on` and `false/0/no/off`, case-insensitive; any other value, or an
    empty one, is ignored as if the variable were not set, so a typo leaves the checks on.
  - The switches apply to the whole process: they also affect contracts your own code declares with uContract, and
    `DBC=off` additionally disables postconditions, invariants and checks, except any whose own switch (`DBC_POST`,
    `DBC_INV`, `DBC_CHECK`) is set.
  - With ezDDD.NET 1.0.0 the checks ran only if the application set `DBC=on` (or `DBC_PRE=on`); an application that set
    it only for this purpose can remove the setting.
  - If you also use uContract directly, read its
    [2.0.0 changelog](https://github.com/cwouyang/uContract.NET/blob/v2.0.0/CHANGELOG.md): contracts in your own code
    are now enforced in every build configuration too. For example, `Contract.Invariant` calls inside an aggregate's
    `_EnsureInvariant` now run whenever an event is applied, including during event replay; when an aggregate is loaded
    through `EsRepository`, a violation during replay surfaces as an `InvalidOperationException` with the contract
    violation in its inner exception chain. A project that references uContract 1.0.0 directly must move to 2.0.0.
- CI now runs the test suite in the Release configuration as well as Debug.

## [1.0.0] - 2026-07-06

Initial release. .NET port of [Java ezddd 6.0.1](https://gitlab.com/TeddyChen/ezddd)
(commit `3aac0f5`) providing tactical Domain-Driven Design patterns, CQRS, and Clean Architecture
support, with both **event sourcing** and **state sourcing** for aggregates and repositories.

The port was developed against Java ezddd 2.1.0 and synchronized upstream
(2.1.0 → 4.1.0 → 6.0.1) before first publication, so the initial release exposes the current
upstream API from day one — there is no older published API and no migration path to worry about.
Semantic parity with Java ezddd 6.0.1 is ~99%.

### Added

Five NuGet packages with a strict dependency chain
(`Common` → `Entity` → `UseCase` → `Cqrs`, aggregated by `Core`):

#### ezDDD.Common (`EzDdd.Common`)

- `BiMap<TKey, TValue>` — thread-safe bidirectional map (forward and reverse lookup)
- `Converter<in TSource, out TTarget>` — type conversion delegate
- `JsonUtil` — System.Text.Json utilities, including `DeepCopy<T>()`

#### ezDDD.Entity (`EzDdd.Entity`)

- `IEntity<out TId>`, `IValueObject` — tactical DDD building blocks
- `IDomainEvent` — base event interface with `Id`, `OccurredOn`, `Source`, and `Metadata`
  (`IReadOnlyDictionary<string, string>`) for idempotency detection, distributed tracing,
  and user/tenant context
- `IInternalDomainEvent` with `IConstructionEvent` / `IDestructionEvent` markers enforcing
  event-stream correctness (first/last event rules)
- `IDomainEventSource<TEvent>` — abstraction over domain-event-raising types
- `AggregateRoot<TId, TEvent>` — state-sourced aggregate root with event collection and
  `Version` for optimistic locking
- `EsAggregateRoot<TId, TEvent>` — event-sourced aggregate root with R1/R2/R3 invariant rules
  (template method pattern), event replay from history, and `{category}-{id}` stream naming
- `DomainEventTypeMapper` — thread-safe event type ↔ name mapping for serialization

#### ezDDD.UseCase (`EzDdd.UseCase`)

- `IUseCase<in TInput, TOutput>` with `ExecuteAsync`, plus `IInput`, `IOutput`,
  `IVersionedInput`, `ExitCode`, and `UseCaseFailureException`
- In-ports: `IReactor<in TInput>` (base of projectors/notifiers) and
  `IReconciler<in TContext, TReport>` with `NullContext` for system state reconciliation
- Out-port: `IExternalDomainEventPublisher<in TEvent>` for publishing integration events
- Repository bridge pattern: `IRepository<TAggregate, in TId, TEvent>` (domain abstraction) ↔
  `IRepositoryPeer<TData, in TId>` (persistence SPI; transaction boundary lives here),
  with `IStoreData<TId>`, `RepositorySaveException`, `RepositoryPeerSaveException`,
  and `PostEventFailureException`
- Event infrastructure: `IExternalDomainEvent`, `DomainEventData` (+ `DomainEventDataBuilder`),
  `DomainEventMapper`, `InternalDomainEventDto`
- `EsRepository<TAggregate, TId>` — generic event sourcing repository with
  cached reflection-based aggregate instantiation
- `OutboxRepository<TAggregate, TData, TId>` (+ `IOutboxData<TId>`,
  `OutboxMapper<TAggregate, TData, TId>`) —
  state sourcing with the Transactional Outbox pattern; event publishing is handled by an
  independent Relay (see `examples/EventInfrastructure/EventStoreRelay.cs`), matching the
  Java architecture

#### ezDDD.Cqrs (`EzDdd.Cqrs`)

- Command side: `ICommand<in TInput, TOutput>`, `IInquiry<in TInput, TOutput>`
  (+ `IInquiryInput`) for validation queries within commands
- Query side: `IQuery<in TInput, TOutput>`, `IProjection<in TInput, TOutput>`
  (+ `IProjectionInput`), and `IArchive<TData, in TId>` as the query-side counterpart
  to `IRepository`
- Reactor hierarchy: `IProjector<in TInput> : IReactor<TInput>` (read-model writer) and
  `INotifier<in TInput> : IReactor<TInput>` (internal → external event dispatch)
- `CqrsOutput<T>` — unified success/failure output with fluent API

#### ezDDD.Core (`EzDdd.Core`)

- Aggregator package — `dotnet add package ezDDD.Core` installs the complete framework

#### Cross-cutting

- Design by Contract via [uContract.NET](https://github.com/cwouyang/uContract.NET) —
  the only runtime dependency beyond the .NET 8 BCL (zero third-party dependencies)
- Async/await throughout all I/O operations; nullable reference types enabled everywhere
- Comprehensive test suite (unit + integration, 100% passing, >90% coverage) with
  banking and order example domains
- 29 Architecture Decision Records under `docs/adr/`
- Documentation: README, API reference, usage examples, and a Java → .NET migration guide
- Note: the packages ship persistence and messaging **abstractions** — no production-ready
  `IRepositoryPeer` or event store implementations are included; the in-memory implementations
  under `examples/` and the test suites serve as reference implementations

### Changed (compared to Java ezddd 6.0.1)

- Method naming from camelCase to PascalCase with `Async` suffix
  (`execute()` → `ExecuteAsync()`, `findById()` → `FindByIdAsync()`)
- All I/O is asynchronous (`Task<T>` instead of synchronous returns)
- `Optional<T>` → nullable reference types (`T?`)
- Immutable events and DTOs use C# record types; event handling uses pattern matching
- Serialization uses System.Text.Json (Java uses Jackson)
- `MessageProducer` is not part of the core packages — upstream 6.0.0 moved it to the
  separate ezddd-gateway artifact; a corresponding ezDDD.Gateway package is deferred to
  a post-release milestone (ADR-0029)

### Fixed

- `OutboxRepository.FindByIdAsync` filters soft-deleted aggregates instead of resurrecting
  them (ports upstream 6.0.1 bug fix, commit `3aac0f5`)

---

## Migration from Java ezddd

See the [Migration Guide](docs/MIGRATION_GUIDE.md) for complete migration instructions.

**Key syntax changes**:
- Method naming: `execute()` → `ExecuteAsync()` (PascalCase + async)
- Lambda syntax: `() -> x > 0` → `() => x > 0`
- Functional types: `Function<T, R>` → `Func<T, TResult>`
- Null handling: `Optional<T>` → `T?` (nullable reference types)
- Event handling: `instanceof` → pattern matching with `switch`

**Semantic changes**:
- All I/O operations are async (use `await`)
- Exception handling: try-catch instead of checked exceptions
- Immutability: record types for events and value objects

---

## How to Report Issues

If you encounter any issues or have suggestions:
- **Bug Reports**: [GitHub Issues](https://github.com/cwouyang/ezDDD.NET/issues)
- **Feature Requests**: [GitHub Issues](https://github.com/cwouyang/ezDDD.NET/issues)

---

## License

MIT License — see [LICENSE](LICENSE). Third-party attributions are listed in
[THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).

---

## Credits

- **Original Java version**: [Java ezddd 6.0.1](https://gitlab.com/TeddyChen/ezddd)
  (commit `3aac0f5`) by Teddy Chen (TeddySoft)
- **.NET port**: ezDDD.NET Contributors (target: .NET 8+, dependency: uContract.NET 1.0.0+)
- **Design by Contract**: Bertrand Meyer
- **Tactical DDD**: Eric Evans (Domain-Driven Design)
- **Clean Architecture**: Robert C. Martin
- **Event Sourcing**: Martin Fowler
- **CQRS**: Greg Young

---

[Unreleased]: https://github.com/cwouyang/ezDDD.NET/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/cwouyang/ezDDD.NET/releases/tag/v2.0.0
[1.0.0]: https://github.com/cwouyang/ezDDD.NET/releases/tag/v1.0.0
