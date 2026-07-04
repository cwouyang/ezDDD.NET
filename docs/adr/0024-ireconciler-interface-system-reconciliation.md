# ADR-0024: IReconciler Interface for System State Reconciliation

## Status

**Accepted**

- **Date**: 2026-01-07
- **Deciders**: Development Team
- **Status Date**: 2026-01-07

---

## Context

### Problem Statement

How should the framework support periodic system state reconciliation and data consistency maintenance tasks, such as cleaning up orphaned records or enforcing business rules across aggregates?

### Relevant Context

- **Java ezddd 4.1.0**: Introduced `Reconciler<Context, Report>` interface for state reconciliation tasks
- **Java pattern**: Reconcilers are invoked by scheduled jobs or administrative tools, separate from user-triggered use cases
- **Use case**: Systems need maintenance tasks to ensure data consistency (e.g., cleanup orphaned workflows whose parent boards no longer exist)
- **Phase 6 requirement**: Synchronize with Java ezddd 4.1.0 features before first NuGet publication
- **Similar patterns**: IProjector (background service for read models), IUseCase (user-triggered operations)

### Constraints

- **Semantic parity**: Must maintain similar design philosophy to Java ezddd 4.1.0 Reconciler
- **Platform idioms**: Should leverage .NET async/await patterns
- **Separation from IUseCase**: Reconcilers have different semantics (system maintenance vs. user operations)
- **Zero dependencies**: Must not introduce third-party dependencies (ADR-0004)

---

## Decision

**Add `IReconciler<in TContext, TReport>` interface for system state reconciliation tasks. Provide `NullContext` singleton for reconcilers that do not require input context.**

### Details

#### IReconciler Interface (EzDdd.UseCase.Port.In namespace)

```csharp
/// <summary>
///     <c>IReconciler</c> is an interface for performing system state reconciliation.
///     Reconcilers are used for maintenance tasks such as cleaning up orphaned data,
///     enforcing data consistency, or performing periodic system checks.
/// </summary>
/// <typeparam name="TContext">The type of context required for reconciliation.</typeparam>
/// <typeparam name="TReport">The type of report returned after reconciliation.</typeparam>
public interface IReconciler<in TContext, TReport>
{
    /// <summary>
    ///     Reconciles system state based on the provided context.
    /// </summary>
    /// <param name="context">The context providing information for reconciliation.</param>
    /// <returns>
    ///     A task representing the asynchronous operation, containing a report
    ///     describing the reconciliation results.
    /// </returns>
    Task<TReport> ReconcileAsync(TContext context);
}
```

#### NullContext Class

```csharp
/// <summary>
///     <c>NullContext</c> is a null object pattern implementation for reconcilers that
///     do not require any input context.
/// </summary>
public sealed class NullContext
{
    /// <summary>
    ///     Gets the singleton instance of <c>NullContext</c>.
    /// </summary>
    public static readonly NullContext Instance = new();

    /// <summary>
    ///     Prevents external instantiation. Use <see cref="Instance" /> instead.
    /// </summary>
    private NullContext()
    {
    }
}
```

#### Example: CleanUpExpiredOrdersReconciler

```csharp
public class CleanUpExpiredOrdersReconciler : IReconciler<OrderCleanupContext, OrderCleanupReport>
{
    private readonly IOrderRepository _orderRepository;

    public CleanUpExpiredOrdersReconciler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderCleanupReport> ReconcileAsync(OrderCleanupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 1. Find expired draft orders
        DateTimeOffset cutoffDate = DateTimeOffset.UtcNow.AddDays(-context.ExpirationDays);
        List<OrderId> expiredOrderIds = await FindExpiredDraftOrdersAsync(cutoffDate);

        // 2. Delete expired orders
        int deletedCount = 0;
        int errorCount = 0;
        List<string> errors = [];

        foreach (OrderId orderId in expiredOrderIds)
        {
            try
            {
                await _orderRepository.DeleteAsync(orderId);
                deletedCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                errors.Add($"Failed to delete order {orderId}: {ex.Message}");
            }
        }

        // 3. Return report
        return new OrderCleanupReport(
            TotalChecked: expiredOrderIds.Count,
            DeletedCount: deletedCount,
            ErrorCount: errorCount,
            Errors: errors
        );
    }
}

public record OrderCleanupContext(int ExpirationDays);
public record OrderCleanupReport(int TotalChecked, int DeletedCount, int ErrorCount, IReadOnlyList<string> Errors);
```

