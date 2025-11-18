# ADR-0020: IProjector Lifecycle Management Integration

## Status

**Accepted**

- **Date**: 2025-11-18
- **Deciders**: Development Team
- **Status Date**: 2025-11-18

---

## Context

### Problem Statement

How should `IProjector` integrate with .NET background service lifecycle management while maintaining separation between domain concerns and infrastructure concerns?

### Relevant Context

- **Java ezcqrs**: `Projector` is a pure marker interface with no methods
- **Java pattern**: Implementations typically also implement `Reactor` for event handling
- **.NET platform**: Provides `IHostedService` and `BackgroundService` for background service lifecycle
- **Phase 4 requirement**: Projectors must listen to domain events and update read models in `IArchive`
- **CQRS flow**: Command → Aggregate → Events → Repository → MessageBus → **Projector** → Archive → Query

### Constraints

- **Semantic parity**: Must maintain similar design philosophy to Java ezcqrs
- **Platform idioms**: Should leverage .NET's built-in lifecycle management
- **Separation of concerns**: Domain concepts (IProjector) should not depend on infrastructure (IHostedService)
- **Implementation flexibility**: Applications should choose their own hosting strategies

---

## Decision

**`IProjector` is a pure marker interface with zero methods. Implementations separately implement both domain (IProjector + IReactor) and infrastructure (BackgroundService) concerns.**

### Details

#### IProjector Interface (EzDdd.Cqrs.Query namespace)

```csharp
/// <summary>
///     <c>IProjector</c> is a marker interface for background services
///     that maintain read models.
/// </summary>
public interface IProjector
{
    // Pure marker interface - no methods
    // Implementations typically also implement IReactor and BackgroundService
}
```

#### Projector Implementation Pattern

```csharp
// Application/Infrastructure layer
public class AccountProjector : IProjector, IReactor, BackgroundService
{
    private readonly IArchive<AccountReadModel, AccountId> _archive;
    private readonly IMessageBus<DomainEventData> _eventBus;
    private readonly DomainEventMapper _eventMapper;

    public AccountProjector(
        IArchive<AccountReadModel, AccountId> archive,
        IMessageBus<DomainEventData> eventBus,
        DomainEventMapper eventMapper)
    {
        _archive = archive;
        _eventBus = eventBus;
        _eventMapper = eventMapper;
    }

    // BackgroundService lifecycle
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Subscribe to event bus during startup
        _eventBus.Subscribe(this);
        return Task.CompletedTask;
    }

    // IReactor event handling
    public async Task UpdateAsync(DomainEventData eventData)
    {
        var domainEvent = _eventMapper.ToDomainEvent(eventData);

        switch (domainEvent)
        {
            case AccountCreated e:
                var readModel = new AccountReadModel(
                    e.AccountId,
                    e.AccountNumber,
                    e.InitialBalance,
                    e.OccurredOn
                );
                await _archive.SaveAsync(readModel);
                break;

            case MoneyDeposited e:
                var account = await _archive.FindByIdAsync(e.AccountId);
                if (account != null)
                {
                    var updated = account with
                    {
                        Balance = account.Balance + e.Amount
                    };
                    await _archive.SaveAsync(updated);
                }
                break;

            case AccountClosed e:
                var toDelete = await _archive.FindByIdAsync(e.AccountId);
                if (toDelete != null)
                {
                    await _archive.DeleteAsync(toDelete);
                }
                break;
        }
    }
}
```

#### Hosting Registration (Program.cs or Startup.cs)

```csharp
// Register projector as hosted service
builder.Services.AddHostedService<AccountProjector>();

// Also register dependencies
builder.Services.AddSingleton<IArchive<AccountReadModel, AccountId>,
    InMemoryArchive<AccountReadModel, AccountId>>();
builder.Services.AddSingleton<IMessageBus<DomainEventData>,
    BlockingMessageBus<DomainEventData>>();
```

---

## Consequences

### Positive Consequences

- ✅ **Separation of Concerns**: Domain interface (IProjector) independent of infrastructure (BackgroundService)
- ✅ **Semantic Parity**: Matches Java ezcqrs pure marker interface approach (~100%)
- ✅ **Platform Integration**: Leverages .NET's built-in `BackgroundService` lifecycle management
- ✅ **Implementation Flexibility**: Applications can choose hosting strategy (IHostedService, BackgroundService, or custom)
- ✅ **Testability**: IProjector can be tested without BackgroundService infrastructure
- ✅ **Event Handling Pattern**: Reuses Phase 3's `IReactor` interface for event subscriptions
- ✅ **Clear Responsibilities**: IProjector identifies purpose, IReactor defines behavior, BackgroundService manages lifecycle

### Negative Consequences

- ❌ **Multiple Interface Implementation**: Implementations must implement 3 interfaces (IProjector + IReactor + BackgroundService)
- ❌ **Pattern Complexity**: Developers must understand multiple concepts (marker interface, observer pattern, background service)
- ❌ **No Lifecycle Methods in Domain**: Domain layer has no explicit control over projector lifecycle

### Neutral Consequences

