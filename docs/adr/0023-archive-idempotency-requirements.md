# ADR-0023: Archive Idempotency Requirements

## Status

**Accepted**

- **Date**: 2025-11-18
- **Deciders**: Development Team
- **Status Date**: 2025-11-18

---

## Context

### Problem Statement

Should IArchive operations (SaveAsync, DeleteAsync) be required to be idempotent, and what are the implications for projector reliability and event replay?

### Relevant Context

- **Event replay**: Projectors may process the same event multiple times due to:
  - Message bus failures and retries
  - Projector restarts and catch-up
  - Network partitions and duplicate deliveries
  - Event store rebuilds
- **CQRS pattern**: Read models are eventually consistent, projectors update them asynchronously
- **Reliability requirement**: System must tolerate duplicate event processing without corruption
- **Phase 4 implementation**: Iteration 5 implemented InMemoryArchive with upsert semantics
- **Database semantics**: Different databases have different default behaviors (INSERT vs UPSERT)

### Constraints

- **Projector simplicity**: Projectors should not need complex duplicate detection logic
- **Database agnostic**: IArchive contract should work with any database
- **Performance**: Idempotent operations should not significantly degrade performance
- **At-least-once delivery**: Event buses typically guarantee at-least-once, not exactly-once

---

## Decision

**IArchive.SaveAsync and IArchive.DeleteAsync MUST be idempotent. SaveAsync uses upsert semantics (insert or update). DeleteAsync silently succeeds if data doesn't exist.**

### Details

#### IArchive Contract

```csharp
namespace EzDdd.Cqrs.Query;

/// <summary>
///     <c>IArchive</c> is the query database access interface for read models.
///     All operations MUST be idempotent to support reliable event replay.
/// </summary>
public interface IArchive<TData, in TId>
{
    /// <summary>
    ///     Finds data by ID.
    /// </summary>
    /// <returns>Data if found, null otherwise.</returns>
    Task<TData?> FindByIdAsync(TId id);

    /// <summary>
    ///     Saves data using upsert semantics (insert or update).
    ///     MUST be idempotent: Calling multiple times with same data has same effect as calling once.
    /// </summary>
    /// <remarks>
    ///     <b>Idempotency Requirement</b>: If data with same ID already exists, update it.
    ///     Otherwise, insert new data. Multiple saves of same data produce same result.
    /// </remarks>
    Task SaveAsync(TData data);

    /// <summary>
    ///     Deletes data.
    ///     MUST be idempotent: Deleting non-existent data silently succeeds (no exception).
    /// </summary>
    /// <remarks>
    ///     <b>Idempotency Requirement</b>: If data doesn't exist, operation succeeds without error.
    ///     Multiple deletes of same data produce same result (data is gone).
    /// </remarks>
    Task DeleteAsync(TData data);
}
```

#### Idempotent SaveAsync Implementation

```csharp
public class InMemoryArchive<TData, TId> : IArchive<TData, TId>
    where TData : class
    where TId : notnull
{
    private readonly ConcurrentDictionary<TId, TData> _store = new();
    private readonly Func<TData, TId> _idExtractor;

    public InMemoryArchive(Func<TData, TId> idExtractor)
    {
        _idExtractor = idExtractor;
    }

    public Task<TData?> FindByIdAsync(TId id)
    {
        _store.TryGetValue(id, out var data);
        return Task.FromResult(data);
    }

    // Idempotent: Upsert semantics (insert or update)
    public Task SaveAsync(TData data)
    {
        var id = _idExtractor(data);
        _store[id] = data; // ✅ Idempotent - overwrites if exists, inserts if not
        return Task.CompletedTask;
    }

    // Idempotent: Silently succeeds if not exists
    public Task DeleteAsync(TData data)
    {
        var id = _idExtractor(data);
        _store.TryRemove(id, out _); // ✅ Idempotent - returns false if not exists, no exception
        return Task.CompletedTask;
    }
}
```

#### SQL Implementation Example

```csharp
public class SqlArchive<TData, TId> : IArchive<TData, TId>
    where TData : class
    where TId : notnull
{
    private readonly DbContext _dbContext;

    // Idempotent SaveAsync using MERGE or INSERT ... ON CONFLICT
    public async Task SaveAsync(TData data)
    {
        var id = ExtractId(data);
        var existing = await _dbContext.Set<TData>().FindAsync(id);

        if (existing != null)
        {
            // Update existing
            _dbContext.Entry(existing).CurrentValues.SetValues(data);
        }
        else
        {
            // Insert new
            _dbContext.Set<TData>().Add(data);
        }

        await _dbContext.SaveChangesAsync();
        // ✅ Idempotent - same data saved multiple times produces same result
    }

    // Idempotent DeleteAsync
    public async Task DeleteAsync(TData data)
    {
        var id = ExtractId(data);
        var existing = await _dbContext.Set<TData>().FindAsync(id);

        if (existing != null)
        {
            _dbContext.Set<TData>().Remove(existing);
            await _dbContext.SaveChangesAsync();
        }
        // ✅ Idempotent - silently succeeds if not exists, no exception thrown
    }
}
```

