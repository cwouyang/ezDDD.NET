using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.Out;
using EzDdd.UseCase.Tests.Integration.TestDomain;

namespace EzDdd.UseCase.Tests.Integration;

public sealed class EventSourcingWorkflowTests : IDisposable
{
    private readonly InMemoryEventStorePeer _peer;
    private readonly EsRepository<BankAccount, AccountId> _repository;

    public EventSourcingWorkflowTests()
    {
        // Register domain events for serialization
        DomainEventTypeMapper.Register<AccountCreated>("AccountCreated");
        DomainEventTypeMapper.Register<MoneyDeposited>("MoneyDeposited");
        DomainEventTypeMapper.Register<MoneyWithdrawn>("MoneyWithdrawn");
        DomainEventTypeMapper.Register<AccountClosed>("AccountClosed");

        _peer = new InMemoryEventStorePeer();
        _repository = new EsRepository<BankAccount, AccountId>(_peer);
    }

    public void Dispose()
    {
        _peer.Clear();
    }

    [Fact]
    public async Task CreateAccount_AndSave_StoresEvents()
    {
        AccountId accountId = new("acc-001");
        BankAccount account = new(accountId, "John Doe", new Money(1000m));

        await _repository.SaveAsync(account);

        BankAccount? loaded = await _repository.FindByIdAsync(accountId);
        Assert.NotNull(loaded);
        Assert.Equal("John Doe", loaded.Owner);
        Assert.Equal(1000m, loaded.Balance.Amount);
        Assert.Equal("USD", loaded.Balance.Currency);
        Assert.False(loaded.IsClosed);
        Assert.Equal(0, loaded.Version); // Version should be 0 after first save
    }

    [Fact]
    public async Task DepositMoney_AndSave_AddsEvent()
    {
        AccountId accountId = new("acc-002");
        BankAccount account = new(accountId, "Jane Smith", new Money(500m));
        await _repository.SaveAsync(account);

        account.Deposit(new Money(200m));
        await _repository.SaveAsync(account);

        BankAccount? loaded = await _repository.FindByIdAsync(accountId);
        Assert.NotNull(loaded);
        Assert.Equal(700m, loaded.Balance.Amount);
        Assert.Equal(1, loaded.Version); // Version incremented
    }

    [Fact]
    public async Task SaveAndLoad_ReconstructsState()
    {
        AccountId accountId = new("acc-003");
        BankAccount account = new(accountId, "Alice Brown", new Money(1000m, "EUR"));
        account.Deposit(new Money(500m, "EUR"));
        account.Withdraw(new Money(200m, "EUR"));
        account.Deposit(new Money(100m, "EUR"));

        await _repository.SaveAsync(account);
        BankAccount? loaded = await _repository.FindByIdAsync(accountId);

        Assert.NotNull(loaded);
        Assert.Equal("Alice Brown", loaded.Owner);
        Assert.Equal(1400m, loaded.Balance.Amount); // 1000 + 500 - 200 + 100
        Assert.Equal("EUR", loaded.Balance.Currency);
        Assert.Equal(3, loaded.Version); // 4 events (create + 3 commands)
    }

    [Fact]
    public async Task EventReplay_MaintainsInvariants()
    {
        AccountId accountId = new("acc-004");
        BankAccount account = new(accountId, "Bob Wilson", new Money(1000m));
        account.Deposit(new Money(500m));
        await _repository.SaveAsync(account);

        BankAccount? loaded = await _repository.FindByIdAsync(accountId);

        Assert.NotNull(loaded);
        Assert.Equal(1500m, loaded.Balance.Amount);
        Assert.True(loaded.Balance.Amount >= 0); // Invariant: non-negative balance
        Assert.False(string.IsNullOrWhiteSpace(loaded.Owner)); // Invariant: owner not empty
    }

    [Fact]
    public async Task CloseAccount_AndLoad_RestoresClosedState()
    {
        AccountId accountId = new("acc-005");
        BankAccount account = new(accountId, "Charlie Davis", new Money(100m));
        account.Close("Customer request");

        await _repository.SaveAsync(account);
        BankAccount? loaded = await _repository.FindByIdAsync(accountId);

        Assert.NotNull(loaded);
        Assert.True(loaded.IsClosed);
        Assert.Equal(100m, loaded.Balance.Amount); // Balance preserved
    }

    [Fact]
    public void WithdrawBeyondBalance_ViolatesInvariant_ThrowsException()
    {
        AccountId accountId = new("acc-006");
        BankAccount account = new(accountId, "David Lee", new Money(100m));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>
        (() =>
            {
                account.Withdraw(new Money(200m)); // Withdraw more than balance
            }
        );
        Assert.Contains("balance cannot be negative", exception.Message);
    }

    [Fact]
    public async Task DomainEventMapper_RoundTrip_PreservesData()
    {
        AccountId accountId = new("acc-007");
        BankAccount account = new(accountId, "Eve Martinez", new Money(1000m, "GBP"));
        account.Deposit(new Money(250m, "GBP"));
        await _repository.SaveAsync(account);

        BankAccount? loaded = await _repository.FindByIdAsync(accountId);

        Assert.NotNull(loaded);
        Assert.Equal(accountId.Value, loaded.Id.Value);
        Assert.Equal("Eve Martinez", loaded.Owner);
        Assert.Equal(1250m, loaded.Balance.Amount);
        Assert.Equal("GBP", loaded.Balance.Currency);
    }

    [Fact]
    public async Task ConcurrentSave_OptimisticLock_ThrowsException()
    {
        AccountId accountId = new("acc-008");
        BankAccount account1 = new(accountId, "Frank Garcia", new Money(1000m));
        await _repository.SaveAsync(account1);

        // Load same account twice (simulating concurrent access)
        BankAccount? account2 = await _repository.FindByIdAsync(accountId);
        BankAccount? account3 = await _repository.FindByIdAsync(accountId);

        Assert.NotNull(account2);
        Assert.NotNull(account3);

        account2.Deposit(new Money(100m));
        await _repository.SaveAsync(account2);

        account3.Deposit(new Money(200m));
        await Assert.ThrowsAsync<RepositorySaveException>
        (async () =>
            {
                await _repository.SaveAsync(account3);
            }
        );
    }

    [Fact]
    public async Task LoadNonExistentAccount_ReturnsNull()
    {
        AccountId accountId = new("non-existent");

        BankAccount? loaded = await _repository.FindByIdAsync(accountId);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task MultipleOperations_CompleteWorkflow_Success()
    {
        AccountId accountId = new("acc-010");
        BankAccount account = new(accountId, "Grace Kim", new Money(5000m));

        account.Deposit(new Money(1000m));
        account.Withdraw(new Money(500m));
        account.Deposit(new Money(250m));
        account.Withdraw(new Money(100m));
        await _repository.SaveAsync(account);

        // Load and verify
        BankAccount? loaded = await _repository.FindByIdAsync(accountId);
        Assert.NotNull(loaded);
        Assert.Equal(5650m, loaded.Balance.Amount); // 5000 + 1000 - 500 + 250 - 100
        Assert.Equal(4, loaded.Version); // 5 events total (create + 4 commands)

        // Continue operations on loaded aggregate
        loaded.Deposit(new Money(350m));
        await _repository.SaveAsync(loaded);

        // Reload and verify
        BankAccount? reloaded = await _repository.FindByIdAsync(accountId);
        Assert.NotNull(reloaded);
        Assert.Equal(6000m, reloaded.Balance.Amount);
        Assert.Equal(5, reloaded.Version);
    }
}