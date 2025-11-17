namespace EzDdd.Cqrs.Tests.Integration.TestDomain;

/// <summary>
///     Output for getting account summary query.
/// </summary>
public sealed class GetAccountSummaryOutput : CqrsOutput<GetAccountSummaryOutput>
{
    /// <summary>
    ///     Gets or sets the account identifier.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the account owner name.
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the current balance.
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    ///     Gets or sets when the account was created.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    ///     Gets or sets when the last transaction occurred.
    /// </summary>
    public DateTimeOffset LastTransactionDate { get; set; }

    /// <summary>
    ///     Gets or sets the total number of transactions.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    ///     Sets the account identifier (fluent API).
    /// </summary>
    public GetAccountSummaryOutput SetAccountId(string accountId)
    {
        AccountId = accountId;
        return this;
    }

    /// <summary>
    ///     Sets the owner name (fluent API).
    /// </summary>
    public GetAccountSummaryOutput SetOwner(string owner)
    {
        Owner = owner;
        return this;
    }

    /// <summary>
    ///     Sets the balance (fluent API).
    /// </summary>
    public GetAccountSummaryOutput SetBalance(decimal balance)
    {
        Balance = balance;
        return this;
    }

    /// <summary>
    ///     Sets the created date (fluent API).
    /// </summary>
    public GetAccountSummaryOutput SetCreatedOn(DateTimeOffset createdOn)
    {
        CreatedOn = createdOn;
        return this;
    }

    /// <summary>
    ///     Sets the last transaction date (fluent API).
    /// </summary>
    public GetAccountSummaryOutput SetLastTransactionDate(DateTimeOffset lastTransactionDate)
    {
        LastTransactionDate = lastTransactionDate;
        return this;
    }

    /// <summary>
    ///     Sets the transaction count (fluent API).
    /// </summary>
    public GetAccountSummaryOutput SetTransactionCount(int transactionCount)
    {
        TransactionCount = transactionCount;
        return this;
    }
}