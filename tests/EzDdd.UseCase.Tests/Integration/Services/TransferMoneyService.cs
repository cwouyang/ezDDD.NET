using EzDdd.Entity;
using EzDdd.UseCase.Port.Out;
using EzDdd.UseCase.Tests.Integration.TestDomain;

namespace EzDdd.UseCase.Tests.Integration.Services;

/// <summary>
/// Service for transferring money between bank accounts.
/// This is an example of the Service Layer pattern for complex cross-aggregate operations.
/// </summary>
public sealed class TransferMoneyService : ITransferMoneyService
{
    private readonly IRepository<BankAccount, AccountId, IInternalDomainEvent> _repository;
    private static readonly Money TransferLimit = new(10000, "USD");

    public TransferMoneyService(IRepository<BankAccount, AccountId, IInternalDomainEvent> repository)
    {
        _repository = repository;
    }

    public async Task<TransferConfirmation> TransferAsync(
        AccountId fromAccountId,
        AccountId toAccountId,
        Money amount)
    {
        // 1. Validate inputs (fail fast)
        ValidateTransferAmount(amount);
        ValidateDifferentAccounts(fromAccountId, toAccountId);

        // 2. Load both accounts
        var fromAccount = await _repository.FindByIdAsync(fromAccountId);
        var toAccount = await _repository.FindByIdAsync(toAccountId);

        if (fromAccount == null)
        {
            throw new AccountNotFoundException(fromAccountId);
        }

        if (toAccount == null)
        {
            throw new AccountNotFoundException(toAccountId);
        }

        // 3. Validate business rules
        ValidateAccountsNotClosed(fromAccount, toAccount);
        ValidateSufficientBalance(fromAccount, amount);
        ValidateTransferLimit(amount);

        // 4. Execute transfer (domain operations)
        fromAccount.Withdraw(amount);
        toAccount.Deposit(amount);

        // 5. Persist both accounts (repository handles transaction at IRepositoryPeer level)
        await _repository.SaveAsync(fromAccount);
        await _repository.SaveAsync(toAccount);

        // 6. Return confirmation
        return new TransferConfirmation(
            TransactionId: Guid.NewGuid(),
            Status: TransferStatus.Success,
            FromAccountId: fromAccountId,
            ToAccountId: toAccountId,
            Amount: amount,
            Timestamp: DateTimeOffset.UtcNow);
    }

    #region Private Validation Methods

    private static void ValidateTransferAmount(Money amount)
    {
        if (amount.Amount <= 0)
        {
            throw new InvalidTransferAmountException(amount);
        }
    }

    private static void ValidateDifferentAccounts(AccountId fromAccountId, AccountId toAccountId)
    {
        if (fromAccountId == toAccountId)
        {
            throw new SameAccountTransferException(fromAccountId);
        }
    }

    private static void ValidateAccountsNotClosed(BankAccount fromAccount, BankAccount toAccount)
    {
        if (fromAccount.IsClosed)
        {
            throw new AccountClosedException(fromAccount.Id);
        }

        if (toAccount.IsClosed)
        {
            throw new AccountClosedException(toAccount.Id);
        }
    }

    private static void ValidateSufficientBalance(BankAccount fromAccount, Money amount)
    {
        if (fromAccount.Balance.Amount < amount.Amount)
        {
            throw new InsufficientBalanceException(fromAccount.Id, fromAccount.Balance, amount);
        }
    }

    private static void ValidateTransferLimit(Money amount)
    {
        if (amount.Amount > TransferLimit.Amount)
        {
            throw new TransferLimitExceededException(amount);
        }
    }

    #endregion
}
