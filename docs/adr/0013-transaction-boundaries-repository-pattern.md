# ADR-0013: Transaction Boundaries in Repository Pattern

## Status

**Accepted**

- **Date**: 2025-11-10
- **Deciders**: Project maintainers
- **Status Date**: 2025-11-10

---

## Context

### Problem Statement

The Repository Pattern in ezDDD.NET follows the **Bridge Pattern** architecture, separating domain-level abstractions (`IRepository`) from infrastructure-level implementations (`IRepositoryPeer`). This raises a critical question: **Where should transaction boundaries be placed?**

Transaction management is essential for ensuring:
- **Atomicity**: Both aggregate state and domain events must be persisted together (Transactional Outbox pattern)
- **Consistency**: Optimistic locking prevents concurrent modification conflicts
- **Isolation**: Multiple concurrent saves must not interfere with each other
- **Durability**: Committed changes must survive system failures

However, placing transactions at the wrong architectural layer violates Clean Architecture principles and harms testability, maintainability, and technology independence.

### Relevant Context

**ezDDD.NET Repository Architecture** (Bridge Pattern):
```
Domain Layer (Use Cases)
    IRepository<TAggregate, TId, TEvent>
         ↓ uses (dependency)
Adapter Layer (Interface Adapters)
    IRepositoryPeer<TData, TId>
         ↓ implements
Infrastructure Layer (Frameworks & Drivers)
    SqlRepositoryPeer, MongoRepositoryPeer, etc.
```

**Key Architectural Principles**:
- **Clean Architecture**: Infrastructure concerns (transactions, database connections) belong in outer layers, NOT inner layers
- **Dependency Rule**: Dependencies point inward; inner layers must not depend on outer layers
- **Technology Independence**: Domain layer (IRepository) must remain agnostic to persistence technology

**Transactional Outbox Pattern Requirements**:
- State sourcing repositories must save BOTH aggregate state and events atomically
- Event sourcing repositories must save events atomically (single-stream consistency)
- Failure in either operation must rollback completely

### Constraints

- Must follow Clean Architecture layer separation (Use Cases → Interface Adapters → Frameworks)
- Must support atomic persistence of aggregate state + events (Transactional Outbox)
- Must support optimistic locking for concurrency control
- Must remain technology-agnostic at domain layer (IRepository)
- Must support testability (unit tests should not require real databases)

---

## Decision

**Transaction boundaries MUST be implemented at the `IRepositoryPeer` layer ONLY. Transaction logic is FORBIDDEN at the `IRepository` layer.**

### Details

**Correct Architecture**:
```csharp
// ✅ CORRECT: IRepository (Use Cases Layer)
// NO transaction logic, domain-focused operations only
public class OutboxRepository<TAggregate, TId, TEvent>
    : IRepository<TAggregate, TId, TEvent>
    where TAggregate : AggregateRoot<TId, TEvent>
    where TEvent : class, IInternalDomainEvent
{
    private readonly IRepositoryPeer<IStoreData<TId>, TId> _peer;

    public async Task SaveAsync(TAggregate aggregate)
    {
        // 1. Convert domain object to persistence DTO
        var storeData = _ConvertToStoreData(aggregate);

        // 2. Delegate to peer (peer handles transaction)
        await _peer.SaveAsync(storeData);

        // 3. Clear events ONLY after successful save
        aggregate.ClearDomainEvents();
    }
}

// ✅ CORRECT: IRepositoryPeer (Interface Adapters Layer)
// Transaction management happens HERE
public class SqlRepositoryPeer : IRepositoryPeer<BankAccountData, AccountId>
{
    private readonly ApplicationDbContext _dbContext;

    public async Task SaveAsync(BankAccountData data)
    {
        // ✅ BEGIN TRANSACTION - This is the CORRECT place
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // 1. Save aggregate state
            if (data.Version == -1)
                _dbContext.BankAccounts.Add(data);
            else
                _dbContext.BankAccounts.Update(data);

            // 2. Save events (Transactional Outbox)
            foreach (var @event in data.Events)
            {
                _dbContext.OutboxEvents.Add(new OutboxEventEntity(@event));
            }

            // 3. Commit atomically (both or neither)
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            throw new RepositoryPeerSaveException(
                "Optimistic locking failure", ex);
        }
    }
}
```

**Key Rules**:

