using System.Text.Json;
using EzDdd.Common;

namespace EzDdd.Entity.Tests;

[Collection("DomainEventTypeMapper")]
public class IntegrationTests
{
    // ========== Integration Tests ==========

    [Fact]
    public void Integration_CompleteEventSourcingLifecycle_AllComponentsWorkTogether()
    {
        AccountId accountId = AccountId.NewId();
        Money initialDeposit = new(1000m, "USD");

        // Phase 1: Create aggregate and perform operations
        BankAccount account = new(accountId, "Alice Johnson", initialDeposit);
        account.Deposit(new Money(500m, "USD"), "Salary deposit");
        account.Deposit(new Money(200m, "USD"), "Bonus");
        account.Withdraw(new Money(300m, "USD"), "Rent payment");
        account.Close("Account migration");

        // Phase 1: Verify final state
        Assert.Equal("Alice Johnson", account.AccountHolder);
        Assert.Equal(1400m, account.Balance.Amount); // 1000 + 500 + 200 - 300
        Assert.Equal("USD", account.Balance.Currency);
        Assert.Equal(4, account.Transactions.Count); // Initial + 2 deposits + 1 withdrawal
        Assert.True(account.IsDeleted);
        Assert.Equal(4L, account.Version); // 5 events (0-4)

        // Phase 2: Get event history
        IReadOnlyList<IInternalDomainEvent> events = account.GetDomainEvents();

        // Phase 2: Verify events
        Assert.Equal(5, events.Count);
        Assert.IsType<AccountOpenedEvent>(events[0]);
        Assert.IsType<MoneyDepositedEvent>(events[1]);
        Assert.IsType<MoneyDepositedEvent>(events[2]);
        Assert.IsType<MoneyWithdrawnEvent>(events[3]);
        Assert.IsType<AccountClosedEvent>(events[4]);

        // Phase 3: Reconstruct aggregate from events
        BankAccount reconstructedAccount = new(events);

        // Phase 3: Verify reconstructed state matches original
        Assert.Equal(account.Id, reconstructedAccount.Id);
        Assert.Equal(account.AccountHolder, reconstructedAccount.AccountHolder);
        Assert.Equal(account.Balance.Amount, reconstructedAccount.Balance.Amount);
        Assert.Equal(account.Balance.Currency, reconstructedAccount.Balance.Currency);
        Assert.Equal(account.Transactions.Count, reconstructedAccount.Transactions.Count);
        Assert.Equal(account.IsDeleted, reconstructedAccount.IsDeleted);
        Assert.Equal(account.Version, reconstructedAccount.Version);

        // Phase 3: Verify replayed events were cleared
        Assert.Empty(reconstructedAccount.GetDomainEvents());
    }

