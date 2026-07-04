using EzDdd.UseCase.Tests.Integration.TestDomain;

namespace EzDdd.Cqrs.Tests.Integration.TestDomain;

/// <summary>
///     Read model for account summary (denormalized view optimized for queries).
///     This read model is maintained by <see cref="AccountProjector" /> by listening
///     to domain events from the write side.
/// </summary>
/// <param name="AccountId">The account identifier.</param>
/// <param name="Owner">The account owner name.</param>
/// <param name="Balance">The current account balance.</param>
/// <param name="CreatedOn">When the account was created.</param>
/// <param name="LastTransactionDate">When the last transaction occurred.</param>
/// <param name="TransactionCount">Total number of transactions (deposits + withdrawals).</param>
public sealed record AccountSummaryReadModel(
    AccountId AccountId,
    string Owner,
    decimal Balance,
    DateTimeOffset CreatedOn,
    DateTimeOffset LastTransactionDate,
    int TransactionCount
);