#### Example: Global Cleanup with NullContext

```csharp
public class GlobalSystemCleanupReconciler : IReconciler<NullContext, CleanupReport>
{
    public async Task<CleanupReport> ReconcileAsync(NullContext context)
    {
        // Perform global cleanup without specific context
        // (e.g., clean up temporary files, expired sessions, etc.)

        return new CleanupReport(ItemsCleaned: 42);
    }
}

// Invocation
var reconciler = new GlobalSystemCleanupReconciler();
var report = await reconciler.ReconcileAsync(NullContext.Instance);
```

---

## Consequences

### Positive Consequences

- ✅ **Semantic Parity**: Matches Java ezddd 4.1.0 `Reconciler<Context, Report>` pattern (~99%)
- ✅ **Clear Separation**: Distinguishes system maintenance tasks (IReconciler) from user operations (IUseCase)
- ✅ **Type Safety**: NullContext provides type-safe alternative to `null` or `object`
- ✅ **Async-First**: `ReconcileAsync` follows .NET async/await patterns (ADR-0016)
- ✅ **Generic Variance**: Contravariant `TContext` allows flexible type hierarchies
- ✅ **Report Pattern**: Consistent reporting of reconciliation results (success/failure counts, errors)
- ✅ **Implementation Flexibility**: Applications can invoke reconcilers via scheduled jobs (Hangfire, Quartz.NET) or administrative endpoints

### Negative Consequences

- ❌ **Another Interface**: Developers must learn another pattern (IUseCase vs. IReconciler vs. IProjector)
- ❌ **Semantic Confusion**: Similarity to IUseCase may cause confusion about when to use each
- ❌ **No Built-in Scheduling**: Framework does not provide scheduling mechanism (applications must implement)

### Neutral Consequences

- ⚖️ **Invocation Responsibility**: Applications must decide when and how to invoke reconcilers (scheduled jobs, admin endpoints, etc.)
- ⚖️ **Error Handling**: Applications must implement retry logic and error handling strategies
- ⚖️ **Idempotency**: Reconcilers should be designed to handle repeated invocations safely

---

## Alternatives Considered

### Alternative 1: Extend IUseCase for Reconciliation

```csharp
// Reuse IUseCase<TInput, TOutput> for reconciliation
public class CleanUpExpiredOrdersUseCase : IUseCase<OrderCleanupInput, OrderCleanupOutput>
{
    // Implementation...
}
```

**Pros**:
- No new interface to learn
- Reuses existing patterns

**Cons**:
- **Semantic mismatch**: IUseCase represents user-triggered operations, not system maintenance
- **Input/Output constraints**: IUseCase requires `IInput` and `IOutput` constraints, which are overly restrictive for reconciliation
- **Conceptual confusion**: Mixes user operations with system operations

**Why rejected**: Reconcilers have fundamentally different semantics from use cases. IUseCase is for user-triggered operations, while IReconciler is for system-triggered maintenance tasks. Mixing these concepts would violate single responsibility principle.

---

### Alternative 2: Synchronous ReconcileSync Method

```csharp
public interface IReconciler<in TContext, TReport>
{
    TReport Reconcile(TContext context); // Synchronous
}
```

**Pros**:
- Simpler API (no async/await)
- Matches Java synchronous method

**Cons**:
- **Violates ADR-0016**: All I/O operations should be async in .NET
- **Not .NET idiomatic**: .NET convention favors async for I/O operations
- **Blocks threads**: Reconciliation typically involves database access, which should be async

**Why rejected**: Violates ADR-0016 (Async/Await Throughout). .NET convention strongly prefers async methods for I/O operations to avoid blocking threads.

---

### Alternative 3: Use object or null Instead of NullContext

```csharp
public interface IReconciler<in TContext, TReport>
{
    Task<TReport> ReconcileAsync(TContext? context); // Allow null
}

// Or
public class GlobalCleanupReconciler : IReconciler<object, CleanupReport>
{
    // Use object as placeholder
}
```

**Pros**:
- No need for NullContext class
- Simpler API

