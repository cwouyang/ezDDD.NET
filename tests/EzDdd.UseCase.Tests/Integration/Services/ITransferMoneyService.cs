using EzDdd.UseCase.Tests.Integration.TestDomain;

namespace EzDdd.UseCase.Tests.Integration.Services;

/// <summary>
/// Service for transferring money between two bank accounts.
/// This is an example of the Service Layer pattern for complex cross-aggregate operations.
/// </summary>
public interface ITransferMoneyService
{
    /// <summary>
    /// Transfers the specified amount from the source account to the destination account.
    /// Validates sufficient balance, enforces transfer limits, and ensures atomic operation.
    /// </summary>
    /// <param name="fromAccountId">The source account identifier</param>
    /// <param name="toAccountId">The destination account identifier</param>
    /// <param name="amount">The amount to transfer (must be positive)</param>
    /// <returns>A transfer confirmation with transaction details</returns>
    /// <exception cref="AccountNotFoundException">When either account is not found</exception>
    /// <exception cref="InsufficientBalanceException">When source account has insufficient balance</exception>
    /// <exception cref="TransferLimitExceededException">When amount exceeds daily limit ($10,000)</exception>
    /// <exception cref="InvalidTransferAmountException">When amount is zero or negative</exception>
    /// <exception cref="AccountClosedException">When either account is closed</exception>
    /// <exception cref="SameAccountTransferException">When attempting to transfer to the same account</exception>
    Task<TransferConfirmation> TransferAsync(
        AccountId fromAccountId,
        AccountId toAccountId,
        Money amount);
}
