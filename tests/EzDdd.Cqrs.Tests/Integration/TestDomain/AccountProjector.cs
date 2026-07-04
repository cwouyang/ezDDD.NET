using EzDdd.Cqrs.Query;
using EzDdd.Entity;
using EzDdd.UseCase.Port.InOut;
using EzDdd.UseCase.Tests.Integration.TestDomain;

namespace EzDdd.Cqrs.Tests.Integration.TestDomain;

/// <summary>
///     Projector that maintains the <see cref="AccountSummaryReadModel" /> by listening
///     to domain events from the write side (BankAccount aggregate).
/// </summary>
/// <remarks>
///     <para>
///         This projector processes domain events and updates the read model in the Archive
///         to keep the query side eventually consistent with the write side.
///     </para>
///     <para>
///         In production scenarios, this projector would typically also implement
///         <c>BackgroundService</c> or <c>IHostedService</c> for lifecycle management,
///         subscribing to events from a message broker (e.g., Kafka, RabbitMQ).
///     </para>
/// </remarks>
public sealed class AccountProjector : IProjector<DomainEventData>
{
    private readonly IArchive<AccountSummaryReadModel, AccountId> _archive;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AccountProjector" /> class.
    /// </summary>
    /// <param name="archive">The archive for storing account read models.</param>
    public AccountProjector(IArchive<AccountSummaryReadModel, AccountId> archive)
    {
        _archive = archive ?? throw new ArgumentNullException(nameof(archive));
    }

    /// <summary>
    ///     Executes the projector logic to update the read model based on the received domain event.
    ///     This method is called by the event relay infrastructure when events are published.
    /// </summary>
    /// <param name="eventData">The domain event data to process.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    ///     <para>
    ///         <b>Error Handling</b>: This implementation uses a try-catch block to prevent
    ///         individual event processing failures from stopping the entire projector.
    ///     </para>
    ///     <para>
    ///         In production scenarios, failed events should be logged and potentially moved
    ///         to a dead-letter queue for manual inspection. This test implementation rethrows
    ///         exceptions to make test failures visible.
    ///     </para>
    /// </remarks>
    public async Task ExecuteAsync(DomainEventData eventData)
    {
        try
        {
            IInternalDomainEvent domainEvent = _DeserializeDomainEvent(eventData);

            switch (domainEvent)
            {
                case AccountCreated e:
                    await _HandleAccountCreatedAsync(e);
                    break;

                case MoneyDeposited e:
                    await _HandleMoneyDepositedAsync(e);
                    break;

                case MoneyWithdrawn e:
                    await _HandleMoneyWithdrawnAsync(e);
                    break;

                case AccountClosed e:
                    await _HandleAccountClosedAsync(e);
                    break;
            }
        }
        catch (Exception ex)
        {
            // In test scenarios: rethrow to make failures visible in test results
            // In production: log error and continue processing (don't crash projector)
            await Console.Error.WriteLineAsync($"Error processing event {eventData.Id} (type: {eventData.EventType}): {ex.Message}");
            throw; // Rethrow for test observability
        }
    }

    /// <summary>
    ///     Handles AccountCreated event by creating a new read model.
    /// </summary>
    private async Task _HandleAccountCreatedAsync(AccountCreated @event)
    {
        AccountSummaryReadModel readModel = new
        (
            @event.AccountId,
            @event.Owner,
            @event.InitialBalance.Amount,
            @event.OccurredOn,
            @event.OccurredOn,
            0
        );

        await _archive.SaveAsync(readModel);
    }

    /// <summary>
    ///     Handles MoneyDeposited event by updating balance and transaction info.
    /// </summary>
    private async Task _HandleMoneyDepositedAsync(MoneyDeposited @event)
    {
        AccountSummaryReadModel? existing = await _archive.FindByIdAsync(@event.AccountId);
        if (existing == null)
        {
            return;
        }

        AccountSummaryReadModel updated = existing with
        {
            Balance = existing.Balance + @event.Amount.Amount,
            LastTransactionDate = @event.OccurredOn,
            TransactionCount = existing.TransactionCount + 1
        };

        await _archive.SaveAsync(updated);
    }

    /// <summary>
    ///     Handles MoneyWithdrawn event by updating balance and transaction info.
    /// </summary>
    private async Task _HandleMoneyWithdrawnAsync(MoneyWithdrawn @event)
    {
        AccountSummaryReadModel? existing = await _archive.FindByIdAsync(@event.AccountId);
        if (existing == null)
        {
            return;
        }

        AccountSummaryReadModel updated = existing with
        {
            Balance = existing.Balance - @event.Amount.Amount,
            LastTransactionDate = @event.OccurredOn,
            TransactionCount = existing.TransactionCount + 1
        };

        await _archive.SaveAsync(updated);
    }

    /// <summary>
    ///     Handles AccountClosed event by removing the read model from the archive.
    /// </summary>
    private async Task _HandleAccountClosedAsync(AccountClosed @event)
    {
        AccountSummaryReadModel? existing = await _archive.FindByIdAsync(@event.AccountId);
        if (existing == null)
        {
            return;
        }

        await _archive.DeleteAsync(existing);
    }

    /// <summary>
    ///     Deserializes domain event data to the appropriate event type.
    /// </summary>
    private static IInternalDomainEvent _DeserializeDomainEvent(DomainEventData eventData)
    {
        return eventData.EventType switch
        {
            "AccountCreated" => DomainEventMapper.ToDomain<AccountCreated>(eventData),
            "MoneyDeposited" => DomainEventMapper.ToDomain<MoneyDeposited>(eventData),
            "MoneyWithdrawn" => DomainEventMapper.ToDomain<MoneyWithdrawn>(eventData),
            "AccountClosed" => DomainEventMapper.ToDomain<AccountClosed>(eventData),
            _ => throw new InvalidOperationException($"Unknown event type: {eventData.EventType}")
        };
    }
}