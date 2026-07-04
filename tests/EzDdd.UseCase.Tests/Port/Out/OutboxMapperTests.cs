using EzDdd.Entity;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.UseCase.Tests.Port.Out;

public class OutboxMapperTests
{
    #region Mapper Creation Tests

    [Fact]
    public void OutboxMapper_CanCreateConcreteImplementation()
    {
        BankAccountMapper mapper = new();

        Assert.NotNull(mapper);
        Assert.IsAssignableFrom<OutboxMapper<BankAccount, BankAccountData, AccountId>>(mapper);
    }

    #endregion

    #region ToData Conversion Tests

    [Fact]
    public void ToData_CopiesId()
    {
        BankAccountMapper mapper = new();
        AccountId accountId = new("acc-123");
        BankAccount account = new(accountId, "John Doe", 1000.00m);

        BankAccountData data = mapper.ToData(account);

        Assert.Equal(accountId, data.Id);
    }

    [Fact]
    public void ToData_CopiesVersion()
    {
        BankAccountMapper mapper = new();
        AccountId accountId = new("acc-456");
        BankAccount account = new(accountId, "Alice", 500.00m);
        account.Deposit(100.00m); // Version should be 1

        BankAccountData data = mapper.ToData(account);

        Assert.Equal(1, data.Version);
    }

    [Fact]
    public void ToData_CopiesDomainEvents()
    {
        BankAccountMapper mapper = new();
        AccountId accountId = new("acc-789");
        BankAccount account = new(accountId, "Bob", 2000.00m);
        account.Deposit(500.00m);

        BankAccountData data = mapper.ToData(account);

        Assert.Equal(2, data.Events.Count); // AccountCreated + MoneyDeposited
    }

    [Fact]
    public void ToData_CopiesStreamName()
    {
        BankAccountMapper mapper = new();
        AccountId accountId = new("acc-101");
        BankAccount account = new(accountId, "Charlie", 750.00m);

        BankAccountData data = mapper.ToData(account);

        Assert.Equal("account-acc-101", data.StreamName);
    }

    [Fact]
    public void ToData_CopiesStateFields()
    {
        BankAccountMapper mapper = new();
        AccountId accountId = new("acc-202");
        BankAccount account = new(accountId, "Diana", 3000.00m);

        BankAccountData data = mapper.ToData(account);

        Assert.Equal("Diana", data.Owner);
        Assert.Equal(3000.00m, data.Balance);
    }

    #endregion

    #region ToDomain Conversion Tests

    [Fact]
    public void ToDomain_ReconstructsId()
    {
        BankAccountMapper mapper = new();
        AccountId accountId = new("acc-303");
        BankAccountData data = new(accountId, 2, Array.Empty<IDomainEvent>(), "account-acc-303", "Eve", 1500.00m);

        BankAccount account = mapper.ToDomain(data);

        Assert.Equal(accountId, account.Id);
    }

    [Fact]
    public void ToDomain_ReconstructsVersion()
    {
        BankAccountMapper mapper = new();
        AccountId accountId = new("acc-404");
        BankAccountData data = new(accountId, 5, Array.Empty<IDomainEvent>(), "account-acc-404", "Frank", 4500.00m);

        BankAccount account = mapper.ToDomain(data);

        Assert.Equal(5, account.Version);
    }

    [Fact]
    public void ToDomain_ReconstructsStateFields()
    {
        BankAccountMapper mapper = new();
        AccountId accountId = new("acc-505");
        BankAccountData data = new(accountId, 3, Array.Empty<IDomainEvent>(), "account-acc-505", "Grace", 6000.00m);

        BankAccount account = mapper.ToDomain(data);

        Assert.Equal("Grace", account.Owner);
        Assert.Equal(6000.00m, account.Balance);
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void Roundtrip_PreservesIdentity()
    {
        BankAccountMapper mapper = new();
        AccountId accountId = new("acc-606");
        BankAccount originalAccount = new(accountId, "Henry", 7000.00m);
        originalAccount.Deposit(1000.00m);

        BankAccountData data = mapper.ToData(originalAccount);
        BankAccount reconstructedAccount = mapper.ToDomain(data);

        Assert.Equal(originalAccount.Id, reconstructedAccount.Id);
        Assert.Equal(originalAccount.Version, reconstructedAccount.Version);
        Assert.Equal(originalAccount.Owner, reconstructedAccount.Owner);
        Assert.Equal(originalAccount.Balance, reconstructedAccount.Balance);
    }

    #endregion

    #region Complex Scenario Tests

    [Fact]
    public void ToData_WithMultipleDeposits_PreservesAllEvents()
    {
        BankAccountMapper mapper = new();
        AccountId accountId = new("acc-707");
        BankAccount account = new(accountId, "Iris", 1000.00m);
        account.Deposit(500.00m);
        account.Deposit(250.00m);
        account.Deposit(125.00m);

        BankAccountData data = mapper.ToData(account);

        Assert.Equal(4, data.Events.Count); // 1 AccountCreated + 3 MoneyDeposited
        Assert.Equal(1875.00m, data.Balance); // 1000 + 500 + 250 + 125
    }

    [Fact]
    public void ToData_AfterMultipleOperations_VersionIncremented()
    {
        BankAccountMapper mapper = new();
        AccountId accountId = new("acc-808");
        BankAccount account = new(accountId, "Jack", 2000.00m);
        account.Deposit(100.00m);
        account.Deposit(200.00m);

        BankAccountData data = mapper.ToData(account);

        Assert.Equal(2, data.Version); // 0 + 1 (first deposit) + 1 (second deposit)
    }

    #endregion

    #region Test Domain Model

    private sealed record AccountId(string Value);

    private sealed record AccountCreated(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string Owner,
        decimal InitialBalance,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent;

    private sealed record MoneyDeposited(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        decimal Amount,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private sealed class BankAccount : AggregateRoot<AccountId, IInternalDomainEvent>
    {
        // Constructor for new aggregate
        public BankAccount(AccountId id, string owner, decimal initialBalance)
        {
            Id = id;
            Owner = owner;
            Balance = initialBalance;

            AccountCreated @event = new(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                id.Value,
                owner,
                initialBalance,
                new Dictionary<string, string>()
            );
            Apply(@event);
        }

        // Constructor for loading from data (state sourcing)
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
            MoneyDeposited @event = new(
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

    private sealed class BankAccountData : IOutboxData<AccountId>
    {
        public BankAccountData(
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

        // State fields
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

    private sealed class BankAccountMapper : OutboxMapper<BankAccount, BankAccountData, AccountId>
    {
        public override BankAccountData ToData(BankAccount aggregate)
        {
            return new BankAccountData(
                aggregate.Id,
                aggregate.Version,
                aggregate.GetDomainEvents(),
                $"account-{aggregate.Id.Value}", // Manually construct stream name
                aggregate.Owner,
                aggregate.Balance
            );
        }

        public override BankAccount ToDomain(BankAccountData data)
        {
            return new BankAccount(data.Id, data.Owner, data.Balance, data.Version);
        }
    }

    #endregion
}
