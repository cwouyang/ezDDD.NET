# ADR-0025: MessageProducer Refactoring - Java 4.1.0 Alignment

## Status

**Accepted (Amended by [ADR-0029](0029-messageproducer-removal-gateway-deferral.md))**

> Partially amended: upstream Java 6.0.0 (commit `67686ac`) moved the `MessageProducer`
> interface out of core into the external `ezddd-gateway` artifact. `IMessageProducer` and
> `InMemoryMessageProducer` are therefore removed from EzDdd.UseCase; the official .NET
> abstraction will ship in the ezDDD.Gateway package post-1.0 (see ADR-0029). The
> producer-only pattern, the MessageBus removal, and the Relay (Transactional Outbox)
> guidance in this ADR remain in force.

- **Date**: 2026-01-07
- **Deciders**: Development Team
- **Status Date**: 2026-07-04

---

## Context

### Problem Statement

The current messaging architecture (`IMessageBus`) mixes two distinct responsibilities: **subscription management** (Register/Unregister) and **message posting** (PostAsync). This violates the Single Responsibility Principle and creates unnecessary complexity for components that only need to publish messages (e.g., repositories, use cases).

Additionally, Java ezddd 4.1.0 (commit `676e0e0`) has simplified its messaging architecture by removing the MessageBus pattern entirely and replacing it with a pure producer-only pattern. ezDDD.NET must align with this design to maintain semantic parity.

### Current Architecture (Java 2.1.0-based)

```csharp
// IMessageBus: Mixed responsibilities
public interface IMessageBus<TMessage>
{
    void Register(IReactor<TMessage> reactor);      // Subscription management
    void Unregister(IReactor<TMessage> reactor);    // Subscription management
    Task PostAsync(TMessage message);               // Message posting
}

// Usage: Complex setup
var eventBus = new BlockingMessageBus<DomainEventData>();
var eventProducer = new EventBusProducer(eventBus);  // Wrapper
var reactor = new GenericReactor<DomainEventData>(...);
eventBus.Register(reactor);
var repository = new EsRepository<BankAccount, AccountId>(peer);

// Manual event posting
await repository.SaveAsync(account);
foreach (var evt in events) {
    await eventProducer.PostAsync(DomainEventMapper.ToData(evt));
}
```

**Issues**:
1. **Mixed Responsibilities**: Subscription management + message posting in same interface
2. **Tight Coupling**: EventBusProducer wraps IMessageBus (unnecessary composition)
3. **Complexity**: Three-layer abstraction (IMessageBus → BlockingMessageBus → EventBusProducer)
4. **Java Divergence**: Java 4.1.0 removed subscription management from MessageProducer

### Relevant Context

- **Java ezddd 4.1.0 Change**: Commit `676e0e0` replaced MessageBus pattern with MessageProducer pattern
- **Current .NET Implementation**: Already has `IMessageProducer<TMessage>` but it wraps `IMessageBus`
- **Semantic Parity Goal**: Maintain ~99% alignment with Java ezddd 4.1.0
- **Pre-Publication Status**: ezDDD.NET has NOT been published to NuGet yet

### Constraints

- ✅ Must align with Java ezddd 4.1.0 MessageProducer design
- ✅ Must support resource cleanup for external message brokers (IDisposable)
- ✅ Must support async/await throughout (PostAsync, not post)
- ✅ Must be simple enough for users to implement custom adapters (Kafka, RabbitMQ)
- ✅ No breaking changes impact (not yet published to NuGet)

---

## Decision

**Replace the mixed IMessageBus pattern with a pure producer-only pattern by removing subscription management from the messaging infrastructure.**

### Details

#### New Architecture

```csharp
/// <summary>
/// Message producer interface for posting messages.
/// This is the PRIMARY interface for components that need to publish messages.
/// </summary>
public interface IMessageProducer<in TMessage> : IDisposable
{
    Task PostAsync(TMessage message);
}
```

**Key Changes**:

1. **IMessageProducer becomes the primary interface** (no longer wraps IMessageBus)
2. **Remove IMessageBus interface** (subscription management removed)
3. **Remove BlockingMessageBus implementation**
4. **Remove EventBusProducer wrapper**
5. **Remove IReactor and GenericReactor** (application layer handles subscriptions)

#### Independent Relay Pattern (Matches Java 4.1.0)