**Cons**:
- **Type safety loss**: Using `null` or `object` reduces type safety
- **Intent unclear**: `null` or `object` doesn't clearly express "no context needed"
- **Nullable reference warnings**: Requires `TContext?` which complicates generic variance

**Why rejected**: NullContext provides better type safety and clearly expresses intent. The singleton pattern avoids unnecessary allocations.

---

## Related Decisions

- **Related to**: [ADR-0003](0003-module-architecture-dependency-chain.md) - IReconciler belongs to UseCase layer (Port/In)
- **Related to**: [ADR-0016](0016-async-await-throughout.md) - ReconcileAsync follows async/await patterns
- **Related to**: [ADR-0020](0020-iprojector-lifecycle-management.md) - Similar lifecycle patterns (both are background maintenance tasks)
- **Depends on**: [ADR-0012](0012-resource-management-event-bus-producers.md) - Reconcilers may interact with repositories and archives

---

## Implementation Notes

### When to Use IReconciler vs. IUseCase

| Aspect | IUseCase | IReconciler |
|--------|----------|-------------|
| **Trigger** | User action (UI, API) | System schedule or admin tool |
| **Purpose** | Business operation | Data consistency maintenance |
| **Frequency** | Per user request | Periodic (hourly, daily) |
| **Failure Impact** | Return error to user | Log and retry later |
| **Examples** | CreateOrder, Withdraw | CleanUpOrphaned, ValidateConsistency |

### Scheduling Strategies

**Option 1: Hangfire (Recommended)**
```csharp
RecurringJob.AddOrUpdate<CleanUpExpiredOrdersReconciler>(
    "cleanup-expired-orders",
    r => r.ReconcileAsync(new OrderCleanupContext(ExpirationDays: 7)),
    Cron.Daily);
```

**Option 2: ASP.NET Core BackgroundService**
```csharp
public class ReconcilerHostedService : BackgroundService
{
    private readonly IReconciler<OrderCleanupContext, OrderCleanupReport> _reconciler;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _reconciler.ReconcileAsync(new OrderCleanupContext(7));
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
```

**Option 3: Quartz.NET**
```csharp
public class ReconciliationJob : IJob
{
    private readonly IReconciler<OrderCleanupContext, OrderCleanupReport> _reconciler;

    public async Task Execute(IJobExecutionContext context)
    {
        await _reconciler.ReconcileAsync(new OrderCleanupContext(7));
    }
}
```

### Design Guidelines

1. **Idempotent**: Reconcilers should be safe to run multiple times
2. **Reporting**: Always return detailed reports (success/failure counts, error messages)
3. **Error Handling**: Catch and report errors, don't throw exceptions for partial failures
4. **Batch Processing**: For large datasets, process in batches to avoid memory issues
5. **Logging**: Log reconciliation results for auditing and debugging

### Testing Strategy

```csharp
[Fact]
public async Task ReconcileAsync_WithExpiredOrders_DeletesThem()
{
    // Arrange
    var repository = new InMemoryOrderRepository();
    await repository.SaveAsync(new Order(new OrderId("expired-123"), "Customer"));
    var reconciler = new CleanUpExpiredOrdersReconciler(repository);
    var context = new OrderCleanupContext(ExpirationDays: 7);

    // Act
    var report = await reconciler.ReconcileAsync(context);

    // Assert
    Assert.Equal(1, report.DeletedCount);
    Assert.Equal(0, report.ErrorCount);
}
```

---

## References

- **Java ezddd 4.1.0**: [Reconciler.java](https://gitlab.com/TeddyChen/ezddd/-/blob/master/src/main/java/io/github/teddychen/ezddd/usecase/Reconciler.java)
- **Java Commits**:
  - `da156c6` - Add Reconciler interface
  - `f377dcf` - Add NullContext class
  - `d4ed869` - Add reconciler examples
- **Internal porting notes (not retained)** - Java 4.1.0 Synchronization Plan, Stage S2 (lines 293-318)
- ROADMAP.md Phase 6 Stage S2 section (historical; the phase breakdown has since been removed from the roadmap)
- **Null Object Pattern**: [Martin Fowler](https://martinfowler.com/eaaCatalog/specialCase.html)

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2026-01-07 | Accepted    | Initial decision for Phase 6 Stage S2 |

---