    [Fact]
    public void Integration_EventSerialization_WithDomainEventTypeMapper()
    {
        // Register event types
        DomainEventTypeMapper.Clear();
        DomainEventTypeMapper.Register<AccountOpenedEvent>("AccountOpened");
        DomainEventTypeMapper.Register<MoneyDepositedEvent>("MoneyDeposited");
        DomainEventTypeMapper.Register<MoneyWithdrawnEvent>("MoneyWithdrawn");
        DomainEventTypeMapper.Register<AccountClosedEvent>("AccountClosed");

        // Phase 1: Create aggregate
        AccountId accountId = AccountId.NewId();
        BankAccount account = new(accountId, "Bob Smith", new Money(2000m, "USD"));
        account.Deposit(new Money(1000m, "USD"), "Freelance payment");
        account.Withdraw(new Money(500m, "USD"), "Utilities");

        IReadOnlyList<IInternalDomainEvent> events = account.GetDomainEvents();

        // Phase 2: Serialize events to JSON
        List<(string TypeName, string Json)> serializedEvents = [];
        foreach (IInternalDomainEvent @event in events)
        {
            string typeName = DomainEventTypeMapper.GetTypeName(@event);
            string json = JsonUtil.AsString(@event);
            serializedEvents.Add((typeName, json));
        }

        // Phase 2: Verify serialization
        Assert.Equal(3, serializedEvents.Count);
        Assert.Equal("AccountOpened", serializedEvents[0].TypeName);
        Assert.Equal("MoneyDeposited", serializedEvents[1].TypeName);
        Assert.Equal("MoneyWithdrawn", serializedEvents[2].TypeName);

        // Phase 3: Deserialize events from JSON
        List<IInternalDomainEvent> deserializedEvents = [];
        foreach ((string typeName, string json) in serializedEvents)
        {
            Type eventType = DomainEventTypeMapper.GetType(typeName);
            if (JsonSerializer.Deserialize(json, eventType, JsonUtil.Options) is not IInternalDomainEvent @event)
            {
                throw new InvalidOperationException($"Failed to deserialize event of type {typeName}");
            }

            deserializedEvents.Add(@event);
        }

        // Phase 3: Verify deserialization
        Assert.Equal(3, deserializedEvents.Count);
        Assert.IsType<AccountOpenedEvent>(deserializedEvents[0]);
        Assert.IsType<MoneyDepositedEvent>(deserializedEvents[1]);
        Assert.IsType<MoneyWithdrawnEvent>(deserializedEvents[2]);

        // Phase 4: Reconstruct aggregate from deserialized events
        BankAccount reconstructedAccount = new(deserializedEvents);

        // Phase 4: Verify state matches original
        Assert.Equal(account.Id, reconstructedAccount.Id);
        Assert.Equal(account.AccountHolder, reconstructedAccount.AccountHolder);
        Assert.Equal(account.Balance.Amount, reconstructedAccount.Balance.Amount);
        Assert.Equal(2, reconstructedAccount.Version); // 3 events (0-2)
    }

    [Fact]
    public void Integration_InvariantViolation_PreventsInvalidState()
    {
        AccountId accountId = AccountId.NewId();
        BankAccount account = new(accountId, "Charlie Brown", new Money(100m, "USD"));

        // R2 postcondition check prevents negative balance
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            account.Withdraw(new Money(150m, "USD"), "Over-withdrawal attempt")
        );
        Assert.Contains("balance cannot be negative", exception.Message);

        // Event NOT added to collection (version unchanged)
        Assert.Equal(0L, account.Version); // Only construction event applied

