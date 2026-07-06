# Changelog

All notable changes to ezDDD.NET will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

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

[Unreleased]: https://github.com/cwouyang/ezDDD.NET/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/cwouyang/ezDDD.NET/releases/tag/v1.0.0