**Repositories do NOT publish events directly**. Event publishing is handled by an independent Relay component (Transactional Outbox pattern):

```csharp
// Repository - Only saves to event store
public class EsRepository<TAggregate, TId> : IRepository<TAggregate, TId, IInternalDomainEvent>
{
    private readonly IRepositoryPeer<EventStoreData<TId>, TId> _peer;

    public EsRepository(IRepositoryPeer<EventStoreData<TId>, TId> peer)
    {
        _peer = peer;
        // NO MessageProducer dependency
    }

    public async Task SaveAsync(TAggregate aggregate)
    {
        await _peer.SaveAsync(data);
        aggregate.ClearDomainEvents();
        // NO event publishing here
    }
}

// EventStoreRelay - Background service publishes events
public class EventStoreRelay : BackgroundService
{
    private readonly IEventStore _eventStore;
    private readonly IMessageProducer<DomainEventData> _messageProducer;
    private int _currentIndex = -1;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var newEvents = await _eventStore.GetEventsAfterAsync(_currentIndex);

            foreach (var evt in newEvents)
            {
                try
                {
                    await _messageProducer.PostAsync(DomainEventMapper.ToData(evt));
                    _currentIndex++;  // Mark as published
                }
                catch (Exception ex)
                {
                    // Caught, will retry on next poll
                    _logger.LogError(ex, "Failed to publish, will retry");
                    break;
                }
            }

            await Task.Delay(_pollingIntervalMs, stoppingToken);
        }
    }
}
```

**See**: `examples/EventInfrastructure/EventStoreRelay.cs` for complete implementation

#### Implementation Classes

**InMemoryMessageProducer** (for testing):
```csharp
public class InMemoryMessageProducer<TMessage> : IMessageProducer<TMessage>
{
    private readonly ConcurrentQueue<TMessage> _postedMessages;
    public IReadOnlyCollection<TMessage> PostedMessages => _postedMessages.ToArray();

    public Task PostAsync(TMessage message)
    {
        _postedMessages.Enqueue(message);
        return Task.CompletedTask;
    }

    public void Dispose() { /* cleanup */ }
}
```

**External Broker Adapters** (production):
```csharp
// Example: Kafka adapter
public class KafkaMessageProducer : IMessageProducer<DomainEventData>
{
    private readonly IProducer<string, byte[]> _kafkaProducer;

    public async Task PostAsync(DomainEventData message)
    {
        await _kafkaProducer.ProduceAsync(topic, new Message<string, byte[]>
        {
            Key = message.Id.ToString(),
            Value = message.EventBody
        });
    }

    public void Dispose() => _kafkaProducer?.Dispose();
}
```

#### Subscription Management (Application Layer)

Subscription logic moves to application layer (e.g., BackgroundService, Relay pattern):

```csharp
// Example: Relay service polls outbox and forwards events
public class EventRelayService : BackgroundService
{
    private readonly IOutboxStore _outboxStore;
    private readonly IMessageProducer<DomainEventData> _eventProducer;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var unpublishedEvents = await _outboxStore.GetUnpublishedEventsAsync();

            foreach (var evt in unpublishedEvents)
            {
                await _eventProducer.PostAsync(evt);
                await _outboxStore.MarkAsPublishedAsync(evt.Id);
            }

            await Task.Delay(pollingInterval, ct);
        }
    }
}
```

---

## Consequences

### Positive Consequences

- ✅ **Simplified API Surface**: IMessageProducer only has PostAsync() method (Single Responsibility)
- ✅ **Clearer Separation of Concerns**: Message posting separate from subscription management
- ✅ **Java 4.1.0 Parity**: Aligns with Java ezddd's simplified MessageProducer design
- ✅ **Easier to Implement**: Custom adapters (Kafka, RabbitMQ) only need PostAsync() + Dispose()
- ✅ **Less Code Complexity**: Removes 3-layer abstraction (IMessageBus → BlockingMessageBus → EventBusProducer)
- ✅ **More Flexible**: Subscription management can be implemented differently per application
- ✅ **Testability**: InMemoryMessageProducer.PostedMessages makes verification simple
- ✅ **No Migration Impact**: Not yet published to NuGet, users get clean API from day one

### Negative Consequences

