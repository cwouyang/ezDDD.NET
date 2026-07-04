using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.UseCase.Tests.Integration.TestDomain;

/// <summary>
///     Input for creating a new bank account.
/// </summary>
public sealed record CreateAccountInput(AccountId AccountId, string Owner, Money InitialBalance) : IInput;

/// <summary>
///     Output for account creation.
/// </summary>
public sealed class CreateAccountOutput : IOutput
{
    public AccountId AccountId { get; init; } = null!;
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
///     Use case for creating a new bank account.
/// </summary>
public sealed class CreateAccountUseCase(IRepository<BankAccount, AccountId, IInternalDomainEvent> repository)
    : IUseCase<CreateAccountInput, CreateAccountOutput>
{
    public async Task<CreateAccountOutput> ExecuteAsync(CreateAccountInput input)
    {
        try
        {
            // Create new account aggregate
            BankAccount account = new(input.AccountId, input.Owner, input.InitialBalance);

            // Save to repository
            await repository.SaveAsync(account);

            // Return success output
            CreateAccountOutput output = new() { AccountId = input.AccountId, Version = account.Version };
            output.Succeed();
            return output;
        }
        catch (InvalidOperationException ex)
        {
            // Business rule violation
            throw new UseCaseFailureException($"Failed to create account: {ex.Message}", ex);
        }
        // RepositorySaveException propagates to caller
    }
}