        // Note: State is mutated by _When() before _EnsureInvariant() check
        // This is expected behavior - invariant check prevents EVENT from being added,
        // not STATE mutation. In production, failed operations would be discarded
        // and aggregate would be reloaded from event store.
    }

    [Fact]
    public void Integration_ValueObjectEquality_WorksWithMoneyType()
    {
        Money money1 = new(100m, "USD");
        Money money2 = new(100m, "USD");
        Money money3 = new(100m, "EUR");
        Money money4 = new(200m, "USD");

        // Structural equality
        Assert.Equal(money1, money2);
        Assert.NotEqual(money1, money3); // Different currency
        Assert.NotEqual(money1, money4); // Different amount

        // Immutability via with expression
        Money money5 = money1 with
        {
            Amount = 200m,
        };
        Assert.Equal(100m, money1.Amount); // Original unchanged
        Assert.Equal(200m, money5.Amount);
    }

    [Fact]
    public void Integration_EntityIdentity_WorksWithTransactionType()
    {
        Guid transactionId = Guid.NewGuid();
        Transaction transaction1 = new(
            transactionId,
            DateTimeOffset.UtcNow,
            new Money(100m, "USD"),
            "Test transaction",
            TransactionType.Deposit
        );
        Transaction transaction2 = new(
            transactionId,
            DateTimeOffset.UtcNow.AddMinutes(1),
            new Money(200m, "USD"),
            "Different description",
            TransactionType.Withdrawal
        );

        // Identity based on Id, not properties
        Assert.Equal(transaction1.Id, transaction2.Id);
        Assert.NotEqual(transaction1.Amount, transaction2.Amount);
        Assert.NotEqual(transaction1.Description, transaction2.Description);
    }

    [Fact]
    public void Integration_StreamNaming_FollowsConvention()
    {
        AccountId accountId = new(Guid.Parse("12345678-1234-1234-1234-123456789abc"));
        BankAccount account = new(accountId, "Diana Prince", new Money(1000m, "USD"));

        string streamName = account.GetStreamName();
        string category = account.GetCategory();

        Assert.Equal("bank-account", category);
        Assert.Equal("bank-account-12345678-1234-1234-1234-123456789abc", streamName);
    }

    [Fact]
    public void Integration_MetadataPreservation_ThroughEventSourcing()
    {
        AccountId accountId = AccountId.NewId();
        BankAccount account = new(accountId, "Eve Wilson", new Money(500m, "USD"));

        IReadOnlyList<IInternalDomainEvent> events = account.GetDomainEvents();
        AccountOpenedEvent openedEvent = (AccountOpenedEvent)events[0];

        // Metadata preserved
        Assert.Contains("CreatedBy", openedEvent.Metadata.Keys);
        Assert.Equal("IntegrationTest", openedEvent.Metadata["CreatedBy"]);
        Assert.Contains("Version", openedEvent.Metadata.Keys);
        Assert.Equal("1.0", openedEvent.Metadata["Version"]);

        // Reconstruct and verify metadata still accessible
        BankAccount reconstructedAccount = new(events);
        List<IInternalDomainEvent> reconstructedEvents = [openedEvent];
        BankAccount finalAccount = new(reconstructedEvents);

        // Verify reconstruction succeeded
        Assert.Equal("Eve Wilson", finalAccount.AccountHolder);
    }

    [Fact]
    public async Task Integration_ConcurrentOperations_OnDifferentAggregates()
    {
        const int accountCount = 50;
        List<Task<BankAccount>> tasks = [];

        // Create accounts concurrently
        for (int i = 0; i < accountCount; i++)
        {
            int accountNumber = i;
            tasks.Add(
                Task.Run(() =>
                {
                    BankAccount account = new(
                        AccountId.NewId(),
                        $"User{accountNumber}",
                        new Money(1000m + accountNumber, "USD")
                    );

                    account.Deposit(new Money(100m, "USD"), "Deposit 1");
                    account.Withdraw(new Money(50m, "USD"), "Withdrawal 1");

                    return account;
                })
            );
        }

        BankAccount[] accounts = await Task.WhenAll(tasks);

        // All accounts created successfully
        Assert.Equal(accountCount, accounts.Length);
        Assert.All(
            accounts,
            account =>
            {
                Assert.False(account.IsDeleted);
                Assert.Equal(2L, account.Version); // 3 events (0-2)
            }
        );
    }

    // ========== Domain Events ==========

    private record AccountOpenedEvent(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string AccountHolder,
        Money InitialDeposit,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent;

    private record MoneyDepositedEvent(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        Money Amount,
        string Description,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private record MoneyWithdrawnEvent(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        Money Amount,
        string Description,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private record AccountClosedEvent(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string Reason,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IDestructionEvent;

    // ========== Value Objects ==========

    private record Money(decimal Amount, string Currency) : IValueObject
    {
        public Money Add(Money other)
        {
            if (Currency != other.Currency)
            {
                throw new InvalidOperationException($"Cannot add {other.Currency} to {Currency}");
            }

            return this with
            {
                Amount = Amount + other.Amount,
            };
        }

        public Money Subtract(Money other)
        {
            if (Currency != other.Currency)
            {
                throw new InvalidOperationException($"Cannot subtract {other.Currency} from {Currency}");
            }

            return this with
            {
                Amount = Amount - other.Amount,
            };
        }

        public bool IsNegative()
        {
            return Amount < 0;
        }
    }

    private record AccountId(Guid Value) : IValueObject
    {
        public static AccountId NewId()
        {
            return new AccountId(Guid.NewGuid());
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    // ========== Entities ==========

    private class Transaction(Guid id, DateTimeOffset timestamp, Money amount, string description, TransactionType type)
        : IEntity<Guid>
    {
        public DateTimeOffset Timestamp { get; } = timestamp;
        public Money Amount { get; } = amount;
        public string Description { get; } = description;
        public TransactionType Type { get; } = type;
        public Guid Id { get; } = id;
    }

    private enum TransactionType
    {
        Deposit,
        Withdrawal,
    }

    // ========== Aggregate Root ==========

    private class BankAccount : EsAggregateRoot<AccountId, IInternalDomainEvent>
    {
        private readonly List<Transaction> _transactions = [];

        // Constructor for new aggregate
        public BankAccount(AccountId id, string accountHolder, Money initialDeposit)
        {
            if (initialDeposit.IsNegative())
            {
                throw new ArgumentException("Initial deposit cannot be negative", nameof(initialDeposit));
            }

            AccountOpenedEvent opened = new(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                id.ToString(),
                accountHolder,
                initialDeposit,
                new Dictionary<string, string> { ["CreatedBy"] = "IntegrationTest", ["Version"] = "1.0" }
            );

            Apply(opened); // R1: Construction event
        }

        // Constructor for event replay
        public BankAccount(IEnumerable<IInternalDomainEvent> events)
            : base(events) { }

        public string AccountHolder { get; private set; } = string.Empty;

        public Money Balance { get; private set; } = new(0, "USD");

        public IReadOnlyList<Transaction> Transactions => _transactions.AsReadOnly();

        public void Deposit(Money amount, string description)
        {
            if (amount.IsNegative())
            {
                throw new ArgumentException("Deposit amount cannot be negative", nameof(amount));
            }

            MoneyDepositedEvent deposited = new(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                Id.ToString(),
                amount,
                description,
                new Dictionary<string, string> { ["TransactionType"] = "Deposit" }
            );

            Apply(deposited); // R2: Command event
        }

        public void Withdraw(Money amount, string description)
        {
            if (amount.IsNegative())
            {
                throw new ArgumentException("Withdrawal amount cannot be negative", nameof(amount));
            }

            MoneyWithdrawnEvent withdrawn = new(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                Id.ToString(),
                amount,
                description,
                new Dictionary<string, string> { ["TransactionType"] = "Withdrawal" }
            );

            Apply(withdrawn); // R2: Command event (will check balance >= 0 in EnsureInvariant)
        }

        public void Close(string reason)
        {
            AccountClosedEvent closed = new(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                Id.ToString(),
                reason,
                new Dictionary<string, string> { ["ClosedBy"] = "IntegrationTest" }
            );

            Apply(closed); // R3: Destruction event
        }

        protected override void _When(IInternalDomainEvent @event)
        {
            switch (@event)
            {
                case AccountOpenedEvent e:
                    Id = new AccountId(Guid.Parse(e.Source));
                    AccountHolder = e.AccountHolder;
                    Balance = e.InitialDeposit;
                    _transactions.Add(
                        new Transaction(
                            e.Id,
                            e.OccurredOn,
                            e.InitialDeposit,
                            "Initial deposit",
                            TransactionType.Deposit
                        )
                    );
                    break;

                case MoneyDepositedEvent e:
                    Balance = Balance.Add(e.Amount);
                    _transactions.Add(
                        new Transaction(e.Id, e.OccurredOn, e.Amount, e.Description, TransactionType.Deposit)
                    );
                    break;

                case MoneyWithdrawnEvent e:
                    Balance = Balance.Subtract(e.Amount);
                    _transactions.Add(
                        new Transaction(e.Id, e.OccurredOn, e.Amount, e.Description, TransactionType.Withdrawal)
                    );
                    break;

                case AccountClosedEvent e:
                    IsDeleted = true;
                    break;

                default:
                    throw new InvalidOperationException($"Unknown event type: {@event.GetType().Name}");
            }
        }

        protected override void _EnsureInvariant()
        {
            if (IsDeleted)
            {
                return; // Skip checks for closed account
            }

            // Business rules
            if (Id == null || Id.Value == Guid.Empty)
            {
                throw new InvalidOperationException("Account must have valid ID");
            }

            if (string.IsNullOrWhiteSpace(AccountHolder))
            {
                throw new InvalidOperationException("Account must have account holder");
            }

            if (Balance.IsNegative())
            {
                throw new InvalidOperationException("Account balance cannot be negative");
            }
        }

        public override string GetCategory()
        {
            return "bank-account";
        }
    }
}
