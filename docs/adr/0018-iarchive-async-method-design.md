# ADR-0018: IArchive Async Method Design

## Status

**Accepted**

- **Date**: 2025-11-17
- **Deciders**: Project maintainers
- **Status Date**: 2025-11-17

---

## Context

### Problem Statement

How should `IArchive<TData, TId>` methods be designed in C# to provide query-side data access while maintaining the semantic intent of Java ezcqrs's synchronous `Archive<T, ID>` interface and adhering to .NET async best practices?

### Relevant Context

**Java ezcqrs Implementation**:
```java
public interface Archive<T, ID> {
    Optional<T> findById(ID id);
    void save(T data);
    void delete(T data);
}
```

**Phase 3 IRepository Pattern** (from EzDdd.UseCase):
```csharp
public interface IRepository<TAggregate, TId, TEvent>
{
    Task<TAggregate?> FindByIdAsync(TId id);
    Task SaveAsync(TAggregate aggregate);
    Task DeleteAsync(TAggregate aggregate);
}
```

**ADR-0016: Async/Await Throughout**:
> "All I/O operations in ezDDD.NET MUST be asynchronous using the async/await pattern. In-memory operations (domain logic, validation) remain synchronous."

**Key Design Questions**:
1. Should Archive methods be async or synchronous?
2. How to handle nullable returns: `Task<TData?>` vs `Task<Optional<TData>>`?
3. Should SaveAsync/DeleteAsync have return values?
4. What are the idempotency requirements for reliable event processing?

### Constraints

- Must maintain semantic parity with Java ezcqrs (~98% target)
- Must follow ADR-0016 (async/await for all I/O operations)
- Must support reliable event processing by projectors
- Must follow .NET idioms (nullable reference types, async/await)
- Must be consistent with Phase 3 IRepository pattern

---

## Decision

**All `IArchive<TData, TId>` methods will be asynchronous, returning `Task<TData?>` for FindByIdAsync and `Task` for SaveAsync/DeleteAsync. SaveAsync and DeleteAsync operations MUST be idempotent to support reliable event replay in projectors.**

### Details

**C# Implementation**:
```csharp
public interface IArchive<TData, in TId>
{
    /// <summary>
    ///     Finds a read model by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the read model to find.</param>
    /// <returns>
    ///     A task containing the read model if found, or <c>null</c> if not found.
    /// </returns>
    Task<TData?> FindByIdAsync(TId id);

    /// <summary>
    ///     Saves (inserts or updates) a read model in the query database.
    /// </summary>
    /// <param name="data">The read model to save.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    /// <remarks>
    ///     This operation MUST be idempotent - saving the same read model multiple
    ///     times should produce the same result (for reliable event processing).
    /// </remarks>
    Task SaveAsync(TData data);

    /// <summary>
    ///     Deletes a read model from the query database.
    /// </summary>
    /// <param name="data">The read model to delete.</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    /// <remarks>
    ///     This operation MUST be idempotent - deleting a non-existent read model
    ///     should silently succeed without throwing an exception.
    /// </remarks>
    Task DeleteAsync(TData data);
}
```

**Key Design Decisions**:

1. **All Methods Async**: Consistent with ADR-0016 and Phase 3 IRepository pattern - all I/O operations are async
2. **Nullable Reference Types**: Use `Task<TData?>` instead of `Task<Optional<TData>>` - C# idiom, compiler-enforced null checking
3. **No Return Values for Save/Delete**: Return `Task` (async void equivalent) - simpler interface, consistent with Phase 3 IRepository
4. **Contravariant TId**: `in TId` enables flexible interface assignments (e.g., base ID types)
5. **Idempotent Operations**: SaveAsync uses upsert semantics (insert or update), DeleteAsync silently succeeds if record doesn't exist

**Idempotency Semantics**:

- **SaveAsync**:
  - First call: Inserts new record or updates existing record
  - Subsequent calls with same data: Updates existing record (same result)
  - **Upsert semantics**: Implementations should use INSERT OR UPDATE (SQL MERGE, MongoDB upsert, etc.)

