# ezDDD.NET

> **Tactical Domain-Driven Design patterns library for .NET 8+**
>
> Based on [Java ezddd 6.0.1](https://gitlab.com/TeddyChen/ezddd)

A modern tactical DDD library for .NET with event sourcing, state sourcing, and CQRS patterns. This is a faithful .NET port of the **Java ezddd 6.0.1** library (GitLab commit: `3aac0f5`) with **~99% semantic parity** and .NET-specific improvements.

[![Build and Test](https://github.com/cwouyang/ezDDD.NET/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/cwouyang/ezDDD.NET/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/v/ezDDD.Core?logo=nuget&label=NuGet&color=004880)](https://www.nuget.org/packages/ezDDD.Core/)
[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Status](https://img.shields.io/badge/status-stable-brightgreen.svg)](#status)

---

## Status

ezDDD.NET follows [Semantic Versioning](https://semver.org/) and is **published on NuGet** — the current version is shown by the NuGet badge above, and you can install it from the [Quick Start](#quick-start). Changes between releases are tracked in [CHANGELOG.md](CHANGELOG.md).

---

## Table of Contents

- [Quick Start](#quick-start)
- [Features](#features)
- [Modules](#modules)
- [API Overview](#api-overview) - Key types ([Complete API Docs →](docs/examples/API_REFERENCE.md))
- [Examples](#examples) - Minimal example ([30+ More Examples →](docs/examples/USAGE_EXAMPLES.md))
- [Requirements](#requirements)
- [Differences from Java Version](#differences-from-java-version)
- [Documentation](#documentation)
- [Contributing](#contributing)
- [License](#license)
- [References](#references)

---

## Quick Start

### Installation

```bash
dotnet add package ezDDD.Core
```

`ezDDD.Core` is the all-in-one aggregator; individual modules (`ezDDD.Common`, `ezDDD.Entity`, `ezDDD.UseCase`, `ezDDD.Cqrs`) can also be installed separately — see [Modules](#modules).

### Basic Usage: Event-Sourced Aggregate

```csharp
using EzDdd.Entity;

// Aggregate ID (ToString() drives the stream name: "account-ACC-001")
public sealed record AccountId(string Value) : IValueObject
{
    public override string ToString() => Value;
}

// Domain events are immutable records
public sealed record AccountCreated(
    Guid Id, DateTimeOffset OccurredOn, AccountId Source, string Owner, decimal InitialBalance
) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent
{
    string IDomainEvent.Source => Source.Value;
    IReadOnlyDictionary<string, string> IDomainEvent.Metadata => new Dictionary<string, string>();
}

public sealed record MoneyDeposited(
    Guid Id, DateTimeOffset OccurredOn, AccountId Source, decimal Amount
) : IInternalDomainEvent
{
    string IDomainEvent.Source => Source.Value;
    IReadOnlyDictionary<string, string> IDomainEvent.Metadata => new Dictionary<string, string>();
}

// Event-sourced aggregate
public sealed class BankAccount : EsAggregateRoot<AccountId, IInternalDomainEvent>
{
    public BankAccount(AccountId id, string owner, decimal initialBalance)
    {
        Id = id;
        Apply(new AccountCreated(Guid.NewGuid(), DateTimeOffset.UtcNow, id, owner, initialBalance)); // R1
    }

    public BankAccount(IEnumerable<IInternalDomainEvent> events) : base(events) { } // event replay

    public string Owner { get; private set; } = string.Empty;
    public decimal Balance { get; private set; }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Deposit amount must be positive");
        Apply(new MoneyDeposited(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, amount)); // R2
    }

    protected override void _When(IInternalDomainEvent @event)
    {
        switch (@event)
        {
            case AccountCreated created:
                Id = created.Source;
                Owner = created.Owner;
                Balance = created.InitialBalance;
                break;
            case MoneyDeposited deposited:
                Balance += deposited.Amount;
                break;
        }
    }

    protected override void _EnsureInvariant()
    {
        if (Balance < 0)
            throw new InvalidOperationException("Balance cannot be negative");
    }

    public override string GetCategory() => "account";
}
```

This is a trimmed version of the compile-verified BankAccount example; the full version (with a `Money` value object, withdraw/close, and the `EsRepository` round-trip) is in [USAGE_EXAMPLES.md](docs/examples/USAGE_EXAMPLES.md#event-sourced-aggregate-bankaccount).

---

## Features

### Core Capabilities

- ✅ **Tactical DDD building blocks**: `IEntity<TId>`, `IValueObject`, `AggregateRoot<TId, TEvent>`, domain events with `Metadata` for idempotency and distributed tracing
- ✅ **Event sourcing**: `EsAggregateRoot<TId, TEvent>` enforcing R1/R2/R3 invariant rules via template method, with event replay and `{category}-{id}` stream naming
- ✅ **State sourcing**: `OutboxRepository` persists aggregate state + events atomically (Transactional Outbox)
- ✅ **Repository bridge pattern**: `IRepository` (domain abstraction) ↔ `IRepositoryPeer` (persistence SPI; the transaction boundary)
- ✅ **CQRS**: `ICommand` / `IQuery` / `IInquiry` / `IProjection` with the `CqrsOutput<T>` fluent output API
- ✅ **Event reaction**: `IReactor<TInput>` hierarchy — `IProjector<TInput>` maintains read models, `INotifier<TInput>` converts internal events to external (integration) events
- ✅ **External publishing**: `IExternalDomainEventPublisher<TEvent>` out-port; repositories never publish — a separate Relay does (see [examples/EventInfrastructure/](examples/EventInfrastructure/))
- ✅ **System reconciliation**: `IReconciler<TContext, TReport>` for maintenance jobs (cleanup, consistency checks)

### Design Philosophy

- 🚀 **Async/await throughout**: All I/O operations return `Task<T>`, never blocking
- 🎯 **Clean Architecture**: Unidirectional dependencies (Common → Entity → UseCase → Cqrs), ports & adapters
- 📦 **Zero external dependencies**: Only .NET BCL + uContract.NET (ecosystem dependency)
- 🔒 **Thread-safe**: Concurrent collections, `Lazy<T>`, and snapshot patterns
- 💎 **Strongly typed**: Generic variance (`in TInput`, `out TOutput`) and nullable reference types
- 🧬 **Modern C# idioms**: Records for events/value objects, pattern matching for event handlers
- 🧪 **Highly tested**: 543 tests passing, >90% coverage across all modules
- 🤝 **Semantic parity**: ~99% parity with Java ezddd 6.0.1, upstream tracked per release

---

## Modules

Five NuGet packages with a unidirectional dependency chain (`Common → Entity → UseCase → Cqrs → Core`):

| NuGet Package | Purpose | Depends on |
|---------------|---------|------------|
| `ezDDD.Common` | Foundation utilities (`BiMap`, `JsonUtil`, `Converter`) | — |
| `ezDDD.Entity` | Core DDD patterns (entities, value objects, aggregates, domain events) | Common, uContract |
| `ezDDD.UseCase` | Use cases, repositories (event/state sourcing), event infrastructure | Entity |
| `ezDDD.Cqrs` | CQRS patterns (commands, queries, projections, `CqrsOutput`) | UseCase |
| `ezDDD.Core` ⭐ | **All-in-one aggregator** (no code of its own) | All of the above |

> Note: Package IDs use the `ezDDD.*` prefix; namespaces use `EzDdd.*` (e.g. `using EzDdd.Entity;`).

---

## API Overview

> 📖 **Complete Documentation**: [API_REFERENCE.md](docs/examples/API_REFERENCE.md) — signatures, parameters, exceptions, and examples for every public API

| Module | Key Types |
|--------|-----------|
| [**Common**](docs/examples/API_REFERENCE.md#ezdddcommon) | `BiMap<TKey, TValue>`, `JsonUtil`, `Converter<TSource, TTarget>` |
| [**Entity**](docs/examples/API_REFERENCE.md#ezdddentity) | `IEntity<TId>`, `IValueObject`, `IDomainEvent`, `IInternalDomainEvent`, `AggregateRoot<TId, TEvent>`, `EsAggregateRoot<TId, TEvent>`, `DomainEventTypeMapper` |
| [**UseCase**](docs/examples/API_REFERENCE.md#ezdddusecase) | `IUseCase<TInput, TOutput>`, `IReactor<TInput>`, `IReconciler<TContext, TReport>`, `IRepository<TAggregate, TId, TEvent>`, `IRepositoryPeer<TData, TId>`, `EsRepository<TAggregate, TId>`, `OutboxRepository<TAggregate, TData, TId>`, `IExternalDomainEventPublisher<TEvent>`, `ExitCode` |
| [**Cqrs**](docs/examples/API_REFERENCE.md#ezdddcqrs) | `ICommand<TInput, TOutput>`, `IQuery<TInput, TOutput>`, `IInquiry<TInput, TOutput>`, `IProjection<TInput, TOutput>`, `IProjector<TInput>`, `INotifier<TInput>`, `IArchive<TData, TId>`, `CqrsOutput<T>` |

---

## Examples

> 📚 **More Examples**: [USAGE_EXAMPLES.md](docs/examples/USAGE_EXAMPLES.md) (30+ compile-verified scenarios)

- **Basic Patterns**: Aggregates, value objects, domain events → [USAGE_EXAMPLES.md](docs/examples/USAGE_EXAMPLES.md#basic-examples)
- **Event Sourcing**: BankAccount aggregate, replay, R1/R2/R3 rules, `EsRepository` → [USAGE_EXAMPLES.md](docs/examples/USAGE_EXAMPLES.md#event-sourcing-examples)
- **State Sourcing**: Transactional Outbox, `OutboxRepository`, `OutboxMapper` → [USAGE_EXAMPLES.md](docs/examples/USAGE_EXAMPLES.md#state-sourcing-examples)
- **CQRS**: Commands, queries, projections, `CqrsOutput` fluent API → [USAGE_EXAMPLES.md](docs/examples/USAGE_EXAMPLES.md#cqrs-examples)
- **System Reconciliation**: Cleanup reconcilers, `NullContext`, scheduling → [USAGE_EXAMPLES.md](docs/examples/USAGE_EXAMPLES.md#system-reconciliation-examples)
- **Real-World Scenarios**: Banking, e-commerce, inventory, order management → [USAGE_EXAMPLES.md](docs/examples/USAGE_EXAMPLES.md#real-world-scenarios)
- **Relay Pattern**: Reference implementation of `EventStoreRelay` (event store → publisher) → [examples/EventInfrastructure/](examples/EventInfrastructure/)

---

## Requirements

- **.NET 8.0 or later** (C# 12, nullable reference types enabled)
- **[uContract](https://github.com/cwouyang/uContract.NET) 1.0.0+** — Design by Contract support (same ecosystem as Java ezddd's uContract); used by `EsAggregateRoot` invariant checking
- **No other dependencies** — production code uses only .NET built-in APIs (`System.Text.Json`, `System.Reflection`, `System.Collections.Concurrent`)

---

## Differences from Java Version

ezDDD.NET maintains **~99% semantic parity** with Java ezddd 6.0.1 — core patterns (aggregates, R1/R2/R3 rules, repository bridge, Transactional Outbox, CQRS separation, reactor hierarchy) behave identically — while adopting .NET platform idioms.

### Syntax and Platform Differences

| Aspect | Java ezddd | C# ezDDD.NET |
|--------|------------|--------------|
| **Async** | Synchronous `execute(input)` | `ExecuteAsync(input)` returns `Task<T>`, non-blocking |
| **Method naming** | `camelCase()` | `PascalCase()` + `Async` suffix for async methods |
| **Protected overrides** | `when()`, `ensureInvariant()` | `_When()`, `_EnsureInvariant()` (underscore prefix) |
| **Null safety** | `Optional<T>`, `@Nullable` | Nullable reference types (`T?`), compiler-enforced |
| **Generics** | `<ID, E>` | `<TId, TEvent>` with variance (`in` / `out`) |
| **Event handling** | `instanceof` chains | Pattern matching with `switch` |
| **Immutability** | `final` fields, getters | `record` types with `init` properties |
| **Serialization** | Jackson | System.Text.Json (built-in) |

### API Mapping Highlights

| Java ezddd | C# ezDDD.NET |
|------------|--------------|
| `Optional<T> findById(ID)` | `Task<T?> FindByIdAsync(TId)` |
| `addDomainEvent(E)` / `getDomainEvents()` | `_AddDomainEvent(TEvent)` / `GetDomainEvents()` |
| `CqrsOutput.create().succeed()` | `CqrsOutput<T>.Create().Succeed()` |
| `MessageProducer` (moved to ezddd-gateway in 6.0.0) | Excluded from core, matching Java; a .NET Gateway package is deferred post-1.0 |

### Example Comparison

**Java:**
```java
public class BankAccount extends EsAggregateRoot<AccountId, InternalDomainEvent> {
    private Money balance;

    public void deposit(Money amount) {
        var event = new MoneyDeposited(UUID.randomUUID(), Instant.now(), id, amount);
        apply(event);
    }

    @Override
    protected void when(InternalDomainEvent event) {
        if (event instanceof MoneyDeposited deposited) {
            this.balance = balance.add(deposited.amount());
        }
    }
}
```

**C#:**
```csharp
public sealed class BankAccount : EsAggregateRoot<AccountId, IInternalDomainEvent>
{
    private Money _balance = new(0);

    public void Deposit(Money amount)
    {
        var @event = new MoneyDeposited(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, amount);
        Apply(@event);
    }

    protected override void _When(IInternalDomainEvent @event)
    {
        switch (@event)
        {
            case MoneyDeposited deposited:
                _balance = _balance.Add(deposited.Amount);
                break;
        }
    }
}
```

### Migrating from Java

See [MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md) for side-by-side Java/C# comparisons of every pattern, syntax mapping tables, and common gotchas.

---

## Documentation

### User Documentation

- 📖 **[API_REFERENCE.md](docs/examples/API_REFERENCE.md)** - Complete API reference
  - Every public type and method with signatures, exceptions, and examples
  - Verified against the shipped public API baseline
- 📚 **[USAGE_EXAMPLES.md](docs/examples/USAGE_EXAMPLES.md)** - Real-world examples
  - 30+ compile-verified scenarios (banking, e-commerce, inventory, order management)
  - Event sourcing, state sourcing, CQRS, and reconciliation walkthroughs
- 🔄 **[MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md)** - Java → .NET migration guide
- 📝 **[CHANGELOG.md](CHANGELOG.md)** - Release history (Keep a Changelog / SemVer)

### Developer Documentation

- 👨‍💻 **[AGENTS.md](AGENTS.md)** - Development standards and workflow (TDD, Tidy First, build/test commands)
- 📋 **[docs/adr/](docs/adr/)** - Architecture Decision Records documenting design rationale
- 🗺️ **[ROADMAP.md](ROADMAP.md)** - Current status and post-1.0 considerations

---

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines.

Before contributing:
1. Read the [Architecture Decision Records](docs/adr/) to understand design rationale
2. Follow the standards in [AGENTS.md](AGENTS.md) — tests first (TDD), >90% coverage

---

## License

**MIT License** — Copyright (c) 2025-2026 ezDDD.NET Contributors. See [LICENSE](LICENSE) for details.

This project is a derivative work of the [Java ezddd library](https://gitlab.com/TeddyChen/ezddd)
by Teddy Chen and contributors, which is licensed under the **Apache License 2.0**.
See [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt) for the required attribution and license text.

---

## References

### Original Java Version (6.0.1)

**This .NET port is based on Java ezddd 6.0.1** (GitLab commit: `3aac0f5`; synchronized 2.1.0 → 4.1.0 → 6.0.1 before first publication)

- **Repository**: [Java ezddd (GitLab)](https://gitlab.com/TeddyChen/ezddd) by [Teddy Chen](https://gitlab.com/TeddyChen) (TeddySoft)
- **Ecosystem**: [uContract.NET](https://github.com/cwouyang/uContract.NET) - Design by Contract dependency

### Theory

- [Domain-Driven Design](https://www.domainlanguage.com/ddd/) - Eric Evans, tactical DDD patterns
- [Event Sourcing](https://martinfowler.com/eaaDev/EventSourcing.html) - Martin Fowler
- [CQRS](https://cqrs.wordpress.com/wp-content/uploads/2010/11/cqrs_documents.pdf) - Greg Young
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) - Robert C. Martin

---
