using EzDdd.Cqrs.Query;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Tests.Integration.TestDomain;

namespace EzDdd.Cqrs.Tests.Integration.TestDomain;

/// <summary>
///     Query for getting account summary from the read model.
///     This query retrieves denormalized account data optimized for display.
/// </summary>
public sealed class GetAccountSummaryQuery : IQuery<GetAccountSummaryInput, GetAccountSummaryOutput>
{
    private readonly IArchive<AccountSummaryReadModel, AccountId> _archive;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GetAccountSummaryQuery" /> class.
    /// </summary>
    /// <param name="archive">The archive for accessing account read models.</param>
    public GetAccountSummaryQuery(IArchive<AccountSummaryReadModel, AccountId> archive)
    {
        _archive = archive ?? throw new ArgumentNullException(nameof(archive));
    }

    /// <summary>
    ///     Executes the query to retrieve account summary.
    /// </summary>
    /// <param name="input">The query input containing account ID.</param>
    /// <returns>A task containing the account summary output.</returns>
    /// <exception cref="UseCaseFailureException">Thrown when the account is not found.</exception>
    public async Task<GetAccountSummaryOutput> ExecuteAsync(GetAccountSummaryInput input)
    {
        AccountSummaryReadModel? readModel = await _archive.FindByIdAsync(input.AccountId);

        if (readModel == null)
        {
            throw new UseCaseFailureException($"Account not found: {input.AccountId}");
        }

        return GetAccountSummaryOutput.Create()
                                      .SetAccountId(readModel.AccountId.Value)
                                      .SetOwner(readModel.Owner)
                                      .SetBalance(readModel.Balance)
                                      .SetCreatedOn(readModel.CreatedOn)
                                      .SetLastTransactionDate(readModel.LastTransactionDate)
                                      .SetTransactionCount(readModel.TransactionCount)
                                      .Succeed()
                                      .SetMessage("Account summary retrieved successfully");
    }
}