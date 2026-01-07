using EzDdd.UseCase.Port.In;

namespace EzDdd.UseCase.Tests.Integration.TestDomain;

/// <summary>
///     Example reconciler that cleans up expired draft orders.
///     Demonstrates how to use <see cref="IReconciler{TContext,TReport}" /> for system maintenance tasks.
/// </summary>
/// <remarks>
///     This reconciler finds all draft orders that have been inactive for longer than the specified
///     expiration period and deletes them to maintain data hygiene.
/// </remarks>
public class CleanUpExpiredOrdersReconciler : IReconciler<OrderCleanupContext, OrderCleanupReport>
{
    private readonly IOrderRepository _orderRepository;

    public CleanUpExpiredOrdersReconciler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    /// <summary>
    ///     Reconciles system state by cleaning up expired draft orders.
    /// </summary>
    /// <param name="context">Context specifying expiration criteria.</param>
    /// <returns>A report detailing the cleanup results.</returns>
    public async Task<OrderCleanupReport> ReconcileAsync(OrderCleanupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ExpirationDays <= 0)
        {
            throw new InvalidOperationException("Expiration days must be positive");
        }

        DateTimeOffset cutoffDate = DateTimeOffset.UtcNow.AddDays(-context.ExpirationDays);

        // 1. Find all draft orders (in a real system, this would query by status and date)
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

        // 3. Return cleanup report
        return new OrderCleanupReport(
            TotalChecked: expiredOrderIds.Count,
            DeletedCount: deletedCount,
            ErrorCount: errorCount,
            Errors: errors
        );
    }

    /// <summary>
    ///     Finds expired draft orders (simplified for demonstration).
    /// </summary>
    /// <remarks>
    ///     In a real system, this would use a query interface to efficiently find
    ///     orders by status and creation date.
    /// </remarks>
    private Task<List<OrderId>> FindExpiredDraftOrdersAsync(DateTimeOffset cutoffDate)
    {
        // In a real system, this would query the database
        // For demonstration purposes, return empty list
        List<OrderId> expiredOrders = [];
        return Task.FromResult(expiredOrders);
    }
}

/// <summary>
///     Context for order cleanup reconciliation.
/// </summary>
/// <param name="ExpirationDays">Number of days after which draft orders are considered expired.</param>
public record OrderCleanupContext(int ExpirationDays);

/// <summary>
///     Report describing the results of order cleanup reconciliation.
/// </summary>
/// <param name="TotalChecked">Total number of orders checked for expiration.</param>
/// <param name="DeletedCount">Number of orders successfully deleted.</param>
/// <param name="ErrorCount">Number of errors encountered during cleanup.</param>
/// <param name="Errors">List of error messages (if any).</param>
public record OrderCleanupReport(
    int TotalChecked,
    int DeletedCount,
    int ErrorCount,
    IReadOnlyList<string> Errors
);

/// <summary>
///     Repository interface for Order aggregate (simplified for example).
/// </summary>
public interface IOrderRepository
{
    Task DeleteAsync(OrderId id);
}
