using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.UseCase.Tests.Integration.TestDomain;

/// <summary>
///     Input for withdrawing money from an account.
///     Implements IVersionedInput for optimistic locking.
/// </summary>
public sealed record WithdrawInput(AccountId AccountId, Money Amount) : IVersionedInput
{
    public long Version { get; set; }
}

/// <summary>
///     Output for withdrawal operation.
/// </summary>
public sealed class WithdrawOutput : IOutput
{
    public Money NewBalance { get; init; } = null!;
    public long Version { get; init; }

    public string Message { get; private set; } = string.Empty;
    public ExitCode ExitCode { get; private set; }
    public string Id { get; private set; } = string.Empty;

    public IOutput SetMessage(string message)
    {
        Message = message;
        return this;
    }

    public IOutput SetExitCode(ExitCode exitCode)
    {
        ExitCode = exitCode;
        return this;
    }

    public IOutput SetId(string id)
    {
        Id = id;
        return this;
    }

    public IOutput Fail()
    {
        ExitCode = ExitCode.Failure;
        return this;
    }

    public IOutput Succeed()
    {
        ExitCode = ExitCode.Success;
        return this;
    }
}

/// <summary>
///     Use case for withdrawing money from a bank account.
///     Validates version for optimistic locking.
/// </summary>
public sealed class WithdrawUseCase(IRepository<BankAccount, AccountId, IInternalDomainEvent> repository)
    : IUseCase<WithdrawInput, WithdrawOutput>
{
    public async Task<WithdrawOutput> ExecuteAsync(WithdrawInput input)
    {
        try
        {
            // Load account
            BankAccount? account = await repository.FindByIdAsync(input.AccountId);
            if (account is null)
            {
                throw new UseCaseFailureException($"Account not found: {input.AccountId.Value}");
            }

            // Validate version (optimistic locking)
            if (account.Version != input.Version)
            {
                throw new UseCaseFailureException(
                    $"Version mismatch: expected {input.Version}, actual {account.Version}"
                );
            }

            // Execute business logic
            account.Withdraw(input.Amount);

            // Save changes
            await repository.SaveAsync(account);

            // Return success output
            WithdrawOutput output = new() { NewBalance = account.Balance, Version = account.Version };
            output.Succeed();
            return output;
        }
        catch (InvalidOperationException ex)
        {
            // Business rule violation (e.g., insufficient balance)
            throw new UseCaseFailureException($"Failed to withdraw: {ex.Message}", ex);
        }
        // RepositorySaveException propagates to caller
    }
}