1. **IRepository layer** (Use Cases):
   - ❌ **NO** transaction management
   - ❌ **NO** database connections
   - ❌ **NO** ORM-specific code
   - ✅ Domain object conversion (Aggregate → StoreData)
   - ✅ Exception translation (RepositoryPeerSaveException → RepositorySaveException)
   - ✅ Event clearing after successful save

2. **IRepositoryPeer layer** (Interface Adapters):
   - ✅ **Transaction management goes HERE**
   - ✅ Database connections and ORM usage
   - ✅ Atomic persistence (state + events)
   - ✅ Optimistic locking enforcement
   - ✅ Rollback on failure

**Enforcement Mechanisms**:

1. **XML Documentation**: Both `IRepository` and `IRepositoryPeer` have explicit warnings in XML docs
2. **TRANSACTION_BOUNDARY_GUIDE.md**: Comprehensive guide with correct/wrong examples
3. **Integration Tests**: `TransactionBoundaryTests.cs` verifies compliance via static analysis
4. **Code Review**: Developers are trained to recognize violations

---

## Consequences

### Positive Consequences

- ✅ **Clean Architecture Compliance**: Clear separation between domain logic (IRepository) and infrastructure concerns (IRepositoryPeer)
- ✅ **Technology Independence**: IRepository remains agnostic to transaction mechanisms (EF Core, TransactionScope, ADO.NET, etc.)
- ✅ **Testability**: IRepository implementations can be unit-tested without real databases (mock IRepositoryPeer)
- ✅ **Atomic Persistence**: IRepositoryPeer ensures state + events are saved together (Transactional Outbox correctness)
- ✅ **Optimistic Locking**: Transaction isolation level enforced at correct layer
- ✅ **Rollback Safety**: Failed saves rollback completely, preventing partial state
- ✅ **Single Responsibility**: IRepository focuses on domain operations, IRepositoryPeer focuses on persistence
- ✅ **Adapter Flexibility**: Different IRepositoryPeer implementations can use different transaction strategies

### Negative Consequences

- ❌ **Learning Curve**: Developers unfamiliar with Clean Architecture may put transactions in wrong layer
- ❌ **Documentation Overhead**: Requires explicit guidance (XML docs, TRANSACTION_BOUNDARY_GUIDE.md)
- ❌ **No Compile-Time Enforcement**: Cannot prevent transaction code in IRepository via type system alone
- ❌ **Peer Implementation Complexity**: Every IRepositoryPeer must implement transaction logic correctly

### Neutral Consequences

- ⚖️ **No Cross-Repository Transactions**: Each repository manages its own transaction (no distributed transactions by default)
- ⚖️ **Transaction Strategy Flexibility**: IRepositoryPeer implementations can choose EF Core transactions, TransactionScope, or database-specific mechanisms
- ⚖️ **Multiple Saves in One Use Case**: If a use case saves multiple aggregates, each gets its own transaction (eventual consistency model)

---

## Alternatives Considered

### Alternative 1: Transactions at IRepository Layer

**Description**: Place transaction logic in `IRepository` implementations (e.g., `OutboxRepository`, `EsRepository`) instead of `IRepositoryPeer`

