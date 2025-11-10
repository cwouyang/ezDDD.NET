# Transaction Boundary Guide

> **Version**: 1.0.0
> **Last Updated**: 2025-11-10
> **Audience**: Developers implementing repositories in ezDDD.NET

---

## ⚠️ CRITICAL ARCHITECTURE RULE

**Transaction boundaries MUST be implemented at the `IRepositoryPeer` layer ONLY.**

- ❌ **WRONG**: Transaction logic in `IRepository` implementations (Use Cases layer)
- ✅ **CORRECT**: Transaction logic in `IRepositoryPeer` implementations (Interface Adapters layer)

---

## 📐 Architecture Layers

ezDDD.NET follows **Clean Architecture** with clear layer separation:

```
┌─────────────────────────────────────────┐
│  Entities Layer                         │
│  - AggregateRoot, EsAggregateRoot       │
│  - Domain Events                        │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│  Use Cases Layer                        │
│  - IUseCase, IRepository                │  ← NO transactions here
│  - Business logic, domain rules         │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│  Interface Adapters Layer               │
│  - IRepositoryPeer                      │  ← ✅ Transactions go here
│  - Database-specific implementations    │
└─────────────────────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│  Frameworks & Drivers Layer             │
│  - EF Core, Dapper, MongoDB.Driver      │
└─────────────────────────────────────────┘
```

---

## ✅ CORRECT Implementation

### IRepositoryPeer with Transaction (EF Core Example)

```csharp
using Microsoft.EntityFrameworkCore;
using EzDdd.UseCase.Port.Out;

namespace MyApp.Adapters.Persistence;

/// <summary>
/// ✅ CORRECT: Transaction management at IRepositoryPeer layer
/// </summary>
public class SqlBankAccountRepositoryPeer : IRepositoryPeer<BankAccountData, AccountId>
{
    private readonly ApplicationDbContext _dbContext;

    public SqlBankAccountRepositoryPeer(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BankAccountData?> FindByIdAsync(AccountId id)
    {
        return await _dbContext.BankAccounts
            .Include(a => a.Events)
            .FirstOrDefaultAsync(a => a.Id == id.Value);
    }

    public async Task SaveAsync(BankAccountData data)
    {
        // ✅ BEGIN TRANSACTION - This is the correct place
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // 1. Save aggregate state (UPSERT based on Version)
            if (data.Version == -1)
            {
                // New aggregate (Version -1)
                _dbContext.BankAccounts.Add(data);
            }
            else
            {
                // Existing aggregate (Version 0+)
                _dbContext.BankAccounts.Update(data);
            }

            // 2. Save domain events (Transactional Outbox pattern)
            foreach (var @event in data.Events)
            {
                _dbContext.OutboxEvents.Add(new OutboxEventEntity
                {
                    Id = @event.Id,
                    AggregateId = data.Id.Value,
                    EventType = @event.GetType().Name,
                    EventData = JsonSerializer.Serialize(@event),
                    OccurredOn = @event.OccurredOn
                });
            }

            // 3. Commit atomically (both state + events or neither)
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Optimistic locking failure
            await transaction.RollbackAsync();
            throw new RepositoryPeerSaveException(
                "Optimistic locking failure: aggregate was modified by another transaction",
                ex
            );
        }
        catch (Exception ex)
        {
            // Other database errors
            await transaction.RollbackAsync();
            throw new RepositoryPeerSaveException("Failed to save aggregate", ex);
        }
    }

    public async Task DeleteAsync(AccountId id)
    {
        // ✅ Transactions also for DELETE operations
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var account = await _dbContext.BankAccounts.FindAsync(id.Value);
            if (account != null)
            {
                _dbContext.BankAccounts.Remove(account);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new RepositoryPeerSaveException("Failed to delete aggregate", ex);
        }
    }
}
```

### Alternative: TransactionScope (Distributed Transactions)

