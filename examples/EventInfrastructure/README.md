# Event Infrastructure Examples - EventStoreRelay

## Overview

This directory contains reference implementations for the **Relay pattern**, which is used in Java ezddd 4.1.0 to publish events from the event store to message brokers while maintaining strict adherence to the **Transactional Outbox pattern**.

## Transactional Outbox Pattern

### Core Principle

The Transactional Outbox pattern guarantees **at-least-once delivery** of domain events to message brokers, even when the broker experiences temporary failures.

### Pattern Components

1. **Repository**: Saves aggregate state + events to database atomically (transaction)
2. **Event Store/Outbox Table**: Persists events durably
3. **Relay (Background Service)**: Polls event store and publishes events to message broker
4. **Automatic Retry**: Relay catches exceptions and retries until success
5. **Eventual Consistency**: All events WILL BE published eventually

### Why Relay is Required

**Without Relay** (direct publishing in Repository):
- ❌ No eventual consistency guarantee
- ❌ Message broker failure affects save operation
- ❌ No automatic retry mechanism
- ❌ Violates Outbox Pattern completeness

**With Relay** (independent background service):
- ✅ Eventual consistency guaranteed
- ✅ Message broker failure isolated from save
- ✅ Automatic retry on failure
- ✅ Complete Outbox Pattern implementation

## Files in This Directory

- **`IEventStore.cs`** - Interface for event stores supporting relay polling
- **`InMemoryEventStore.cs`** - In-memory implementation for testing
- **`IMessageProducer.cs`** - Minimal producer abstraction used by the relay (example-local, see below)
- **`EventStoreRelay.cs`** - Background service that polls event store and publishes events
- **`README.md`** - This file

## About `IMessageProducer<TMessage>` (Example-Local Abstraction)

The ezDDD.NET **core packages do not contain a message producer abstraction**. Upstream Java
ezddd 6.0.0 (commit `67686ac`) moved the `MessageProducer` interface out of the core library
into the external `ezddd-gateway` artifact, and ezDDD.NET mirrors that module boundary
([ADR-0029](../../docs/adr/0029-messageproducer-removal-gateway-deferral.md)).

The official .NET counterpart will be provided by the **ezDDD.Gateway** package (planned
post-1.0). Until then, this example carries its own minimal `IMessageProducer<TMessage>`
declaration (`IMessageProducer.cs`), and applications that need a producer port should do the
same in their composition root:

```csharp
public interface IMessageProducer<in TMessage> : IDisposable
{
    Task PostAsync(TMessage message);
}
```

## Usage

### Basic Setup

```csharp
// Startup.cs or Program.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register event store
    services.AddSingleton<IEventStore, InMemoryEventStore>();

    // Register message producer (Kafka, RabbitMQ, etc.)
    services.AddSingleton<IMessageProducer<DomainEventData>>(sp =>
        new KafkaMessageProducer(Configuration.GetSection("Kafka")));

    // Register EventStoreRelay as hosted service
    services.AddHostedService<EventStoreRelay>();

    // Register repositories (NO MessageProducer parameter)
    services.AddScoped<IRepository<Order, OrderId>>(sp =>
        new EsRepository<Order, OrderId>(
            sp.GetRequiredService<IRepositoryPeer<EventStoreData<OrderId>, OrderId>>()));
}
```

### Relay Configuration

```csharp
// Configure polling interval
services.AddHostedService<EventStoreRelay>(sp =>
    new EventStoreRelay(
        sp.GetRequiredService<IEventStore>(),
        sp.GetRequiredService<IMessageProducer<DomainEventData>>(),
        sp.GetRequiredService<ILogger<EventStoreRelay>>(),
        pollingIntervalMs: 100  // Poll every 100ms
    ));
```

### Testing with Relay

```csharp
// Simple test double implementing the example-local IMessageProducer<TMessage>
public sealed class FakeMessageProducer<TMessage> : IMessageProducer<TMessage>
{
    private readonly List<TMessage> _postedMessages = [];
    public IReadOnlyList<TMessage> PostedMessages => _postedMessages;

    public Task PostAsync(TMessage message)
    {
        _postedMessages.Add(message);
        return Task.CompletedTask;
    }

    public void Dispose() { }
}

[Fact]
public async Task OrderCreated_EventPublishedViaRelay()
{
    // Arrange
    var eventStore = new InMemoryEventStore();
    var producer = new FakeMessageProducer<DomainEventData>();
    var repository = new EsRepository<Order, OrderId>(
        new InMemoryEventStorePeer(eventStore));

    var relay = new EventStoreRelay(
        eventStore,
        producer,
        NullLogger<EventStoreRelay>.Instance,
        pollingIntervalMs: 50);

    await relay.StartAsync(CancellationToken.None);

    try
    {
        // Act
        var order = new Order(orderId, customerId);
        await repository.SaveAsync(order);

        // Wait for relay to poll and publish (50ms + buffer)
        await Task.Delay(100);

        // Assert
        Assert.Single(producer.PostedMessages);
        Assert.Equal("OrderCreated", producer.PostedMessages.First().EventType);
    }
    finally
    {
        await relay.StopAsync(CancellationToken.None);
    }
}
```

