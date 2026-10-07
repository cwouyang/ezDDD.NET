using EzDdd.Cqrs.Entity.Query;
using EzDdd.Cqrs.Query;
using EzDdd.Entity;
using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.InOut;
using EzDdd.UseCase.Tests.Integration.TestDomain;

namespace EzDdd.Cqrs.Tests.Integration.TestDomain;

/// <summary>
///     Reactor that maintains the <see cref="AccountSummaryReadModel" /> by listening
///     to domain events from the write side (BankAccount aggregate).
/// </summary>
/// <remarks>
///     <para>
///         It loads the read model from the archive, applies the pure <see cref="AccountSummaryProjector" />,
///         records the event id in the read model's deduplication record, and saves once. Wrap it in an
///         <see cref="IdempotentDecorator{TInput,TOutput}" /> to ignore redelivered events.
///     </para>
///     <para>
///         In production scenarios, this reactor would typically also be hosted by a
///         <c>BackgroundService</c> or <c>IHostedService</c>, subscribing to events from a message broker.
///     </para>
/// </remarks>
public sealed class AccountProjector : IReactor<DomainEventDataInput>
{
    private readonly IArchive<AccountSummaryReadModel, AccountId> _archive;
    private readonly AccountSummaryProjector _projector = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="AccountProjector" /> class.
    /// </summary>
    /// <param name="archive">The archive for storing account read models.</param>
    public AccountProjector(IArchive<AccountSummaryReadModel, AccountId> archive)
    {
        _archive = archive ?? throw new ArgumentNullException(nameof(archive));
    }

    /// <summary>
    ///     Projects the received domain event into the read model.
    /// </summary>
    /// <param name="input">The input carrying the domain event data to process.</param>
    /// <returns>A task yielding a successful <see cref="DefaultOutput" />.</returns>
    /// <remarks>
    ///     Failures are rethrown so the caller can redeliver the event; a failed save records nothing.
    /// </remarks>
    public async Task<DefaultOutput> ExecuteAsync(DomainEventDataInput input)
    {
        DomainEventData eventData = input.Event;
        try
        {
            IInternalDomainEvent domainEvent = _DeserializeDomainEvent(eventData);
            if (domainEvent is AccountClosed closed)
            {
                await _DeleteAsync(closed.AccountId);
            }
            else
            {
                await _ProjectAsync(domainEvent);
            }

            return DefaultOutput.Create().Succeed();
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(
                $"Error processing event {eventData.Id} (type: {eventData.EventType}): {ex.Message}"
            );
            throw;
        }
    }

    private async Task _ProjectAsync(IInternalDomainEvent domainEvent)
    {
        AccountId accountId = new(domainEvent.Source);
        AccountSummaryReadModel? current = await _archive.FindByIdAsync(accountId);

        AccountSummaryReadModel? projected = _projector.Project(new AccountProjectionInput(current, domainEvent));
        if (projected is null)
        {
            return;
        }

        projected.UpdateEventDeduplicationRecord(domainEvent.Id);
        await _archive.SaveAsync(projected);
    }

    private async Task _DeleteAsync(AccountId accountId)
    {
        AccountSummaryReadModel? existing = await _archive.FindByIdAsync(accountId);
        if (existing is not null)
        {
            await _archive.DeleteAsync(existing);
        }
    }

    private static IInternalDomainEvent _DeserializeDomainEvent(DomainEventData eventData)
    {
        return eventData.EventType switch
        {
            "AccountCreated" => DomainEventMapper.ToDomain<AccountCreated>(eventData),
            "MoneyDeposited" => DomainEventMapper.ToDomain<MoneyDeposited>(eventData),
            "MoneyWithdrawn" => DomainEventMapper.ToDomain<MoneyWithdrawn>(eventData),
            "AccountClosed" => DomainEventMapper.ToDomain<AccountClosed>(eventData),
            _ => throw new InvalidOperationException($"Unknown event type: {eventData.EventType}"),
        };
    }
}
