using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.InOut;

namespace EzDdd.UseCase.Port.Out;

/// <summary>
///     Generic state sourcing repository implementation using the Transactional Outbox pattern.
///     Persists both aggregate state and domain events atomically.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type</typeparam>
/// <typeparam name="TData">The outbox data type for persistence</typeparam>
/// <typeparam name="TId">The type of the aggregate identifier</typeparam>
/// <remarks>
///     <para>
///         OutboxRepository implements the <see cref="IRepository{TAggregate, TId, TEvent}" /> interface
///         and uses the Bridge pattern to delegate actual persistence to <see cref="IRepositoryPeer{TData, TId}" />.
///         This decouples the domain layer from infrastructure concerns.
///     </para>
///     <para>
///         <strong>Transactional Outbox Pattern:</strong>
///         This repository ensures atomic persistence of both aggregate state and domain events
///         in a single database transaction. The <see cref="IRepositoryPeer{TData, TId}" /> implementation
///         is responsible for managing the transaction boundary.
///     </para>
///     <para>
///         <strong>Key Operations:</strong>
///         <list type="bullet">
///             <item>
///                 <description>
///                     <see cref="FindByIdAsync" />: Loads data from peer, converts to aggregate using mapper, filters soft-deleted aggregates
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <see cref="SaveAsync" />: Converts aggregate to data, persists via peer, clears domain events
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <see cref="DeleteAsync" />: Converts aggregate to data, removes via peer
///                 </description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <strong>Exception Translation:</strong>
///         Translates infrastructure exceptions (<see cref="RepositoryPeerSaveException" />)
///         to domain exceptions (<see cref="RepositorySaveException" />) to maintain layer boundaries.
///     </para>
///     <para>
///         <strong>Event Clearing:</strong>
///         After a successful save, domain events are cleared from the aggregate via
///         <see cref="AggregateRoot{TId,TEvent}.ClearDomainEvents" />. This prevents
///         duplicate event publishing. Events are NOT cleared if the save fails.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Setup
/// var peer = new SqlRepositoryPeer();
/// var mapper = new BankAccountMapper();
/// var repository = new OutboxRepository&lt;BankAccount, BankAccountData, AccountId&gt;(peer, mapper);
/// 
/// // Load aggregate
/// var account = await repository.FindByIdAsync(accountId);
/// if (account != null)
/// {
///     // Modify aggregate
///     account.Deposit(100.00m);
/// 
///     // Save (atomically persists state + events)
///     await repository.SaveAsync(account);
/// }
/// </code>
/// </example>
public class OutboxRepository<TAggregate, TData, TId> : IRepository<TAggregate, TId, IInternalDomainEvent>
    where TAggregate : AggregateRoot<TId, IInternalDomainEvent>
    where TData : IOutboxData<TId>
{
    private readonly OutboxMapper<TAggregate, TData, TId> _mapper;
    private readonly IRepositoryPeer<TData, TId> _peer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxRepository{TAggregate, TData, TId}" /> class.
    /// </summary>
    /// <param name="peer">The repository peer for actual persistence operations</param>
    /// <param name="mapper">The mapper for converting between aggregates and outbox data</param>
    /// <remarks>
    ///     <para>
    ///         The peer and mapper are injected via constructor to support dependency injection
    ///         and enable easy testing with mock implementations.
    ///     </para>
    /// </remarks>
    public OutboxRepository
    (
        IRepositoryPeer<TData, TId> peer,
        OutboxMapper<TAggregate, TData, TId> mapper
    )
    {
        ArgumentNullException.ThrowIfNull(peer);
        ArgumentNullException.ThrowIfNull(mapper);

        _peer = peer;
        _mapper = mapper;
    }

    /// <summary>
    ///     Finds an aggregate by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the aggregate</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     the aggregate if found and not soft-deleted; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         <strong>Soft Delete Filtering:</strong>
    ///         If the reconstructed aggregate is marked as deleted
    ///         (<see cref="AggregateRoot{TId, TEvent}.IsDeleted" /> is <c>true</c>),
    ///         this method returns <c>null</c>. With the Transactional Outbox pattern,
    ///         soft-deleted aggregates remain in storage so their domain events can still
    ///         be relayed, but they are logically deleted and must not be observable
    ///         through the repository.
    ///     </para>
    ///     <para>
    ///         This method loads the persisted data from the peer and uses the mapper
    ///         to reconstruct the aggregate. The reconstructed aggregate has:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>State from the persisted data</description>
    ///             </item>
    ///             <item>
    ///                 <description>Version from the persisted data</description>
    ///             </item>
    ///             <item>
    ///                 <description>Empty domain events list (events are not restored)</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public async Task<TAggregate?> FindByIdAsync(TId id)
    {
        TData? data = await _peer.FindByIdAsync(id);
        if (data == null)
        {
            return null;
        }

        TAggregate aggregate = _mapper.ToDomain(data);
        if (aggregate.IsDeleted)
        {
            return null;
        }

        return aggregate;
    }

    /// <summary>
    ///     Saves an aggregate to the repository.
    /// </summary>
    /// <param name="aggregate">The aggregate to save</param>
    /// <returns>A task that represents the asynchronous save operation</returns>
    /// <exception cref="RepositorySaveException">
    ///     Thrown when the save operation fails. The inner exception contains
    ///     the original <see cref="RepositoryPeerSaveException" />.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         <strong>Save Process:</strong>
    ///         <list type="number">
    ///             <item>
    ///                 <description>Convert aggregate to outbox data using mapper</description>
    ///             </item>
    ///             <item>
    ///                 <description>Persist data via peer (includes transaction management)</description>
    ///             </item>
    ///             <item>
    ///                 <description>Clear domain events from aggregate on success</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <strong>Transaction Boundary:</strong>
    ///         The peer implementation is responsible for managing the transaction that ensures
    ///         atomic persistence of both aggregate state and domain events. The repository layer
    ///         does NOT manage transactions.
    ///     </para>
    ///     <para>
    ///         <strong>Error Handling:</strong>
    ///         If the peer throws <see cref="RepositoryPeerSaveException" />, it is caught and
    ///         wrapped in <see cref="RepositorySaveException" /> to maintain layer boundaries.
    ///         Domain events are NOT cleared if the save fails.
    ///     </para>
    ///     <para>
    ///         <strong>Event Publishing:</strong>
    ///         This repository does NOT publish events directly. To publish events to a message broker,
    ///         use the Relay pattern (see EventStoreRelay example in documentation).
    ///     </para>
    /// </remarks>
    public async Task SaveAsync(TAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        TData data = _mapper.ToData(aggregate);

        try
        {
            await _peer.SaveAsync(data);
        }
        catch (RepositoryPeerSaveException e)
        {
            throw new RepositorySaveException("Failed to save aggregate", e);
        }

        aggregate.ClearDomainEvents();
    }

    /// <summary>
    ///     Deletes an aggregate from the repository.
    /// </summary>
    /// <param name="aggregate">The aggregate to delete</param>
    /// <returns>A task that represents the asynchronous delete operation</returns>
    /// <remarks>
    ///     <para>
    ///         This method converts the aggregate to outbox data and delegates the deletion
    ///         to the peer. Note that this performs a physical deletion (removes the data
    ///         from storage).
    ///     </para>
    ///     <para>
    ///         For soft delete scenarios (logical deletion), consider adding an <c>IsDeleted</c>
    ///         flag to the aggregate and using <see cref="SaveAsync" /> instead.
    ///     </para>
    /// </remarks>
    public async Task DeleteAsync(TAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        TData data = _mapper.ToData(aggregate);
        await _peer.DeleteAsync(data);
    }
}