namespace EzDdd.Cqrs.Query;

/// <summary>
///     <c>IArchive</c> is the query-side counterpart to IRepository.
///     It provides CRUD operations for read models stored in the query database.
/// </summary>
/// <typeparam name="TData">The read model type (denormalized view optimized for queries).</typeparam>
/// <typeparam name="TId">The identifier type (typically matches aggregate ID on write side).</typeparam>
/// <remarks>
///     <para>
///         <b>CQRS Separation</b>:
///     </para>
///     <list type="bullet">
///         <item>
///             <b>Write Side</b>: IRepository stores aggregates
///             (normalized, with business logic, for commands).
///         </item>
///         <item>
///             <b>Read Side</b>: <c>IArchive&lt;TData, TId&gt;</c> stores read models
///             (denormalized, optimized views, for queries).
///         </item>
///     </list>
///     <para>
///         <b>Key Characteristics</b>:
///     </para>
///     <list type="bullet">
///         <item>
///             <b>Separate Database</b>: Typically backed by a different database than
///             the write model (or at least different tables/schema).
///         </item>
///         <item>
///             <b>Denormalized Data</b>: Read models are often flattened, pre-joined views
///             optimized for specific query scenarios.
///         </item>
///         <item>
///             <b>Eventually Consistent</b>: Updated by <see cref="IProjector{TInput}" /> services
///             listening to events, so may lag slightly behind write model.
///         </item>
///         <item>
///             <b>Read-Optimized</b>: Database structure optimized for fast reads
///             (indexes, materialized views, caching).
///         </item>
///     </list>
///     <para>
///         <b>Operations</b>:
///     </para>
///     <list type="bullet">
///         <item>
///             <see cref="FindByIdAsync" />: Retrieve read model by ID (returns null if not found).
///         </item>
///         <item>
///             <see cref="SaveAsync" />: Insert or update read model (idempotent).
///         </item>
///         <item>
///             <see cref="DeleteAsync" />: Remove read model.
///         </item>
///     </list>
///     <para>
///         <b>Design Rationale</b>:
///     </para>
///     <list type="bullet">
///         <item>
///             Write models (aggregates) are normalized for transactional consistency and
///             business rule enforcement.
///         </item>
///         <item>
///             Read models (in Archive) are denormalized for query performance and can
///             be rebuilt from events if needed.
///         </item>
///         <item>
///             This separation enables independent scaling and optimization of read vs write workloads.
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
///         // Read model (denormalized view)
///         public record AccountSummaryReadModel(
///             AccountId AccountId,
///             string AccountNumber,
///             string CustomerName,
///             decimal Balance,
///             int TransactionCount,
///             DateTimeOffset LastTransactionDate
///         );
/// 
///         // Archive implementation (example: in-memory)
///         public class InMemoryAccountArchive : IArchive&lt;AccountSummaryReadModel, AccountId&gt;
///         {
///             private readonly ConcurrentDictionary&lt;AccountId, AccountSummaryReadModel&gt; _store = new();
/// 
///             public async Task&lt;AccountSummaryReadModel?&gt; FindByIdAsync(AccountId id)
///             {
///                 await Task.CompletedTask;
///                 _store.TryGetValue(id, out var readModel);
///                 return readModel;
///             }
/// 
///             public async Task SaveAsync(AccountSummaryReadModel data)
///             {
///                 await Task.CompletedTask;
///                 _store[data.AccountId] = data;
///             }
/// 
///             public async Task DeleteAsync(AccountSummaryReadModel data)
///             {
///                 await Task.CompletedTask;
///                 _store.TryRemove(data.AccountId, out _);
///             }
///         }
/// 
///         // Usage in query
///         public class GetAccountSummaryQuery
///             : IQuery&lt;GetAccountSummaryInput, GetAccountSummaryOutput&gt;
///         {
///             private readonly IArchive&lt;AccountSummaryReadModel, AccountId&gt; _archive;
/// 
///             public async Task&lt;GetAccountSummaryOutput&gt; ExecuteAsync(
///                 GetAccountSummaryInput input)
///             {
///                 var readModel = await _archive.FindByIdAsync(input.AccountId);
/// 
///                 if (readModel == null)
///                 {
///                     throw new UseCaseFailureException("Account not found");
///                 }
/// 
///                 return GetAccountSummaryOutput.Create()
///                     .SetAccountNumber(readModel.AccountNumber)
///                     .SetCustomerName(readModel.CustomerName)
///                     .SetBalance(readModel.Balance)
///                     .Succeed();
///             }
///         }
///     </code>
/// </example>
public interface IArchive<TData, in TId>
{
    /// <summary>
    ///     Finds a read model by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the read model to find.</param>
    /// <returns>
    ///     A task containing the read model if found, or <c>null</c> if not found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="id" /> is null.
    /// </exception>
    Task<TData?> FindByIdAsync(TId id);

    /// <summary>
    ///     Saves (inserts or updates) a read model in the query database.
    /// </summary>
    /// <param name="data">The read model to save.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="data" /> is null.
    /// </exception>
    /// <remarks>
    ///     This operation should be idempotent - saving the same read model multiple
    ///     times should produce the same result (for reliable event processing).
    /// </remarks>
    Task SaveAsync(TData data);

    /// <summary>
    ///     Deletes a read model from the query database.
    /// </summary>
    /// <param name="data">The read model to delete.</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="data" /> is null.
    /// </exception>
    /// <remarks>
    ///     This operation should be idempotent - deleting a non-existent read model
    ///     should not throw an exception.
    /// </remarks>
    Task DeleteAsync(TData data);
}