- ❌ **Application Layer Responsibility**: Applications must implement subscription logic (BackgroundService/Relay)
- ❌ **More Code for Complex Scenarios**: Multi-subscriber scenarios require custom relay implementation
- ❌ **Learning Curve**: Users must understand Relay pattern for event forwarding

### Neutral Consequences

- ⚖️ **Subscription Patterns Vary**: Different applications may implement subscriptions differently (polling, push, hybrid)
- ⚖️ **Framework Agnostic**: No built-in subscription mechanism means more flexibility but less guidance

---

## Alternatives Considered

### Alternative 1: Keep IMessageBus with Subscription Management

**Description**: Maintain the current `IMessageBus` interface with Register/Unregister/PostAsync methods.

**Pros**:
- ✅ No breaking changes needed
- ✅ Built-in subscription management (convenient for simple cases)
- ✅ Familiar pattern for users already using ezDDD.NET internally

**Cons**:
- ❌ Violates Single Responsibility Principle
- ❌ Deviates from Java ezddd 4.1.0 design
- ❌ Complex 3-layer abstraction
- ❌ Locks users into Observer pattern (less flexible)
- ❌ Harder to implement custom message broker adapters

**Why rejected**:
- Semantic parity with Java 4.1.0 is a core goal
- SRP violation creates long-term maintenance burden
- Users are better served by simpler, focused interfaces

---

### Alternative 2: Gradual Deprecation ([Obsolete] → Removal)

**Description**: Mark `IMessageBus` as `[Obsolete]` in Stage S3, remove in Stage S7.

**Pros**:
- ✅ Gradual transition gives users time to adapt
- ✅ Both patterns available during transition period

**Cons**:
- ❌ Increases codebase complexity temporarily
- ❌ Maintenance burden of supporting both patterns
- ❌ Confusing for users (which pattern to use?)
- ❌ ezDDD.NET not yet published, so no migration needed

**Why rejected**:
- Since not yet published to NuGet, we can deliver clean API from day one
- No users affected by immediate removal
- Simpler to maintain single pattern

---

### Alternative 3: Relay Pattern in Framework (Built-in BackgroundService)

**Description**: Provide a built-in `EventRelayService : BackgroundService` that polls event stores and forwards events.

**Pros**:
- ✅ Convenience for users (batteries-included approach)
- ✅ Reference implementation for relay pattern

**Cons**:
- ❌ Adds framework opinion on subscription management
- ❌ Increases framework complexity
- ❌ May not fit all application architectures
- ❌ Java ezddd doesn't provide built-in relay (divergence)

