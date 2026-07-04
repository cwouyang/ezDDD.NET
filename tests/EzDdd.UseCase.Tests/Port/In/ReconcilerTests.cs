using EzDdd.UseCase.Port.In;

namespace EzDdd.UseCase.Tests.Port.In;

public class ReconcilerTests
{
    #region Basic Reconciliation Tests

    [Fact]
    public async Task ReconcileAsync_WithValidContext_ReturnsReport()
    {
        TestReconciler reconciler = new();
        TestContext context = new(3);

        TestReport report = await reconciler.ReconcileAsync(context);

        Assert.NotNull(report);
        Assert.Equal(3, report.ProcessedCount);
        Assert.Equal(0, report.ErrorCount);
    }

    [Fact]
    public async Task ReconcileAsync_WithNullContext_UsesInstance()
    {
        NullContextReconciler reconciler = new();

        TestReport report = await reconciler.ReconcileAsync(NullContext.Instance);

        Assert.NotNull(report);
        Assert.Equal(0, report.ProcessedCount);
    }

    [Fact]
    public async Task ReconcileAsync_Async_CompletesSuccessfully()
    {
        AsyncReconciler reconciler = new();
        TestContext context = new(5);

        TestReport report = await reconciler.ReconcileAsync(context);

        Assert.NotNull(report);
        Assert.Equal(5, report.ProcessedCount);
        Assert.True(report.WasAsync);
    }

    #endregion

    #region Reconciliation Logic Tests

    [Fact]
    public async Task ReconcileAsync_WithMultipleItems_ReconcilesAll()
    {
        MultiItemReconciler reconciler = new();
        TestContext context = new(10);

        TestReport report = await reconciler.ReconcileAsync(context);

        Assert.Equal(10, report.ProcessedCount);
        Assert.Equal(10, report.ReconciledCount);
    }

    [Fact]
    public async Task ReconcileAsync_WithNoChangesNeeded_ReturnsEmptyReport()
    {
        NoChangeReconciler reconciler = new();
        TestContext context = new(5);

        TestReport report = await reconciler.ReconcileAsync(context);

        Assert.Equal(5, report.ProcessedCount);
        Assert.Equal(0, report.ReconciledCount);
    }

    [Fact]
    public async Task ReconcileAsync_WithConflicts_ResolvesCorrectly()
    {
        ConflictReconciler reconciler = new();
        TestContext context = new(9);

        TestReport report = await reconciler.ReconcileAsync(context);

        Assert.Equal(9, report.ProcessedCount);
        Assert.Equal(3, report.ReconciledCount); // 3 conflicts resolved (every 3rd item)
    }

    [Fact]
    public async Task ReconcileAsync_WithPartialFailure_ReportsErrors()
    {
        PartialFailureReconciler reconciler = new();
        TestContext context = new(10);

        TestReport report = await reconciler.ReconcileAsync(context);

        Assert.Equal(10, report.ProcessedCount);
        Assert.Equal(7, report.ReconciledCount); // 7 succeeded
        Assert.Equal(3, report.ErrorCount); // 3 failed
    }

    #endregion

    #region Edge Cases and Validation Tests