## Implementation Details

### EventStoreRelay Flow

```
┌─────────────────────────────────────────────────────────────┐
│  1. Poll event store for new events (every 100ms)          │
│     ↓                                                        │
│  2. For each new event:                                     │
│     a. Convert to DomainEventData                           │
│     b. Publish to MessageProducer                           │
│     c. If success: increment index, continue                │
│     d. If failure: log error, break, retry on next poll    │
│     ↓                                                        │
│  3. Sleep for polling interval                              │
│     ↓                                                        │
│  4. Repeat until cancelled                                  │
└─────────────────────────────────────────────────────────────┘
```

### Error Handling

The relay uses a **fail-safe approach**:

1. **Publish Failure**: Caught, logged, stops current batch, retries on next poll
2. **Event Store Failure**: Caught, logged, continues polling
3. **Any Other Exception**: Caught, logged, continues polling

**Key Guarantee**: The relay **never gives up**. Failed events will be retried indefinitely until successful.

### Performance Considerations

**Polling Interval Recommendations**:
- **Production**: 100-500ms (balance latency and load)
- **Testing**: 10-50ms (faster feedback)
- **High-throughput**: 50-100ms (minimize latency)
- **Low-priority**: 500-1000ms (reduce load)

**Database Optimization**:
- Index the sequence/timestamp column used by `GetEventsAfterAsync`
- Use efficient range queries (avoid full table scans)
- Consider partitioning for high-volume scenarios

## Production Implementations

### SQL Server Event Store

```csharp
public class SqlEventStore : IEventStore
{
    private readonly string _connectionString;

    public async Task<IReadOnlyList<IInternalDomainEvent>> GetEventsAfterAsync(
        int afterIndex,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = new SqlCommand(
            "SELECT EventData FROM EventStore WHERE SequenceNumber > @AfterIndex ORDER BY SequenceNumber",
            connection);
        command.Parameters.AddWithValue("@AfterIndex", afterIndex);

        var events = new List<IInternalDomainEvent>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var eventData = reader.GetString(0);
            var @event = JsonUtil.Deserialize<IInternalDomainEvent>(eventData);
            events.Add(@event);
        }

        return events.AsReadOnly();
    }
}
```

### PostgreSQL Event Store

```csharp
public class PostgresEventStore : IEventStore
{
    private readonly string _connectionString;

    public async Task<IReadOnlyList<IInternalDomainEvent>> GetEventsAfterAsync(
        int afterIndex,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            "SELECT event_data FROM event_store WHERE sequence_number > $1 ORDER BY sequence_number",
            connection);
        command.Parameters.AddWithValue(afterIndex);

        var events = new List<IInternalDomainEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var eventData = reader.GetString(0);
            var @event = JsonUtil.Deserialize<IInternalDomainEvent>(eventData);
            events.Add(@event);
        }

        return events.AsReadOnly();
    }
}
```

## References

- **Java ezddd InMemoryEventStoreRelay**: Reference implementation in Java
- **Transactional Outbox Pattern**: [Chris Richardson - Microservices.io](https://microservices.io/patterns/data/transactional-outbox.html)
- **ADR-0025**: [MessageProducer Refactoring](../../docs/adr/0025-messageproducer-refactoring-java-4-1-0-alignment.md)
- **ADR-0029**: [MessageProducer Removal from Core & Gateway Package Deferral](../../docs/adr/0029-messageproducer-removal-gateway-deferral.md)
- **Session Handoff**: [Repository MessageProducer Removal](../../docs/SESSION_HANDOFF_REPOSITORY_MESSAGEPRODUCER_REMOVAL.md)

## FAQ

### Q: Why not publish events directly in Repository?

**A**: Direct publishing violates the Outbox Pattern and provides no eventual consistency guarantee. If the message broker is down during `SaveAsync`, the event is saved to the database but never published.

### Q: What if the relay crashes?

**A**: The relay maintains a `currentIndex` that tracks the last successfully published event. When restarted, it resumes from that index. Events are never lost (assuming durable event store).

### Q: Can multiple relay instances run concurrently?

**A**: The current implementation does not support distributed locking. For multiple instances, implement a distributed lock (Redis, database lock) or use message broker partitioning.

### Q: What about at-most-once semantics?

**A**: The relay provides **at-least-once** delivery. Events may be published multiple times if the relay crashes after publishing but before incrementing the index. Your consumers should implement idempotency.

### Q: How do I monitor the relay?

**A**: The relay logs all operations at different levels:
- **Information**: Start, stop, event counts
- **Debug**: Individual event publishing (enable for troubleshooting)
- **Error**: Publish failures with retry information

Use structured logging (Serilog, NLog) to send logs to monitoring systems (Application Insights, Datadog, etc.).

## License

This example code is part of ezDDD.NET and follows the same license as the main project.
