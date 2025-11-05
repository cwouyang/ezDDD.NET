using EzDdd.Entity;

namespace EzDdd.UseCase.Port.Out;

/// <summary>
///     Static utility for converting between event-sourced aggregates and <see cref="EventStoreData{TId}"/>.
/// </summary>
/// <remarks>
///     <para>
///         EventStoreMapper provides one-way conversion from EsAggregateRoot to EventStoreData
///         for persistence. The reverse conversion (ToDomain) is intentionally not supported
///         because event sourcing aggregates are reconstructed through event replay using
///         their event replay constructor, not through a mapper.
///     </para>
///     <para>
///         <strong>Why One-Way Mapping?</strong>
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <strong>ToData:</strong> Used during save operations to extract events from
///                 an aggregate for persistence to the event store.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>ToDomain:</strong> NOT USED in event sourcing. Aggregates are reconstructed
///                 by passing event streams to their constructor (e.g., <c>new BankAccount(events)</c>).
///                 This ensures proper invariant checking during reconstruction via the template method
///                 pattern in <see cref="EsAggregateRoot{TId,TEvent}.Apply" />.
///             </description>
///         </item>
///     </list>
///     <para>
///         This design enforces the event sourcing principle that aggregates are always
///         reconstructed through event replay, never through direct state hydration.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Saving an aggregate (uses ToData)
/// var aggregate = new BankAccount(accountId, "John Doe", 1000m);
/// aggregate.Deposit(500m);
/// 
/// var data = EventStoreMapper.ToData(aggregate);
/// await eventStorePeer.SaveAsync(data);
/// 
/// // Loading an aggregate (does NOT use ToDomain - uses constructor instead)
/// var storedData = await eventStorePeer.FindByIdAsync(accountId);
/// var reconstructed = new BankAccount(storedData.Events); // Event replay constructor
/// </code>
/// </example>
public static class EventStoreMapper
{
    /// <summary>
    ///     Converts an event-sourced aggregate to <see cref="EventStoreData{TId}"/> for persistence.
    /// </summary>
    /// <typeparam name="TId">The type of the aggregate identifier</typeparam>
    /// <param name="aggregate">The aggregate to convert</param>
    /// <returns>EventStoreData containing the aggregate's events and metadata</returns>
    /// <remarks>
    ///     <para>
    ///         Creates a defensive copy of the aggregate's domain events to prevent
    ///         external modifications after the data is returned. The EventStoreData
    ///         includes:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description><strong>Id:</strong> The aggregate identifier</description>
    ///         </item>
    ///         <item>
    ///             <description><strong>Version:</strong> Current version for optimistic locking</description>
    ///         </item>
    ///         <item>
    ///             <description><strong>Events:</strong> Defensive copy of domain events</description>
    ///         </item>
    ///         <item>
    ///             <description><strong>StreamName:</strong> Event stream name (format: "{category}-{id}")</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         <strong>Defensive Copy:</strong> The events list is copied using <c>ToList()</c> to ensure
    ///         that subsequent modifications to the aggregate's event collection do not affect
    ///         the persisted data.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var account = new BankAccount(accountId, "Jane Doe", 500m);
    /// account.Deposit(200m);
    /// account.Withdraw(100m);
    /// 
    /// var data = EventStoreMapper.ToData(account);
    /// // data.Events contains: [AccountCreated, MoneyDeposited, MoneyWithdrawn]
    /// // data.StreamName is "account-{accountId}"
    /// // data.Version is current aggregate version
    /// </code>
    /// </example>
    public static EventStoreData<TId> ToData<TId>(EsAggregateRoot<TId, IInternalDomainEvent> aggregate)
    {
        return new EventStoreData<TId>
        {
            Id = aggregate.Id,
            Version = aggregate.Version,
            Events = aggregate.GetDomainEvents().ToList(), // Defensive copy
            StreamName = aggregate.GetStreamName()
        };
    }

    /// <summary>
    ///     Not supported for event sourcing. Use aggregate's event replay constructor instead.
    /// </summary>
    /// <typeparam name="T">The aggregate type</typeparam>
    /// <typeparam name="TId">The identifier type</typeparam>
    /// <param name="data">The event store data (parameter exists for type consistency only)</param>
    /// <returns>This method never returns; always throws NotSupportedException</returns>
    /// <exception cref="NotSupportedException">
    ///     Always thrown. Event sourcing aggregates must be reconstructed through event replay
    ///     using their constructor, not through a mapper.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         This method intentionally throws <see cref="NotSupportedException" /> to enforce
    ///         the event sourcing principle that aggregates are reconstructed through event replay.
    ///     </para>
    ///     <para>
    ///         <strong>Correct Approach:</strong>
    ///     </para>
    ///     <code>
    /// // ❌ WRONG: Attempting to use mapper (will throw)
    /// var aggregate = EventStoreMapper.ToDomain&lt;BankAccount, AccountId&gt;(data);
    /// 
    /// // ✅ CORRECT: Use event replay constructor
    /// var aggregate = new BankAccount(data.Events);
    /// </code>
    ///     <para>
    ///         <strong>Why This Design?</strong>
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Enforces proper invariant checking during reconstruction through the
    ///                 <see cref="EsAggregateRoot{TId,TEvent}.Apply" /> template method
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Ensures R1/R2/R3 event sourcing correctness rules are applied during replay
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Makes the code's intent explicit: event sourcing uses event replay, not state hydration
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static T ToDomain<T, TId>(EventStoreData<TId> data)
        where T : EsAggregateRoot<TId, IInternalDomainEvent>
    {
        throw new NotSupportedException
        (
            "Event sourcing aggregates are reconstructed from events, not from EventStoreData. " +
            "Use the aggregate's event replay constructor instead: new T(data.Events)"
        );
    }
}