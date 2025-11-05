using EzDdd.Entity;

namespace EzDdd.UseCase.Port.Out;

/// <summary>
///     Abstract mapper for converting between aggregates and outbox data in state sourcing repositories.
///     Implementations define the specific mapping logic for converting aggregate state to/from persistence format.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type</typeparam>
/// <typeparam name="TData">The outbox data type for persistence</typeparam>
/// <typeparam name="TId">The type of the aggregate identifier</typeparam>
/// <remarks>
///     <para>
///         OutboxMapper is used by <see cref="OutboxRepository{TAggregate, TData, TId}" /> to convert
///         between domain aggregates and persistence data structures that support the Transactional Outbox pattern.
///     </para>
///     <para>
///         The mapper must handle bidirectional conversion:
///         <list type="bullet">
///             <item>
///                 <description><see cref="ToData" />: Aggregate → OutboxData (for saving)</description>
///             </item>
///             <item>
///                 <description><see cref="ToDomain" />: OutboxData → Aggregate (for loading)</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         When implementing this mapper, ensure that:
///         <list type="number">
///             <item>
///                 <description>All aggregate state fields are copied to/from data</description>
///             </item>
///             <item>
///                 <description>Version numbers are preserved for optimistic locking</description>
///             </item>
///             <item>
///                 <description>Domain events are copied to data for Transactional Outbox</description>
///             </item>
///             <item>
///                 <description>Stream names are set appropriately</description>
///             </item>
///         </list>
///     </para>
/// </remarks>
/// <example>
///     <code>
/// public class BankAccountMapper : OutboxMapper&lt;BankAccount, BankAccountData, AccountId&gt;
/// {
///     public override BankAccountData ToData(BankAccount aggregate)
///     {
///         return new BankAccountData(
///             aggregate.Id,
///             aggregate.Version,
///             aggregate.GetDomainEvents(),
///             $"account-{aggregate.Id}",
///             aggregate.Owner,      // State field
///             aggregate.Balance     // State field
///         );
///     }
/// 
///     public override BankAccount ToDomain(BankAccountData data)
///     {
///         return new BankAccount(
///             data.Id,
///             data.Owner,
///             data.Balance,
///             data.Version
///         );
///     }
/// }
/// </code>
/// </example>
public abstract class OutboxMapper<TAggregate, TData, TId>
    where TAggregate : AggregateRoot<TId, IInternalDomainEvent>
    where TData : IOutboxData<TId>
{
    /// <summary>
    ///     Converts an aggregate to outbox data for persistence.
    ///     This method is called by <see cref="OutboxRepository{TAggregate, TData, TId}.SaveAsync" />
    ///     before persisting the aggregate state and events.
    /// </summary>
    /// <param name="aggregate">The aggregate to convert</param>
    /// <returns>The outbox data representation containing both state and events</returns>
    /// <remarks>
    ///     Implementations must copy:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Id: Aggregate identifier</description>
    ///         </item>
    ///         <item>
    ///             <description>Version: For optimistic locking</description>
    ///         </item>
    ///         <item>
    ///             <description>Events: Domain events for Transactional Outbox</description>
    ///         </item>
    ///         <item>
    ///             <description>StreamName: Event stream identifier</description>
    ///         </item>
    ///         <item>
    ///             <description>State fields: All business state (e.g., Owner, Balance)</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public abstract TData ToData(TAggregate aggregate);

    /// <summary>
    ///     Converts outbox data back to an aggregate for domain logic execution.
    ///     This method is called by <see cref="OutboxRepository{TAggregate, TData, TId}.FindByIdAsync" />
    ///     after loading the persisted data.
    /// </summary>
    /// <param name="data">The outbox data to convert</param>
    /// <returns>The reconstructed aggregate with state from data</returns>
    /// <remarks>
    ///     <para>
    ///         Implementations must reconstruct the aggregate with:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>Id: From data.Id</description>
    ///             </item>
    ///             <item>
    ///                 <description>Version: From data.Version</description>
    ///             </item>
    ///             <item>
    ///                 <description>State fields: From data properties (e.g., data.Owner, data.Balance)</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Note: Domain events from data.Events are NOT typically restored to the aggregate,
    ///         as they represent past events already processed. The reconstructed aggregate
    ///         starts with an empty event list for new operations.
    ///     </para>
    /// </remarks>
    public abstract TAggregate ToDomain(TData data);
}