using EzDdd.Cqrs.Entity.Query;
using EzDdd.Entity;
using EzDdd.UseCase.Tests.Integration.TestDomain;

namespace EzDdd.Cqrs.Tests.Integration.TestDomain;

/// <summary>
///     The input of <see cref="AccountSummaryProjector" />: the read model as stored (if any) and the event to apply.
/// </summary>
/// <param name="Current">The stored read model, or <c>null</c> when none exists yet.</param>
/// <param name="Event">The domain event to apply.</param>
public sealed record AccountProjectionInput(AccountSummaryReadModel? Current, IInternalDomainEvent Event);

/// <summary>
///     Pure projection logic of the account summary: no archive, no deduplication bookkeeping.
/// </summary>
public sealed class AccountSummaryProjector : IProjector<AccountProjectionInput, AccountSummaryReadModel?>
{
    /// <summary>
    ///     Applies the event to the read model.
    /// </summary>
    /// <param name="input">The stored read model and the event.</param>
    /// <returns>The read model to store, or <c>null</c> when the event changes nothing.</returns>
    public AccountSummaryReadModel? Project(AccountProjectionInput input)
    {
        AccountSummaryReadModel? current = input.Current;
        return input.Event switch
        {
            AccountCreated e => new AccountSummaryReadModel(
                e.AccountId,
                e.Owner,
                e.InitialBalance.Amount,
                e.OccurredOn,
                e.OccurredOn,
                0
            ),
            MoneyDeposited e => current is null ? null : _Apply(current, e.Amount.Amount, e.OccurredOn),
            MoneyWithdrawn e => current is null ? null : _Apply(current, -e.Amount.Amount, e.OccurredOn),
            _ => null,
        };
    }

    private static AccountSummaryReadModel _Apply(
        AccountSummaryReadModel current,
        decimal delta,
        DateTimeOffset occurredOn
    )
    {
        return current with
        {
            Balance = current.Balance + delta,
            LastTransactionDate = occurredOn,
            TransactionCount = current.TransactionCount + 1,
        };
    }
}