```csharp
using System.Transactions;

public async Task SaveAsync(BankAccountData data)
{
    // ✅ Using TransactionScope for distributed transactions
    var options = new TransactionOptions
    {
        IsolationLevel = IsolationLevel.ReadCommitted,
        Timeout = TransactionManager.DefaultTimeout
    };

    using var scope = new TransactionScope(
        TransactionScopeOption.Required,
        options,
        TransactionScopeAsyncFlowOption.Enabled  // ⚠️ Required for async/await
    );

    try
    {
        // Save to SQL Server
        await _dbContext.BankAccounts.UpdateAsync(data);

        // Save to MongoDB (different database)
        await _mongoCollection.InsertOneAsync(data.Events);

        await _dbContext.SaveChangesAsync();

        // ✅ Commit if both operations succeed
        scope.Complete();
    }
    catch (Exception ex)
    {
        // ✅ Automatic rollback if exception (scope.Complete() not called)
        throw new RepositoryPeerSaveException("Transaction failed", ex);
    }
}
```

---

## ❌ WRONG Implementation

### IRepository with Transaction (VIOLATES Clean Architecture)

```csharp
using EzDdd.UseCase.Port.Out;

namespace MyApp.UseCase.Repository;

/// <summary>
/// ❌ WRONG: Transaction management at IRepository layer
/// This violates Clean Architecture layer separation
/// </summary>
public class BankAccountRepository : IRepository<BankAccount, AccountId, InternalDomainEvent>
{
    private readonly ApplicationDbContext _dbContext;  // ❌ Direct database dependency in Use Cases layer
    private readonly IRepositoryPeer<BankAccountData, AccountId> _peer;

    public async Task SaveAsync(BankAccount aggregate)
    {
        // ❌ WRONG: Transaction started in IRepository (Use Cases layer)
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var data = _MapToData(aggregate);

            // This logic should be in IRepositoryPeer, not here
            _dbContext.BankAccounts.Update(data);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // Clear events after save
            aggregate.ClearDomainEvents();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new RepositorySaveException("Save failed", ex);
        }
    }
}
```

**Why is this wrong?**

1. **Layer Violation**: Use Cases layer should not depend on `ApplicationDbContext`
2. **Infrastructure Concern**: Transaction management is infrastructure, not domain logic
3. **Testing Difficulty**: Hard to unit test without database
4. **Technology Coupling**: IRepository becomes coupled to specific database technology

---

## 🎯 Why This Rule Exists

### 1. **Clean Architecture Enforcement**

**Dependency Rule**: Dependencies point inward (outer layers depend on inner, never reverse)

```
Infrastructure → Adapters → Use Cases → Entities
(EF Core)       (Peer)      (Repository)  (Aggregate)
```

Transactions are an **infrastructure concern**, not a domain concern.

### 2. **Testability**

**With Correct Approach** (Transaction in IRepositoryPeer):
```csharp
// Unit test IRepository without database
var mockPeer = new Mock<IRepositoryPeer<BankAccountData, AccountId>>();
var repository = new OutboxRepository<BankAccount, AccountId, InternalDomainEvent>(
    mockPeer.Object,
    mockMapper.Object
);

// No database, no transactions needed
await repository.SaveAsync(aggregate);
mockPeer.Verify(p => p.SaveAsync(It.IsAny<BankAccountData>()), Times.Once);
```

**With Wrong Approach** (Transaction in IRepository):
```csharp
// ❌ Cannot unit test without database
var repository = new BankAccountRepository(realDbContext);  // Requires database
await repository.SaveAsync(aggregate);  // Slow integration test
```

### 3. **Technology Independence**

IRepository should be **database-agnostic**:

```csharp
// Same IRepository implementation works with ANY IRepositoryPeer
IRepository<BankAccount, AccountId, InternalDomainEvent> repository;

// Use SQL Server peer
repository = new OutboxRepository(sqlPeer, mapper);

// Use MongoDB peer
repository = new OutboxRepository(mongoPeer, mapper);

// Use in-memory peer (testing)
repository = new OutboxRepository(inMemoryPeer, mapper);
```

If transactions are in IRepository, it becomes tightly coupled to one technology.

---

## 🔍 How to Verify Compliance

### Static Analysis (Code Review)

**Check IRepository implementations** - Should NOT contain:
- `BeginTransaction()` / `CommitAsync()` / `RollbackAsync()`
- `TransactionScope` / `CommittableTransaction`
- Direct database context (`DbContext`, `IMongoDatabase`, etc.)

**Check IRepositoryPeer implementations** - Should contain:
- Transaction management (EF Core transaction or TransactionScope)
- Database-specific code
- Error handling with rollback logic

### Integration Tests

