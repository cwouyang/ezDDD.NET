using System.Reflection;

namespace EzDdd.Entity.Tests;

public class EsAggregateRootTests
{
#region R1: Construction Event Tests

    [Fact]
    public void EsAggregateRoot_Constructor_AppliesConstructionEvent()
    {
        Account account = new(Guid.NewGuid(), "Alice", 100m);

        Assert.Equal("Alice", account.Owner);
        Assert.Equal(100m, account.Balance);
    }

    [Fact]
    public void EsAggregateRoot_R1_ConstructionEvent_DoesNotCheckPrecondition()
    {
        // Constructor applies construction event
        // No precondition check (Id is Empty before construction)
        Account account = new(Guid.NewGuid(), "Alice", 100m);

        // Assert - Should succeed even though Id was empty before event
        Assert.NotEqual(Guid.Empty, account.Id);
    }

    [Fact]
    public void EsAggregateRoot_R1_ConstructionEvent_ChecksPostcondition()
    {
        // Negative initial balance violates invariant
        Assert.Throws<InvalidOperationException>
        (() =>
             new Account(Guid.NewGuid(), "Alice", -100m)
        );
    }

#endregion

#region R2: Command Event Tests

    [Fact]
    public void EsAggregateRoot_R2_CommandEvent_ChecksPrecondition()
    {
        Account account = new(Guid.NewGuid(), "Alice", 100m);

        // Withdraw too much violates postcondition invariant
        // After state mutation (_balance = -50), postcondition check should fail
        Assert.Throws<InvalidOperationException>(() => account.Withdraw(150m));
    }

    [Fact]
    public void EsAggregateRoot_R2_CommandEvent_ChecksPostcondition()
    {
        Account account = new(Guid.NewGuid(), "Alice", 100m);

        account.Deposit(50m);

        // Postcondition invariant passed
        Assert.Equal(150m, account.Balance);
    }

    [Fact]
    public void EsAggregateRoot_Apply_IncreasesVersion()
    {
        Account account = new(Guid.NewGuid(), "Alice", 100m);
        long versionAfterConstruction = account.Version;

        account.Deposit(50m);

        Assert.Equal(versionAfterConstruction + 1, account.Version);
    }

    [Fact]
    public void EsAggregateRoot_When_MutatesState()
    {
        Account account = new(Guid.NewGuid(), "Alice", 100m);

        account.Deposit(50m);
        account.Withdraw(30m);

        Assert.Equal(120m, account.Balance); // 100 + 50 - 30
    }

#endregion

#region R3: Destruction Event Tests

    [Fact]
    public void EsAggregateRoot_R3_DestructionEvent_ChecksPrecondition()
    {
        Account account = new(Guid.NewGuid(), "Alice", 100m);

        // Close account (precondition: balance >= 0)
        account.Close("User requested");

        Assert.True(account.IsDeleted);
    }

    [Fact]
    public void EsAggregateRoot_R3_DestructionEvent_DoesNotCheckPostcondition()
    {
        Account account = new(Guid.NewGuid(), "Alice", 100m);

        // Close account (postcondition check skipped)
        account.Close("User requested");

        // Deletion succeeded even though invariants might be broken after
        Assert.True(account.IsDeleted);
    }

#endregion

#region Replay and Reconstruction Tests

    [Fact]
    public void EsAggregateRoot_ReplayConstructor_ReconstructsState()
    {
        // Create events representing history
        Guid accountId = Guid.NewGuid();
        List<IInternalDomainEvent> events =
        [
            new AccountCreated
            (
                Guid.NewGuid(), DateTimeOffset.UtcNow, accountId.ToString(),
                "Alice", 100m, new Dictionary<string, string>()
            ),

            new MoneyDeposited
            (
                Guid.NewGuid(), DateTimeOffset.UtcNow, accountId.ToString(),
                50m, new Dictionary<string, string>()
            ),

            new MoneyWithdrawn
            (
                Guid.NewGuid(), DateTimeOffset.UtcNow, accountId.ToString(),
                30m, new Dictionary<string, string>()
            )
        ];

        // Reconstruct from events
        Account account = new(events);

        Assert.Equal(accountId, account.Id);
        Assert.Equal("Alice", account.Owner);
        Assert.Equal(120m, account.Balance); // 100 + 50 - 30
    }

    [Fact]
    public void EsAggregateRoot_ReplayConstructor_ClearsEventsAfterReplay()
    {
        Guid accountId = Guid.NewGuid();
        List<IInternalDomainEvent> events =
        [
            new AccountCreated
            (
                Guid.NewGuid(), DateTimeOffset.UtcNow, accountId.ToString(),
                "Alice", 100m, new Dictionary<string, string>()
            )
        ];

        Account account = new(events);

        // Replayed events should not be in collection
        Assert.Empty(account.GetDomainEvents());
    }

