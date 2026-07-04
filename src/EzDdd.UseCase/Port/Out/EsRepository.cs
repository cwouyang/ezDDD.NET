using System.Collections.Concurrent;
using System.Reflection;
using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.InOut;

namespace EzDdd.UseCase.Port.Out;

/// <summary>
///     Generic event sourcing repository implementation.
///     Reconstructs aggregates from event streams using reflection and caches constructor information for performance.
/// </summary>
/// <typeparam name="TAggregate">The aggregate type that extends EsAggregateRoot</typeparam>
/// <typeparam name="TId">The type of the aggregate identifier</typeparam>
/// <remarks>
///     <para>
///         EsRepository implements the Repository pattern for event-sourced aggregates. It delegates
///         actual persistence operations to an <see cref="IRepositoryPeer{TData,TId}" /> while handling
///         aggregate reconstruction from event streams.
///     </para>
///     <para>
///         <strong>Event Sourcing Flow:</strong>
///     </para>
///     <list type="number">
///         <item>
///             <description>
///                 <strong>Save:</strong> Extracts events from aggregate using <see cref="EventStoreMapper" />,
///                 persists via peer, then clears aggregate's domain events
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>Load:</strong> Retrieves event stream from peer, reconstructs aggregate by invoking
///                 its event replay constructor via reflection
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>Delete:</strong> Delegates to peer for physical deletion
///             </description>
///         </item>
///     </list>
///     <para>
///         <strong>Performance Optimization:</strong> Constructor reflection information is cached
///         per aggregate type to avoid repeated reflection overhead.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Create repository with event store peer
/// var eventStorePeer = new PostgresEventStorePeer();
/// var repository = new EsRepository&lt;BankAccount, AccountId&gt;(eventStorePeer);
///
/// // Save aggregate (stores events)
/// var account = new BankAccount(accountId, "John Doe", 1000m);
/// account.Deposit(500m);
/// await repository.SaveAsync(account);
///
/// // Load aggregate (reconstructs from events)
/// var loaded = await repository.FindByIdAsync(accountId);
/// // loaded.Balance == 1500m (reconstructed by replaying events)
/// </code>
/// </example>
public class EsRepository<TAggregate, TId> : IRepository<TAggregate, TId, IInternalDomainEvent>
    where TAggregate : EsAggregateRoot<TId, IInternalDomainEvent>
{
    /// <summary>
    ///     Thread-safe cache for constructor information, keyed by aggregate type.
    /// </summary>
    /// <remarks>
    ///     Using <see cref="ConcurrentDictionary{TKey,TValue}" /> ensures thread-safe access
    ///     without explicit locking. Constructor reflection is expensive (~100-1000x slower
    ///     than direct instantiation), so caching provides significant performance benefits
    ///     for repositories that load many aggregates.
    /// </remarks>
    // ReSharper disable once StaticMemberInGenericType
    private static readonly ConcurrentDictionary<Type, ConstructorInfo> ConstructorCache = new();

    private readonly IRepositoryPeer<EventStoreData<TId>, TId> _peer;

    /// <summary>
    ///     Initializes a new instance of the EsRepository class.
    /// </summary>
    /// <param name="peer">The repository peer that handles actual event store persistence</param>
    /// <exception cref="ArgumentNullException">Thrown when peer is null</exception>
    /// <remarks>
    ///     <para>
    ///         The peer is responsible for the actual persistence operations. EsRepository
    ///         delegates to the peer while handling the event sourcing-specific logic of
    ///         aggregate reconstruction and event extraction.
    ///     </para>
    /// </remarks>
    public EsRepository(IRepositoryPeer<EventStoreData<TId>, TId> peer)
    {
        _peer = peer ?? throw new ArgumentNullException(nameof(peer));
    }

    /// <summary>
    ///     Finds an aggregate by its identifier and reconstructs it from its event stream.
    /// </summary>
    /// <param name="id">The aggregate identifier</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains
    ///     the reconstructed aggregate, or null if no aggregate with the specified ID exists.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when id is null</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the aggregate type does not have a public constructor accepting
    ///     IEnumerable&lt;IInternalDomainEvent&gt;, or when event replay fails
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         This method reconstructs the aggregate through event replay:
    ///     </para>
    ///     <list type="number">
    ///         <item>
    ///             <description>Retrieves event stream from peer</description>
    ///         </item>
    ///         <item>
    ///             <description>Finds or caches the aggregate's event replay constructor</description>
    ///         </item>
    ///         <item>
    ///             <description>Invokes constructor with events via reflection</description>
    ///         </item>
    ///         <item>
    ///             <description>Returns reconstructed aggregate with cleared domain events</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         <strong>Performance Note:</strong> Constructor information is cached after first use,
    ///         making subsequent loads much faster (reflection overhead paid only once per type).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var repository = new EsRepository&lt;Order, OrderId&gt;(eventStorePeer);
    /// var order = await repository.FindByIdAsync(orderId);
    ///
    /// if (order != null)
    /// {
    ///     // Order reconstructed from its event stream
    ///     // All business rules validated during reconstruction
    ///     Console.WriteLine($"Order total: {order.Total}");
    /// }
    /// </code>
    /// </example>
    public async Task<TAggregate?> FindByIdAsync(TId id)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        EventStoreData<TId>? data = await _peer.FindByIdAsync(id).ConfigureAwait(false);
        if (data == null)
        {
            return null;
        }

        // Cast events to IInternalDomainEvent (event store only contains internal events)
        List<IInternalDomainEvent> internalEvents = data.Events.Cast<IInternalDomainEvent>().ToList();
        return _ReconstructAggregate(internalEvents);
    }

    /// <summary>
    ///     Saves an aggregate by persisting its pending domain events to the event store.
    /// </summary>
    /// <param name="aggregate">The aggregate to save</param>
    /// <returns>A task that represents the asynchronous save operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when aggregate is null</exception>
    /// <exception cref="RepositorySaveException">
    ///     Thrown when the peer fails to save (wraps <see cref="RepositoryPeerSaveException" />)
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         Save operation workflow:
    ///     </para>
    ///     <list type="number">
    ///         <item>
    ///             <description>Converts aggregate to EventStoreData using <see cref="EventStoreMapper" /></description>
    ///         </item>
    ///         <item>
    ///             <description>Delegates to peer for actual persistence</description>
    ///         </item>
    ///         <item>
    ///             <description>Updates aggregate version after successful save</description>
    ///         </item>
    ///         <item>
    ///             <description>Clears aggregate's domain events (prevents re-publication)</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         <strong>Exception Handling:</strong> Peer-level exceptions (<see cref="RepositoryPeerSaveException" />)
    ///         are caught and wrapped in <see cref="RepositorySaveException" /> to maintain proper layering.
    ///         Domain events are only cleared after successful persistence.
    ///     </para>
    ///     <para>
    ///         <strong>Event Publishing:</strong>
    ///         This repository does NOT publish events directly. To publish events to a message broker,
    ///         use the Relay pattern (see EventStoreRelay example in documentation).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var account = new BankAccount(accountId, "Jane Doe", 500m);
    /// account.Deposit(200m);  // Generates MoneyDeposited event
    /// account.Withdraw(100m); // Generates MoneyWithdrawn event
    ///
    /// try
    /// {
    ///     await repository.SaveAsync(account);
    ///     // Success: Both events persisted, account.GetDomainEvents() now empty
    /// }
    /// catch (RepositorySaveException ex)
    /// {
    ///     // Failure: Events still in account.GetDomainEvents(), can retry
    ///     logger.LogError(ex, "Failed to save account");
    /// }
    /// </code>
    /// </example>
    public async Task SaveAsync(TAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        EventStoreData<TId> data = EventStoreMapper.ToData(aggregate);

        try
        {
            await _peer.SaveAsync(data).ConfigureAwait(false);
        }
        catch (RepositoryPeerSaveException ex)
        {
            throw new RepositorySaveException($"Failed to save aggregate of type {typeof(TAggregate).Name}", ex);
        }

        aggregate.ClearDomainEvents();
    }

    /// <summary>
    ///     Deletes an aggregate by removing its event stream from the event store.
    /// </summary>
    /// <param name="aggregate">The aggregate to delete</param>
    /// <returns>A task that represents the asynchronous delete operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when aggregate is null</exception>
    /// <remarks>
    ///     <para>
    ///         Delegates to the peer for actual deletion. Note that in event sourcing, deletion
    ///         is often handled by adding a destruction event rather than physical deletion.
    ///         This method supports both approaches depending on peer implementation.
    ///     </para>
    ///     <para>
    ///         <strong>Best Practice:</strong> For audit trail purposes, consider using a
    ///         destruction event (implementing <see cref="IInternalDomainEvent.IDestructionEvent" />)
    ///         instead of physical deletion. Physical deletion should only be used when required
    ///         by regulations (e.g., GDPR "right to be forgotten").
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var account = await repository.FindByIdAsync(accountId);
    /// if (account != null)
    /// {
    ///     await repository.DeleteAsync(account);
    ///     // Event stream removed from event store
    /// }
    /// </code>
    /// </example>
    public async Task DeleteAsync(TAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        EventStoreData<TId> data = EventStoreMapper.ToData(aggregate);
        await _peer.DeleteAsync(data).ConfigureAwait(false);
    }

    /// <summary>
    ///     Reconstructs an aggregate from its event stream using reflection.
    /// </summary>
    /// <param name="events">The event stream to replay</param>
    /// <returns>The reconstructed aggregate</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the aggregate type lacks a public constructor accepting
    ///     IEnumerable&lt;IInternalDomainEvent&gt;, or when constructor invocation fails
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         This method uses reflection to invoke the aggregate's event replay constructor.
    ///         The constructor signature must be:
    ///     </para>
    ///     <code>
    /// public TAggregate(IEnumerable&lt;IInternalDomainEvent&gt; events)
    /// </code>
    ///     <para>
    ///         <strong>Performance Optimization:</strong> Constructor information is cached in a
    ///         static <see cref="ConcurrentDictionary{TKey,TValue}" /> keyed by aggregate type.
    ///         First invocation per type performs reflection lookup (~100-1000x slower than direct call),
    ///         but subsequent invocations use the cached constructor (~10-50x slower than direct call).
    ///     </para>
    ///     <para>
    ///         <strong>Thread Safety:</strong> The cache uses <see cref="ConcurrentDictionary{TKey,TValue}" />
    ///         which is thread-safe without explicit locking. The GetOrAdd method ensures only one
    ///         thread performs the expensive reflection lookup per type.
    ///     </para>
    /// </remarks>
    private static TAggregate _ReconstructAggregate(IReadOnlyList<IInternalDomainEvent> events)
    {
        Type aggregateType = typeof(TAggregate);

        // Get or cache the constructor
        ConstructorInfo constructor = ConstructorCache.GetOrAdd(
            aggregateType,
            type =>
            {
                ConstructorInfo? ctor = type.GetConstructor(
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    [typeof(IEnumerable<IInternalDomainEvent>)],
                    null
                );

                if (ctor == null)
                {
                    throw new InvalidOperationException(
                        $"{type.Name} must have a public constructor accepting IEnumerable<IInternalDomainEvent>. "
                            + $"This constructor is required for event sourcing to reconstruct aggregates from event streams."
                    );
                }

                return ctor;
            }
        );

        try
        {
            return (TAggregate)constructor.Invoke([events]);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to reconstruct {aggregateType.Name} from event stream. "
                    + $"This may indicate an issue with event replay or invariant validation during reconstruction.",
                ex
            );
        }
    }
}