---

## Consequences

### Positive Consequences

- ✅ **Reliable Event Replay**: Projectors can replay events without corruption
- ✅ **Duplicate Tolerance**: System tolerates duplicate event deliveries gracefully
- ✅ **Simplified Projectors**: No need for complex duplicate detection in projectors
- ✅ **At-Least-Once Delivery**: Compatible with typical event bus guarantees
- ✅ **Crash Recovery**: Projectors can restart and catch up without manual intervention
- ✅ **Testability**: Easy to verify idempotency in unit tests

### Negative Consequences

- ❌ **Performance Overhead**: Upsert may be slower than pure insert (database-dependent)
- ❌ **Implementation Complexity**: Database-specific upsert syntax varies (MERGE, INSERT ON CONFLICT, etc.)
- ❌ **Concurrency Issues**: Multiple projectors updating same read model may race

### Neutral Consequences

- ⚖️ **Database Choice**: Some databases (e.g., PostgreSQL, SQL Server) handle upsert efficiently, others don't
- ⚖️ **Optimistic Locking**: May need version fields for conflict detection in highly concurrent scenarios
- ⚖️ **Documentation Burden**: Must clearly document idempotency requirement for implementers

---

## Alternatives Considered

### Alternative 1: Non-Idempotent Operations with Duplicate Detection

```csharp
public interface IArchive<TData, TId>
{
    Task<TData?> FindByIdAsync(TId id);

    // Non-idempotent: Throws exception if already exists
    Task InsertAsync(TData data); // ❌ Throws on duplicate

    // Non-idempotent: Throws exception if not exists
    Task UpdateAsync(TData data); // ❌ Throws if missing

    Task DeleteAsync(TData data); // ❌ Throws if not exists
}
```

**Projector handles duplicates**:
```csharp
public async Task UpdateAsync(DomainEventData eventData)
{
    var domainEvent = _eventMapper.ToDomainEvent(eventData);

    switch (domainEvent)
    {
        case AccountCreated e:
            // Check if already exists
            var existing = await _archive.FindByIdAsync(e.AccountId);
            if (existing == null)
            {
                var readModel = new AccountSummaryReadModel(/*...*/);
                await _archive.InsertAsync(readModel); // ❌ May still race
            }
            break;
    }
}
```

**Pros**:
- Explicit insert vs update operations
- Potential performance benefit (no upsert overhead)

**Cons**:
- **Complex projector logic** - every projector must check for duplicates
- **Race conditions** - check-then-insert is not atomic
- **Error handling burden** - projectors must handle duplicate exceptions
- **Not truly idempotent** - exceptions break idempotency guarantee
- **Fragile** - easy to forget duplicate handling in new projectors

**Why rejected**: Violates "pit of success" principle. Projectors should be simple, archive should handle complexity.

---

### Alternative 2: Event Deduplication at Message Bus Level

**Message bus tracks processed events**:
```csharp
public class DeduplicatingMessageBus<T> : IMessageBus<T>
{
    private readonly HashSet<string> _processedEventIds = new();

    public async Task PublishAsync(T message)
    {
        var eventId = ExtractEventId(message);

        if (_processedEventIds.Contains(eventId))
        {
            return; // ✅ Skip duplicate
        }

        _processedEventIds.Add(eventId);
        await _actualBus.PublishAsync(message);
    }
}
```

**Pros**:
- Prevents duplicates at source
- No archive idempotency needed

**Cons**:
- **State management** - message bus must persist processed IDs (new complexity)
- **Scalability** - HashSet grows unbounded without cleanup
- **Distributed systems** - doesn't work across multiple instances
- **False sense of security** - cannot guarantee exactly-once in distributed systems
- **Incomplete solution** - doesn't handle projector restarts or catch-up

**Why rejected**: Impossible to guarantee exactly-once delivery in distributed systems. Archives must be idempotent regardless.

---

### Alternative 3: Separate Insert and Update Methods (Non-Idempotent)

```csharp
public interface IArchive<TData, TId>
{
    Task<TData?> FindByIdAsync(TId id);
    Task InsertAsync(TData data);  // Throws if exists
    Task UpdateAsync(TData data);  // Throws if not exists
    Task DeleteAsync(TData data);  // Throws if not exists
}
```

