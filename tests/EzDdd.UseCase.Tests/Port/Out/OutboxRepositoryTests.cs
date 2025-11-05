using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.UseCase.Tests.Port.Out;

public class OutboxRepositoryTests
{
#region Repository Instantiation Tests

    [Fact]
    public void OutboxRepository_CanBeInstantiated()
    {
        InMemoryRepositoryPeer peer = new();
        BankAccountMapper mapper = new();

        OutboxRepository<BankAccount, BankAccountData, AccountId> repository = new(peer, mapper);

        Assert.NotNull(repository);
        Assert.IsAssignableFrom<IRepository<BankAccount, AccountId, IInternalDomainEvent>>(repository);
    }

#endregion

#region Query Tests

    [Fact]
    public async Task FindByIdAsync_WhenNotFound_ReturnsNull()
    {
        InMemoryRepositoryPeer peer = new();
        BankAccountMapper mapper = new();
        OutboxRepository<BankAccount, BankAccountData, AccountId> repository = new(peer, mapper);
        AccountId accountId = new("acc-999");

        BankAccount? result = await repository.FindByIdAsync(accountId);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindByIdAsync_WhenFound_ReturnsAggregate()
    {
        InMemoryRepositoryPeer peer = new();
        BankAccountMapper mapper = new();
        OutboxRepository<BankAccount, BankAccountData, AccountId> repository = new(peer, mapper);

        AccountId accountId = new("acc-123");
        BankAccount account = new(accountId, "John Doe", 1000.00m);
        await repository.SaveAsync(account);

        BankAccount? result = await repository.FindByIdAsync(accountId);

        Assert.NotNull(result);
        Assert.Equal(accountId, result.Id);
        Assert.Equal("John Doe", result.Owner);
        Assert.Equal(1000.00m, result.Balance);
    }

#endregion

#region Save Tests

    [Fact]
    public async Task SaveAsync_PersistsAggregate()
    {
        InMemoryRepositoryPeer peer = new();
        BankAccountMapper mapper = new();
        OutboxRepository<BankAccount, BankAccountData, AccountId> repository = new(peer, mapper);

        AccountId accountId = new("acc-456");
        BankAccount account = new(accountId, "Alice", 500.00m);

        await repository.SaveAsync(account);

        BankAccount? loaded = await repository.FindByIdAsync(accountId);
        Assert.NotNull(loaded);
        Assert.Equal(500.00m, loaded.Balance);
    }

    [Fact]
    public async Task SaveAsync_ClearsDomainEvents()
    {
        InMemoryRepositoryPeer peer = new();
        BankAccountMapper mapper = new();
        OutboxRepository<BankAccount, BankAccountData, AccountId> repository = new(peer, mapper);

        AccountId accountId = new("acc-789");
        BankAccount account = new(accountId, "Bob", 2000.00m);
        account.Deposit(500.00m);

        Assert.Equal(2, account.GetDomainEvents().Count); // Before save

        await repository.SaveAsync(account);

        Assert.Empty(account.GetDomainEvents()); // After save
    }

    [Fact]
    public async Task SaveAsync_WhenPeerThrows_TranslatesException()
    {
        InMemoryRepositoryPeer peer = new() { ThrowOnSave = true };
        BankAccountMapper mapper = new();
        OutboxRepository<BankAccount, BankAccountData, AccountId> repository = new(peer, mapper);

        AccountId accountId = new("acc-101");
        BankAccount account = new(accountId, "Charlie", 750.00m);

        RepositorySaveException exception = await Assert.ThrowsAsync<RepositorySaveException>
        (async () => await repository.SaveAsync(account)
        );

        Assert.NotNull(exception.InnerException);
        Assert.IsType<RepositoryPeerSaveException>(exception.InnerException);
    }

    [Fact]
    public async Task SaveAsync_WhenPeerThrows_DoesNotClearEvents()
    {
        InMemoryRepositoryPeer peer = new() { ThrowOnSave = true };
        BankAccountMapper mapper = new();
        OutboxRepository<BankAccount, BankAccountData, AccountId> repository = new(peer, mapper);

        AccountId accountId = new("acc-202");
        BankAccount account = new(accountId, "Diana", 3000.00m);
        account.Deposit(1000.00m);

        int eventCountBeforeSave = account.GetDomainEvents().Count;

        try
        {
            await repository.SaveAsync(account);
        }
        catch (RepositorySaveException)
        {
            // Expected
        }

        Assert.Equal(eventCountBeforeSave, account.GetDomainEvents().Count); // Events NOT cleared
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingAggregate()
    {
        InMemoryRepositoryPeer peer = new();
        BankAccountMapper mapper = new();
        OutboxRepository<BankAccount, BankAccountData, AccountId> repository = new(peer, mapper);

        AccountId accountId = new("acc-303");
        BankAccount account = new(accountId, "Eve", 1500.00m);
        await repository.SaveAsync(account);

        BankAccount? loadedAccount = await repository.FindByIdAsync(accountId);
        Assert.NotNull(loadedAccount);
        loadedAccount.Deposit(500.00m);
        await repository.SaveAsync(loadedAccount);

        BankAccount? reloadedAccount = await repository.FindByIdAsync(accountId);
        Assert.NotNull(reloadedAccount);
        Assert.Equal(2000.00m, reloadedAccount.Balance); // 1500 + 500
    }

    [Fact]
    public async Task SaveAsync_PreservesVersion()
    {
        InMemoryRepositoryPeer peer = new();
        BankAccountMapper mapper = new();
        OutboxRepository<BankAccount, BankAccountData, AccountId> repository = new(peer, mapper);

        AccountId accountId = new("acc-505");
        BankAccount account = new(accountId, "Grace", 6000.00m);
        account.Deposit(1000.00m);
        account.Deposit(500.00m);

        await repository.SaveAsync(account);

        BankAccount? loaded = await repository.FindByIdAsync(accountId);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.Version); // 0 (AccountCreated) + 1 (Deposit 1) + 1 (Deposit 2) = 2
    }

#endregion

#region Delete Tests

    [Fact]
    public async Task DeleteAsync_RemovesAggregate()
    {
        InMemoryRepositoryPeer peer = new();
        BankAccountMapper mapper = new();
        OutboxRepository<BankAccount, BankAccountData, AccountId> repository = new(peer, mapper);

        AccountId accountId = new("acc-404");
        BankAccount account = new(accountId, "Frank", 4500.00m);
        await repository.SaveAsync(account);

        await repository.DeleteAsync(account);

        BankAccount? result = await repository.FindByIdAsync(accountId);
        Assert.Null(result);
    }

#endregion

#region Integration Tests

    [Fact]
    public async Task FindByIdAsync_UsesMapper()
    {
        InMemoryRepositoryPeer peer = new();
        BankAccountMapper mapper = new();
        OutboxRepository<BankAccount, BankAccountData, AccountId> repository = new(peer, mapper);

        AccountId accountId = new("acc-606");
        BankAccount account = new(accountId, "Henry", 7000.00m);
        await repository.SaveAsync(account);

        BankAccount? result = await repository.FindByIdAsync(accountId);

        Assert.NotNull(result);
        Assert.Equal("Henry", result.Owner);
        Assert.Equal(7000.00m, result.Balance);
        Assert.Equal(0, result.Version); // Version after 1 event (AccountCreated)
    }

    [Fact]
    public async Task SaveAsync_WithMultipleOperations_MaintainsConsistency()
    {
        InMemoryRepositoryPeer peer = new();
        BankAccountMapper mapper = new();
        OutboxRepository<BankAccount, BankAccountData, AccountId> repository = new(peer, mapper);

        AccountId accountId = new("acc-707");
        BankAccount account = new(accountId, "Iris", 1000.00m);

        await repository.SaveAsync(account);

        BankAccount? loaded1 = await repository.FindByIdAsync(accountId);
        Assert.NotNull(loaded1);
        loaded1.Deposit(500.00m);
        await repository.SaveAsync(loaded1);

        BankAccount? loaded2 = await repository.FindByIdAsync(accountId);
        Assert.NotNull(loaded2);
        loaded2.Deposit(250.00m);
        await repository.SaveAsync(loaded2);

        BankAccount? final = await repository.FindByIdAsync(accountId);
        Assert.NotNull(final);
        Assert.Equal(1750.00m, final.Balance); // 1000 + 500 + 250
    }

#endregion

#region Test Infrastructure

    private record AccountId(string Value);

    private record AccountCreated
    (
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string Owner,
        decimal InitialBalance,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent;

    private record MoneyDeposited
    (
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        decimal Amount,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private class BankAccount : AggregateRoot<AccountId, IInternalDomainEvent>
    {
        public BankAccount(AccountId id, string owner, decimal initialBalance)
        {
            Id = id;
            Owner = owner;
            Balance = initialBalance;

            AccountCreated @event = new
            (
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                id.Value,
                owner,
                initialBalance,
                new Dictionary<string, string>()
            );
            Apply(@event);
        }

        public BankAccount(AccountId id, string owner, decimal balance, long version)
        {
            Id = id;
            Owner = owner;
            Balance = balance;
            Version = version;
        }

        public string Owner { get; }
        public decimal Balance { get; private set; }

        public void Deposit(decimal amount)
        {
            MoneyDeposited @event = new
            (
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                Id.Value,
                amount,
                new Dictionary<string, string>()
            );
            Apply(@event);
            Balance += amount;
        }
    }

    private class BankAccountData : IOutboxData<AccountId>
    {
        public BankAccountData
        (
            AccountId id,
            long version,
            IReadOnlyList<IDomainEvent> events,
            string streamName,
            string owner,
            decimal balance
        )
        {
            Id = id;
            Version = version;
            Events = events;
            StreamName = streamName;
            Owner = owner;
            Balance = balance;
        }

        public string Owner { get; }
        public decimal Balance { get; }
        public AccountId Id { get; set; }
        public long Version { get; set; }
        public IReadOnlyList<IDomainEvent> Events { get; set; }
        public string StreamName { get; set; }

        public long GetOptimisticLockVersion()
        {
            return Version;
        }
    }

    private class BankAccountMapper : OutboxMapper<BankAccount, BankAccountData, AccountId>
    {
        public override BankAccountData ToData(BankAccount aggregate)
        {
            return new BankAccountData
            (
                aggregate.Id,
                aggregate.Version,
                aggregate.GetDomainEvents(),
                $"account-{aggregate.Id.Value}",
                aggregate.Owner,
                aggregate.Balance
            );
        }

        public override BankAccount ToDomain(BankAccountData data)
        {
            return new BankAccount
            (
                data.Id,
                data.Owner,
                data.Balance,
                data.Version
            );
        }
    }

    // Mock RepositoryPeer for testing
    private class InMemoryRepositoryPeer : IRepositoryPeer<BankAccountData, AccountId>
    {
        private readonly Dictionary<AccountId, BankAccountData> _storage = new();
        public bool ThrowOnSave { get; set; }

        public Task<BankAccountData?> FindByIdAsync(AccountId id)
        {
            _storage.TryGetValue(id, out BankAccountData? data);
            return Task.FromResult(data);
        }

        public Task SaveAsync(BankAccountData data)
        {
            if (ThrowOnSave)
            {
                throw new RepositoryPeerSaveException("Simulated peer save failure");
            }

            _storage[data.Id] = data;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(BankAccountData data)
        {
            _storage.Remove(data.Id);
            return Task.CompletedTask;
        }
    }

#endregion
}