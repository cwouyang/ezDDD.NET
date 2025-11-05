namespace EzDdd.UseCase.Port.Out;

/// <summary>
///     Marker interface for Transactional Outbox data structures.
///     Implementations must store both aggregate state and pending domain events
///     to support the Transactional Outbox pattern for atomic persistence.
/// </summary>
/// <typeparam name="TId">The type of the aggregate identifier</typeparam>
/// <remarks>
///     <para>
///         The Transactional Outbox pattern ensures atomic persistence of both:
///         1. Current aggregate state (columns in database table)
///         2. Pending domain events (for outbox/event bus publishing)
///     </para>
///     <para>
///         This marker interface extends <see cref="IStoreData{TId}" /> and is used
///         as a type constraint for <see cref="OutboxMapper{TAggregate, TData, TId}" />
///         and <see cref="OutboxRepository{TAggregate, TData, TId}" />.
///     </para>
///     <para>
///         Typical transaction boundary in IRepositoryPeer implementation:
///         <code>
/// BEGIN TRANSACTION
///   UPDATE aggregate_table SET state = ... WHERE id = ...
///   INSERT INTO outbox_table (event_id, event_type, event_body) VALUES (...)
/// COMMIT TRANSACTION
/// </code>
///     </para>
/// </remarks>
/// <example>
///     <code>
/// public class BankAccountData : IOutboxData&lt;AccountId&gt;
/// {
///     // IStoreData members
///     public AccountId Id { get; set; }
///     public long Version { get; set; }
///     public List&lt;IDomainEvent&gt; Events { get; set; }
///     public string StreamName { get; set; }
/// 
///     // State fields (specific to BankAccount aggregate)
///     public string Owner { get; set; }
///     public decimal Balance { get; set; }
/// }
/// </code>
/// </example>
public interface IOutboxData<TId> : IStoreData<TId>
{
    // Marker interface - inherits all members from IStoreData<TId>:
    // - TId Id { get; set; }
    // - long Version { get; set; }
    // - List<IDomainEvent> Events { get; set; }
    // - string StreamName { get; set; }
    // - long GetOptimisticLockVersion()
}