**Pros**:
- Explicit operations
- Matches traditional CRUD

**Cons**:
- **Not idempotent** - duplicate events cause exceptions
- **Complex projectors** - must decide insert vs update
- **Race conditions** - check-then-act pattern is inherently unsafe
- **Poor reliability** - cannot handle duplicate events

**Why rejected**: Same problems as Alternative 1. Non-idempotent operations are fundamentally incompatible with reliable event processing.

---

## Related Decisions

- **Related to**: [ADR-0020](0020-iprojector-lifecycle-management.md) (Projectors rely on idempotent archives)
- **Related to**: [ADR-0018](0018-iarchive-async-method-design.md) (IArchive method signatures)
- **Related to**: [ADR-0022](0022-read-model-design-patterns.md) (Read models are stored in archives)
- **Related to**: Phase 3 message bus (at-least-once delivery semantics)

---

## Implementation Notes

### Testing Idempotency

```csharp
[Fact]
public async Task SaveAsync_CalledTwice_Idempotent()
{
    // Arrange
    var archive = new InMemoryArchive<AccountReadModel, AccountId>(x => x.AccountId);
    var readModel = new AccountReadModel(
        new AccountId("ACC-001"),
        "1234567890",
        100.00m,
        DateTimeOffset.UtcNow
    );

    // Act
    await archive.SaveAsync(readModel);
    await archive.SaveAsync(readModel); // Second save

    // Assert
    var result = await archive.FindByIdAsync(new AccountId("ACC-001"));
    Assert.NotNull(result);
    Assert.Equal(100.00m, result.Balance);
    // ✅ Second save has no different effect
}

[Fact]
public async Task DeleteAsync_NonExistent_SilentlySucceeds()
{
    // Arrange
    var archive = new InMemoryArchive<AccountReadModel, AccountId>(x => x.AccountId);
    var readModel = new AccountReadModel(
        new AccountId("ACC-999"),
        "9999999999",
        0m,
        DateTimeOffset.UtcNow
    );

    // Act & Assert - should not throw
    await archive.DeleteAsync(readModel); // Deleting non-existent
    await archive.DeleteAsync(readModel); // Delete again
    // ✅ Both operations succeed silently
}
```

### Database-Specific Upsert Syntax

**PostgreSQL**:
```sql
INSERT INTO read_models (id, data, version)
VALUES (@id, @data, @version)
ON CONFLICT (id) DO UPDATE
SET data = EXCLUDED.data, version = EXCLUDED.version;
```

**SQL Server**:
```sql
MERGE INTO read_models AS target
USING (SELECT @id AS id, @data AS data, @version AS version) AS source
ON target.id = source.id
WHEN MATCHED THEN
    UPDATE SET data = source.data, version = source.version
WHEN NOT MATCHED THEN
    INSERT (id, data, version) VALUES (source.id, source.data, source.version);
```

**MySQL**:
```sql
INSERT INTO read_models (id, data, version)
VALUES (@id, @data, @version)
ON DUPLICATE KEY UPDATE
data = VALUES(data), version = VALUES(version);
```

### Concurrency Handling (Optional)

For highly concurrent scenarios, consider optimistic locking:

```csharp
public record AccountReadModel(
    AccountId AccountId,
    string AccountNumber,
    decimal Balance,
    DateTimeOffset LastUpdated,
    long Version // Optimistic lock version
);

public async Task SaveAsync(AccountReadModel data)
{
    var existing = await _archive.FindByIdAsync(data.AccountId);

    if (existing != null && existing.Version > data.Version)
    {
        // Stale data, skip update
        return;
    }

    var updated = data with { Version = data.Version + 1 };
    await _archive.SaveAsync(updated);
}
```

---

## References

- **Martin Fowler**: [Event Sourcing - Idempotent Receivers](https://martinfowler.com/eaaDev/EventSourcing.html)
- Phase 4 implementation plan - Iteration 5 (IArchive) and Iteration 6 (projector tests) (internal working note, not retained in the repository)
- Phase 4 session notes - Implementation evidence (InMemoryArchive with upsert semantics; internal working notes, not retained in the repository)
- **PostgreSQL Docs**: [INSERT ... ON CONFLICT](https://www.postgresql.org/docs/current/sql-insert.html#SQL-ON-CONFLICT)
- **SQL Server Docs**: [MERGE Statement](https://learn.microsoft.com/en-us/sql/t-sql/statements/merge-transact-sql)

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2025-11-18 | Accepted    | Initial decision after Iteration 5 & 6 implementation |

---
