using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.Out;
using EzDdd.UseCase.Tests.Integration.TestDomain;

namespace EzDdd.UseCase.Tests.Integration;

public sealed class UseCaseExecutionFlowTests
{
    [Fact]
    public async Task CreateAccountUseCase_WithValidInput_CreatesAccountSuccessfully()
    {
        IRepository<BankAccount, AccountId, IInternalDomainEvent> repository = CreateRepository();
        CreateAccountUseCase useCase = new(repository);
        CreateAccountInput input = new(new AccountId("acc-001"), "John Doe", new Money(1000m));

        CreateAccountOutput output = await useCase.ExecuteAsync(input);

        Assert.NotNull(output);
        Assert.Equal(ExitCode.Success, output.ExitCode);
        Assert.Equal("acc-001", output.AccountId.Value);
        Assert.Equal(0L, output.Version); // First version after creation

        // Verify aggregate was saved
        BankAccount? saved = await repository.FindByIdAsync(input.AccountId);
        Assert.NotNull(saved);
        Assert.Equal("John Doe", saved.Owner);
        Assert.Equal(1000m, saved.Balance.Amount);
        Assert.False(saved.IsClosed);
    }

    [Fact]
    public async Task DepositUseCase_WithValidInputAndVersion_DepositsSuccessfully()
    {
        IRepository<BankAccount, AccountId, IInternalDomainEvent> repository = CreateRepository();
        AccountId accountId = new("acc-002");

        // Create account first
        BankAccount account = new(accountId, "Jane Doe", new Money(500m));
        await repository.SaveAsync(account);
        long currentVersion = account.Version;

        DepositUseCase useCase = new(repository);
        DepositInput input = new(accountId, new Money(300m)) { Version = currentVersion };

        DepositOutput output = await useCase.ExecuteAsync(input);

        Assert.NotNull(output);
        Assert.Equal(ExitCode.Success, output.ExitCode);
        Assert.Equal(800m, output.NewBalance.Amount);
        Assert.Equal(currentVersion + 1, output.Version);

        // Verify aggregate state
        BankAccount? updated = await repository.FindByIdAsync(accountId);
        Assert.NotNull(updated);
        Assert.Equal(800m, updated.Balance.Amount);
    }

    [Fact]
    public async Task WithdrawUseCase_WithValidInput_WithdrawsSuccessfully()
    {
        IRepository<BankAccount, AccountId, IInternalDomainEvent> repository = CreateRepository();
        AccountId accountId = new("acc-003");

        BankAccount account = new(accountId, "Bob Smith", new Money(1000m));
        await repository.SaveAsync(account);
        long currentVersion = account.Version;

        WithdrawUseCase useCase = new(repository);
        WithdrawInput input = new(accountId, new Money(200m)) { Version = currentVersion };

        WithdrawOutput output = await useCase.ExecuteAsync(input);

        Assert.Equal(ExitCode.Success, output.ExitCode);
        Assert.Equal(800m, output.NewBalance.Amount);
        Assert.Equal(currentVersion + 1, output.Version);
    }

    [Fact]
    public async Task DepositUseCase_WithNegativeAmount_ThrowsUseCaseFailureException()
    {
        IRepository<BankAccount, AccountId, IInternalDomainEvent> repository = CreateRepository();
        AccountId accountId = new("acc-004");

        BankAccount account = new(accountId, "Alice Brown", new Money(1000m));
        await repository.SaveAsync(account);

        DepositUseCase useCase = new(repository);
        DepositInput input = new(
            accountId,
            new Money(-100m) // Invalid: negative amount
        )
        {
            Version = account.Version,
        };

        UseCaseFailureException exception = await Assert.ThrowsAsync<UseCaseFailureException>(async () =>
            await useCase.ExecuteAsync(input)
        );

        Assert.Contains("positive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WithdrawUseCase_WithInsufficientBalance_ThrowsUseCaseFailureException()
    {
        IRepository<BankAccount, AccountId, IInternalDomainEvent> repository = CreateRepository();
        AccountId accountId = new("acc-005");

        BankAccount account = new(accountId, "Charlie Green", new Money(100m));
        await repository.SaveAsync(account);

        WithdrawUseCase useCase = new(repository);
        WithdrawInput input = new(
            accountId,
            new Money(500m) // More than balance
        )
        {
            Version = account.Version,
        };

        UseCaseFailureException exception = await Assert.ThrowsAsync<UseCaseFailureException>(async () =>
            await useCase.ExecuteAsync(input)
        );

        Assert.Contains("negative", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DepositUseCase_WithWrongVersion_ThrowsUseCaseFailureException()
    {
        IRepository<BankAccount, AccountId, IInternalDomainEvent> repository = CreateRepository();
        AccountId accountId = new("acc-006");

        BankAccount account = new(accountId, "David White", new Money(1000m));
        await repository.SaveAsync(account);
        long currentVersion = account.Version;

        DepositUseCase useCase = new(repository);
        DepositInput input = new(accountId, new Money(100m))
        {
            Version = currentVersion + 999, // Wrong version (optimistic locking)
        };

        UseCaseFailureException exception = await Assert.ThrowsAsync<UseCaseFailureException>(async () =>
            await useCase.ExecuteAsync(input)
        );

        Assert.Contains("version", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DepositUseCase_WhenAccountNotFound_ThrowsUseCaseFailureException()
    {
        IRepository<BankAccount, AccountId, IInternalDomainEvent> repository = CreateRepository();
        DepositUseCase useCase = new(repository);
        DepositInput input = new(new AccountId("non-existent"), new Money(100m)) { Version = 0 };

        UseCaseFailureException exception = await Assert.ThrowsAsync<UseCaseFailureException>(async () =>
            await useCase.ExecuteAsync(input)
        );

        Assert.Contains("not found", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAccountUseCase_WhenRepositorySaveFails_ThrowsRepositorySaveException()
    {
        FailingRepositoryPeer failingPeer = new();
        EsRepository<BankAccount, AccountId> repository = new(failingPeer);
        CreateAccountUseCase useCase = new(repository);
        CreateAccountInput input = new(new AccountId("acc-007"), "Eve Black", new Money(1000m));

        RepositorySaveException exception = await Assert.ThrowsAsync<RepositorySaveException>(async () =>
            await useCase.ExecuteAsync(input)
        );

        // Verify the exception is properly wrapped
        Assert.Contains("BankAccount", exception.Message); // Outer exception
        Assert.NotNull(exception.InnerException);
        Assert.IsType<RepositoryPeerSaveException>(exception.InnerException);
        Assert.Contains("Simulated save failure", exception.InnerException.Message);
    }

    // Helper method
    private static IRepository<BankAccount, AccountId, IInternalDomainEvent> CreateRepository()
    {
        InMemoryEventStorePeer peer = new();
        return new EsRepository<BankAccount, AccountId>(peer);
    }

    /// <summary>
    ///     Mock repository peer that always fails on save (for testing exception handling).
    /// </summary>
    private sealed class FailingRepositoryPeer : IRepositoryPeer<EventStoreData<AccountId>, AccountId>
    {
        public Task<EventStoreData<AccountId>?> FindByIdAsync(AccountId id)
        {
            return Task.FromResult<EventStoreData<AccountId>?>(null);
        }

        public Task SaveAsync(EventStoreData<AccountId> data)
        {
            throw new RepositoryPeerSaveException("Simulated save failure");
        }

        public Task DeleteAsync(EventStoreData<AccountId> data)
        {
            throw new NotImplementedException();
        }
    }
}
