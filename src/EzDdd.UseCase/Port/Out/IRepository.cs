using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;

namespace EzDdd.UseCase.Port.Out;

/// <summary>
///     Repository abstraction for persisting and retrieving aggregates.
/// </summary>
/// <typeparam name="TAggregate">The type of aggregate root</typeparam>
/// <typeparam name="TId">The type of the aggregate identifier</typeparam>
/// <typeparam name="TEvent">The type of domain events</typeparam>
/// <remarks>
///     <para>
///         <strong>Repository Pattern (Domain Layer Abstraction)</strong>
///     </para>
///     <para>
///         This interface belongs to the <strong>use case layer (domain layer)</strong> in Clean Architecture,
///         providing a domain-centric abstraction for aggregate persistence. It focuses on business
///         operations without exposing infrastructure details.
///     </para>
///     <para>
///         <strong>Bridge Pattern:</strong>
///         This interface serves as the <em>Abstraction</em> in the Bridge pattern:
///     </para>
///     <code>
/// Domain Layer (Use Case)
///     IRepository&lt;TAggregate, TId&gt;
///          ↓ uses (dependency)
/// Adapter Layer (Interface Adapters)
///     IRepositoryPeer&lt;TData, TId&gt;
///          ↓ implements
/// Infrastructure Layer (Frameworks &amp; Drivers)
///     SqlRepositoryPeer, MongoRepositoryPeer, etc.
/// </code>
///     <para>
///         <strong>Key Design Principles:</strong>
///     </para>
///     <list type="bullet">
///         <item>
///             <term>Works with Domain Objects</term>
///             <description>Operates on <see cref="AggregateRoot{TId,TEvent}" /> instances</description>
///         </item>
///         <item>
///             <term>Throws Domain Exceptions</term>
///             <description>Uses <see cref="RepositorySaveException" /> for business-level errors</description>
///         </item>
///         <item>
///             <term>No Transaction Management</term>
///             <description>
///                 Repository does NOT manage transactions. Transaction boundaries MUST be
///                 at the <see cref="IRepositoryPeer{TData,TId}" /> layer.
///             </description>
///         </item>
///         <item>
///             <term>Infrastructure Agnostic</term>
///             <description>No knowledge of database, ORM, or persistence technology</description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// // In Use Case (Application Service)
/// public class DepositMoneyCommand : ICommand&lt;DepositInput, DepositOutput&gt;
/// {
///     private readonly IRepository&lt;BankAccount, AccountId&gt; _repository;
///
///     public async Task&lt;DepositOutput&gt; ExecuteAsync(DepositInput input)
///     {
///         // 1. Load aggregate from repository
///         var account = await _repository.FindByIdAsync(input.AccountId);
///         if (account == null)
///             return new DepositOutput { ExitCode = ExitCode.ResourceNotFoundFailure };
///
///         // 2. Execute domain logic
///         account.Deposit(input.Amount);
///
///         // 3. Save aggregate (may throw RepositorySaveException)
///         try
///         {
///             await _repository.SaveAsync(account);
///         }
///         catch (RepositorySaveException ex)
///             when (ex.Message == RepositorySaveException.OptimisticLockingFailure)
///         {
///             return new DepositOutput { ExitCode = ExitCode.ConflictFailure };
///         }
///
///         return new DepositOutput { ExitCode = ExitCode.Success };
///     }
/// }
/// </code>
/// </example>
public interface IRepository<TAggregate, in TId, TEvent>
    where TAggregate : AggregateRoot<TId, TEvent>
    where TEvent : class, IInternalDomainEvent
{
    /// <summary>
    ///     Finds an aggregate by its identifier.
    /// </summary>
    /// <param name="id">The aggregate identifier</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the aggregate if found, or <c>null</c> if not found.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method performs a read operation and should NOT throw exceptions for
    ///         "not found" cases. Instead, it returns <c>null</c> to allow the caller
    ///         to handle the absence appropriately.
    ///     </para>
    ///     <para>
    ///         <strong>Event Sourcing:</strong>
    ///         For event-sourced aggregates, this method reconstructs the aggregate
    ///         state by replaying all events from the event stream.
    ///     </para>
    ///     <para>
    ///         <strong>State Sourcing:</strong>
    ///         For state-sourced aggregates, this method loads the current state
    ///         from the database.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var account = await _repository.FindByIdAsync(accountId);
    /// if (account == null)
    /// {
    ///     return new Output { ExitCode = ExitCode.ResourceNotFoundFailure };
    /// }
    /// </code>
    /// </example>
    Task<TAggregate?> FindByIdAsync(TId id);

    /// <summary>
    ///     Saves an aggregate (create or update).
    /// </summary>
    /// <param name="aggregate">The aggregate to save</param>
    /// <returns>A task that represents the asynchronous save operation</returns>
    /// <exception cref="RepositorySaveException">
    ///     Thrown when the save operation fails. Common causes:
    ///     <list type="bullet">
    ///         <item>
    ///             <term>Optimistic Locking Failure</term>
    ///             <description>
    ///                 Concurrent modification detected. The aggregate was modified by another
    ///                 transaction since it was loaded. Exception message will be
    ///                 <see cref="RepositorySaveException.OptimisticLockingFailure" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>Database Constraint Violation</term>
    ///             <description>
    ///                 Unique constraint, foreign key constraint, or check constraint violated.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>Database Connection Error</term>
    ///             <description>
    ///                 Connection lost or transaction timeout.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         <strong>Create or Update:</strong>
    ///         This method handles both creation of new aggregates and updates of existing ones.
    ///         The implementation determines whether to INSERT or UPDATE based on the aggregate's
    ///         version number (typically -1 for new aggregates, 0+ for existing).
    ///     </para>
    ///     <para>
    ///         <strong>Domain Events:</strong>
    ///         After successful persistence, domain events from the aggregate are typically:
    ///     </para>
    ///     <list type="number">
    ///         <item>Published to an event bus (for ExternalDomainEvent)</item>
    ///         <item>Stored in an outbox table (Transactional Outbox pattern)</item>
    ///         <item>Cleared from the aggregate</item>
    ///     </list>
    ///     <para>
    ///         <strong>Optimistic Locking:</strong>
    ///         Most implementations use optimistic locking to handle concurrent modifications.
    ///         When a conflict is detected, <see cref="RepositorySaveException" /> is thrown
    ///         with the message <see cref="RepositorySaveException.OptimisticLockingFailure" />.
    ///     </para>
    ///     <para>
    ///         <strong>⚠️ CRITICAL ARCHITECTURE RULE - Transaction Boundary:</strong>
    ///     </para>
    ///     <para>
    ///         ❌ <strong>WRONG</strong>: IRepository implementations MUST NOT contain transaction logic
    ///         (no <c>BeginTransaction()</c>, <c>TransactionScope</c>, or similar).
    ///     </para>
    ///     <para>
    ///         ✅ <strong>CORRECT</strong>: Transaction boundaries MUST be implemented ONLY at the
    ///         <see cref="IRepositoryPeer{TData,TId}" /> layer (Interface Adapters layer).
    ///     </para>
    ///     <para>
    ///         <strong>Rationale</strong>: This enforces Clean Architecture layer separation.
    ///         IRepository resides in the Use Cases layer (domain logic), while IRepositoryPeer
    ///         resides in the Interface Adapters layer (infrastructure concerns like transactions).
    ///     </para>
    ///     <para>
    ///         See <c>docs/TRANSACTION_BOUNDARY_GUIDE.md</c> for detailed examples and best practices.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Handle optimistic locking conflicts
    /// try
    /// {
    ///     await _repository.SaveAsync(account);
    /// }
    /// catch (RepositorySaveException ex)
    ///     when (ex.Message == RepositorySaveException.OptimisticLockingFailure)
    /// {
    ///     // Retry logic or return conflict error
    ///     return new Output { ExitCode = ExitCode.ConflictFailure };
    /// }
    /// </code>
    /// </example>
    Task SaveAsync(TAggregate aggregate);

    /// <summary>
    ///     Deletes an aggregate.
    /// </summary>
    /// <param name="aggregate">The aggregate to delete</param>
    /// <returns>A task that represents the asynchronous delete operation</returns>
    /// <remarks>
    ///     <para>
    ///         <strong>Event Sourcing:</strong>
    ///         For event-sourced aggregates, this typically means appending a
    ///         <see cref="IInternalDomainEvent.IDestructionEvent" /> to the event stream.
    ///         The event stream itself is NOT deleted (append-only principle).
    ///     </para>
    ///     <para>
    ///         <strong>State Sourcing:</strong>
    ///         For state-sourced aggregates, this typically means:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>Hard delete: Physically remove the record from the database</item>
    ///         <item>Soft delete: Set a "deleted" flag and keep the record</item>
    ///     </list>
    ///     <para>
    ///         The deletion strategy is implementation-specific and should align with
    ///         business requirements.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var account = await _repository.FindByIdAsync(accountId);
    /// if (account != null)
    /// {
    ///     // Perform domain logic for deletion (e.g., check balances)
    ///     account.Close();
    ///
    ///     // Delete aggregate
    ///     await _repository.DeleteAsync(account);
    /// }
    /// </code>
    /// </example>
    Task DeleteAsync(TAggregate aggregate);
}
