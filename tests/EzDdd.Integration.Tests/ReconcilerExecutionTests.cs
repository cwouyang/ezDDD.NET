using EzDdd.Integration.Tests.TestDomain;
using EzDdd.UseCase.Port.In;

namespace EzDdd.Integration.Tests;

/// <summary>
///     Integration tests for IReconciler execution.
///     Verifies reconciler workflows, context processing, and report generation.
/// </summary>
/// <remarks>
///     <para>
///         These tests validate the IReconciler interface introduced in Java ezddd 4.1.0
///         for system state reconciliation tasks such as:
///         <list type="bullet">
///             <item>
///                 <description>Data cleanup (expired records, orphaned data)</description>
///             </item>
///             <item>
///                 <description>Consistency checks (referential integrity, business rules)</description>
///             </item>
///             <item>
///                 <description>Periodic maintenance (archiving, aggregation)</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <strong>Java 4.1.0 Feature</strong>: IReconciler&lt;TContext, TReport&gt; pattern for
///         scheduled background tasks and administrative operations.
///     </para>
/// </remarks>
public sealed class ReconcilerExecutionTests
{
#region Realistic Workflow Tests

    [Fact]
    public async Task CleanupWorkflow_WithRealisticData_ShouldWorkEndToEnd()
    {
        InMemoryDataItemRepository repository = new();
        ExpiredDataCleanupReconciler reconciler = new(repository);

        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Scenario: E-commerce system with draft orders that need cleanup
        // Add 10 draft orders created 30-60 days ago (expired)
        for (int i = 1; i <= 10; i++)
        {
            repository.Add
            (
                new DataItem
                (
                    $"ORDER-DRAFT-{i:D3}",
                    "DRAFT",
                    now.AddDays(-30 - i),
                    now.AddDays(-i)
                )
            ); // Expired i days ago
        }

        // Add 5 active orders (should not be deleted)
        for (int i = 1; i <= 5; i++)
        {
            repository.Add
            (
                new DataItem
                (
                    $"ORDER-ACTIVE-{i:D3}",
                    "ACTIVE",
                    now.AddDays(-10),
                    now.AddDays(30)
                )
            ); // Expires in future
        }

        // Add 3 pending orders (different status, should not be deleted even if expired)
        for (int i = 1; i <= 3; i++)
        {
            repository.Add
            (
                new DataItem
                (
                    $"ORDER-PENDING-{i:D3}",
                    "PENDING",
                    now.AddDays(-40),
                    now.AddDays(-5)
                )
            );
        }

        CleanupContext context = new(now, "DRAFT");

        // Act
        CleanupReport report = await reconciler.ReconcileAsync(context);

        // Assert: Report should show cleanup results
        Assert.Equal(10, report.TotalChecked); // 10 expired DRAFT orders
        Assert.Equal(10, report.DeletedCount);
        Assert.Equal(0, report.ErrorCount);
        Assert.Equal(10, report.DeletedIds.Count);
        Assert.Empty(report.Errors);

        // Assert: Only active and pending orders should remain
        Assert.Equal(8, repository.Count); // 5 active + 3 pending

        // Verify specific orders remain
        Assert.NotNull(await repository.FindByIdAsync("ORDER-ACTIVE-001"));
        Assert.NotNull(await repository.FindByIdAsync("ORDER-PENDING-001"));

        // Verify expired drafts are gone
        Assert.Null(await repository.FindByIdAsync("ORDER-DRAFT-001"));
    }

#endregion

#region Basic Reconciler Execution Tests

    [Fact]
    public async Task Reconciler_WithNoExpiredItems_ShouldReturnEmptyReport()
    {
        InMemoryDataItemRepository repository = new();
        ExpiredDataCleanupReconciler reconciler = new(repository);

        // Add non-expired items
        repository.Add(new DataItem("ITEM-001", "ACTIVE", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(10)));
        repository.Add(new DataItem("ITEM-002", "ACTIVE", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(20)));

        CleanupContext context = new(DateTimeOffset.UtcNow, "ACTIVE");

        // Act
        CleanupReport report = await reconciler.ReconcileAsync(context);

        // Assert
        Assert.Equal(0, report.TotalChecked);
        Assert.Equal(0, report.DeletedCount);
        Assert.Equal(0, report.ErrorCount);
        Assert.Empty(report.DeletedIds);
        Assert.Empty(report.Errors);

        // All items should still be in repository
        Assert.Equal(2, repository.Count);
    }