**Implementation**:
```csharp
// ❌ WRONG: Transaction in IRepository layer
public class OutboxRepository<TAggregate, TId, TEvent>
    : IRepository<TAggregate, TId, TEvent>
{
    private readonly IRepositoryPeer<IStoreData<TId>, TId> _peer;
    private readonly ApplicationDbContext _dbContext;  // ❌ Infrastructure dependency

    public async Task SaveAsync(TAggregate aggregate)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();  // ❌ Wrong layer
        try
        {
            var storeData = _ConvertToStoreData(aggregate);
            await _peer.SaveAsync(storeData);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

**Pros**:
- Simpler IRepositoryPeer implementations (no transaction logic needed)
- Single place to control transaction strategy

**Cons**:
- **Violates Clean Architecture**: Use Cases layer depends on infrastructure (DbContext, TransactionScope)
- **Technology Coupling**: IRepository tied to specific ORM (EF Core, Dapper, etc.)
- **Testability Loss**: Unit tests require real database or complex mocks
- **Breaks Bridge Pattern**: Abstraction (IRepository) depends on implementation details
- **Inconsistent with Java ezddd**: Java places transactions at RepositoryPeer level

**Why rejected**: Fundamentally violates Clean Architecture's Dependency Rule. Inner layers (Use Cases) must NOT depend on outer layers (Infrastructure). This approach would make IRepository implementations aware of specific database technologies, defeating the purpose of the Bridge Pattern.

---

### Alternative 2: No Transactions (Rely on Database Auto-Commit)

**Description**: Omit explicit transactions entirely, relying on database auto-commit for each individual INSERT/UPDATE operation

**Implementation**:
```csharp
// ❌ WRONG: No transaction management
public class SqlRepositoryPeer : IRepositoryPeer<BankAccountData, AccountId>
{
    public async Task SaveAsync(BankAccountData data)
    {
        // No transaction
        _dbContext.BankAccounts.Update(data);
        await _dbContext.SaveChangesAsync();  // Auto-commit (state only)

        foreach (var @event in data.Events)
        {
            _dbContext.OutboxEvents.Add(new OutboxEventEntity(@event));
            await _dbContext.SaveChangesAsync();  // Auto-commit (each event)
        }
    }
}
```

**Pros**:
- Simpler code (no transaction management)
- No need for rollback logic

**Cons**:
- **Violates Atomicity**: State and events saved separately (partial failure possible)
- **Transactional Outbox Broken**: Events may be saved even if aggregate save fails
- **Optimistic Locking Unreliable**: Version check and update not atomic
- **Data Consistency Risk**: System crash between saves leaves inconsistent state
- **No Rollback**: Errors leave partial writes in database

**Why rejected**: Breaks Transactional Outbox pattern correctness. State sourcing requires atomic persistence of BOTH aggregate state and events. Without transactions, a failure after saving state but before saving events would leave the system in an inconsistent state (aggregate persisted but events lost).

---

### Alternative 3: Transactions at Both Layers

**Description**: Allow transaction management at both IRepository AND IRepositoryPeer layers for flexibility

**Implementation**:
```csharp
// ❌ WRONG: Transaction at both layers
public class OutboxRepository : IRepository<...>
{
    public async Task SaveAsync(TAggregate aggregate)
    {
        using var outerTransaction = /* ... */;  // Outer transaction
        try
        {
            await _peer.SaveAsync(storeData);  // Peer also has transaction
            await outerTransaction.CommitAsync();
        }
        catch { /* ... */ }
    }
}

public class SqlRepositoryPeer : IRepositoryPeer<...>
{
    public async Task SaveAsync(TData data)
    {
        using var innerTransaction = /* ... */;  // Inner transaction
        try
        {
            // ... save operations ...
            await innerTransaction.CommitAsync();
        }
        catch { /* ... */ }
    }
}
```

**Pros**:
- Maximum flexibility (developers choose layer)
- Supports nested transactions (if database supports)

**Cons**:
- **Nested Transaction Complexity**: Not all databases support nested transactions
- **Ambiguous Responsibility**: Unclear which layer is authoritative for transaction management
- **Transaction Scope Conflicts**: Outer and inner transactions may conflict
- **Testing Confusion**: Unit tests unclear which layer to mock
- **Maintenance Burden**: Changes to transaction logic must be coordinated across layers
- **Violates Single Responsibility**: Both layers share transaction responsibility

**Why rejected**: Creates ambiguity and violates Single Responsibility Principle. Having transactions at both layers makes it unclear which layer is responsible for atomicity. Nested transactions are complex and not universally supported. Clean Architecture requires clear responsibility assignment: infrastructure concerns (transactions) belong ONLY in outer layers (IRepositoryPeer).

---

## Related Decisions

- **Related to**: [ADR-0003](0003-module-architecture-dependency-chain.md) - Module dependency chain enforces layer separation
- **Related to**: [ADR-0005](0005-complete-reimplementation-approach.md) - Complete reimplementation allows correct architectural patterns
- **Related to**: [ADR-0009](0009-aggregate-root-base-class-design.md) - AggregateRoot version management enables optimistic locking
- **Influences**: All IRepositoryPeer implementations (SqlRepositoryPeer, MongoRepositoryPeer, etc.) must follow this rule

---

## Implementation Notes

### Implementation Checklist (Phase F.4 - Completed 2025-11-10)

- ✅ Updated `IRepository.cs` XML documentation with ❌ WRONG / ✅ CORRECT markers
- ✅ Updated `IRepositoryPeer.cs` XML documentation with ✅ CORRECT / ❌ WRONG markers
- ✅ Created `docs/TRANSACTION_BOUNDARY_GUIDE.md` (comprehensive 7-section guide)
- ✅ Added 7 integration tests in `TransactionBoundaryTests.cs`:
  - Verify OutboxRepository contains no transaction logic
  - Verify EsRepository contains no transaction logic
  - Verify IRepositoryPeer documentation mentions transaction requirement
  - Verify IRepository documentation warns against transactions
  - Verify TRANSACTION_BOUNDARY_GUIDE.md exists with correct content
  - Verify guide contains correct examples
  - Verify guide explains rationale
- ✅ All 433 tests passing (7 new tests, 426 existing tests preserved)
- ✅ Zero new compiler warnings

### Verification Methods

**Static Analysis via Integration Tests**:
```csharp
[Fact]
public void OutboxRepository_DoesNotContainTransactionLogic()
{
    var sourceCode = File.ReadAllText("OutboxRepository.cs");

    // ❌ Should NOT contain transaction keywords
    Assert.DoesNotContain("BeginTransaction", sourceCode);
    Assert.DoesNotContain("TransactionScope", sourceCode);
    Assert.DoesNotContain("CommitAsync", sourceCode);
    Assert.DoesNotContain("RollbackAsync", sourceCode);
}

