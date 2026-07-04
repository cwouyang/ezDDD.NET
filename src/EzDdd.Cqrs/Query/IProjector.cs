using EzDdd.UseCase.Port.In;

namespace EzDdd.Cqrs.Query;

/// <summary>
///     <c>IProjector</c> is a kind of <see cref="IReactor{TInput}" /> that represents a service
///     in the use cases layer that writes read models in a query database.
///     Projectors listen to domain events and update read models in <see cref="IArchive{TData, TId}" />
///     to keep the query side eventually consistent with the write side.
/// </summary>
/// <remarks>
///     <para>
///         <b>Responsibility</b>: Projectors receive domain events published by the write model
///         and project them into denormalized read models optimized for queries. The event
///         handling contract is inherited from <see cref="IReactor{TInput}" />:
///         <c>ExecuteAsync(TInput)</c>.
///     </para>
///     <para>
///         <b>Key Characteristics</b>:
///     </para>
///     <list type="bullet">
///         <item>
///             <b>Reactor</b>: Handles each received event via
///             <see cref="IReactor{TInput}.ExecuteAsync(TInput)" />.
///         </item>
///         <item>
///             <b>Read Model Updater</b>: Updates <see cref="IArchive{TData, TId}" /> when
///             relevant events occur.
///         </item>
///         <item>
///             <b>Eventual Consistency</b>: Read models may lag slightly behind write models.
///         </item>
///     </list>
///     <para>
///         <b>Implementation Notes</b>:
///     </para>
///     <list type="bullet">
///         <item>
///             In .NET, projectors typically also implement <c>IHostedService</c> or
///             <c>BackgroundService</c> for lifecycle management (Start/Stop). The lifecycle
///             remains an infrastructure concern outside this interface.
///         </item>
///         <item>
///             Projectors should handle events idempotently (same event processed multiple
///             times produces same result) for reliability.
///         </item>
///         <item>
///             Consider using event store position tracking to avoid reprocessing events.
///         </item>
///     </list>
///     <para>
///         <b>CQRS Flow</b>:
///     </para>
///     <code>
///         Command → Aggregate → Events → Repository → Relay → Projector → Archive → Query
///     </code>
///     <para>
///         <b>Extensibility</b>:
///     </para>
///     <list type="bullet">
///         <item>Implement <c>ExecuteAsync</c> to process domain events and update read models</item>
///         <item>Combine with .NET <c>BackgroundService</c> or <c>IHostedService</c> for lifecycle management (Start/Stop)</item>
///         <item>Multiple projectors can run concurrently, each maintaining different read models</item>
///         <item>Supports custom event filtering logic in implementations (process only relevant events)</item>
///         <item>Can use event store position tracking for resume capability after restarts</item>
///         <item>
///             Compatible with various persistence strategies (SQL, NoSQL, in-memory) via
///             <see cref="IArchive{TData,TId}" />
///         </item>
///     </list>
///     <para>
///         See ADR-0028 (Reactor Type Hierarchy) for the generic contract, and ADR-0020
///         (IProjector Lifecycle Management, superseded by ADR-0028) for the original
///         lifecycle integration patterns that remain applicable.
///     </para>
/// </remarks>
/// <typeparam name="TInput">The type of input message (typically domain event data) this projector processes.</typeparam>
/// <example>
///     <code>
///         // Read model
///         public record AccountReadModel(
///             AccountId AccountId,
///             string AccountNumber,
///             decimal Balance,
///             DateTimeOffset CreatedOn
///         );
///
///         // Projector implementation
///         public class AccountProjector : IProjector&lt;DomainEventData&gt;
///         {
///             private readonly IArchive&lt;AccountReadModel, AccountId&gt; _archive;
///             private readonly DomainEventMapper _eventMapper;
///
///             public AccountProjector(
///                 IArchive&lt;AccountReadModel, AccountId&gt; archive,
///                 DomainEventMapper eventMapper)
///             {
///                 _archive = archive;
///                 _eventMapper = eventMapper;
///             }
///
///             public async Task ExecuteAsync(DomainEventData eventData)
///             {
///                 var domainEvent = _eventMapper.ToDomainEvent(eventData);
///
///                 switch (domainEvent)
///                 {
///                     case AccountCreated e:
///                         var readModel = new AccountReadModel(
///                             e.AccountId,
///                             e.AccountNumber,
///                             e.InitialBalance,
///                             e.OccurredOn
///                         );
///                         await _archive.SaveAsync(readModel);
///                         break;
///
///                     case MoneyDeposited e:
///                         var account = await _archive.FindByIdAsync(e.AccountId);
///                         if (account != null)
///                         {
///                             var updated = account with { Balance = account.Balance + e.Amount };
///                             await _archive.SaveAsync(updated);
///                         }
///                         break;
///
///                     case AccountClosed e:
///                         var toDelete = await _archive.FindByIdAsync(e.AccountId);
///                         if (toDelete != null)
///                         {
///                             await _archive.DeleteAsync(toDelete);
///                         }
///                         break;
///                 }
///             }
///         }
///     </code>
/// </example>
public interface IProjector<in TInput> : IReactor<TInput>
{
    // Inherits Task ExecuteAsync(TInput input) from IReactor<TInput>
}
