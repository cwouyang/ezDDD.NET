using EzDdd.UseCase.Port.In;

namespace EzDdd.Cqrs.Query;

/// <summary>
///     <c>IProjector</c> is a marker interface for background services that maintain read models.
///     Projectors listen to domain events and update read models in <see cref="IArchive{TData, TId}" />
///     to keep the query side eventually consistent with the write side.
/// </summary>
/// <remarks>
///     <para>
///         <b>Responsibility</b>: Projectors implement the Observer pattern, subscribing to
///         domain events published by the write model and projecting them into denormalized
///         read models optimized for queries.
///     </para>
///     <para>
///         <b>Key Characteristics</b>:
///     </para>
///     <list type="bullet">
///         <item>
///             <b>Background Service</b>: Runs continuously, listening for events.
///         </item>
///         <item>
///             <b>Event Handler</b>: Typically also implements <see cref="IReactor{TInput}" /> to
///             receive domain events from message bus.
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
///             <c>BackgroundService</c> for lifecycle management (Start/Stop).
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
///         Command → Aggregate → Events → Repository → MessageBus → Projector → Archive → Query
///     </code>
///     <para>
///         <b>Extensibility</b>:
///     </para>
///     <list type="bullet">
///         <item>Combine this marker with <see cref="IReactor{TInput}" /> to handle domain events</item>
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
///         See ADR-0020 (IProjector Lifecycle Management) for detailed lifecycle integration patterns.
///     </para>
/// </remarks>
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
///         public class AccountProjector : IProjector, IReactor, BackgroundService
///         {
///             private readonly IArchive&lt;AccountReadModel, AccountId&gt; _archive;
///             private readonly IMessageBus&lt;DomainEventData&gt; _eventBus;
///             private readonly DomainEventMapper _eventMapper;
/// 
///             public AccountProjector(
///                 IArchive&lt;AccountReadModel, AccountId&gt; archive,
///                 IMessageBus&lt;DomainEventData&gt; eventBus,
///                 DomainEventMapper eventMapper)
///             {
///                 _archive = archive;
///                 _eventBus = eventBus;
///                 _eventMapper = eventMapper;
///             }
/// 
///             protected override Task ExecuteAsync(CancellationToken stoppingToken)
///             {
///                 // Subscribe to event bus
///                 _eventBus.Subscribe(this);
///                 return Task.CompletedTask;
///             }
/// 
///             public async Task UpdateAsync(DomainEventData eventData)
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
public interface IProjector
{
    // Pure marker interface - no methods
    // Implementations typically also implement IReactor and IHostedService/BackgroundService
}