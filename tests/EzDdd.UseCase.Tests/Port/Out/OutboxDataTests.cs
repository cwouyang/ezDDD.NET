using EzDdd.Entity;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.UseCase.Tests.Port.Out;

public class OutboxDataTests
{
    #region Interface Characteristics Tests

    [Fact]
    public void IOutboxData_ExtendsIStoreData()
    {
        Type type = typeof(IOutboxData<AccountId>);

        Assert.True(typeof(IStoreData<AccountId>).IsAssignableFrom(type));
    }

    [Fact]
    public void IOutboxData_IsInterface()
    {
        Type type = typeof(IOutboxData<AccountId>);

        Assert.True(type.IsInterface);
    }

    #endregion

    #region Implementation Tests

    [Fact]
    public void IOutboxData_CanBeImplemented()
    {
        AccountId accountId = new("acc-123");
        AccountCreated @event = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "acc-123",
            "John Doe",
            new Dictionary<string, string>()
        );
        IReadOnlyList<IDomainEvent> events = [@event];

        BankAccountData data = new(accountId, 1, events, "account-acc-123", "John Doe", 1000.00m);

        Assert.NotNull(data);
        Assert.Equal(accountId, data.Id);
        Assert.Equal(1, data.Version);
        Assert.Single(data.Events);
        Assert.Equal("account-acc-123", data.StreamName);
        Assert.Equal("John Doe", data.Owner);
        Assert.Equal(1000.00m, data.Balance);
    }

    #endregion

    #region Inherited Property Tests

    [Fact]
    public void IOutboxData_InheritsIdProperty()
    {
        AccountId accountId = new("acc-456");
        BankAccountData data = new(accountId, 0, [], "stream", "Alice", 500.00m);

        AccountId id = data.Id;

        Assert.Equal(accountId, id);
    }

    [Fact]
    public void IOutboxData_InheritsVersionProperty()
    {
        AccountId accountId = new("acc-789");
        BankAccountData data = new(accountId, 5, [], "stream", "Bob", 2000.00m);

        long version = data.Version;

        Assert.Equal(5, version);
    }

    [Fact]
    public void IOutboxData_InheritsEventsProperty()
    {
        AccountId accountId = new("acc-101");
        AccountCreated @event = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "acc-101",
            "Charlie",
            new Dictionary<string, string>()
        );
        IReadOnlyList<IDomainEvent> events = [@event];
        BankAccountData data = new(accountId, 0, events, "stream", "Charlie", 0.00m);

        IReadOnlyList<IDomainEvent> retrievedEvents = data.Events;

        Assert.Single(retrievedEvents);
        Assert.Equal(@event, retrievedEvents[0]);
    }

    [Fact]
    public void IOutboxData_InheritsStreamNameProperty()
    {
        AccountId accountId = new("acc-202");
        const string streamName = "account-acc-202";
        BankAccountData data = new(accountId, 0, [], streamName, "Diana", 750.00m);

        string retrievedStreamName = data.StreamName;

        Assert.Equal(streamName, retrievedStreamName);
    }

    [Fact]
    public void IOutboxData_InheritsGetOptimisticLockVersion()
    {
        AccountId accountId = new("acc-303");
        BankAccountData data = new(accountId, 3, [], "stream", "Eve", 1500.00m);

        long lockVersion = data.GetOptimisticLockVersion();

        Assert.Equal(3, lockVersion);
    }

    #endregion

    #region State Field Tests

    [Fact]
    public void IOutboxData_CanStoreStateFields()
    {
        AccountId accountId = new("acc-404");
        const string owner = "Frank";
        const decimal balance = 3000.00m;

        BankAccountData data = new(accountId, 0, [], "stream", owner, balance);

        Assert.Equal(owner, data.Owner);
        Assert.Equal(balance, data.Balance);
    }

    [Fact]
    public void IOutboxData_SupportsMultipleStateFields()
    {
        AccountId accountId = new("acc-505");

        BankAccountData data = new(accountId, 2, [], "account-acc-505", "Grace", 4500.00m);

        Assert.Equal(accountId, data.Id);
        Assert.Equal(2, data.Version);
        Assert.Equal("account-acc-505", data.StreamName);
        Assert.Equal("Grace", data.Owner);
        Assert.Equal(4500.00m, data.Balance);
    }

    #endregion

    #region Test Data Structures

    private sealed record AccountId(string Value);

    private sealed record AccountCreated(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string Owner,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent;

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

        // State fields (specific to BankAccount)
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

    #endregion
}
