# ezDDD.NET

> Tactical Domain-Driven Design patterns library for .NET

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-Apache%202.0-green)](LICENSE)
[![Status](https://img.shields.io/badge/status-in%20development-orange)](https://github.com)

**ezDDD.NET** is a .NET port of the Java [ezddd](https://gitlab.com/TeddyChen/ezddd) library, providing tactical Domain-Driven Design (DDD) patterns, Command Query Responsibility Segregation (CQRS), and Clean Architecture (CA) support. It supports both **state sourcing** and **event sourcing** for implementing aggregates and repositories.

---

## ⚠️ Project Status

**Currently in development** - Phase 1: Foundation

This library is being actively developed and is not yet ready for production use. See [DOTNET_PORT.md](DOTNET_PORT.md) for the complete porting plan.

---

## 🎯 Features

### Core DDD Tactical Patterns
- **Entities** - Domain objects with unique identity
- **Value Objects** - Immutable domain concepts
- **Aggregate Roots** - Consistency boundaries for domain models
- **Domain Events** - Capture state changes as events

### Event Sourcing Support
- **EsAggregateRoot** - Event-sourced aggregate root with correctness rules (R1, R2, R3)
- **Event replay** - Reconstruct aggregate state from event history
- **Event Store** - Append-only event persistence

### State Sourcing with Transactional Outbox
- **Outbox Repository** - Atomic persistence of state and events
- **Faster reads** - Query current state without event replay
- **Event publishing** - Reliable event distribution pattern

### CQRS Patterns
- **Commands** - Write operations
- **Queries** - Read operations
- **Projections** - Read model builders
- **Projectors** - Background services for maintaining read models

### Clean Architecture
- **Use Cases** - Application logic layer
- **Repository/RepositoryPeer** - Bridge pattern for persistence
- **Ports & Adapters** - Hexagonal architecture support

---

## 📦 Modules

ezDDD.NET is organized into 5 modules:

```
EzDdd.Common       → Foundation utilities (BiMap, IConverter, JsonUtil)
EzDdd.Entity       → Core DDD building blocks (Entity, AggregateRoot, DomainEvent)
EzDdd.UseCase      → Use cases & repositories (IUseCase, IRepository, EsRepository)
EzDdd.Cqrs         → CQRS patterns (ICommand, IQuery, IProjection)
EzDdd.Core         → Aggregator package (references all modules)
```

### Package IDs

- `ezDDD.Common`
- `ezDDD.Entity`
- `ezDDD.UseCase`
- `ezDDD.Cqrs`
- `ezDDD.Core` *(install this for all features)*

---

## 🚀 Quick Start

### Installation

```bash
# Install the core package (includes all modules)
dotnet add package ezDDD.Core

# Or install specific modules
dotnet add package ezDDD.Entity
dotnet add package ezDDD.UseCase
```

### Basic Usage

```csharp
using EzDdd.Entity;
using EzDdd.UseCase;

// Define your aggregate
public class Workflow : EsAggregateRoot<Guid, InternalDomainEvent>
{
    private string _name = null!;

    public Workflow(IEnumerable<InternalDomainEvent> events) : base(events) { }

    protected override void When(InternalDomainEvent @event)
    {
        switch (@event)
        {
            case WorkflowCreated e:
                Id = e.WorkflowId;
                _name = e.Name;
                break;
        }
    }

    public override string GetCategory() => "workflow";
}

// Use in a command
public class CreateWorkflowUseCase : ICommand<CreateWorkflowInput, CqrsOutput<Guid>>
{
    private readonly IRepository<Workflow, Guid> _repository;

    public async Task<CqrsOutput<Guid>> ExecuteAsync(CreateWorkflowInput input)
    {
        var workflow = Workflow.Create(input.Name);
        await _repository.SaveAsync(workflow);
        return CqrsOutput<Guid>.Success(workflow.Id);
    }
}
```

---

## 📚 Documentation

- **[DOTNET_PORT.md](DOTNET_PORT.md)** - Complete porting plan and technical decisions
- **[CLAUDE.md](CLAUDE.md)** - Development guidance
- **[docs/](docs/)** - Detailed documentation (coming soon)

---

## 🏗️ Architecture

### Module Dependency Chain

```
EzDdd.Common (utilities: BiMap, IConverter, JsonUtil)
    ↓
EzDdd.Entity (core DDD: IEntity, AggregateRoot, EsAggregateRoot, DomainEvent)
    ↓
EzDdd.UseCase (use cases: IUseCase, IRepository, EsRepository, OutboxRepository)
    ↓
EzDdd.Cqrs (CQRS: ICommand, IQuery, IProjection, IProjector, IArchive)
    ↓
EzDdd.Core (aggregator module)
```

### Clean Architecture Layers

- **Entities Layer**: `IEntity`, `IValueObject`, `AggregateRoot`
- **Use Cases Layer**: `IUseCase`, `IRepository`, `IInput`, `IOutput`
- **Interface Adapters Layer**: `IRepositoryPeer` implementations, Mappers
- **Frameworks/Drivers Layer**: ASP.NET Core, databases (user implementations)

---

## 🆚 Differences from Java Version

### Expected Syntax Differences

- **Naming**: Java uses `AggregateRoot`, C# uses `IEntity`, `AggregateRoot<TId, TEvent>`
- **Async**: All I/O operations use `async/await` (`Task<T>`)
- **Nullability**: Nullable reference types enabled (`T?`)

### .NET Platform Improvements

- **Pattern Matching**: More concise event handling with switch expressions
- **Records**: Immutable domain events and value objects
- **Async/Await**: Native asynchronous programming throughout
- **Init-only Properties**: Clearer immutability for domain models

See [DOTNET_PORT.md](DOTNET_PORT.md) for complete comparison.

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/EzDdd.Entity.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## 🛠️ Build from Source

```bash
# Clone the repository
git clone https://github.com/TeddyChen/ezddd.NET.git
cd ezddd.NET

# Build all projects
dotnet build

# Run tests
dotnet test

# Create NuGet packages
dotnet pack
```

---

## 📋 Requirements

- **.NET 8.0 SDK** or higher
- **C# 12** or higher

---

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) (coming soon) for guidelines.

---

## 📄 License

This project is licensed under the **Apache License 2.0** - see the [LICENSE](LICENSE) file for details.

Same license as the original Java [ezddd](https://gitlab.com/TeddyChen/ezddd) library.

---

## 🙏 Acknowledgments

- **Original Java ezddd**: [Teddy Chen](https://gitlab.com/TeddyChen) (TeddySoft)
- **Inspiration**: Domain-Driven Design by Eric Evans
- **Architecture**: Clean Architecture by Robert C. Martin

---

## 📞 Links

- **Java ezddd (original)**: https://gitlab.com/TeddyChen/ezddd
- **Documentation**: [docs/](docs/) (coming soon)
- **Issues**: [GitHub Issues](https://github.com/TeddyChen/ezddd.NET/issues) (coming soon)
- **NuGet**: (not yet published)

---

*Last updated: 2025-10-28*