    [Fact]
    public async Task ReconcileAsync_WithNullContextParameter_ThrowsArgumentNullException()
    {
        TestReconciler reconciler = new();

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await reconciler.ReconcileAsync(null!));
    }

    [Fact]
    public async Task ReconcileAsync_WithInvalidContext_ThrowsInvalidOperationException()
    {
        InvalidContextReconciler reconciler = new();
        TestContext context = new(-1); // Invalid count

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await reconciler.ReconcileAsync(context));
    }

    [Fact]
    public async Task ReconcileAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        CancellableReconciler reconciler = new();
        TestContext context = new(100);

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await reconciler.ReconcileAsync(context));
    }

    #endregion

    #region NullContext Tests

    [Fact]
    public void NullContext_Instance_IsSingleton()
    {
        NullContext instance1 = NullContext.Instance;
        NullContext instance2 = NullContext.Instance;

        Assert.Same(instance1, instance2);
    }

    [Fact]
    public async Task IReconciler_WithNullContext_WorksCorrectly()
    {
        GlobalCleanupReconciler reconciler = new();

        TestReport report = await reconciler.ReconcileAsync(NullContext.Instance);

        Assert.NotNull(report);
        Assert.True(report.WasGlobalCleanup);
    }

    #endregion

    #region Test Helper Classes

    private record TestContext(int ItemCount);

    private record TestReport(
        int ProcessedCount,
        int ReconciledCount = 0,
        int ErrorCount = 0,
        bool WasAsync = false,
        bool WasGlobalCleanup = false
    );

    private class TestReconciler : IReconciler<TestContext, TestReport>
    {
        public Task<TestReport> ReconcileAsync(TestContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            TestReport report = new(context.ItemCount, 0, 0);

            return Task.FromResult(report);
        }
    }

    private class NullContextReconciler : IReconciler<NullContext, TestReport>
    {
        public Task<TestReport> ReconcileAsync(NullContext context)
        {
            TestReport report = new(0);
            return Task.FromResult(report);
        }
    }

    private class AsyncReconciler : IReconciler<TestContext, TestReport>
    {
        public async Task<TestReport> ReconcileAsync(TestContext context)
        {
            await Task.Delay(10); // Simulate async work

            TestReport report = new(context.ItemCount, WasAsync: true);

            return report;
        }
    }

    private class MultiItemReconciler : IReconciler<TestContext, TestReport>
    {
        public Task<TestReport> ReconcileAsync(TestContext context)
        {
            // All items reconciled
            TestReport report = new(context.ItemCount, context.ItemCount);

            return Task.FromResult(report);
        }
    }

    private class NoChangeReconciler : IReconciler<TestContext, TestReport>
    {
        public Task<TestReport> ReconcileAsync(TestContext context)
        {
            // All items checked, but no changes needed
            TestReport report = new(context.ItemCount, 0);

            return Task.FromResult(report);
        }
    }

    private class ConflictReconciler : IReconciler<TestContext, TestReport>
    {
        public Task<TestReport> ReconcileAsync(TestContext context)
        {
            // Simulate conflict resolution (every 3rd item has conflict)
            int conflicts = context.ItemCount / 3;

            TestReport report = new(context.ItemCount, conflicts);

            return Task.FromResult(report);
        }
    }

    private class PartialFailureReconciler : IReconciler<TestContext, TestReport>
    {
        public Task<TestReport> ReconcileAsync(TestContext context)
        {
            // Simulate partial failures (30% failure rate)
            int errors = context.ItemCount * 3 / 10;
            int succeeded = context.ItemCount - errors;

            TestReport report = new(context.ItemCount, succeeded, errors);

            return Task.FromResult(report);
        }
    }

    private class InvalidContextReconciler : IReconciler<TestContext, TestReport>
    {
        public Task<TestReport> ReconcileAsync(TestContext context)
        {
            if (context.ItemCount < 0)
            {
                throw new InvalidOperationException("Item count cannot be negative");
            }

            return Task.FromResult(new TestReport(context.ItemCount));
        }
    }

    private class CancellableReconciler : IReconciler<TestContext, TestReport>
    {
        public Task<TestReport> ReconcileAsync(TestContext context)
        {
            // Simulate cancellation during reconciliation
            throw new OperationCanceledException("Reconciliation was cancelled");
        }
    }

    private class GlobalCleanupReconciler : IReconciler<NullContext, TestReport>
    {
        public Task<TestReport> ReconcileAsync(NullContext context)
        {
            // Global cleanup without specific context
            TestReport report = new(0, WasGlobalCleanup: true);

            return Task.FromResult(report);
        }
    }

    #endregion
}
