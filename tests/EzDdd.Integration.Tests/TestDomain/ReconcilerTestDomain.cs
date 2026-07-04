using EzDdd.UseCase.Port.In;

namespace EzDdd.Integration.Tests.TestDomain;

/// <summary>
///     Simple data item for reconciler testing.
/// </summary>
public sealed record DataItem(string Id, string Status, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt = null);

/// <summary>
///     In-memory repository for testing reconciler operations.
/// </summary>
public sealed class InMemoryDataItemRepository
{
    private readonly Dictionary<string, DataItem> _storage = [];

    public int Count => _storage.Count;

    public void Add(DataItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _storage[item.Id] = item;
    }

    public Task<DataItem?> FindByIdAsync(string id)
    {
        _storage.TryGetValue(id, out DataItem? item);
        return Task.FromResult(item);
    }

    public Task<IList<DataItem>> FindByStatusAsync(string status)
    {
        List<DataItem> items = _storage
            .Values.Where(i => string.Equals(i.Status, status, StringComparison.Ordinal))
            .ToList();
        return Task.FromResult<IList<DataItem>>(items);
    }

    public Task<IList<DataItem>> FindExpiredAsync(DateTimeOffset cutoffDate)
    {
        List<DataItem> items = _storage
            .Values.Where(i => i.ExpiresAt.HasValue && i.ExpiresAt.Value < cutoffDate)
            .ToList();
        return Task.FromResult<IList<DataItem>>(items);
    }

    public Task DeleteAsync(string id)
    {
        _storage.Remove(id);
        return Task.CompletedTask;
    }

    public void Clear()
    {
        _storage.Clear();
    }
}

/// <summary>
///     Context for cleanup reconciliation.
/// </summary>
public sealed record CleanupContext(DateTimeOffset CutoffDate, string TargetStatus);

/// <summary>
///     Report for cleanup reconciliation results.
/// </summary>
public sealed record CleanupReport(
    int TotalChecked,
    int DeletedCount,
    int SkippedCount,
    int ErrorCount,
    IReadOnlyList<string> DeletedIds,
    IReadOnlyList<string> Errors
);

/// <summary>
///     Reconciler that cleans up expired data items.
/// </summary>
public sealed class ExpiredDataCleanupReconciler : IReconciler<CleanupContext, CleanupReport>
{
    private readonly InMemoryDataItemRepository _repository;

    public ExpiredDataCleanupReconciler(InMemoryDataItemRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<CleanupReport> ReconcileAsync(CleanupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Validate context
        if (context.CutoffDate > DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("CutoffDate cannot be in the future");
        }

        // Find expired items
        IList<DataItem> expiredItems = await _repository.FindExpiredAsync(context.CutoffDate);

        // Filter by target status if specified
        if (!string.IsNullOrEmpty(context.TargetStatus))
        {
            expiredItems = expiredItems
                .Where(i => string.Equals(i.Status, context.TargetStatus, StringComparison.Ordinal))
                .ToList();
        }

        int totalChecked = expiredItems.Count;
        int deletedCount = 0;
        int skippedCount = 0;
        int errorCount = 0;
        List<string> deletedIds = [];
        List<string> errors = [];

        // Process each expired item
        foreach (DataItem item in expiredItems)
        {
            try
            {
                await _repository.DeleteAsync(item.Id);
                deletedCount++;
                deletedIds.Add(item.Id);
            }
            catch (Exception ex)
            {
                errorCount++;
                errors.Add($"Failed to delete {item.Id}: {ex.Message}");
            }
        }

        return new CleanupReport(totalChecked, deletedCount, skippedCount, errorCount, deletedIds, errors);
    }
}

/// <summary>
///     Simple reconciler that doesn't need context (demonstrates NullContext usage).
/// </summary>
public sealed class SimpleStatusCheckReconciler : IReconciler<NullContext, StatusCheckReport>
{
    private readonly InMemoryDataItemRepository _repository;

    public SimpleStatusCheckReconciler(InMemoryDataItemRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public Task<StatusCheckReport> ReconcileAsync(NullContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        int totalItems = _repository.Count;
        StatusCheckReport report = new(totalItems, DateTimeOffset.UtcNow, totalItems > 0 ? "OK" : "EMPTY");

        return Task.FromResult(report);
    }
}

/// <summary>
///     Report for simple status check.
/// </summary>
public sealed record StatusCheckReport(int TotalItems, DateTimeOffset CheckedAt, string Status);
