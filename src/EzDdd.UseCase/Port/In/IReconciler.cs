namespace EzDdd.UseCase.Port.In;

/// <summary>
///     <c>IReconciler</c> is an interface for performing system state reconciliation.
///     Reconcilers are used for maintenance tasks such as cleaning up orphaned data,
///     enforcing data consistency, or performing periodic system checks.
/// </summary>
/// <typeparam name="TContext">The type of context required for reconciliation.</typeparam>
/// <typeparam name="TReport">The type of report returned after reconciliation.</typeparam>
/// <remarks>
///     Unlike <see cref="IUseCase{TInput,TOutput}" />, reconcilers are typically invoked
///     by scheduled background jobs or administrative tools rather than direct user actions.
/// </remarks>
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

/// <summary>
///     <c>NullContext</c> is a null object pattern implementation for reconcilers that
///     do not require any input context. Use this instead of <c>null</c> or <c>object</c>
///     to maintain type safety.
/// </summary>
/// <remarks>
///     This class uses the singleton pattern to avoid unnecessary object allocations.
///     Access the single instance via <see cref="Instance" />.
/// </remarks>
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