- **DeleteAsync**:
  - First call: Deletes existing record
  - Subsequent calls: Silently succeeds (no-op if record doesn't exist)
  - **Never throws**: Missing record is not an error condition

**Usage Example**:
```csharp
// Projector updating read models (idempotent event handling)
public class AccountProjector : IProjector, IReactor
{
    private readonly IArchive<AccountReadModel, AccountId> _archive;

    public async Task ExecuteAsync(DomainEventData eventData)
    {
        switch (eventData)
        {
            case AccountCreated e:
                var readModel = new AccountReadModel(e.AccountId, e.InitialBalance);
                await _archive.SaveAsync(readModel);  // Upsert
                break;

            case MoneyDeposited e:
                var account = await _archive.FindByIdAsync(e.AccountId);  // Returns null if not found
                if (account != null)
                {
                    var updated = account with { Balance = account.Balance + e.Amount };
                    await _archive.SaveAsync(updated);  // Idempotent update
                }
                break;

            case AccountClosed e:
                var toDelete = await _archive.FindByIdAsync(e.AccountId);
                if (toDelete != null)
                {
                    await _archive.DeleteAsync(toDelete);  // Idempotent delete
                }
                break;
        }
    }
}
```

---

## Consequences

### Positive Consequences

- ✅ **Consistent with ADR-0016**: All I/O operations are async throughout ezDDD.NET
- ✅ **C# Nullable Reference Types**: Compile-time null safety with `TData?` (no Optional<T> wrapper needed)
- ✅ **Better Scalability**: Non-blocking I/O enables handling thousands of concurrent queries
- ✅ **Idempotency Enables Reliable Event Replay**: Projectors can safely reprocess events without side effects
- ✅ **Consistent with Phase 3**: Same pattern as IRepository (FindByIdAsync returns nullable, Save/Delete return Task)
- ✅ **Cancellation Support**: Async methods can accept CancellationToken for timeout/cancellation (future extension)

### Negative Consequences

- ⚠️ **Minor Semantic Difference from Java**: Java uses synchronous methods, C# uses async (platform difference)
- ⚠️ **Implementations Must Handle Idempotency**: Developers must ensure SaveAsync uses upsert semantics and DeleteAsync handles missing records
- ⚠️ **Async Complexity**: Async methods add complexity compared to Java's synchronous API (but standard in .NET)

### Neutral Consequences

- ⚖️ **Documentation Critical**: Idempotency requirements must be clearly documented for implementers
- ⚖️ **Testing Required**: Integration tests must explicitly verify idempotency behavior
- ⚖️ **Performance Trade-off**: Async adds minimal overhead (~100ns per call) but gains massive scalability benefits

---

## Alternatives Considered

### Alternative 1: Synchronous Methods (Java Approach)

**Description**: Keep Java's synchronous API exactly

```csharp
public interface IArchive<TData, TId>
{
    TData? FindById(TId id);
    void Save(TData data);
    void Delete(TData data);
}
```

**Pros**:
- Matches Java exactly (semantic parity ~100%)
- Simpler method signatures (no async/await complexity)
- Slightly less overhead (no Task allocation)

**Cons**:
- Violates ADR-0016 (async/await throughout)
- Blocks threads during I/O waits (poor scalability)
- Cannot be used in ASP.NET Core async pipeline without blocking
- Cannot support cancellation tokens
- Inconsistent with Phase 3 IRepository pattern

**Why rejected**: Violates established async pattern (ADR-0016) and .NET best practices. Blocking threads during database I/O is unacceptable in modern .NET applications, especially in ASP.NET Core where it limits scalability to ~200 concurrent requests.

---

### Alternative 2: Optional<T> Return Type

**Description**: Use Optional<T> wrapper like Java instead of nullable reference types

```csharp
public interface IArchive<TData, TId>
{
    Task<Optional<TData>> FindByIdAsync(TId id);
    Task SaveAsync(TData data);
    Task DeleteAsync(TData data);
}
```

**Pros**:
- Matches Java semantics exactly (Optional<T>)
- Explicit handling of "not found" case
- More functional programming style

**Cons**:
- C# has nullable reference types (better native solution)
- Adds unnecessary wrapper type (extra allocation)
- Less idiomatic in C# ecosystem
- Requires Optional<T> implementation (increases API surface)

**Why rejected**: C# nullable reference types (`TData?`) provide compile-time null safety without extra wrapper types. Optional<T> is idiomatic in Java (no native nullability) but not in C# where `?` is the standard idiom.

---

### Alternative 3: Return bool from Save/Delete

**Description**: Return success/failure indicator from mutation methods

```csharp
public interface IArchive<TData, TId>
{
    Task<TData?> FindByIdAsync(TId id);
    Task<bool> SaveAsync(TData data);     // Returns true if saved
    Task<bool> DeleteAsync(TData data);   // Returns true if deleted
}
```

**Pros**:
- Indicates whether operation actually modified data
- Allows caller to detect "already exists" or "not found" conditions
- More explicit about operation results

**Cons**:
- More complex interface (callers must check return value)
- Exceptions are preferred for errors in .NET
- Java doesn't do this (semantic parity concern)
- Idempotency becomes ambiguous (what does false mean?)
- Unnecessary complexity (save failures should throw exceptions)

**Why rejected**: Exceptions should handle error cases, not return values. Boolean returns add complexity without clear benefit. Idempotent operations should always "succeed" (either do the operation or no-op), making boolean returns misleading.

---

### Alternative 4: Non-Idempotent Operations

**Description**: Don't require idempotency, throw exceptions on duplicate save or missing delete

```csharp
public interface IArchive<TData, TId>
{
    Task<TData?> FindByIdAsync(TId id);
    Task SaveAsync(TData data);     // Throws if already exists
    Task DeleteAsync(TData data);   // Throws if not found
}
```

**Pros**:
- Simpler implementations (no duplicate checking)
- More explicit about preconditions
- Catches implementation bugs (duplicate operations)

**Cons**:
- Breaks event replay (projectors cannot replay events idempotently)
- Unreliable for event sourcing (events may be reprocessed)
- Violates CQRS best practices (read models should be rebuildable)
- Requires complex error handling in projectors

**Why rejected**: Idempotency is critical for reliable CQRS systems. Projectors may receive duplicate events (network retries, event store replays, system recovery), and operations must be safe to retry. Non-idempotent operations would make the system unreliable and impossible to recover from failures.

---

## Related Decisions

- **Depends on ADR-0016**: Async/Await Throughout - Establishes that all I/O operations must be async
- **Consistent with Phase 3 IRepository**: Same async pattern (FindByIdAsync returns nullable, Save/Delete return Task)
- **Related to ADR-0023** (to be written): Archive Idempotency Requirements - Will detail specific idempotency implementation requirements

---

## Implementation Notes

### Idempotency Requirements for Implementations

**SaveAsync Implementation Pattern**:
```csharp
public async Task SaveAsync(AccountReadModel data)
{
    // ✅ Correct: Upsert semantics (INSERT OR UPDATE)
    await _dbContext.AccountReadModels
        .Upsert(data)
        .On(a => a.AccountId)  // Key for upsert
        .RunAsync();

    // ❌ Incorrect: Throws on duplicate
    await _dbContext.AccountReadModels.AddAsync(data);  // Throws if exists
}
```

**DeleteAsync Implementation Pattern**:
```csharp
public async Task DeleteAsync(AccountReadModel data)
{
    // ✅ Correct: Silently succeeds if not found
    var existing = await _dbContext.AccountReadModels
        .FindAsync(data.AccountId);
    if (existing != null)
    {
        _dbContext.AccountReadModels.Remove(existing);
        await _dbContext.SaveChangesAsync();
    }
    // No exception if not found

    // ❌ Incorrect: Throws if not found
    _dbContext.AccountReadModels.Remove(data);  // Throws if not tracked
}
```

### Testing Idempotency

```csharp
[Fact]
public async Task SaveAsync_CalledTwice_ShouldBeIdempotent()
{
    var readModel = new AccountReadModel(AccountId.New(), 100m);

    // First save
    await _archive.SaveAsync(readModel);
    var first = await _archive.FindByIdAsync(readModel.AccountId);

    // Second save (same data)
    await _archive.SaveAsync(readModel);
    var second = await _archive.FindByIdAsync(readModel.AccountId);

    // Should be identical
    Assert.Equal(first, second);
}

[Fact]
public async Task DeleteAsync_CalledOnNonExistent_ShouldNotThrow()
{
    var nonExistent = new AccountReadModel(AccountId.New(), 0m);

    // Should not throw
    await _archive.DeleteAsync(nonExistent);

    // Verify still doesn't exist
    var result = await _archive.FindByIdAsync(nonExistent.AccountId);
    Assert.Null(result);
}
```

### Projector Error Handling

```csharp
public async Task ExecuteAsync(DomainEventData eventData)
{
    try
    {
        // Process event idempotently
        await ProcessEventAsync(eventData);
    }
    catch (Exception ex)
    {
        // Log error but don't crash projector
        _logger.LogError(ex, "Failed to process event {EventId}", eventData.Id);

        // Consider: Dead letter queue for poison messages
        // Consider: Retry with exponential backoff
    }
}
```

---

## References

### Analysis Documents
- Phase 4 Java source analysis - Lines 220-250: Java Archive analysis (internal working note, not retained in the repository)
- Phase 4 API design notes - Lines 857-1053: C# IArchive design and comparison (internal working note, not retained in the repository)
- Phase 4 ADR planning notes - Lines 256-392: ADR-0018 planning details (internal working note, not retained in the repository)

### Source Code References
- [Phase 3 IRepository Interface](../../src/EzDdd.UseCase/Port/Out/IRepository.cs) - Write-side repository pattern precedent
- Java ezcqrs: `src/main/java/tw/teddysoft/ezddd/cqrs/usecase/query/Archive.java` - Original Java implementation

### Related ADRs
- [ADR-0016: Async/Await Throughout](0016-async-await-throughout.md) - Establishes async pattern for all I/O operations
- [ADR-0023: Archive Idempotency Requirements](0023-archive-idempotency-requirements.md) (to be written) - Detailed idempotency implementation guidance

### External References
- [Microsoft: Async/Await Best Practices](https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming) - Async programming guidelines
- [CQRS: Read Model Patterns](https://martinfowler.com/bliki/CQRS.html) - Martin Fowler on CQRS
- [Event Sourcing: Idempotency](https://eventstore.com/blog/idempotence-and-event-sourcing/) - Importance of idempotent projections

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2025-11-17 | Proposed    | Initial draft for Phase 4      |
| 2025-11-17 | Accepted    | Decision finalized before Phase 4 implementation |

---

*This ADR documents the IArchive async method design for ezDDD.NET Phase 4 (EzDdd.Cqrs module), establishing the query-side data access pattern for read models.*