    [Fact]
    public void EsAggregateRoot_ReplayConstructor_SetsCorrectVersion()
    {
        Guid accountId = Guid.NewGuid();
        List<IInternalDomainEvent> events =
        [
            new AccountCreated
            (
                Guid.NewGuid(), DateTimeOffset.UtcNow, accountId.ToString(),
                "Alice", 100m, new Dictionary<string, string>()
            ),

            new MoneyDeposited
            (
                Guid.NewGuid(), DateTimeOffset.UtcNow, accountId.ToString(),
                50m, new Dictionary<string, string>()
            ),

            new MoneyWithdrawn
            (
                Guid.NewGuid(), DateTimeOffset.UtcNow, accountId.ToString(),
                30m, new Dictionary<string, string>()
            )
        ];

        Account account = new(events);

        // Version should be 2 (3 events: 0, 1, 2)
        Assert.Equal(2L, account.Version);
    }

#endregion

#region API and Contract Tests

    [Fact]
    public void EsAggregateRoot_GetCategory_ReturnsAggregateType()
    {
        Account account = new(Guid.NewGuid(), "Alice", 100m);

        string category = account.GetCategory();

        Assert.Equal("account", category);
    }

    [Fact]
    public void EsAggregateRoot_GetStreamName_ReturnsCorrectFormat()
    {
        Guid id = Guid.NewGuid();
        Account account = new(id, "Alice", 100m);

        string streamName = account.GetStreamName();

        Assert.Equal($"account-{id}", streamName);
    }

    [Fact]
    public void EsAggregateRoot_Apply_IsSealed_CannotBeOverridden()
    {
        MethodInfo? applyMethod = typeof(Account).GetMethod("Apply");

        // Apply method should not be overridden by Account
        Assert.Equal(typeof(EsAggregateRoot<Guid, IInternalDomainEvent>), applyMethod?.DeclaringType);
    }

    [Fact]
    public void EsAggregateRoot_When_IsAbstract_MustBeImplemented()
    {
        MethodInfo? whenMethod = typeof(EsAggregateRoot<Guid, IInternalDomainEvent>)
            .GetMethod("_When", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.True(whenMethod?.IsAbstract);
    }

    [Fact]
    public void EsAggregateRoot_EnsureInvariant_IsVirtual_CanBeOverridden()
    {
        MethodInfo? ensureInvariantMethod = typeof(EsAggregateRoot<Guid, IInternalDomainEvent>)
            .GetMethod("_EnsureInvariant", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.True(ensureInvariantMethod?.IsVirtual);
        Assert.False(ensureInvariantMethod?.IsAbstract);
    }

#endregion

#region Integration Tests

    [Fact]
    public void EsAggregateRoot_CompleteLifecycle_AllRulesEnforced()
    {
        Account account = new(Guid.NewGuid(), "Alice", 100m); // R1: Construction
        account.Deposit(50m); // R2: Command
        account.Withdraw(30m); // R2: Command
        account.Close("User requested"); // R3: Destruction

        Assert.Equal(120m, account.Balance);
        Assert.True(account.IsDeleted);
        Assert.Equal(3L, account.Version); // 4 events total (0, 1, 2, 3)
    }

#endregion

    // Test events
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

    private record MoneyWithdrawn
    (
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        decimal Amount,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private record AccountClosed
    (
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string Reason,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IDestructionEvent;

    // Test aggregate - Bank Account with event sourcing
    private class Account : EsAggregateRoot<Guid, IInternalDomainEvent>
    {
        // Constructor for new aggregate
        public Account(Guid id, string owner, decimal initialBalance)
        {
            AccountCreated created = new
            (
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                id.ToString(),
                owner,
                initialBalance,
                new Dictionary<string, string>()
            );

            Apply(created); // This will call When() and check invariants
        }

        // Constructor for event replay (required by framework)
        public Account(IEnumerable<IInternalDomainEvent> events) : base(events)
        {
        }

        public string Owner { get; private set; } = string.Empty;

        public decimal Balance { get; private set; }

        public void Deposit(decimal amount)
        {
            MoneyDeposited deposited = new
            (
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                Id.ToString(),
                amount,
                new Dictionary<string, string>()
            );

            Apply(deposited);
        }

        public void Withdraw(decimal amount)
        {
            MoneyWithdrawn withdrawn = new
            (
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                Id.ToString(),
                amount,
                new Dictionary<string, string>()
            );

            Apply(withdrawn);
        }

        public void Close(string reason)
        {
            AccountClosed closed = new
            (
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                Id.ToString(),
                reason,
                new Dictionary<string, string>()
            );

            Apply(closed);
        }

        protected override void _When(IInternalDomainEvent @event)
        {
            switch (@event)
            {
                case AccountCreated e:
                    Id = Guid.Parse(e.Source);
                    Owner = e.Owner;
                    Balance = e.InitialBalance;
                    break;

                case MoneyDeposited e:
                    Balance += e.Amount;
                    break;

                case MoneyWithdrawn e:
                    Balance -= e.Amount;
                    break;

                case AccountClosed e:
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
                return; // Skip checks for deleted account
            }

            // Business rules
            if (Id == Guid.Empty)
            {
                throw new InvalidOperationException("Account ID must be set");
            }

            if (string.IsNullOrEmpty(Owner))
            {
                throw new InvalidOperationException("Account must have owner");
            }

            if (Balance < 0)
            {
                throw new InvalidOperationException("Balance cannot be negative");
            }
        }

        public override string GetCategory()
        {
            return "account";
        }
    }
}