**Why rejected**:
- Keep framework minimal and unopinionated
- Users can implement relay based on their architecture
- Provide guidance in documentation, not built-in implementation
- Maintain Java parity (Java also doesn't provide built-in relay)

---

## Related Decisions

- **Related to**: [ADR-0012 - Resource Management for Event Bus Producers](0012-resource-management-event-bus-producers.md)
  - ADR-0012 established IDisposable requirement for resource cleanup
  - This ADR simplifies the interface while maintaining resource management

- **Builds on**: [ADR-0016 - Async/Await Throughout](0016-async-await-throughout.md)
  - Continues async-first approach with PostAsync()

- **Supersedes**: Implicit IMessageBus design from Phase 3
  - IMessageBus pattern removed entirely
  - Pure producer pattern replaces mixed responsibility pattern

---

## Implementation Notes

### Migration Pattern (Pre-Publication)

Since ezDDD.NET has NOT been published to NuGet, we use the **Clean Break** strategy:

```
1. ✅ Remove IMessageBus, BlockingMessageBus, EventBusProducer immediately
2. ✅ Update all internal code to use InMemoryMessageProducer
3. ✅ Update all integration tests
4. ✅ Users get clean API from first 1.0.0 release
```

No backward compatibility needed.

### Event Publishing Flow

```
Repository.SaveAsync(aggregate):
  1. Persist to event store via IRepositoryPeer
  2. If (_eventProducer != null)
       foreach (event in aggregate.GetDomainEvents())
         await _eventProducer.PostAsync(eventData)
  3. aggregate.ClearDomainEvents()
```

**Exception Handling**:
- Persistence failure → `RepositorySaveException` (events remain in aggregate)
- Event publishing failure → `PostEventFailureException` (aggregate already persisted)

### Testing Strategy

**Unit Tests**:
- `InMemoryMessageProducerTests.cs` - 17 tests (initialization, posting, thread safety, disposal)
- `PostEventFailureExceptionTests.cs` - 3 tests (constructors)

**Integration Tests**:
- Updated to use `InMemoryMessageProducer` instead of `BlockingMessageBus`
- Verify automatic event publishing via `eventProducer.PostedMessages`
- Total: 487 tests passing

### Code Changes Summary

**New Files** (3):
- `src/EzDdd.UseCase/Port/InOut/Messaging/InMemoryMessageProducer.cs`
- `src/EzDdd.UseCase/Exceptions/PostEventFailureException.cs`
- `tests/EzDdd.UseCase.Tests/Port/InOut/Messaging/InMemoryMessageProducerTests.cs`
- `tests/EzDdd.UseCase.Tests/Exceptions/PostEventFailureExceptionTests.cs`

**Removed Files** (9):
- `src/EzDdd.UseCase/Port/InOut/Messaging/IMessageBus.cs`
- `src/EzDdd.UseCase/Port/InOut/Messaging/BlockingMessageBus.cs`
- `src/EzDdd.UseCase/Port/InOut/Messaging/EventBusProducer.cs`
- `src/EzDdd.UseCase/Port/InOut/Messaging/GenericReactor.cs`
- `src/EzDdd.UseCase/Port/In/IReactor.cs`
- `tests/EzDdd.UseCase.Tests/Port/InOut/Messaging/BlockingMessageBusTests.cs`
- `tests/EzDdd.UseCase.Tests/Port/InOut/Messaging/EventBusProducerTests.cs`
- `tests/EzDdd.UseCase.Tests/Port/InOut/Messaging/GenericReactorTests.cs`
- `tests/EzDdd.UseCase.Tests/Port/InOut/Messaging/MessageBusTests.cs`
- `tests/EzDdd.UseCase.Tests/Integration/MessageBusIntegrationTests.cs`

**Modified Files** (6):
- `src/EzDdd.UseCase/Port/InOut/Messaging/IMessageProducer.cs` (documentation update)
- `src/EzDdd.UseCase/Port/Out/EsRepository.cs` (add eventProducer parameter)
- `src/EzDdd.UseCase/Port/Out/OutboxRepository.cs` (add eventProducer parameter)
- `src/EzDdd.Cqrs/Command/ICommand.cs` (XML docs: IMessageBus → IMessageProducer)
- `src/EzDdd.Cqrs/Query/IProjector.cs` (XML docs: remove IReactor references)
- `tests/EzDdd.UseCase.Tests/Integration/CrossComponentIntegrationTests.cs` (use InMemoryMessageProducer)
- `tests/EzDdd.UseCase.Tests/Port/In/InputOutputTests.cs` (remove IReactor tests)
- `tests/EzDdd.Cqrs.Tests/Integration/CompleteCqrsFlowTests.cs` (use InMemoryMessageProducer)
- `tests/EzDdd.Cqrs.Tests/Integration/TestDomain/AccountProjector.cs` (remove IReactor)
- `tests/EzDdd.Cqrs.Tests/Query/ProjectorTests.cs` (remove IReactor test)

### Relay Pattern Guidance (Application Layer)

For applications that need to forward events from an outbox store to a message broker:

```csharp
public class OutboxRelayService : BackgroundService
{
    private readonly IOutboxStore _outboxStore;
    private readonly IMessageProducer<DomainEventData> _messageProducer;
    private readonly int _pollingIntervalMs;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var unpublishedEvents = await _outboxStore.GetUnpublishedEventsAsync();

                foreach (var eventData in unpublishedEvents)
                {
                    await _messageProducer.PostAsync(eventData);
                    await _outboxStore.MarkAsPublishedAsync(eventData.Id);
                }

                await Task.Delay(_pollingIntervalMs, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in outbox relay service");
                await Task.Delay(_pollingIntervalMs * 2, ct); // Back off on error
            }
        }
    }
}
```

This pattern provides:
- ✅ Decoupling between persistence and message broker
- ✅ Retry capability (events remain in outbox on failure)
- ✅ Independent lifecycle management (can start/stop relay independently)

---

## Consequences

### Positive Consequences

- ✅ **Simplified API**: IMessageProducer has single responsibility (posting only)
- ✅ **Java 4.1.0 Parity**: ~99% semantic alignment maintained
- ✅ **Easier Implementation**: Custom message broker adapters only need PostAsync() + Dispose()
- ✅ **Clearer Layering**: Subscription management explicitly application-layer concern
- ✅ **Less Framework Code**: Removed 9 files, ~1,500+ lines of code
- ✅ **More Testable**: InMemoryMessageProducer.PostedMessages enables simple verification
- ✅ **Resource Management**: Maintained IDisposable for broker connections
- ✅ **Backward Compatible**: Optional parameter in repositories preserves existing tests

### Negative Consequences

- ❌ **More Application Code**: Users must implement relay services for event forwarding
- ❌ **No Built-in Subscription**: Framework doesn't provide subscription infrastructure
- ❌ **Learning Curve**: Users must understand Relay pattern for complex scenarios

### Neutral Consequences

- ⚖️ **Flexibility vs Convenience**: More flexible but less convenient than built-in subscriptions
- ⚖️ **Pattern Diversity**: Different apps may implement subscriptions differently

---

## Alternatives Considered

See "Alternatives Considered" section above for detailed analysis of:
1. Alternative 1: Keep IMessageBus (rejected - SRP violation, Java divergence)
2. Alternative 2: Gradual Deprecation (rejected - no users yet, unnecessary complexity)
3. Alternative 3: Built-in Relay Pattern (rejected - adds framework opinion)

---

## References

### Java ezddd 4.1.0
- **Commit**: `676e0e0` - "[Refactoring] Replace MessageBus pattern with MessageProducer pattern"
- **Date**: 2025-11-24
- **Repository**: https://gitlab.com/TeddyChen/ezddd
- **Key Files**:
  - `MessageProducer.java` (interface with post() + Closeable)
  - `FakeMessageProducer.java` (test implementation with Queue)
  - `InMemoryOutboxStoreRelay.java` (relay pattern example)

### Internal Documents
- [DOTNET_PORT.md](../../DOTNET_PORT.md) - Java 4.1.0 Synchronization Plan
- Stage S3 implementation plan (internal working note, superseded by this ADR)
- Checkpoint 3 redo guide - Repository integration guidance (internal working note, not retained in the repository)
- [ADR-0012](0012-resource-management-event-bus-producers.md) - Resource management foundation

### Design Patterns
- **Producer-Consumer Pattern**: https://en.wikipedia.org/wiki/Producer%E2%80%93consumer_problem
- **Relay Pattern**: Application-layer service forwards messages between stores
- **Single Responsibility Principle**: https://en.wikipedia.org/wiki/Single-responsibility_principle

---

## Revision History

| Date       | Status      | Notes                                                  |
|------------|-------------|--------------------------------------------------------|
| 2026-01-07 | Proposed    | Initial draft based on Java 4.1.0 synchronization plan |
| 2026-01-07 | Accepted    | Decision finalized after Checkpoint 4 completion        |
| 2026-07-04 | Amended     | Amended by ADR-0029: IMessageProducer moved out of core (upstream 6.0.0) |

---

## Implementation Checklist

- [x] Remove IMessageBus interface
- [x] Remove BlockingMessageBus implementation
- [x] Remove EventBusProducer wrapper
- [x] Remove IReactor and GenericReactor
- [x] Create InMemoryMessageProducer (17 tests)
- [x] Create PostEventFailureException (3 tests)
- [x] Update IMessageProducer documentation
- [x] Update EsRepository (add optional eventProducer parameter)
- [x] Update OutboxRepository (add optional eventProducer parameter)
- [x] Update all integration tests (CrossComponentIntegrationTests, CompleteCqrsFlowTests)
- [x] Update ICommand and IProjector XML documentation
- [x] Remove IReactor tests from InputOutputTests and ProjectorTests
- [x] Verify all 487 tests passing
- [x] Update README.md with new pattern examples
- [x] Update DOTNET_PORT.md with ADR-0025 reference
- [x] Update ROADMAP.md marking Stage S3 complete
- [x] Update CLAUDE.md reflecting Stage S3 completion
- [x] Update docs/adr/README.md with ADR-0025 entry

---

**Status**: ✅ Accepted and Implemented
**Phase**: Phase 6 Stage S3 (Java 4.1.0 Synchronization)
**Test Coverage**: 487 tests passing (100% pass rate)