```csharp
[Fact]
public async Task RepositoryPeer_SaveAsync_UsesTransaction_EnsuresAtomicity()
{
    // Arrange
    var dbContext = CreateTestDbContext();
    var peer = new SqlBankAccountRepositoryPeer(dbContext);
    var data = new BankAccountData
    {
        Id = new AccountId("acc-001"),
        Version = -1,
        Events = new List<DomainEventData> { /* ... */ }
    };

    // Inject failure after state save but before event save
    dbContext.OnEventSaveFail = true;

    // Act & Assert
    await Assert.ThrowsAsync<RepositoryPeerSaveException>(
        () => peer.SaveAsync(data)
    );

    // Verify atomicity: neither state nor events should be saved
    var savedAccount = await dbContext.BankAccounts.FindAsync("acc-001");
    var savedEvents = await dbContext.OutboxEvents.Where(e => e.AggregateId == "acc-001").ToListAsync();

    Assert.Null(savedAccount);  // ✅ Transaction rolled back
    Assert.Empty(savedEvents);   // ✅ Events not saved either
}
```

---

## 📚 Related Documentation

- **ADR-0013**: Transaction Boundary Enforcement (planned)
- **IRepository**: `src/EzDdd.UseCase/Port/Out/IRepository.cs` (lines 185-203)
- **IRepositoryPeer**: `src/EzDdd.UseCase/Port/Out/IRepositoryPeer.cs` (lines 67-103)
- **OutboxRepository**: `src/EzDdd.UseCase/Port/Out/OutboxRepository.cs` (generic implementation example)
- **EsRepository**: `src/EzDdd.UseCase/Port/Out/EsRepository.cs` (event sourcing example)

---

## 🤔 FAQ

### Q: Can I use `TransactionScope` in IRepository for simplicity?

**A**: ❌ **NO**. Even though `TransactionScope` is easier to use, it's still a transaction mechanism and violates layer separation. Keep IRepository technology-agnostic.

### Q: What if I need transactions across multiple repositories?

**A**: Use a **Unit of Work pattern** at the Use Case layer, but the actual transaction implementation should still be in IRepositoryPeer:

```csharp
public class TransferMoneyUseCase : IUseCase<TransferInput, TransferOutput>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<BankAccount, AccountId, InternalDomainEvent> _repository;

    public async Task<TransferOutput> ExecuteAsync(TransferInput input)
    {
        // ✅ IUnitOfWork abstracts transaction, actual implementation in adapters layer
        using var transaction = _unitOfWork.BeginTransaction();

        var fromAccount = await _repository.FindByIdAsync(input.FromAccountId);
        var toAccount = await _repository.FindByIdAsync(input.ToAccountId);

        fromAccount.Withdraw(input.Amount);
        toAccount.Deposit(input.Amount);

        await _repository.SaveAsync(fromAccount);
        await _repository.SaveAsync(toAccount);

        await transaction.CommitAsync();  // Delegates to IRepositoryPeer layer

        return new TransferOutput { Success = true };
    }
}
```

### Q: What about in-memory repositories for testing?

**A**: In-memory peers can use lightweight transactions or no-op transactions:

```csharp
public class InMemoryBankAccountRepositoryPeer : IRepositoryPeer<BankAccountData, AccountId>
{
    private readonly ConcurrentDictionary<string, BankAccountData> _store = new();

    public async Task SaveAsync(BankAccountData data)
    {
        // ✅ No transaction needed for in-memory (atomic dictionary operation)
        _store.AddOrUpdate(data.Id.Value, data, (key, existing) => data);

        // Simulate optimistic locking
        if (data.Version != existing.Version + 1)
        {
            throw new RepositoryPeerSaveException("Optimistic locking failure");
        }

        await Task.CompletedTask;
    }
}
```

---

## ✅ Summary

| Layer | Transaction Allowed? | Reason |
|-------|---------------------|---------|
| **IRepository** (Use Cases) | ❌ NO | Domain logic layer, technology-agnostic |
| **IRepositoryPeer** (Adapters) | ✅ YES | Infrastructure layer, database-specific |

**Remember**: Clean Architecture is about **layer separation** and **dependency direction**. Transactions are infrastructure concerns that belong in the outer layers (Interface Adapters), not the inner layers (Use Cases).

---

*Last updated: 2025-11-10 - Phase F.4 (Transaction Boundary Enforcement)*
