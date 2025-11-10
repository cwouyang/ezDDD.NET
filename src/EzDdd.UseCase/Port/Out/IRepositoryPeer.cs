using EzDdd.UseCase.Exceptions;

namespace EzDdd.UseCase.Port.Out;

/// <summary>
///     Repository Service Provider Interface (SPI) for actual persistence implementation.
/// </summary>
/// <typeparam name="TData">The type of persistence data structure</typeparam>
/// <typeparam name="TId">The type of the identifier</typeparam>
/// <remarks>
///     <para>
///         <strong>Bridge Pattern (Implementor Role)</strong>
///     </para>
///     <para>
///         This interface belongs to the <strong>adapter layer (interface adapters layer)</strong>
///         in Clean Architecture, serving as the <em>Implementor</em> in the Bridge pattern.
///         It provides the actual persistence implementation while <see cref="IRepository{TAggregate,TId,TEvent}" />
///         provides the domain-level abstraction.
///     </para>
///     <para>
///         <strong>Architecture Layers:</strong>
///     </para>
///     <code>
/// Domain Layer (Use Case)
///     IRepository&lt;TAggregate, TId, TEvent&gt; (Abstraction)
///          ↓ depends on
/// Adapter Layer (Interface Adapters)
///     IRepositoryPeer&lt;TData, TId&gt; (Implementor) ← YOU ARE HERE
///          ↓ implemented by
/// Infrastructure Layer (Frameworks &amp; Drivers)
///     SqlRepositoryPeer, MongoRepositoryPeer, InMemoryRepositoryPeer, etc.
/// </code>
///     <para>
///         <strong>Key Design Principles:</strong>
///     </para>
///     <list type="bullet">
///         <item>
///             <term>Works with Data Structures</term>
///             <description>
///                 Operates on <see cref="IStoreData{TId}" /> persistence DTOs, not domain objects
///             </description>
///         </item>
///         <item>
///             <term>Throws Infrastructure Exceptions</term>
///             <description>
///                 Uses <see cref="RepositoryPeerSaveException" /> for database-level errors.
///                 These are typically caught and translated to <see cref="RepositorySaveException" />
///                 by the Repository layer.
///             </description>
///         </item>
///         <item>
///             <term>⚠️ Transaction Boundary MUST Be Here</term>
///             <description>
///                 Implementations MUST manage transactions at this layer. This ensures atomic
///                 persistence of both aggregate state and domain events (Transactional Outbox pattern).
///             </description>
///         </item>
///         <item>
///             <term>Database Technology Specific</term>
///             <description>
///                 Implementations are tied to specific persistence technologies
///                 (SQL, MongoDB, Redis, etc.)
///             </description>
///         </item>
///     </list>
///     <para>
///         <strong>⚠️ CRITICAL ARCHITECTURE RULE - Transaction Boundary:</strong>
///     </para>
///     <para>
///         ✅ <strong>CORRECT</strong>: IRepositoryPeer implementations MUST manage transactions
///         to ensure atomic persistence of aggregate state and domain events.
///     </para>
///     <para>
///         ❌ <strong>WRONG</strong>: Transaction logic belongs ONLY here, NOT in IRepository
///         implementations (which reside in the Use Cases layer).
///     </para>
///     <para>
///         <strong>Rationale</strong>: This enforces Clean Architecture layer separation.
///         Transaction management is an infrastructure concern (Interface Adapters layer),
///         not a domain concern (Use Cases layer).
///     </para>
///     <para>
///         See <c>docs/TRANSACTION_BOUNDARY_GUIDE.md</c> for detailed examples and best practices.
///     </para>
///     <para>
///         <strong>Transaction Implementation Examples:</strong>
///     </para>
///     <code>
/// public async Task SaveAsync(TData data)
/// {
///     // Option 1: EF Core Transaction
///     using var transaction = await _dbContext.Database.BeginTransactionAsync();
///     try
///     {
///         // 1. Save aggregate state
///         _dbContext.Aggregates.Update(data);
/// 
///         // 2. Save events (Transactional Outbox)
///         foreach (var @event in data.Events)
///         {
///             _dbContext.OutboxEvents.Add(new OutboxEventEntity(@event));
///         }
/// 
///         // 3. Commit atomically
///         await _dbContext.SaveChangesAsync();
///         await transaction.CommitAsync();
///     }
///     catch (DbUpdateConcurrencyException ex)
///     {
///         await transaction.RollbackAsync();
///         throw new RepositoryPeerSaveException("Optimistic locking failure", ex);
///     }
/// }
/// 
/// // Option 2: TransactionScope
/// using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
/// // ... save operations ...
/// scope.Complete();
/// </code>
/// </remarks>
/// <example>
///     <para>
///         <strong>SQL Server Implementation Example:</strong>
///     </para>
///     <code>
/// public class SqlBankAccountRepositoryPeer : IRepositoryPeer&lt;BankAccountData, AccountId&gt;
/// {
///     private readonly ApplicationDbContext _dbContext;
/// 
///     public async Task&lt;BankAccountData?&gt; FindByIdAsync(AccountId id)
///     {
///         return await _dbContext.BankAccounts
///             .FirstOrDefaultAsync(a => a.Id == id);
///     }
/// 
///     public async Task SaveAsync(BankAccountData data)
///     {
///         using var transaction = await _dbContext.Database.BeginTransactionAsync();
///         try
///         {
///             // Upsert aggregate
///             if (data.Version == -1)
///                 _dbContext.BankAccounts.Add(data);
///             else
///                 _dbContext.BankAccounts.Update(data);
/// 
///             // Store events in outbox
///             foreach (var @event in data.Events)
///             {
///                 _dbContext.OutboxEvents.Add(new OutboxEvent
///                 {
///                     EventId = @event.Id,
///                     EventType = @event.GetType().Name,
///                     EventData = JsonSerializer.Serialize(@event),
///                     OccurredOn = @event.OccurredOn
///                 });
///             }
/// 
///             await _dbContext.SaveChangesAsync();
///             await transaction.CommitAsync();
///         }
///         catch (DbUpdateConcurrencyException ex)
///         {
///             await transaction.RollbackAsync();
///             throw new RepositoryPeerSaveException("Optimistic locking failure", ex);
///         }
///         catch (Exception ex)
///         {
///             await transaction.RollbackAsync();
///             throw new RepositoryPeerSaveException("Database save failed", ex);
///         }
///     }
/// 
///     public async Task DeleteAsync(BankAccountData data)
///     {
///         _dbContext.BankAccounts.Remove(data);
///         await _dbContext.SaveChangesAsync();
///     }
/// }
/// </code>
/// </example>
public interface IRepositoryPeer<TData, in TId>
    where TData : IStoreData<TId>
{
    /// <summary>
    ///     Loads data from persistence by identifier.
    /// </summary>
    /// <param name="id">The data identifier</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the data if found, or <c>null</c> if not found.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method performs a database query to load the persistence data structure.
    ///         It should NOT throw exceptions for "not found" cases.
    ///     </para>
    ///     <para>
    ///         <strong>Event Sourcing:</strong>
    ///         For event-sourced implementations, this method loads all events from the
    ///         event stream identified by <see cref="IStoreData{TId}.StreamName" />.
    ///     </para>
    ///     <para>
    ///         <strong>State Sourcing:</strong>
    ///         For state-sourced implementations, this method loads the current state
    ///         record from the database table.
    ///     </para>
    /// </remarks>
    Task<TData?> FindByIdAsync(TId id);

    /// <summary>
    ///     Persists data to storage.
    /// </summary>
    /// <param name="data">The data to save</param>
    /// <returns>A task that represents the asynchronous save operation</returns>
    /// <exception cref="RepositoryPeerSaveException">
    ///     Thrown when the save operation fails at the database level.
    ///     Common causes include:
    ///     <list type="bullet">
    ///         <item>Database concurrency errors (optimistic locking)</item>
    ///         <item>Connection failures or timeouts</item>
    ///         <item>Constraint violations</item>
    ///         <item>Disk space exhaustion</item>
    ///     </list>
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         <strong>⚠️ MUST Use Transactions:</strong>
    ///         Implementations MUST wrap all database operations in a transaction to ensure
    ///         atomic persistence of:
    ///     </para>
    ///     <list type="number">
    ///         <item>Aggregate state (current snapshot)</item>
    ///         <item>Domain events (from <see cref="IStoreData{TId}.Events" />)</item>
    ///     </list>
    ///     <para>
    ///         If either operation fails, the entire transaction MUST be rolled back.
    ///     </para>
    ///     <para>
    ///         <strong>Optimistic Locking:</strong>
    ///         Implementations typically use the <see cref="IStoreData{TId}.Version" /> property
    ///         to implement optimistic locking:
    ///     </para>
    ///     <code>
    /// UPDATE aggregates
    /// SET state = @state, version = @newVersion
    /// WHERE id = @id AND version = @expectedVersion
    /// </code>
    ///     <para>
    ///         If the UPDATE affects 0 rows, a concurrent modification has occurred, and
    ///         <see cref="RepositoryPeerSaveException" /> should be thrown.
    ///     </para>
    ///     <para>
    ///         <strong>Transaction Strategies:</strong>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <term>EF Core:</term>
    ///             <description>
    ///                 <c>await _dbContext.Database.BeginTransactionAsync()</c>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>ADO.NET:</term>
    ///             <description>
    ///                 <c>await connection.BeginTransactionAsync()</c>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>TransactionScope:</term>
    ///             <description>
    ///                 <c>new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)</c>
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    Task SaveAsync(TData data);

    /// <summary>
    ///     Removes data from storage.
    /// </summary>
    /// <param name="data">The data to delete</param>
    /// <returns>A task that represents the asynchronous delete operation</returns>
    /// <remarks>
    ///     <para>
    ///         <strong>Event Sourcing:</strong>
    ///         For event-sourced implementations, deletion typically means appending a
    ///         destruction event to the stream. The stream itself is NOT deleted
    ///         (append-only principle).
    ///     </para>
    ///     <para>
    ///         <strong>State Sourcing:</strong>
    ///         For state-sourced implementations, this can be:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <term>Hard Delete:</term>
    ///             <description>Physically remove the record: <c>DELETE FROM table WHERE id = @id</c></description>
    ///         </item>
    ///         <item>
    ///             <term>Soft Delete:</term>
    ///             <description>Set a flag: <c>UPDATE table SET is_deleted = true WHERE id = @id</c></description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         The deletion strategy should align with business requirements and regulatory
    ///         compliance (e.g., GDPR, audit trails).
    ///     </para>
    /// </remarks>
    Task DeleteAsync(TData data);
}