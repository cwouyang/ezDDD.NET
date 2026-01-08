using EzDdd.UseCase.Tests.Integration.TestDomain;

namespace EzDdd.UseCase.Tests.Integration.Services;

/// <summary>
/// Transfer confirmation record containing transaction details.
/// </summary>
public sealed record TransferConfirmation(
    Guid TransactionId,
    TransferStatus Status,
    AccountId FromAccountId,
    AccountId ToAccountId,
    Money Amount,
    DateTimeOffset Timestamp);

/// <summary>
/// Transfer status enumeration.
/// </summary>
public enum TransferStatus
{
    Success,
    Failed
}
