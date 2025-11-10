# ADR-0012: Resource Management Pattern for External Event Bus Producers

## Status

**Accepted**

- **Date**: 2025-11-10
- **Deciders**: Project maintainers
- **Status Date**: 2025-11-10

---

## Context

### Problem Statement

The `IMessageProducer<TMessage>` interface provides a simplified adapter for posting messages to event buses. While the initial implementation focused on in-memory message buses (e.g., `BlockingMessageBus`), real-world applications need to integrate with external event brokers such as:

- **Apache Kafka**: Distributed streaming platform
- **RabbitMQ**: Message broker with AMQP protocol
- **Azure Service Bus**: Cloud-based messaging service
- **AWS EventBridge**: Serverless event bus

These external adapters typically hold unmanaged resources (network connections, file handles, thread pools) that must be explicitly released to prevent resource leaks, connection pool exhaustion, and memory leaks.

**The question**: Should `IMessageProducer<TMessage>` provide a standard resource management mechanism?

### Relevant Context

- **Java ezddd design**: `MessageProducer<Message>` extends `Closeable`, enabling `try-with-resources` pattern for automatic cleanup
- **Initial C# implementation**: `IMessageProducer<TMessage>` had NO resource management interface (missing `IDisposable`)
- **Phase 3 Review finding**: Identified as **CRITICAL** issue in GROUP_7_REVIEW.md (semantic parity: -8%)
- **.NET idiom**: `IDisposable` is the standard pattern for deterministic resource cleanup
- **Impact scope**: Affects all external event bus adapter implementations (not just in-memory)

### Constraints