- ⚖️ **Subscription Responsibility**: Event bus subscription is implementation detail (in ExecuteAsync), not enforced by interface
- ⚖️ **Error Handling**: Applications must implement their own error handling and retry logic
- ⚖️ **Graceful Shutdown**: Relies on BackgroundService's `StopAsync` for graceful termination

---

## Alternatives Considered

### Alternative 1: IProjector with Lifecycle Methods

```csharp
public interface IProjector
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
```

**Pros**:
- Explicit lifecycle control in domain interface
- No need to implement BackgroundService separately

**Cons**:
- **Violates separation of concerns** - domain interface depends on infrastructure lifecycle
- **Breaks semantic parity** - Java Projector has no methods
- **Less flexible** - locks implementation into specific lifecycle pattern

**Why rejected**: Violates clean architecture by mixing domain and infrastructure concerns. Would make EzDdd.Cqrs depend on hosting concepts.

---

### Alternative 2: IProjector extends IHostedService

```csharp
public interface IProjector : IHostedService, IReactor
{
    // Inherits StartAsync/StopAsync from IHostedService
}
```

**Pros**:
- Single interface implementation
- Enforces lifecycle methods

**Cons**:
- **Severe violation of separation of concerns** - domain depends directly on infrastructure
- **Breaks semantic parity** - Java Projector is pure marker
- **Tight coupling** - EzDdd.Cqrs would depend on Microsoft.Extensions.Hosting
- **Violates ADR-0004** - Zero third-party dependency principle (Microsoft.Extensions.Hosting is external)

**Why rejected**: Completely unacceptable coupling between domain and infrastructure layers. Violates core architectural principles.

---

### Alternative 3: Abstract Base Class ProjectorService

```csharp
public abstract class ProjectorService<TReadModel, TId>
    : BackgroundService, IProjector, IReactor
{
    protected abstract Task HandleEventAsync(DomainEventData eventData);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Common subscription logic
    }
}
```

**Pros**:
- Reduces boilerplate in implementations
- Enforces pattern consistency

**Cons**:
- **Breaks semantic parity** - Java has no base class
- **Less flexible** - implementations locked into inheritance hierarchy
- **Generic complexity** - not all projectors update single read model
- **Violates "composition over inheritance"** - forces inheritance when composition is better

**Why rejected**: While convenient, base classes reduce flexibility and violate composition over inheritance principle. Java's pure marker interface + composition is superior.

---

## Related Decisions

- **Related to**: [ADR-0016](0016-async-await-throughout.md) (IReactor.UpdateAsync is async)
- **Related to**: [ADR-0008](0008-idomain-event-hierarchy.md) (Projectors handle InternalDomainEvent types)
- **Related to**: [ADR-0022](0022-read-model-design-patterns.md) (Projectors update read models in IArchive)
- **Related to**: [ADR-0023](0023-archive-idempotency-requirements.md) (Projectors rely on idempotent archive operations)

---

## Implementation Notes

### Event Subscription Best Practices

1. **Subscribe in ExecuteAsync**: Event bus subscription should occur in `BackgroundService.ExecuteAsync`
2. **Unsubscribe on Disposal**: Implement `IDisposable` if projector needs cleanup
3. **Idempotent Updates**: Always design projectors to handle duplicate events gracefully
4. **Error Handling**: Wrap event handling in try-catch to prevent projector crashes

### Graceful Shutdown

```csharp
protected override async Task StopAsync(CancellationToken cancellationToken)
{
    // Unsubscribe from event bus
    _eventBus.Unsubscribe(this);

    await base.StopAsync(cancellationToken);
}
```

### Testing Strategy

```csharp
// Unit test projector without BackgroundService infrastructure
[Fact]
public async Task UpdateAsync_AccountCreated_SavesReadModel()
{
    // Arrange
    var archive = new InMemoryArchive<AccountReadModel, AccountId>(x => x.AccountId);
    var eventMapper = new DomainEventMapper();
    var projector = new AccountProjector(archive, null, eventMapper);

    var @event = new AccountCreated(/*...*/);
    var eventData = eventMapper.ToEventData(@event);

    // Act
    await projector.UpdateAsync(eventData);

    // Assert
    var readModel = await archive.FindByIdAsync(@event.AccountId);
    Assert.NotNull(readModel);
}
```

---

## References

- **[PHASE4_JAVA_ANALYSIS.md](../PHASE4_JAVA_ANALYSIS.md)** - Projector analysis (lines 189-216)
- **[PHASE4_API_DESIGN.md](../PHASE4_API_DESIGN.md)** - IProjector C# design
- **[PHASE4_IMPLEMENTATION_PLAN.md](../PHASE4_IMPLEMENTATION_PLAN.md)** - Iteration 4 (IProjector) and Iteration 6 (integration tests)
- **[PHASE4_SESSION_STATE.md](../PHASE4_SESSION_STATE.md)** - Implementation evidence (Iteration 4 and 6 complete)
- **Microsoft Docs**: [BackgroundService](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.backgroundservice)
- **Microsoft Docs**: [IHostedService](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)
- **Java ezcqrs**: [Projector.java](https://gitlab.com/TeddyChen/ezcqrs/-/blob/main/src/main/java/io/github/teddychen/ezcqrs/query/Projector.java)

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2025-11-18 | Accepted    | Initial decision after Iteration 4 & 6 implementation |

---
