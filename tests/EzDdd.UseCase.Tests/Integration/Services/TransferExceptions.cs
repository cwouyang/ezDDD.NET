using EzDdd.UseCase.Tests.Integration.TestDomain;

namespace EzDdd.UseCase.Tests.Integration.Services;

/// <summary>
/// Exception thrown when account is not found.
/// </summary>
public sealed class AccountNotFoundException : Exception
{
    public AccountNotFoundException(AccountId accountId)
        : base($"Account not found: {accountId.Value}")
    {
        AccountId = accountId;
    }

    public AccountId AccountId { get; }
}

/// <summary>
/// Exception thrown when account has insufficient balance for transfer.
/// </summary>
public sealed class InsufficientBalanceException : Exception
{
    public InsufficientBalanceException(
        AccountId accountId,
        Money currentBalance,
        Money requestedAmount)
        : base($"Insufficient balance in account {accountId.Value}. Current: {currentBalance}, Requested: {requestedAmount}")
    {
        AccountId = accountId;
        CurrentBalance = currentBalance;
        RequestedAmount = requestedAmount;
    }

    public AccountId AccountId { get; }
    public Money CurrentBalance { get; }
    public Money RequestedAmount { get; }
}

/// <summary>
/// Exception thrown when transfer amount exceeds limit.
/// </summary>
public sealed class TransferLimitExceededException : Exception
{
    public TransferLimitExceededException(Money requestedAmount)
        : base($"Transfer amount {requestedAmount} exceeds maximum limit of $10,000 USD")
    {
        RequestedAmount = requestedAmount;
        MaxLimit = new Money(10000, "USD");
    }

    public Money RequestedAmount { get; }
    public Money MaxLimit { get; }
}

/// <summary>
/// Exception thrown when transfer amount is invalid (zero or negative).
/// </summary>
public sealed class InvalidTransferAmountException : Exception
{
    public InvalidTransferAmountException(Money amount)
        : base($"Transfer amount must be positive, got: {amount}")
    {
        Amount = amount;
    }

    public Money Amount { get; }
}

/// <summary>
/// Exception thrown when account is closed.
/// </summary>
public sealed class AccountClosedException : Exception
{
    public AccountClosedException(AccountId accountId)
        : base($"Account is closed: {accountId.Value}")
    {
        AccountId = accountId;
    }

    public AccountId AccountId { get; }
}

/// <summary>
/// Exception thrown when attempting to transfer to the same account.
/// </summary>
public sealed class SameAccountTransferException : Exception
{
    public SameAccountTransferException(AccountId accountId)
        : base($"Cannot transfer to the same account: {accountId.Value}")
    {
        AccountId = accountId;
    }

    public AccountId AccountId { get; }
}