- Must follow .NET platform conventions (`IDisposable`, not Java's `Closeable`)
- Must maintain semantic parity with Java ezddd (resource management capability)
- Should not impact existing in-memory implementations negatively
- Must support `using` statement pattern for deterministic cleanup
- Must be idempotent (multiple `Dispose()` calls should be safe)

---

## Decision

**We will make `IMessageProducer<TMessage>` extend `IDisposable`.**

### Details

**Updated Interface Signature**:
```csharp
public interface IMessageProducer<in TMessage> : IDisposable
{
    Task PostAsync(TMessage message);
}
```

**Implementation Requirements**:

1. **All implementations MUST implement `Dispose()`**:
   - In-memory adapters (e.g., `EventBusProducer`): No-op disposal (track state, prevent usage after disposal)
   - External adapters (e.g., `KafkaEventBusProducer`): Close network connections, release resources

2. **`Dispose()` must be idempotent**:
   - Multiple calls to `Dispose()` should be safe
   - Use disposal flag to track state

3. **`PostAsync()` must guard against disposed state**:
   - Throw `ObjectDisposedException` if called after disposal
   - Prevents use-after-dispose bugs

4. **Usage with `using` statement**:
   ```csharp
   using (var producer = new KafkaEventBusProducer(config))
   {
       await producer.PostAsync(eventData);
   } // Dispose() called automatically
   ```

**Example Implementation** (EventBusProducer):
```csharp
public class EventBusProducer : IMessageProducer<DomainEventData>
{
    private readonly IMessageBus<DomainEventData> _eventBus;
    private bool _disposed;

    public EventBusProducer(IMessageBus<DomainEventData> eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task PostAsync(DomainEventData message)
    {
        _ThrowIfDisposed();
        await _eventBus.PostAsync(message);
    }

    public void Dispose()
    {
        _disposed = true;
        // No-op for in-memory bus (no external resources to release)
    }

    private void _ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(EventBusProducer),
                "Cannot post messages to a disposed event bus producer.");
        }
    }
}
```

---

## Consequences

### Positive Consequences

- ✅ **Prevents resource leaks**: External event bus adapters can properly release network connections, file handles, and thread pools
- ✅ **Semantic parity with Java**: Matches Java's `Closeable` interface semantics (100% parity restored)
- ✅ **Follows .NET idiom**: Uses `IDisposable`, the standard .NET pattern for deterministic cleanup
- ✅ **`using` statement support**: Enables idiomatic C# resource management with automatic disposal
- ✅ **Connection pool management**: Prevents connection pool exhaustion in long-running applications
- ✅ **Testability**: Can verify proper disposal in unit tests (e.g., `using` statement behavior)
- ✅ **Fail-fast behavior**: `ObjectDisposedException` prevents subtle bugs from using disposed producers

### Negative Consequences

- ❌ **Breaking change**: Existing `IMessageProducer` implementations must add `Dispose()` method (but only alpha users affected)
- ❌ **Additional boilerplate**: In-memory implementations must track disposal state even if no resources need cleanup
- ❌ **Async disposal limitation**: `IDisposable.Dispose()` is synchronous; async disposal requires `IAsyncDisposable` (deferred for now)

### Neutral Consequences

- ⚖️ **Explicit disposal required**: Developers must remember to dispose (but `using` statement makes this natural)
- ⚖️ **No-op for in-memory**: EventBusProducer disposal is a no-op (only tracks state), but adds consistency

---

## Alternatives Considered

### Alternative 1: Manual `Close()` Method

**Description**: Add a `void Close()` method to `IMessageProducer<TMessage>` instead of implementing `IDisposable`

**Pros**:
- More explicit naming (`Close()` vs `Dispose()`)
- No .NET platform assumptions
- Simpler for beginners (no need to understand IDisposable)

**Cons**:
- **Not idiomatic .NET**: Violates platform conventions
- **No `using` statement support**: Cannot use automatic resource management
- **Inconsistent with .NET BCL**: File streams, network clients, database connections all use IDisposable
- **Confusing for .NET developers**: Introduces unfamiliar resource management pattern

**Why rejected**: Goes against .NET platform conventions. Developers expect resource-holding objects to implement `IDisposable` and support `using` statements. Introducing a custom `Close()` method would make the API feel foreign in the .NET ecosystem.

---

### Alternative 2: Keep Without `IDisposable`

**Description**: Do not add any resource management mechanism to `IMessageProducer<TMessage>`; let external adapter implementations handle cleanup independently

**Pros**:
- No breaking changes
- Simpler interface (one method only)
- In-memory implementations remain trivial

**Cons**:
- **Resource leaks**: External event bus adapters cannot properly clean up connections
- **No standard pattern**: Each adapter would need custom cleanup mechanism
- **Semantic mismatch with Java**: Java has `Closeable`, C# doesn't (-8% semantic parity)
- **Connection pool exhaustion**: Long-running applications would leak connections
- **Memory leaks**: Unmanaged resources held by external client libraries won't be released

**Why rejected**: This is the original problem identified in Phase 3 Review (GROUP_7_REVIEW.md, Critical Issue #1). External event bus integrations are a core use case for `IMessageProducer`, and resource leaks are unacceptable in production systems. The -8% semantic parity penalty is too significant.

---

### Alternative 3: Separate Disposable Interface

**Description**: Create `IDisposableMessageProducer<TMessage> : IMessageProducer<TMessage>, IDisposable`; in-memory producers use base interface, external producers use disposable variant

**Pros**:
- No breaking changes to existing `IMessageProducer`
- In-memory implementations don't need disposal logic
- Consumers can choose disposable vs non-disposable

**Cons**:
- **Type system complexity**: Two similar interfaces create confusion
- **Liskov Substitution violation**: Cannot substitute `IMessageProducer` with `IDisposableMessageProducer` safely in all contexts
- **API fragmentation**: Some methods accept `IMessageProducer`, some accept `IDisposableMessageProducer`
- **Inconsistent with .NET**: .NET typically has one interface, with no-op `Dispose()` when resources don't need cleanup

**Why rejected**: Over-engineering. .NET convention is to implement `IDisposable` universally and use no-op `Dispose()` when resources don't need cleanup (e.g., `MemoryStream.Dispose()`). Type system complexity outweighs the minor benefit of avoiding no-op disposals.

---

### Alternative 4: `IAsyncDisposable` Instead of `IDisposable`

**Description**: Implement `IAsyncDisposable` for async disposal pattern (`await using`)

**Pros**:
- Supports async cleanup (e.g., flushing buffers, graceful connection shutdown)
- More consistent with async `PostAsync()` method
- Modern C# 8+ feature

**Cons**:
- **Unnecessary complexity**: Most external event bus clients have synchronous `Dispose()` methods
- **Java mismatch**: Java's `Closeable.close()` is synchronous (throws `IOException`)
- **Adoption barrier**: `IAsyncDisposable` is less commonly used than `IDisposable`
- **No compelling use case**: Network connection disposal is typically synchronous in client libraries

**Why rejected**: YAGNI (You Aren't Gonna Need It). Synchronous `IDisposable` is sufficient for 99% of external event bus adapters. If async disposal becomes necessary in the future, we can add `IAsyncDisposable` in addition to `IDisposable` (they are not mutually exclusive). Starting with the simpler, more widely adopted `IDisposable` is the pragmatic choice.

---

## Related Decisions

- **Related to**: [ADR-0004](0004-zero-third-party-dependency-principle.md) - External event bus adapters (Kafka, RabbitMQ) are external dependencies; this ADR ensures proper resource management when integrating them
- **Related to**: [ADR-0005](0005-complete-reimplementation-approach.md) - Complete reimplementation requires maintaining semantic parity with Java (resource management is part of Java's API contract)
- **Influences**: Future external event bus adapter implementations (KafkaEventBusProducer, RabbitMQEventBusProducer, AzureServiceBusProducer)

---

## Implementation Notes

### Implementation Checklist (Phase F.1 - Completed 2025-11-10)

- ✅ Updated `IMessageProducer<in TMessage>` to extend `IDisposable`
- ✅ Implemented `Dispose()` in `EventBusProducer` (no-op with state tracking)
- ✅ Added `_disposed` flag and `_ThrowIfDisposed()` guard in `EventBusProducer`
- ✅ Updated XML documentation to explain disposal semantics
- ✅ Added 6 new disposal tests:
  - Dispose sets disposed state
  - Dispose is idempotent (multiple calls safe)
  - PostAsync after Dispose throws ObjectDisposedException
  - Using statement disposes producer
  - Using statement with exception still disposes
  - Dispose does not throw exceptions
- ✅ All 414 tests passing (6 new tests, 408 existing tests preserved)
- ✅ Zero new compiler warnings

### Example External Adapter (Not Yet Implemented)

```csharp
public class KafkaEventBusProducer : IMessageProducer<DomainEventData>
{
    private readonly IProducer<string, byte[]> _kafkaProducer;
    private bool _disposed;

    public KafkaEventBusProducer(ProducerConfig config)
    {
        _kafkaProducer = new ProducerBuilder<string, byte[]>(config).Build();
    }

    public async Task PostAsync(DomainEventData message)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(KafkaEventBusProducer));

        var kafkaMessage = new Message<string, byte[]>
        {
            Key = message.Id.ToString(),
            Value = message.EventBody
        };

        await _kafkaProducer.ProduceAsync("events-topic", kafkaMessage);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _kafkaProducer?.Dispose();  // Close Kafka connection
        _disposed = true;
    }
}
```

### Documentation Guidelines

When implementing external event bus adapters:

1. **Always implement `IDisposable`** (required by interface)
2. **Close network connections in `Dispose()`** (release resources)
3. **Make `Dispose()` idempotent** (safe to call multiple times)
4. **Guard `PostAsync()` with disposal check** (throw `ObjectDisposedException`)
5. **Document disposal requirements in XML docs** (when to call `Dispose()`)
6. **Use `using` statement in examples** (demonstrate proper usage)

---

## References

- [Phase 3 Final Review Report](../review/PHASE3_FINAL_REVIEW_REPORT.md) - Identified IDisposable as critical issue
- [GROUP_7_REVIEW.md](../review/GROUP_7_REVIEW.md) - Detailed analysis of Message Bus group (Critical Issue #1)
- [IMessageProducer.cs](../../src/EzDdd.UseCase/Port/InOut/Messaging/IMessageProducer.cs) - Implementation (lines 64-79)
- [EventBusProducer.cs](../../src/EzDdd.UseCase/Port/InOut/Messaging/EventBusProducer.cs) - Implementation (lines 113-116)
- [Java MessageProducer.java](../../../../ezddd/ezddd-usecase/src/main/java/tw/teddysoft/ezddd/usecase/port/inout/messaging/MessageProducer.java) - Java equivalent with Closeable
- [.NET IDisposable Pattern](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose) - Microsoft documentation
- [PHASE3_POST_REVIEW_SESSION_STATE.md](../../PHASE3_POST_REVIEW_SESSION_STATE.md) - F.1 implementation record (lines 29-58)

---

## Revision History

| Date       | Status   | Notes                          |
|------------|----------|--------------------------------|
| 2025-11-10 | Accepted | Decision finalized, F.1 implementation complete |

---