    [Fact]
    public async Task Reconciler_WithExpiredItems_ShouldDeleteThemAndReportCorrectly()
    {
        InMemoryDataItemRepository repository = new();
        ExpiredDataCleanupReconciler reconciler = new(repository);

        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Add expired items (expiration date in the past)
        repository.Add(new DataItem("EXPIRED-001", "DRAFT", now.AddDays(-10), now.AddDays(-5)));
        repository.Add(new DataItem("EXPIRED-002", "DRAFT", now.AddDays(-20), now.AddDays(-15)));
        repository.Add(new DataItem("EXPIRED-003", "DRAFT", now.AddDays(-7), now.AddDays(-2)));

        // Add non-expired items
        repository.Add(new DataItem("ACTIVE-001", "DRAFT", now, now.AddDays(10)));
        repository.Add(new DataItem("ACTIVE-002", "ACTIVE", now.AddDays(-5), now.AddDays(5)));

        CleanupContext context = new(now, "DRAFT");

        // Act
        CleanupReport report = await reconciler.ReconcileAsync(context);

        // Assert: Report should show 3 items checked and deleted
        Assert.Equal(3, report.TotalChecked);
        Assert.Equal(3, report.DeletedCount);
        Assert.Equal(0, report.ErrorCount);
        Assert.Equal(3, report.DeletedIds.Count);
        Assert.Contains("EXPIRED-001", report.DeletedIds);
        Assert.Contains("EXPIRED-002", report.DeletedIds);
        Assert.Contains("EXPIRED-003", report.DeletedIds);
        Assert.Empty(report.Errors);

        // Assert: Only 2 non-expired items should remain
        Assert.Equal(2, repository.Count);
        Assert.NotNull(await repository.FindByIdAsync("ACTIVE-001"));
        Assert.NotNull(await repository.FindByIdAsync("ACTIVE-002"));
    }

    [Fact]
    public async Task Reconciler_WithMixedResults_ShouldReportAllOutcomes()
    {
        InMemoryDataItemRepository repository = new();
        ExpiredDataCleanupReconciler reconciler = new(repository);

        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Add 5 expired draft items
        for (int i = 1; i <= 5; i++)
        {
            repository.Add
            (
                new DataItem
                (
                    $"DRAFT-{i:D3}",
                    "DRAFT",
                    now.AddDays(-30),
                    now.AddDays(-10)
                )
            );
        }

        CleanupContext context = new(now, "DRAFT");

        // Act
        CleanupReport report = await reconciler.ReconcileAsync(context);

        // Assert
        Assert.Equal(5, report.TotalChecked);
        Assert.Equal(5, report.DeletedCount);
        Assert.Equal(0, report.ErrorCount);
        Assert.Equal(5, report.DeletedIds.Count);

        // All items should be deleted
        Assert.Equal(0, repository.Count);
    }

#endregion

#region NullContext Reconciler Tests

    [Fact]
    public async Task NullContextReconciler_ShouldExecuteCorrectly()
    {
        InMemoryDataItemRepository repository = new();
        SimpleStatusCheckReconciler reconciler = new(repository);

        // Add some items
        repository.Add(new DataItem("ITEM-001", "OK", DateTimeOffset.UtcNow));
        repository.Add(new DataItem("ITEM-002", "OK", DateTimeOffset.UtcNow));

        // Act: Execute reconciler with NullContext
        StatusCheckReport report = await reconciler.ReconcileAsync(NullContext.Instance);

        // Assert
        Assert.Equal(2, report.TotalItems);
        Assert.Equal("OK", report.Status);
        Assert.True(report.CheckedAt <= DateTimeOffset.UtcNow);
        Assert.True(report.CheckedAt >= DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task NullContextReconciler_WithEmptyRepository_ShouldReportEmpty()
    {
        InMemoryDataItemRepository repository = new();
        SimpleStatusCheckReconciler reconciler = new(repository);

        // Act: Execute with no items in repository
        StatusCheckReport report = await reconciler.ReconcileAsync(NullContext.Instance);

        // Assert
        Assert.Equal(0, report.TotalItems);
        Assert.Equal("EMPTY", report.Status);
    }

#endregion

#region Context Validation Tests

    [Fact]
    public async Task Reconciler_WithNullContext_ShouldThrowArgumentNullException()
    {
        InMemoryDataItemRepository repository = new();
        ExpiredDataCleanupReconciler reconciler = new(repository);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>
        (() =>
             reconciler.ReconcileAsync(null!)
        );
    }

    [Fact]
    public async Task Reconciler_WithFutureCutoffDate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        InMemoryDataItemRepository repository = new();
        ExpiredDataCleanupReconciler reconciler = new(repository);

        // Invalid context: CutoffDate in the future
        CleanupContext invalidContext = new(
            CutoffDate: DateTimeOffset.UtcNow.AddDays(10), // Future date
            TargetStatus: "DRAFT"
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => reconciler.ReconcileAsync(invalidContext)
        );
    }

#endregion
}