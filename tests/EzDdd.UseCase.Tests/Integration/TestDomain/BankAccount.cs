using EzDdd.Entity;

namespace EzDdd.UseCase.Tests.Integration.TestDomain;

/// <summary>
///     Bank account aggregate root (event-sourced).
/// </summary>
public sealed class BankAccount : EsAggregateRoot<AccountId, IInternalDomainEvent>
{
    // Constructor for creation
    public BankAccount(AccountId id, string owner, Money initialBalance)
    {
        Id = id;
        AccountCreated @event = new
        (
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            id,
            owner,
            initialBalance
        );
        Apply(@event);
    }

    // Constructor for event replay
    public BankAccount(IEnumerable<IInternalDomainEvent> events)
        : base(events)
    {
    }

    // Properties for testing
    public string Owner { get; private set; } = string.Empty;

    public Money Balance { get; private set; } = new(0);

    public bool IsClosed { get; private set; }

    public void Deposit(Money amount)
    {
        if (amount.Amount <= 0)
        {
            throw new InvalidOperationException("Deposit amount must be positive");
        }

        MoneyDeposited @event = new
        (
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Id,
            amount
        );
        Apply(@event);
    }

    public void Withdraw(Money amount)
    {
        if (amount.Amount <= 0)
        {
            throw new InvalidOperationException("Withdrawal amount must be positive");
        }

        MoneyWithdrawn @event = new
        (
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Id,
            amount
        );
        Apply(@event);
    }

    public void Close(string reason)
    {
        if (IsClosed)
        {
            throw new InvalidOperationException("Account is already closed");
        }

        AccountClosed @event = new
        (
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Id,
            reason
        );
        Apply(@event);
    }

    protected override void _When(IInternalDomainEvent @event)
    {
        switch (@event)
        {
            case AccountCreated created:
                Id = created.AccountId; // Set Id during event replay
                Owner = created.Owner;
                Balance = created.InitialBalance;
                IsClosed = false;
                break;

            case MoneyDeposited deposited:
                Balance = Balance.Add(deposited.Amount);
                break;

            case MoneyWithdrawn withdrawn:
                Balance = Balance.Subtract(withdrawn.Amount);
                break;

            case AccountClosed closed:
                IsClosed = true;
                break;

            default:
                throw new InvalidOperationException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    protected override void _EnsureInvariant()
    {
        // Skip invariant checks for closed accounts
        if (IsClosed)
        {
            return;
        }

        // Business rule: Balance must not be negative
        if (Balance.Amount < 0)
        {
            throw new InvalidOperationException($"Account balance cannot be negative: {Balance}");
        }

        // Business rule: Owner must not be empty
        if (string.IsNullOrWhiteSpace(Owner))
        {
            throw new InvalidOperationException("Account owner cannot be empty");
        }
    }

    public override string GetCategory()
    {
        return "account";
    }
}