[Fact]
public void IRepositoryPeer_Documentation_MustMentionTransactionRequirement()
{
    var xmlDocs = File.ReadAllText("IRepositoryPeer.cs");

    // ✅ MUST document transaction boundary requirement
    Assert.Contains("Transaction Boundary MUST Be Here", xmlDocs);
    Assert.Contains("CRITICAL ARCHITECTURE RULE", xmlDocs);
}
```

### Developer Training Resources

1. **docs/TRANSACTION_BOUNDARY_GUIDE.md**:
   - 7 sections covering rule, architecture, correct/wrong examples, rationale, FAQ
   - EF Core, TransactionScope, and ADO.NET examples
   - Troubleshooting guide

2. **XML Documentation**:
   - `IRepository.cs`: Explicit warning "❌ No Transaction Management"
   - `IRepositoryPeer.cs`: Explicit requirement "✅ Transaction Boundary MUST Be Here"
   - Code examples showing correct placement

3. **Integration Tests**:
   - `TransactionBoundaryTests.cs`: 7 tests verify architectural compliance
   - Run automatically in CI/CD pipeline

### Code Review Checklist

When reviewing IRepositoryPeer implementations:

- [ ] Does `SaveAsync()` begin a transaction?
- [ ] Are state and events saved within the same transaction?
- [ ] Is there a try-catch with rollback on failure?
- [ ] Does optimistic locking throw appropriate exception?
- [ ] Are all database operations awaited (no `.Result` or `.Wait()`)?

When reviewing IRepository implementations:

- [ ] Does it avoid transaction management?
- [ ] Does it avoid database connection management?
- [ ] Does it delegate to IRepositoryPeer for persistence?
- [ ] Does it clear events ONLY after successful save?

---

## References

- Phase 3 final review report - Identified transaction boundary enforcement as critical (internal working note, not retained in the repository)
- Phase 3 Group 3 review - INFO #4: Transaction Boundary Documentation, lines 194-216 (internal working note, not retained in the repository)
- [IRepository.cs](../../src/EzDdd.UseCase/Port/Out/IRepository.cs) - "No Transaction Management" (lines 48-53)
- [IRepositoryPeer.cs](../../src/EzDdd.UseCase/Port/Out/IRepositoryPeer.cs) - "Transaction Boundary MUST Be Here" (lines 52-119)
- [TRANSACTION_BOUNDARY_GUIDE.md](../../docs/TRANSACTION_BOUNDARY_GUIDE.md) - Comprehensive guide (created in F.4)
- [TransactionBoundaryTests.cs](../../tests/EzDdd.UseCase.Tests/Integration/TransactionBoundaryTests.cs) - Verification tests
- Phase 3 post-review session notes - F.4 implementation record, lines 145-194 (internal working note, not retained in the repository)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) - Robert C. Martin
- [Transactional Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html) - Chris Richardson

---

## Revision History

| Date       | Status   | Notes                          |
|------------|----------|--------------------------------|
| 2025-11-10 | Accepted | Decision finalized, F.4 implementation complete |

---
