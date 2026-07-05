# ezDDD.NET Usage Examples

Practical examples demonstrating how to use ezDDD.NET in real-world scenarios.

> **Version**: 1.0.0-alpha.1
> **Last Updated**: 2026-07-05

---

## Table of Contents

- [Basic Examples](#basic-examples)
  - [Creating a Simple Aggregate](#creating-a-simple-aggregate)
  - [Defining Value Objects](#defining-value-objects)
  - [Raising Domain Events](#raising-domain-events)
- [Event Sourcing Examples](#event-sourcing-examples)
  - [Event-Sourced Aggregate (BankAccount)](#event-sourced-aggregate-bankaccount)
  - [Event Replay and State Reconstruction](#event-replay-and-state-reconstruction)
  - [R1/R2/R3 Invariant Rules](#r1r2r3-invariant-rules)
  - [When() Method with Pattern Matching](#when-method-with-pattern-matching)
  - [EsRepository Usage](#esrepository-usage)
- [State Sourcing Examples](#state-sourcing-examples)
  - [State-Sourced Aggregate with Outbox](#state-sourced-aggregate-with-outbox)
  - [Transactional Outbox Pattern](#transactional-outbox-pattern)
  - [OutboxRepository Implementation](#outboxrepository-implementation)
  - [OutboxMapper Implementation](#outboxmapper-implementation)
- [CQRS Examples](#cqrs-examples)
  - [Command Side (Write Model)](#command-side-write-model)
  - [Query Side (Read Model)](#query-side-read-model)
  - [Projection and Projector](#projection-and-projector)
  - [Complete CQRS Flow](#complete-cqrs-flow)
  - [CqrsOutput Fluent API](#cqrsoutput-fluent-api)
- [Real-World Scenarios](#real-world-scenarios)
  - [Banking System (Event Sourcing)](#banking-system-event-sourcing)
- [System Reconciliation Examples](#system-reconciliation-examples)
  - [Cleanup Reconciler with Context](#cleanup-reconciler-with-context)
  - [Global Reconciler with NullContext](#global-reconciler-with-nullcontext)
  - [Scheduling with BackgroundService](#scheduling-with-backgroundservice)
  - [Scheduling with Hangfire](#scheduling-with-hangfire)

---

## Basic Examples

### Creating a Simple Aggregate

**Scenario**: Create a basic aggregate root without event sourcing.

**Key Concepts**:
- AggregateRoot base class
- Domain events
- Versioning

**Complete Code**:

```csharp
using EzDdd.Entity;

// Define the aggregate ID
public sealed record OrderId(string Value) : IValueObject;

// Define domain events
public sealed record OrderCreated
(
    Guid Id,
    DateTimeOffset OccurredOn,
    OrderId Source,
    string CustomerName,
    decimal InitialTotal
) : IInternalDomainEvent
{
    string IDomainEvent.Source => Source.Value;
    IReadOnlyDictionary<string, string> IDomainEvent.Metadata => new Dictionary<string, string>();
}

// Define the aggregate
public sealed class Order : AggregateRoot<OrderId, IInternalDomainEvent>
{
    private decimal _totalAmount;

    public Order(OrderId id, string customerName)
    {
        Id = id;
        CustomerName = customerName;
        _totalAmount = 0;

        // Raise domain event
        var @event = new OrderCreated(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            id,
            customerName,
            0);

        Apply(@event);
    }

    public string CustomerName { get; private set; }
    public decimal TotalAmount => _totalAmount;

    public void AddItem(decimal itemPrice)
    {
        _totalAmount += itemPrice;
        // Could raise OrderItemAdded event here
    }
}

// Usage
var orderId = new OrderId("ORD-001");
var order = new Order(orderId, "John Doe");
order.AddItem(99.99m);

Console.WriteLine($"Order {order.Id.Value} for {order.CustomerName}");
Console.WriteLine($"Total: ${order.TotalAmount}");
Console.WriteLine($"Version: {order.Version}");
Console.WriteLine($"Events raised: {order.GetDomainEvents().Count}");
```

**Explanation**:

1. **Define Aggregate ID**: Use record types with `IValueObject` for type-safe IDs
2. **Define Events**: Records implementing `IInternalDomainEvent` for immutability
3. **Extend AggregateRoot**: Inherit from `AggregateRoot<TId, TEvent>`
4. **Raise Events**: Call `Apply(@event)` to add events and increment version

**Output**:
```
Order ORD-001 for John Doe
Total: $99.99
Version: 0
Events raised: 1
```

**Notes**:
- ✅ Use record types for immutable events and value objects
- ✅ Events are automatically collected via `Apply()`
- ✅ Version starts at -1 and increments with each event (after N events: `Version = N - 1`)
- ⚠️ Simple aggregates don't need event sourcing - use `AggregateRoot` directly

---

### Defining Value Objects

**Scenario**: Create immutable value objects with domain logic.

**Key Concepts**:
- Record types
- Value-based equality
- Domain operations

**Complete Code**:

```csharp
using EzDdd.Entity;

// Money value object with currency
public sealed record Money(decimal Amount, string Currency = "USD") : IValueObject
{
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot add different currencies: {Currency} and {other.Currency}");
        }

        return this with { Amount = Amount + other.Amount };
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot subtract different currencies: {Currency} and {other.Currency}");
        }

        return this with { Amount = Amount - other.Amount };
    }

    public bool IsPositive() => Amount > 0;
    public bool IsNegative() => Amount < 0;
    public bool IsZero() => Amount == 0;

    public override string ToString() => $"{Amount:F2} {Currency}";
}

// Email value object with validation
public sealed record Email : IValueObject
{
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        if (!value.Contains("@"))
            throw new ArgumentException("Invalid email format", nameof(value));

        Value = value.ToLowerInvariant();
    }

    public string Value { get; }

    public override string ToString() => Value;
}

// Address value object with complex structure
public sealed record Address
(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country = "USA"
) : IValueObject
{
    public string FullAddress => $"{Street}, {City}, {State} {ZipCode}, {Country}";
}

// Usage
var usd100 = new Money(100m, "USD");
var usd50 = new Money(50m, "USD");
var total = usd100.Add(usd50);
Console.WriteLine($"Total: {total}"); // Total: 150.00 USD

var email = new Email("john.doe@example.com");
Console.WriteLine($"Email: {email}"); // Email: john.doe@example.com

var address = new Address(
    "123 Main St",
    "San Francisco",
    "CA",
    "94102"
);
Console.WriteLine($"Address: {address.FullAddress}");
```

**Explanation**:

1. **Record Types**: Use `record` for automatic value-based equality
2. **Validation**: Enforce invariants in constructor or factory method
3. **Immutability**: All properties are init-only or readonly
4. **Domain Logic**: Add business operations (Add, Subtract, etc.)

**Output**:
```
Total: 150.00 USD
Email: john.doe@example.com
Address: 123 Main St, San Francisco, CA 94102, USA
```

**Notes**:
- ✅ Records provide automatic value equality
- ✅ Validate in constructor to ensure valid state
- ✅ Use `with` expressions for immutable updates
- ⚠️ Keep value objects small and focused

---

### Raising Domain Events

**Scenario**: Raise and collect domain events from aggregates.

**Key Concepts**:
- Event creation
- Event collection
- Event metadata

**Complete Code**:

```csharp
using EzDdd.Entity;

// Define multiple event types
public sealed record CustomerCreated
(
    Guid Id,
    DateTimeOffset OccurredOn,
    CustomerId Source,
    string Name,
    Email Email
) : IInternalDomainEvent
{
    string IDomainEvent.Source => Source.Value;
    IReadOnlyDictionary<string, string> IDomainEvent.Metadata => new Dictionary<string, string>();
}

public sealed record CustomerEmailChanged
(
    Guid Id,
    DateTimeOffset OccurredOn,
    CustomerId Source,
    Email OldEmail,
    Email NewEmail
) : IInternalDomainEvent
{
    string IDomainEvent.Source => Source.Value;

    // Add metadata for audit trail
    IReadOnlyDictionary<string, string> IDomainEvent.Metadata => new Dictionary<string, string>
    {
        ["ChangedAt"] = OccurredOn.ToString("O"),
        ["Reason"] = "Customer request"
    };
}

public sealed record CustomerId(string Value) : IValueObject;

// Aggregate that raises events
public sealed class Customer : AggregateRoot<CustomerId, IInternalDomainEvent>
{
    private Email _email = null!;

    public Customer(CustomerId id, string name, Email email)
    {
        Id = id;
        Name = name;
        _email = email;

        // Raise creation event
        var @event = new CustomerCreated(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            id,
            name,
            email);

        Apply(@event);
    }

    public string Name { get; private set; } = string.Empty;
    public Email Email => _email;

    public void ChangeEmail(Email newEmail)
    {
        if (_email.Equals(newEmail))
            return; // No change

        var oldEmail = _email;
        _email = newEmail;

        // Raise change event
        var @event = new CustomerEmailChanged(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Id,
            oldEmail,
            newEmail);

        Apply(@event);
    }
}

// Usage
var customerId = new CustomerId("CUST-001");
var email = new Email("john@example.com");
var customer = new Customer(customerId, "John Doe", email);

Console.WriteLine($"Version: {customer.Version}"); // 0
Console.WriteLine($"Events: {customer.GetDomainEvents().Count}"); // 1

customer.ChangeEmail(new Email("john.doe@example.com"));

Console.WriteLine($"Version: {customer.Version}"); // 1
Console.WriteLine($"Events: {customer.GetDomainEvents().Count}"); // 2

// Examine events
foreach (var evt in customer.GetDomainEvents())
{
    Console.WriteLine($"Event: {evt.GetType().Name}");
    Console.WriteLine($"  ID: {evt.Id}");
    Console.WriteLine($"  Source: {evt.Source}");
    Console.WriteLine($"  OccurredOn: {evt.OccurredOn:u}");
    foreach (var (key, value) in evt.Metadata)
    {
        Console.WriteLine($"  Metadata[{key}]: {value}");
    }
}

// Clear events (typically done after persistence)
customer.ClearDomainEvents();
Console.WriteLine($"Events after clear: {customer.GetDomainEvents().Count}"); // 0
```

**Explanation**:

1. **Event Creation**: Create events with required properties (Id, OccurredOn, Source)
2. **Apply Events**: Call `Apply(@event)` to add to collection
3. **Get Events**: Use `GetDomainEvents()` to retrieve raised events
4. **Clear Events**: Call `ClearDomainEvents()` after persistence

**Output**:
```
Version: 0
Events: 1
Version: 1
Events: 2
Event: CustomerCreated
  ID: [guid]
  Source: CUST-001
  OccurredOn: [timestamp]
Event: CustomerEmailChanged
  ID: [guid]
  Source: CUST-001
  OccurredOn: [timestamp]
  Metadata[ChangedAt]: [timestamp]
  Metadata[Reason]: Customer request
Events after clear: 0
```

**Notes**:
- ✅ Events are immutable records
- ✅ Use metadata for additional context
- ✅ Clear events after successful save to avoid duplicate publishing
- ⚠️ Don't clear events if save fails - allows retry

---

## Event Sourcing Examples

### Event-Sourced Aggregate (BankAccount)

**Scenario**: Complete event-sourced aggregate with full event sourcing pattern.

**Key Concepts**:
- EsAggregateRoot base class
- Construction/Command/Destruction events
- Event replay constructor
- Invariant checking

**Complete Code**:

```csharp
using EzDdd.Entity;

// Define aggregate ID
// (override ToString so GetStreamName() produces "account-ACC-001")
public sealed record AccountId(string Value) : IValueObject
{
    public override string ToString() => Value;
}

// Define domain events
public sealed record AccountCreated
(
    Guid Id,
    DateTimeOffset OccurredOn,
    AccountId Source,
    string Owner,
    Money InitialBalance
) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent
{
    string IDomainEvent.Source => Source.Value;
    IReadOnlyDictionary<string, string> IDomainEvent.Metadata => new Dictionary<string, string>();
}

public sealed record MoneyDeposited
(
    Guid Id,
    DateTimeOffset OccurredOn,
    AccountId Source,
    Money Amount
) : IInternalDomainEvent
{
    string IDomainEvent.Source => Source.Value;
    IReadOnlyDictionary<string, string> IDomainEvent.Metadata => new Dictionary<string, string>();
}

public sealed record MoneyWithdrawn
(
    Guid Id,
    DateTimeOffset OccurredOn,
    AccountId Source,
    Money Amount
) : IInternalDomainEvent
{
    string IDomainEvent.Source => Source.Value;
    IReadOnlyDictionary<string, string> IDomainEvent.Metadata => new Dictionary<string, string>();
}

public sealed record AccountClosed
(
    Guid Id,
    DateTimeOffset OccurredOn,
    AccountId Source,
    string Reason
) : IInternalDomainEvent, IInternalDomainEvent.IDestructionEvent
{
    string IDomainEvent.Source => Source.Value;
    IReadOnlyDictionary<string, string> IDomainEvent.Metadata => new Dictionary<string, string>();
}

// Event-sourced aggregate
public sealed class BankAccount : EsAggregateRoot<AccountId, IInternalDomainEvent>
{
    // Constructor for new account creation
    public BankAccount(AccountId id, string owner, Money initialBalance)
    {
        Id = id;
        var @event = new AccountCreated(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            id,
            owner,
            initialBalance);
        Apply(@event);
    }

    // Constructor for event replay (REQUIRED for event sourcing)
    public BankAccount(IEnumerable<IInternalDomainEvent> events)
        : base(events)
    {
    }

    // State properties
    public string Owner { get; private set; } = string.Empty;
    public Money Balance { get; private set; } = new(0);
    public bool IsClosed { get; private set; }

    // Business methods
    public void Deposit(Money amount)
    {
        if (amount.Amount <= 0)
            throw new InvalidOperationException("Deposit amount must be positive");

        var @event = new MoneyDeposited(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Id,
            amount);
        Apply(@event);
    }

    public void Withdraw(Money amount)
    {
        if (amount.Amount <= 0)
            throw new InvalidOperationException("Withdrawal amount must be positive");

        var @event = new MoneyWithdrawn(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Id,
            amount);
        Apply(@event);
    }

    public void Close(string reason)
    {
        if (IsClosed)
            throw new InvalidOperationException("Account is already closed");

        var @event = new AccountClosed(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Id,
            reason);
        Apply(@event);
    }

    // State mutation (pattern matching)
    protected override void _When(IInternalDomainEvent @event)
    {
        switch (@event)
        {
            case AccountCreated created:
                Id = created.Source;
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
                throw new InvalidOperationException(
                    $"Unknown event type: {@event.GetType().Name}");
        }
    }

    // Invariant checking (business rules)
    protected override void _EnsureInvariant()
    {
        if (IsClosed)
            return; // Skip invariants for closed accounts

        if (Balance.Amount < 0)
            throw new InvalidOperationException(
                $"Account balance cannot be negative: {Balance}");

        if (string.IsNullOrWhiteSpace(Owner))
            throw new InvalidOperationException("Account owner cannot be empty");
    }

    // Stream naming
    public override string GetCategory() => "account";
}

// Usage
var accountId = new AccountId("ACC-001");
var initialBalance = new Money(1000m);

// Create new account
var account = new BankAccount(accountId, "John Doe", initialBalance);

// Perform operations
account.Deposit(new Money(500m));
account.Withdraw(new Money(200m));

Console.WriteLine($"Owner: {account.Owner}");
Console.WriteLine($"Balance: {account.Balance}");
Console.WriteLine($"Version: {account.Version}");
Console.WriteLine($"Events: {account.GetDomainEvents().Count}");
Console.WriteLine($"Stream: {account.GetStreamName()}");
```

**Explanation**:

1. **Construction Event**: `AccountCreated` implements `IConstructionEvent` marker
2. **Destruction Event**: `AccountClosed` implements `IDestructionEvent` marker
3. **Replay Constructor**: Required for reconstructing from event stream
4. **_When() Method**: Handles state mutations for each event type
5. **_EnsureInvariant()**: Validates business rules before/after events
6. **GetCategory()**: Returns stream name prefix

**Output**:
```
Owner: John Doe
Balance: 1300.00 USD
Version: 2
Events: 3
Stream: account-ACC-001
```

**Notes**:
- ✅ First event MUST implement IConstructionEvent
- ✅ Last event (deletion) MUST implement IDestructionEvent
- ✅ Replay constructor MUST accept IEnumerable<TEvent>
- ✅ _When() ONLY mutates state (no business logic)
- ✅ _EnsureInvariant() checks business rules
- ⚠️ Skip invariants for deleted aggregates

---

### Event Replay and State Reconstruction

**Scenario**: Demonstrate how event sourcing reconstructs aggregate state from events.

**Key Concepts**:
- Event replay
- State reconstruction
- Version tracking

**Complete Code**:

```csharp
using EzDdd.Entity;

// Using BankAccount from previous example

// Simulate event stream from event store
var accountId = new AccountId("ACC-002");
var eventStream = new List<IInternalDomainEvent>
{
    new AccountCreated(
        Guid.NewGuid(),
        DateTimeOffset.Parse("2025-01-01T10:00:00Z"),
        accountId,
        "Jane Smith",
        new Money(5000m)),

    new MoneyDeposited(
        Guid.NewGuid(),
        DateTimeOffset.Parse("2025-01-02T14:30:00Z"),
        accountId,
        new Money(1500m)),

    new MoneyWithdrawn(
        Guid.NewGuid(),
        DateTimeOffset.Parse("2025-01-03T09:15:00Z"),
        accountId,
        new Money(2000m)),

    new MoneyDeposited(
        Guid.NewGuid(),
        DateTimeOffset.Parse("2025-01-04T16:45:00Z"),
        accountId,
        new Money(800m))
};

// Reconstruct aggregate from event stream
var account = new BankAccount(eventStream);

Console.WriteLine("=== Account Reconstructed from Events ===");
Console.WriteLine($"Account ID: {account.Id.Value}");
Console.WriteLine($"Owner: {account.Owner}");
Console.WriteLine($"Balance: {account.Balance}");
Console.WriteLine($"Version: {account.Version}");
Console.WriteLine($"IsClosed: {account.IsClosed}");
Console.WriteLine($"Domain Events (after replay): {account.GetDomainEvents().Count}");

Console.WriteLine("\n=== Event History ===");
foreach (var evt in eventStream)
{
    var eventType = evt.GetType().Name;
    var timestamp = evt.OccurredOn.ToString("yyyy-MM-dd HH:mm:ss");

    var details = evt switch
    {
        AccountCreated created =>
            $"Created account for {created.Owner} with ${created.InitialBalance.Amount}",
        MoneyDeposited deposited =>
            $"Deposited ${deposited.Amount.Amount}",
        MoneyWithdrawn withdrawn =>
            $"Withdrew ${withdrawn.Amount.Amount}",
        _ => "Unknown event"
    };

    Console.WriteLine($"[{timestamp}] {eventType}: {details}");
}

// Demonstrate state calculation
Console.WriteLine("\n=== Balance Calculation ===");
Console.WriteLine("5000 (initial) + 1500 (deposit) - 2000 (withdraw) + 800 (deposit) = 5300");
Console.WriteLine($"Actual balance: {account.Balance}");
```

**Explanation**:

1. **Event Stream**: Represents historical events from event store
2. **Replay Constructor**: Invoked with event stream to rebuild state
3. **State Reconstruction**: Each event is applied via `_When()` method
4. **Version Tracking**: Version starts at -1 and increments with each event (after N events: `Version = N - 1`)
5. **No Domain Events**: Replayed events are cleared (not re-published)

**Output**:
```
=== Account Reconstructed from Events ===
Account ID: ACC-002
Owner: Jane Smith
Balance: 5300.00 USD
Version: 3
IsClosed: False
Domain Events (after replay): 0

=== Event History ===
[2025-01-01 10:00:00] AccountCreated: Created account for Jane Smith with $5000
[2025-01-02 14:30:00] MoneyDeposited: Deposited $1500
[2025-01-03 09:15:00] MoneyWithdrawn: Withdrew $2000
[2025-01-04 16:45:00] MoneyDeposited: Deposited $800

=== Balance Calculation ===
5000 (initial) + 1500 (deposit) - 2000 (withdraw) + 800 (deposit) = 5300
Actual balance: 5300.00 USD
```

**Notes**:
- ✅ Events are applied in chronological order
- ✅ Invariants are checked during replay
- ✅ Replayed events are cleared after reconstruction
- ⚠️ Invalid event stream throws exception during replay

---

### R1/R2/R3 Invariant Rules

**Scenario**: Demonstrate the three event sourcing correctness rules.

**Key Concepts**:
- R1: Construction rule (no precondition check)
- R2: Command rule (check before and after)
- R3: Destruction rule (no postcondition check)

**Complete Code**:

```csharp
using EzDdd.Entity;

// Using BankAccount from previous examples

Console.WriteLine("=== R1: Construction Rule ===");
Console.WriteLine("Rule: {pre₀} fun₀ {post₀ & INV}");
Console.WriteLine("No precondition check, postcondition + invariant checked\n");

try
{
    // Valid construction (passes postcondition + invariant)
    var account1 = new BankAccount(
        new AccountId("ACC-R1-VALID"),
        "Alice",
        new Money(1000m));
    Console.WriteLine("✅ Valid construction succeeded");
    Console.WriteLine($"   Owner: {account1.Owner}, Balance: {account1.Balance}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Construction failed: {ex.Message}");
}

try
{
    // Invalid construction (negative balance violates invariant)
    var account2 = new BankAccount(
        new AccountId("ACC-R1-INVALID"),
        "Bob",
        new Money(-100m));
    Console.WriteLine("❌ Should have failed but didn't!");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine("✅ Invalid construction correctly rejected");
    Console.WriteLine($"   Error: {ex.Message}");
}

Console.WriteLine("\n=== R2: Command Rule ===");
Console.WriteLine("Rule: {preₜ & INV} funₜ {postₜ & INV}");
Console.WriteLine("Both precondition and postcondition + invariants checked\n");

var account3 = new BankAccount(
    new AccountId("ACC-R2"),
    "Charlie",
    new Money(1000m));

try
{
    // Valid command (passes pre + post invariants)
    account3.Withdraw(new Money(300m));
    Console.WriteLine("✅ Valid withdraw succeeded");
    Console.WriteLine($"   Balance after withdraw: {account3.Balance}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Withdraw failed: {ex.Message}");
}

try
{
    // Invalid command (would violate invariant)
    account3.Withdraw(new Money(2000m));
    Console.WriteLine("❌ Should have failed but didn't!");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine("✅ Invalid withdraw correctly rejected");
    Console.WriteLine($"   Error: {ex.Message}");
}

Console.WriteLine("\n=== R3: Destruction Rule ===");
Console.WriteLine("Rule: {preᵤ & INV} funᵤ {postᵤ}");
Console.WriteLine("Precondition + invariant checked, no postcondition check\n");

try
{
    // Close account (destruction event)
    account3.Close("Account closure requested");
    Console.WriteLine("✅ Account closed successfully");
    Console.WriteLine($"   IsClosed: {account3.IsClosed}");
    Console.WriteLine($"   Balance: {account3.Balance} (can be non-zero after close)");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Close failed: {ex.Message}");
}

try
{
    // Try to close again (violates precondition)
    account3.Close("Already closed");
    Console.WriteLine("❌ Should have failed but didn't!");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine("✅ Double close correctly rejected");
    Console.WriteLine($"   Error: {ex.Message}");
}

Console.WriteLine("\n=== Event Replay with Rules ===");
var eventStream = new List<IInternalDomainEvent>
{
    new AccountCreated(Guid.NewGuid(), DateTimeOffset.UtcNow,
        new AccountId("ACC-R4"), "Diana", new Money(500m)),
    new MoneyDeposited(Guid.NewGuid(), DateTimeOffset.UtcNow,
        new AccountId("ACC-R4"), new Money(200m)),
    new AccountClosed(Guid.NewGuid(), DateTimeOffset.UtcNow,
        new AccountId("ACC-R4"), "Test closure")
};

var account4 = new BankAccount(eventStream);
Console.WriteLine("✅ Replay successful with all rules enforced");
Console.WriteLine($"   Final state: Owner={account4.Owner}, " +
                  $"Balance={account4.Balance}, IsClosed={account4.IsClosed}");
```

**Explanation**:

1. **R1 (Construction)**: First event has NO precondition check (aggregate doesn't exist yet), but postcondition and invariants ARE checked
2. **R2 (Command)**: Middle events have BOTH precondition and postcondition invariant checks
3. **R3 (Destruction)**: Last event has precondition check but NO postcondition check (aggregate is being deleted)
4. **Template Method**: `Apply()` is sealed and enforces these rules automatically

**Output**:
```
=== R1: Construction Rule ===
Rule: {pre₀} fun₀ {post₀ & INV}
No precondition check, postcondition + invariant checked

✅ Valid construction succeeded
   Owner: Alice, Balance: 1000.00 USD
✅ Invalid construction correctly rejected
   Error: Account balance cannot be negative: -100.00 USD

=== R2: Command Rule ===
Rule: {preₜ & INV} funₜ {postₜ & INV}
Both precondition and postcondition + invariants checked

✅ Valid withdraw succeeded
   Balance after withdraw: 700.00 USD
✅ Invalid withdraw correctly rejected
   Error: Account balance cannot be negative: -1300.00 USD

=== R3: Destruction Rule ===
Rule: {preᵤ & INV} funᵤ {postᵤ}
Precondition + invariant checked, no postcondition check

✅ Account closed successfully
   IsClosed: True
   Balance: 700.00 USD (can be non-zero after close)
✅ Double close correctly rejected
   Error: Account is already closed

=== Event Replay with Rules ===
✅ Replay successful with all rules enforced
   Final state: Owner=Diana, Balance=700.00 USD, IsClosed=True
```

**Notes**:
- ✅ R1: No precondition (aggregate doesn't exist)
- ✅ R2: Full invariant checking before and after
- ✅ R3: No postcondition (aggregate is deleted)
- ✅ Rules enforced automatically by `Apply()`
- ⚠️ Mark construction events with `IConstructionEvent`
- ⚠️ Mark destruction events with `IDestructionEvent`

---

### When() Method with Pattern Matching

**Scenario**: Implement state mutations using modern C# pattern matching.

**Key Concepts**:
- Pattern matching with switch
- Type patterns
- Property patterns
- Guard clauses

**Complete Code**:

```csharp
using EzDdd.Entity;

// Extended example with more event types
public sealed record PaymentProcessed
(
    Guid Id,
    DateTimeOffset OccurredOn,
    AccountId Source,
    Money Amount,
    string PaymentMethod
) : IInternalDomainEvent
{
    string IDomainEvent.Source => Source.Value;
    IReadOnlyDictionary<string, string> IDomainEvent.Metadata => new Dictionary<string, string>();
}

public sealed record InterestEarned
(
    Guid Id,
    DateTimeOffset OccurredOn,
    AccountId Source,
    Money Amount,
    decimal InterestRate
) : IInternalDomainEvent
{
    string IDomainEvent.Source => Source.Value;
    IReadOnlyDictionary<string, string> IDomainEvent.Metadata => new Dictionary<string, string>();
}

public sealed class EnhancedBankAccount : EsAggregateRoot<AccountId, IInternalDomainEvent>
{
    private int _transactionCount;
    private DateTimeOffset _lastTransactionDate;

    public EnhancedBankAccount(AccountId id, string owner, Money initialBalance)
    {
        Id = id;
        var @event = new AccountCreated(
            Guid.NewGuid(), DateTimeOffset.UtcNow, id, owner, initialBalance);
        Apply(@event);
    }

    public EnhancedBankAccount(IEnumerable<IInternalDomainEvent> events)
        : base(events) { }

    public string Owner { get; private set; } = string.Empty;
    public Money Balance { get; private set; } = new(0);
    public bool IsClosed { get; private set; }
    public int TransactionCount => _transactionCount;
    public DateTimeOffset LastTransactionDate => _lastTransactionDate;

    protected override void _When(IInternalDomainEvent @event)
    {
        // Pattern 1: Basic type pattern
        switch (@event)
        {
            case AccountCreated created:
                Id = created.Source;
                Owner = created.Owner;
                Balance = created.InitialBalance;
                IsClosed = false;
                _transactionCount = 0;
                _lastTransactionDate = created.OccurredOn;
                break;

            // Pattern 2: Type pattern with guard clause
            case MoneyDeposited deposited when deposited.Amount.Amount > 0:
                Balance = Balance.Add(deposited.Amount);
                _transactionCount++;
                _lastTransactionDate = deposited.OccurredOn;
                break;

            // Pattern 3: Type pattern for withdrawal
            case MoneyWithdrawn withdrawn:
                Balance = Balance.Subtract(withdrawn.Amount);
                _transactionCount++;
                _lastTransactionDate = withdrawn.OccurredOn;
                break;

            // Pattern 4: Type pattern with property pattern
            case PaymentProcessed { PaymentMethod: "CreditCard" } payment:
                Balance = Balance.Subtract(payment.Amount);
                _transactionCount++;
                _lastTransactionDate = payment.OccurredOn;
                // Could apply credit card fee here
                break;

            case PaymentProcessed payment:
                Balance = Balance.Subtract(payment.Amount);
                _transactionCount++;
                _lastTransactionDate = payment.OccurredOn;
                break;

            // Pattern 5: Type pattern with deconstruction
            case InterestEarned interest:
                Balance = Balance.Add(interest.Amount);
                _lastTransactionDate = interest.OccurredOn;
                // Interest doesn't count as transaction
                break;

            case AccountClosed:
                IsClosed = true;
                break;

            // Pattern 6: Default case for unknown events
            default:
                throw new InvalidOperationException(
                    $"Unknown event type: {@event.GetType().Name}");
        }
    }

    protected override void _EnsureInvariant()
    {
        if (IsClosed) return;

        if (Balance.Amount < 0)
            throw new InvalidOperationException("Balance cannot be negative");

        if (string.IsNullOrWhiteSpace(Owner))
            throw new InvalidOperationException("Owner cannot be empty");
    }

    public override string GetCategory() => "enhanced-account";
}

// Demonstrate different pattern matching styles
var accountId = new AccountId("ACC-PM-001");
var events = new List<IInternalDomainEvent>
{
    new AccountCreated(
        Guid.NewGuid(), DateTimeOffset.UtcNow,
        accountId, "Pattern Matcher", new Money(1000m)),

    new MoneyDeposited(
        Guid.NewGuid(), DateTimeOffset.UtcNow,
        accountId, new Money(500m)),

    new PaymentProcessed(
        Guid.NewGuid(), DateTimeOffset.UtcNow,
        accountId, new Money(200m), "CreditCard"),

    new InterestEarned(
        Guid.NewGuid(), DateTimeOffset.UtcNow,
        accountId, new Money(25m), 0.05m)
};

var account = new EnhancedBankAccount(events);

Console.WriteLine("=== Pattern Matching Results ===");
Console.WriteLine($"Owner: {account.Owner}");
Console.WriteLine($"Balance: {account.Balance}");
Console.WriteLine($"Transactions: {account.TransactionCount}");
Console.WriteLine($"Last Transaction: {account.LastTransactionDate:u}");
Console.WriteLine($"Version: {account.Version}");
```

**Explanation**:

1. **Basic Type Pattern**: `case AccountCreated created:` - matches type and binds variable
2. **Guard Clause**: `case MoneyDeposited deposited when deposited.Amount.Amount > 0:` - additional condition
3. **Property Pattern**: `case PaymentProcessed { PaymentMethod: "CreditCard" }` - matches property value
4. **Deconstruction**: Can deconstruct records in pattern
5. **Default Case**: Always throw for unknown events (fail fast)

**Output**:
```
=== Pattern Matching Results ===
Owner: Pattern Matcher
Balance: 1325.00 USD
Transactions: 2
Last Transaction: [timestamp]
Version: 3
```

**Notes**:
- ✅ Use switch expressions for cleaner code
- ✅ Guard clauses filter specific cases
- ✅ Property patterns match on nested properties
- ✅ Always have default case that throws
- ⚠️ Keep _When() pure (no business logic)

---

### EsRepository Usage

**Scenario**: Use EsRepository to persist and load event-sourced aggregates.

**Key Concepts**:
- EsRepository
- IRepositoryPeer
- Event persistence
- Aggregate reconstruction

**Complete Code**:

```csharp
using EzDdd.Entity;
using EzDdd.UseCase.Port.Out;
using System.Collections.Concurrent;

// In-memory event store peer for testing
public sealed class InMemoryEventStorePeer : IRepositoryPeer<EventStoreData<AccountId>, AccountId>
{
    private readonly ConcurrentDictionary<string, EventStoreData<AccountId>> _store = new();

    public Task<EventStoreData<AccountId>?> FindByIdAsync(AccountId id)
    {
        _store.TryGetValue(id.Value, out EventStoreData<AccountId>? data);
        return Task.FromResult(data);
    }

    public Task SaveAsync(EventStoreData<AccountId> data)
    {
        // On save, data.Events contains only the aggregate's PENDING events.
        // Event sourcing: append them to the already-stored stream.
        if (_store.TryGetValue(data.Id.Value, out EventStoreData<AccountId>? existing))
        {
            var allEvents = new List<IDomainEvent>(existing.Events);
            allEvents.AddRange(data.Events);

            _store[data.Id.Value] = new EventStoreData<AccountId>
            {
                Id = data.Id,
                Version = data.Version,
                Events = allEvents,
                StreamName = data.StreamName,
            };
        }
        else
        {
            _store[data.Id.Value] = data; // First save: store as-is
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(EventStoreData<AccountId> data)
    {
        _store.TryRemove(data.Id.Value, out _);
        return Task.CompletedTask;
    }
}

// Demo: Complete event sourcing flow
public static async Task DemoEventSourcingRepositoryAsync()
{
    // Register event types for serialization
    DomainEventTypeMapper.Register<AccountCreated>("AccountCreated");
    DomainEventTypeMapper.Register<MoneyDeposited>("MoneyDeposited");
    DomainEventTypeMapper.Register<MoneyWithdrawn>("MoneyWithdrawn");

    // Create repository with event store peer
    var peer = new InMemoryEventStorePeer();
    var repository = new EsRepository<BankAccount, AccountId>(peer);

    Console.WriteLine("=== Event Sourcing Repository Demo ===\n");

    // Step 1: Create and save new aggregate
    Console.WriteLine("Step 1: Create and save new account");
    var accountId = new AccountId("ACC-ES-001");
    var account = new BankAccount(accountId, "Event Sourcer", new Money(1000m));

    account.Deposit(new Money(500m));
    account.Withdraw(new Money(200m));

    Console.WriteLine($"Before save - Version: {account.Version}, Events: {account.GetDomainEvents().Count}");

    await repository.SaveAsync(account);

    Console.WriteLine($"After save - Version: {account.Version}, Events: {account.GetDomainEvents().Count}");
    Console.WriteLine("(Events cleared after successful save)\n");

    // Step 2: Load aggregate from event store
    Console.WriteLine("Step 2: Load account from event store");
    var loaded = await repository.FindByIdAsync(accountId);

    if (loaded != null)
    {
        Console.WriteLine($"Loaded - Owner: {loaded.Owner}");
        Console.WriteLine($"Loaded - Balance: {loaded.Balance}");
        Console.WriteLine($"Loaded - Version: {loaded.Version}");
        Console.WriteLine($"Loaded - Events: {loaded.GetDomainEvents().Count}");
        Console.WriteLine("(Events cleared after replay)\n");
    }

    // Step 3: Modify and save again
    Console.WriteLine("Step 3: Modify and save again");
    loaded!.Deposit(new Money(300m));

    Console.WriteLine($"Before save - Version: {loaded.Version}, Events: {loaded.GetDomainEvents().Count}");

    await repository.SaveAsync(loaded);

    Console.WriteLine($"After save - Version: {loaded.Version}, Events: {loaded.GetDomainEvents().Count}\n");

    // Step 4: Reload to verify persistence
    Console.WriteLine("Step 4: Reload to verify all events persisted");
    var reloaded = await repository.FindByIdAsync(accountId);

    if (reloaded != null)
    {
        Console.WriteLine($"Final - Owner: {reloaded.Owner}");
        Console.WriteLine($"Final - Balance: {reloaded.Balance}");
        Console.WriteLine($"Final - Version: {reloaded.Version}");
        Console.WriteLine($"Stream Name: {reloaded.GetStreamName()}");
    }

    // Step 5: Non-existent aggregate
    Console.WriteLine("\nStep 5: Load non-existent account");
    var notFound = await repository.FindByIdAsync(new AccountId("DOES-NOT-EXIST"));
    Console.WriteLine($"Result: {(notFound == null ? "null (as expected)" : "Found (unexpected!)")}");
}

// Run the demo
await DemoEventSourcingRepositoryAsync();
```

**Explanation**:

1. **Register Event Types**: Use `DomainEventTypeMapper` for serialization
2. **Create Repository**: Inject peer implementation
3. **Save Flow**: Events → EventStoreData → Peer → Storage
4. **Load Flow**: Storage → Peer → EventStoreData → Events → Aggregate
5. **Event Clearing**: Automatic after save/load

**Output**:
```
=== Event Sourcing Repository Demo ===

Step 1: Create and save new account
Before save - Version: 2, Events: 3
After save - Version: 2, Events: 0
(Events cleared after successful save)

Step 2: Load account from event store
Loaded - Owner: Event Sourcer
Loaded - Balance: 1300.00 USD
Loaded - Version: 2
Loaded - Events: 0
(Events cleared after replay)

Step 3: Modify and save again
Before save - Version: 3, Events: 1
After save - Version: 3, Events: 0

Step 4: Reload to verify all events persisted
Final - Owner: Event Sourcer
Final - Balance: 1600.00 USD
Final - Version: 3
Stream Name: account-ACC-ES-001

Step 5: Load non-existent account
Result: null (as expected)
```

**Notes**:
- ✅ Repository automatically reconstructs from events
- ✅ Events cleared after successful save
- ✅ Constructor caching for performance
- ✅ Null return for non-existent aggregates
- ⚠️ Peer handles transaction boundaries

---

## State Sourcing Examples

### State-Sourced Aggregate with Outbox

**Scenario**: Create aggregate using state sourcing instead of event sourcing.

**Key Concepts**:
- AggregateRoot (not EsAggregateRoot)
- Current state persistence
- Transactional Outbox pattern

**Complete Code**:

```csharp
using EzDdd.Entity;

// State-sourced Order aggregate
public sealed record OrderId(string Value) : IValueObject;

public sealed record OrderCreated
(
    Guid Id,
    DateTimeOffset OccurredOn,
    OrderId Source,
    string CustomerName,
    decimal InitialTotal
) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent
{
    string IDomainEvent.Source => Source.Value;
    IReadOnlyDictionary<string, string> IDomainEvent.Metadata => new Dictionary<string, string>();
}

public sealed record OrderItemAdded
(
    Guid Id,
    DateTimeOffset OccurredOn,
    OrderId Source,
    string ProductName,
    int Quantity,
    decimal Price
) : IInternalDomainEvent
{
    string IDomainEvent.Source => Source.Value;
    IReadOnlyDictionary<string, string> IDomainEvent.Metadata => new Dictionary<string, string>();
}

public sealed record OrderConfirmed
(
    Guid Id,
    DateTimeOffset OccurredOn,
    OrderId Source
) : IInternalDomainEvent
{
    string IDomainEvent.Source => Source.Value;
    IReadOnlyDictionary<string, string> IDomainEvent.Metadata => new Dictionary<string, string>();
}

public sealed class OrderItem
{
    public OrderItem(string productName, int quantity, decimal price)
    {
        ProductName = productName;
        Quantity = quantity;
        Price = price;
    }

    public string ProductName { get; }
    public int Quantity { get; }
    public decimal Price { get; }
    public decimal Subtotal => Quantity * Price;
}

public enum OrderStatus { Draft, Confirmed, Cancelled }

// State-sourced aggregate (extends AggregateRoot, not EsAggregateRoot)
public sealed class Order : AggregateRoot<OrderId, IInternalDomainEvent>
{
    private readonly List<OrderItem> _items = [];

    // Parameterless constructor for OutboxMapper reconstruction
    public Order() { }

    // Constructor for creation
    public Order(OrderId id, string customerName)
    {
        Id = id;
        CustomerName = customerName;
        TotalAmount = 0;
        Status = OrderStatus.Draft;

        var @event = new OrderCreated(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            id,
            customerName,
            0);
        Apply(@event);
    }

    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    public void AddItem(string productName, int quantity, decimal price)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException(
                $"Cannot add items to order in {Status} status");

        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be positive");

        if (price < 0)
            throw new InvalidOperationException("Price cannot be negative");

        var item = new OrderItem(productName, quantity, price);
        _items.Add(item);
        TotalAmount += item.Subtotal;

        var @event = new OrderItemAdded(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Id,
            productName,
            quantity,
            price);
        Apply(@event);
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException(
                $"Cannot confirm order in {Status} status");

        if (_items.Count == 0)
            throw new InvalidOperationException("Cannot confirm order with no items");

        Status = OrderStatus.Confirmed;

        var @event = new OrderConfirmed(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Id);
        Apply(@event);
    }
}

// Usage
var orderId = new OrderId("ORD-SS-001");
var order = new Order(orderId, "State Sourcer");

order.AddItem("Widget A", 2, 29.99m);
order.AddItem("Widget B", 1, 49.99m);
order.Confirm();

Console.WriteLine("=== State-Sourced Order ===");
Console.WriteLine($"Order ID: {order.Id.Value}");
Console.WriteLine($"Customer: {order.CustomerName}");
Console.WriteLine($"Status: {order.Status}");
Console.WriteLine($"Items: {order.Items.Count}");
Console.WriteLine($"Total: ${order.TotalAmount:F2}");
Console.WriteLine($"Version: {order.Version}");
Console.WriteLine($"Events: {order.GetDomainEvents().Count}");

Console.WriteLine("\n=== Order Items ===");
foreach (var item in order.Items)
{
    Console.WriteLine($"{item.ProductName} x{item.Quantity} @ ${item.Price:F2} = ${item.Subtotal:F2}");
}
```

**Explanation**:

1. **AggregateRoot**: Use `AggregateRoot` (not `EsAggregateRoot`) for state sourcing
2. **Parameterless Constructor**: Required for OutboxMapper to reconstruct state
3. **Public Setters**: Properties need setters for mapper to restore state
4. **Current State**: Entire object state is persisted, not just events
5. **Events Still Raised**: Events published to message broker for integration

**Output**:
```
=== State-Sourced Order ===
Order ID: ORD-SS-001
Customer: State Sourcer
Status: Confirmed
Items: 2
Total: $109.97
Version: 2
Events: 3

=== Order Items ===
Widget A x2 @ $29.99 = $59.98
Widget B x1 @ $49.99 = $49.99
```

**Notes**:
- ✅ Faster reads (no event replay needed)
- ✅ Simpler implementation than event sourcing
- ✅ Events still raised for integration
- ⚠️ Requires parameterless constructor
- ⚠️ Properties need public setters
- ⚠️ No automatic audit trail like event sourcing

---

### Transactional Outbox Pattern

**Scenario**: Demonstrate atomic persistence of state and events.

**Key Concepts**:
- Outbox pattern
- Atomic writes
- Event publication
- Transaction boundary

**Complete Code**:

```csharp
using EzDdd.Entity;
using EzDdd.UseCase.Port.Out;

// Outbox data structure (implements all IOutboxData<TId> members)
public sealed class OrderData : IOutboxData<OrderId>
{
    // IStoreData members
    public OrderId Id { get; set; } = null!;
    public long Version { get; set; }
    public string StreamName { get; set; } = string.Empty;
    public IReadOnlyList<IDomainEvent> Events { get; set; } = Array.Empty<IDomainEvent>();

    // Aggregate state
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public List<OrderItemData> Items { get; set; } = [];
}

public sealed class OrderItemData
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

// Demonstrate transactional outbox
Console.WriteLine("=== Transactional Outbox Pattern ===\n");

// Step 1: Create order with multiple operations
var orderId = new OrderId("ORD-OUTBOX-001");
var order = new Order(orderId, "Outbox User");

order.AddItem("Product A", 3, 15.00m);
order.AddItem("Product B", 1, 25.00m);

Console.WriteLine("Step 1: Order created with 2 items");
Console.WriteLine($"Events before save: {order.GetDomainEvents().Count}");
Console.WriteLine($"Version: {order.Version}");

// Step 2: Convert to outbox data
var mapper = new OrderMapper(); // See OutboxMapper example for implementation
var outboxData = mapper.ToData(order);

Console.WriteLine("\nStep 2: Convert to outbox data");
Console.WriteLine($"Outbox events: {outboxData.Events.Count}");
Console.WriteLine($"Outbox version: {outboxData.Version}");

// Step 3: Simulate atomic transaction
Console.WriteLine("\nStep 3: Atomic transaction (pseudocode):");
Console.WriteLine("""
    BEGIN TRANSACTION

    -- Write aggregate state
    INSERT INTO orders (id, customer_name, total_amount, status, version)
    VALUES (@Id, @CustomerName, @TotalAmount, @Status, @Version)

    -- Write order items
    INSERT INTO order_items (order_id, product_name, quantity, price)
    VALUES ...

    -- Write outbox events (for async publication)
    INSERT INTO outbox_events (aggregate_id, event_type, event_data, version)
    VALUES ...

    COMMIT TRANSACTION
    """);

// Step 4: Show atomicity guarantee
Console.WriteLine("\nStep 4: Atomicity guarantee");
Console.WriteLine("✅ Both state AND events persisted in single transaction");
Console.WriteLine("✅ Either both succeed or both rollback");
Console.WriteLine("✅ No partial writes possible");
Console.WriteLine("✅ Events available for async publication");

// Step 5: Event publication from outbox
Console.WriteLine("\nStep 5: Async event publication (separate process):");
Console.WriteLine("""
    -- Background worker polls outbox
    SELECT * FROM outbox_events WHERE published = false

    -- For each event:
    --   1. Publish to message broker
    --   2. Mark as published
    --   3. Update last_published_at

    UPDATE outbox_events
    SET published = true, last_published_at = NOW()
    WHERE id = @EventId
    """);

Console.WriteLine("\n=== Benefits ===");
Console.WriteLine("✅ Guaranteed event delivery (at-least-once)");
Console.WriteLine("✅ No lost events due to message broker failures");
Console.WriteLine("✅ Can retry failed event publications");
Console.WriteLine("✅ Audit trail of all published events");
```

**Explanation**:

1. **Outbox Data**: Contains both aggregate state AND events
2. **Atomic Write**: Single transaction writes both state and events
3. **Event Publication**: Separate async process publishes from outbox
4. **At-Least-Once**: Events guaranteed to be published eventually
5. **Transaction Boundary**: At peer level, NOT repository level

**Output**:
```
=== Transactional Outbox Pattern ===

Step 1: Order created with 2 items
Events before save: 3
Version: 2

Step 2: Convert to outbox data
Outbox events: 3
Outbox version: 2

Step 3: Atomic transaction (pseudocode):
    BEGIN TRANSACTION

    -- Write aggregate state
    INSERT INTO orders (id, customer_name, total_amount, status, version)
    VALUES (@Id, @CustomerName, @TotalAmount, @Status, @Version)

    -- Write order items
    INSERT INTO order_items (order_id, product_name, quantity, price)
    VALUES ...

    -- Write outbox events (for async publication)
    INSERT INTO outbox_events (aggregate_id, event_type, event_data, version)
    VALUES ...

    COMMIT TRANSACTION

Step 4: Atomicity guarantee
✅ Both state AND events persisted in single transaction
✅ Either both succeed or both rollback
✅ No partial writes possible
✅ Events available for async publication

Step 5: Async event publication (separate process):
    -- Background worker polls outbox
    SELECT * FROM outbox_events WHERE published = false

    -- For each event:
    --   1. Publish to message broker
    --   2. Mark as published
    --   3. Update last_published_at

    UPDATE outbox_events
    SET published = true, last_published_at = NOW()
    WHERE id = @EventId

=== Benefits ===
✅ Guaranteed event delivery (at-least-once)
✅ No lost events due to message broker failures
✅ Can retry failed event publications
✅ Audit trail of all published events
```

**Notes**:
- ✅ Atomic persistence of state + events
- ✅ Transaction at peer level
- ✅ Async event publication
- ✅ At-least-once delivery guarantee
- ⚠️ Requires background worker
- ⚠️ May need idempotent event handlers

---

### OutboxRepository Implementation

**Scenario**: Use OutboxRepository to persist state-sourced aggregates.

**Key Concepts**:
- OutboxRepository
- OutboxMapper
- IRepositoryPeer
- Atomic persistence

**Complete Code**:

```csharp
using EzDdd.Entity;
using EzDdd.UseCase.Port.Out;
using System.Collections.Concurrent;

// In-memory outbox peer for testing
public sealed class InMemoryOutboxPeer : IRepositoryPeer<OrderData, OrderId>
{
    private readonly ConcurrentDictionary<string, OrderData> _store = new();

    public Task<OrderData?> FindByIdAsync(OrderId id)
    {
        _store.TryGetValue(id.Value, out var data);
        return Task.FromResult(data);
    }

    public Task SaveAsync(OrderData data)
    {
        // In production: use database transaction to atomically save
        // both aggregate state and outbox events
        _store[data.Id.Value] = data;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(OrderData data)
    {
        _store.TryRemove(data.Id.Value, out _);
        return Task.CompletedTask;
    }
}

// Demo: Complete state sourcing flow
public static async Task DemoStateSourcingRepositoryAsync()
{
    // Register event types
    DomainEventTypeMapper.Register<OrderCreated>("OrderCreated");
    DomainEventTypeMapper.Register<OrderItemAdded>("OrderItemAdded");
    DomainEventTypeMapper.Register<OrderConfirmed>("OrderConfirmed");

    // Create repository with outbox peer and mapper
    var peer = new InMemoryOutboxPeer();
    var mapper = new OrderMapper(); // See next example
    var repository = new OutboxRepository<Order, OrderData, OrderId>(peer, mapper);

    Console.WriteLine("=== State Sourcing Repository Demo ===\n");

    // Step 1: Create and save new aggregate
    Console.WriteLine("Step 1: Create and save new order");
    var orderId = new OrderId("ORD-SS-001");
    var order = new Order(orderId, "State Sourcer");

    order.AddItem("Widget X", 2, 19.99m);
    order.AddItem("Widget Y", 1, 29.99m);

    Console.WriteLine($"Before save - Version: {order.Version}, Events: {order.GetDomainEvents().Count}");

    await repository.SaveAsync(order);

    Console.WriteLine($"After save - Version: {order.Version}, Events: {order.GetDomainEvents().Count}");
    Console.WriteLine("(Events cleared after successful save)\n");

    // Step 2: Load aggregate (state restored, events not included)
    Console.WriteLine("Step 2: Load order from storage");
    var loaded = await repository.FindByIdAsync(orderId);

    if (loaded != null)
    {
        Console.WriteLine($"Loaded - Customer: {loaded.CustomerName}");
        Console.WriteLine($"Loaded - Total: ${loaded.TotalAmount:F2}");
        Console.WriteLine($"Loaded - Items: {loaded.Items.Count}");
        Console.WriteLine($"Loaded - Version: {loaded.Version}");
        Console.WriteLine($"Loaded - Events: {loaded.GetDomainEvents().Count}");
        Console.WriteLine("(State restored, events cleared)\n");
    }

    // Step 3: Modify and save again
    Console.WriteLine("Step 3: Modify and save again");
    loaded!.Confirm();

    Console.WriteLine($"Before save - Status: {loaded.Status}, Events: {loaded.GetDomainEvents().Count}");

    await repository.SaveAsync(loaded);

    Console.WriteLine($"After save - Status: {loaded.Status}, Events: {loaded.GetDomainEvents().Count}\n");

    // Step 4: Reload to verify persistence
    Console.WriteLine("Step 4: Reload to verify state persisted");
    var reloaded = await repository.FindByIdAsync(orderId);

    if (reloaded != null)
    {
        Console.WriteLine($"Final - Customer: {reloaded.CustomerName}");
        Console.WriteLine($"Final - Status: {reloaded.Status}");
        Console.WriteLine($"Final - Total: ${reloaded.TotalAmount:F2}");
        Console.WriteLine($"Final - Version: {reloaded.Version}");
    }

    // Step 5: Non-existent aggregate
    Console.WriteLine("\nStep 5: Load non-existent order");
    var notFound = await repository.FindByIdAsync(new OrderId("DOES-NOT-EXIST"));
    Console.WriteLine($"Result: {(notFound == null ? "null (as expected)" : "Found (unexpected!)")}");
}

// Run the demo
await DemoStateSourcingRepositoryAsync();
```

**Explanation**:

1. **OutboxRepository**: Generic repository for state sourcing
2. **OutboxMapper**: Converts between aggregate and outbox data
3. **Save Flow**: Aggregate → OutboxData → Peer → Storage (atomic)
4. **Load Flow**: Storage → Peer → OutboxData → Aggregate (via mapper)
5. **Event Clearing**: Automatic after save

**Output**:
```
=== State Sourcing Repository Demo ===

Step 1: Create and save new order
Before save - Version: 2, Events: 3
After save - Version: 2, Events: 0
(Events cleared after successful save)

Step 2: Load order from storage
Loaded - Customer: State Sourcer
Loaded - Total: $69.97
Loaded - Items: 2
Loaded - Version: 2
Loaded - Events: 0
(State restored, events cleared)

Step 3: Modify and save again
Before save - Status: Confirmed, Events: 1
After save - Status: Confirmed, Events: 0

Step 4: Reload to verify state persisted
Final - Customer: State Sourcer
Final - Status: Confirmed
Final - Total: $69.97
Final - Version: 3

Step 5: Load non-existent order
Result: null (as expected)
```

**Notes**:
- ✅ Simpler than event sourcing (no replay)
- ✅ Faster loads (direct state access)
- ✅ Events still available for integration
- ✅ Transaction at peer level
- ⚠️ No automatic audit trail
- ⚠️ Requires mapper implementation

---

### OutboxMapper Implementation

**Scenario**: Implement custom mapper for aggregate <-> data conversion.

**Key Concepts**:
- OutboxMapper abstract class
- ToData() conversion
- ToDomain() conversion
- Event mapping

**Complete Code**:

```csharp
using System.Reflection;
using EzDdd.Entity;
using EzDdd.UseCase.Port.Out;

// Outbox data structures: see OrderData / OrderItemData in the
// "Transactional Outbox Pattern" example above.

// Custom mapper implementation
// (OutboxMapper declares `public abstract TData ToData(...)` and
//  `public abstract TAggregate ToDomain(...)` - override them directly)
public sealed class OrderMapper : OutboxMapper<Order, OrderData, OrderId>
{
    public override OrderData ToData(Order aggregate)
    {
        return new OrderData
        {
            Id = aggregate.Id,
            Version = aggregate.Version,
            StreamName = $"order-{aggregate.Id.Value}",
            CustomerName = aggregate.CustomerName,
            TotalAmount = aggregate.TotalAmount,
            Status = aggregate.Status,
            Items = aggregate.Items.Select(item => new OrderItemData
            {
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList(),
            // Outbox: include the pending domain events as-is
            Events = aggregate.GetDomainEvents().ToList()
        };
    }

    public override Order ToDomain(OrderData data)
    {
        // Use parameterless constructor, then restore state.
        // CustomerName/TotalAmount/Status have public setters in this example.
        var order = new Order
        {
            CustomerName = data.CustomerName,
            TotalAmount = data.TotalAmount,
            Status = data.Status
        };

        // Id and Version have protected setters on AggregateRoot -
        // restore them via their auto-property backing fields
        typeof(Order).BaseType!
            .GetField("<Id>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(order, data.Id);

        typeof(Order).BaseType!
            .GetField("<Version>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(order, data.Version);

        // Restore items (private field)
        var itemsField = typeof(Order)
            .GetField("_items",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
        var items = (List<OrderItem>)itemsField.GetValue(order)!;

        foreach (var itemData in data.Items)
        {
            items.Add(new OrderItem(
                itemData.ProductName,
                itemData.Quantity,
                itemData.Price));
        }

        // Domain events are NOT restored (already persisted)
        return order;
    }
}

// Demo: Mapper usage
var orderId = new OrderId("ORD-MAP-001");
var order = new Order(orderId, "Mapper User");
order.AddItem("Product A", 2, 15.00m);
order.Confirm();

var mapper = new OrderMapper();

Console.WriteLine("=== OutboxMapper Demo ===\n");

// ToData conversion
Console.WriteLine("Step 1: Aggregate → Data");
var data = mapper.ToData(order);
Console.WriteLine($"Data ID: {data.Id.Value}");
Console.WriteLine($"Data Customer: {data.CustomerName}");
Console.WriteLine($"Data Status: {data.Status}");
Console.WriteLine($"Data Items: {data.Items.Count}");
Console.WriteLine($"Data Events: {data.Events.Count}");
Console.WriteLine($"Data Version: {data.Version}");

// ToDomain conversion
Console.WriteLine("\nStep 2: Data → Aggregate");
var restored = mapper.ToDomain(data);
Console.WriteLine($"Restored ID: {restored.Id.Value}");
Console.WriteLine($"Restored Customer: {restored.CustomerName}");
Console.WriteLine($"Restored Status: {restored.Status}");
Console.WriteLine($"Restored Items: {restored.Items.Count}");
Console.WriteLine($"Restored Version: {restored.Version}");
Console.WriteLine($"Restored Events: {restored.GetDomainEvents().Count}");

// Verify data integrity
Console.WriteLine("\nStep 3: Verify data integrity");
Console.WriteLine($"IDs match: {order.Id.Equals(restored.Id)}");
Console.WriteLine($"Customers match: {order.CustomerName == restored.CustomerName}");
Console.WriteLine($"Status match: {order.Status == restored.Status}");
Console.WriteLine($"Totals match: {order.TotalAmount == restored.TotalAmount}");
Console.WriteLine($"Versions match: {order.Version == restored.Version}");
```

**Explanation**:

1. **ToData()**: Convert aggregate to outbox data
   - Map properties (including `Id`, `Version`, `StreamName`)
   - Map items/collections
   - Include pending events from `GetDomainEvents()`
2. **ToDomain()**: Convert outbox data to aggregate
   - Use parameterless constructor
   - Restore properties (reflection for protected `Id`/`Version`)
   - Restore collections
   - Do NOT restore events (already persisted)

**Output**:
```
=== OutboxMapper Demo ===

Step 1: Aggregate → Data
Data ID: ORD-MAP-001
Data Customer: Mapper User
Data Status: Confirmed
Data Items: 1
Data Events: 3
Data Version: 2

Step 2: Data → Aggregate
Restored ID: ORD-MAP-001
Restored Customer: Mapper User
Restored Status: Confirmed
Restored Items: 1
Restored Version: 2
Restored Events: 0

Step 3: Verify data integrity
IDs match: True
Customers match: True
Status match: True
Totals match: True
Versions match: True
```

**Notes**:
- ✅ Override the public abstract `ToData()` and `ToDomain()`
- ✅ Use parameterless constructor for reconstruction
- ✅ May need reflection for protected/private members
- ✅ Events are not restored during reconstruction
- ⚠️ Keep mappings simple and focused

---

## CQRS Examples

### Command Side (Write Model)

**Scenario**: Implement commands for write operations.

**Key Concepts**:
- ICommand interface
- IInput/IOutput
- Use case pattern
- Command execution

**Complete Code**:

```csharp
using EzDdd.Cqrs;
using EzDdd.Cqrs.Command;
using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.Out;

// Command input
public sealed record CreateAccountInput
(
    AccountId AccountId,
    string Owner,
    Money InitialBalance
) : IInput;

// Command output
// (ICommand requires TOutput : CqrsOutput<TOutput>, new() - the base class
//  already provides Id/Message/ExitCode plus the fluent Set*/Succeed/Fail API)
public sealed class CreateAccountOutput : CqrsOutput<CreateAccountOutput>
{
    public long Version { get; set; }

    public CreateAccountOutput SetVersion(long version)
    {
        Version = version;
        return this;
    }
}

// Command implementation
public sealed class CreateAccountCommand
    (IRepository<BankAccount, AccountId, IInternalDomainEvent> repository)
    : ICommand<CreateAccountInput, CreateAccountOutput>
{
    public async Task<CreateAccountOutput> ExecuteAsync(CreateAccountInput input)
    {
        try
        {
            // Create aggregate
            var account = new BankAccount(
                input.AccountId,
                input.Owner,
                input.InitialBalance);

            // Persist via repository
            await repository.SaveAsync(account);

            // Return success output (fluent API preserves the concrete type)
            return CreateAccountOutput.Create()
                .SetId(input.AccountId.Value)
                .SetVersion(account.Version)
                .SetMessage($"Account {input.AccountId.Value} created successfully")
                .Succeed();
        }
        catch (InvalidOperationException ex)
        {
            throw new UseCaseFailureException(
                $"Failed to create account: {ex.Message}", ex);
        }
    }
}

// Additional command: DepositMoneyCommand
// (IVersionedInput requires a settable Version property, so it cannot be an
//  init-only positional record parameter)
public sealed record DepositMoneyInput
(
    AccountId AccountId,
    Money Amount
) : IVersionedInput
{
    public long Version { get; set; }
}

public sealed class DepositMoneyOutput : CqrsOutput<DepositMoneyOutput>
{
    public decimal NewBalance { get; set; }
    public long Version { get; set; }

    public DepositMoneyOutput SetNewBalance(decimal newBalance)
    {
        NewBalance = newBalance;
        return this;
    }

    public DepositMoneyOutput SetVersion(long version)
    {
        Version = version;
        return this;
    }
}

public sealed class DepositMoneyCommand
    (IRepository<BankAccount, AccountId, IInternalDomainEvent> repository)
    : ICommand<DepositMoneyInput, DepositMoneyOutput>
{
    public async Task<DepositMoneyOutput> ExecuteAsync(DepositMoneyInput input)
    {
        // Load aggregate
        var account = await repository.FindByIdAsync(input.AccountId);
        if (account == null)
        {
            throw new UseCaseFailureException(
                $"Account not found: {input.AccountId.Value}");
        }

        // Check version (optimistic locking)
        if (account.Version != input.Version)
        {
            throw new UseCaseFailureException(
                $"Version mismatch: expected {input.Version}, " +
                $"actual {account.Version}");
        }

        // Execute business logic
        account.Deposit(input.Amount);

        // Persist
        await repository.SaveAsync(account);

        // Return success
        return DepositMoneyOutput.Create()
            .SetNewBalance(account.Balance.Amount)
            .SetVersion(account.Version)
            .SetMessage($"Deposited {input.Amount}")
            .Succeed();
    }
}

// Demo: Command execution
public static async Task DemoCommandsAsync()
{
    // Setup
    DomainEventTypeMapper.Register<AccountCreated>("AccountCreated");
    DomainEventTypeMapper.Register<MoneyDeposited>("MoneyDeposited");

    var peer = new InMemoryEventStorePeer();
    var repository = new EsRepository<BankAccount, AccountId>(peer);
    var createCommand = new CreateAccountCommand(repository);
    var depositCommand = new DepositMoneyCommand(repository);

    Console.WriteLine("=== CQRS Commands Demo ===\n");

    // Execute CreateAccount command
    Console.WriteLine("Command 1: CreateAccount");
    var accountId = new AccountId("ACC-CMD-001");
    var createInput = new CreateAccountInput(
        accountId,
        "Command User",
        new Money(1000m));

    var createOutput = await createCommand.ExecuteAsync(createInput);
    Console.WriteLine($"Result: {createOutput.ExitCode}");
    Console.WriteLine($"Message: {createOutput.Message}");
    Console.WriteLine($"Version: {createOutput.Version}");

    // Execute DepositMoney command
    Console.WriteLine("\nCommand 2: DepositMoney");
    var depositInput = new DepositMoneyInput(accountId, new Money(500m))
    {
        Version = createOutput.Version
    };

    var depositOutput = await depositCommand.ExecuteAsync(depositInput);
    Console.WriteLine($"Result: {depositOutput.ExitCode}");
    Console.WriteLine($"Message: {depositOutput.Message}");
    Console.WriteLine($"New Balance: ${depositOutput.NewBalance:F2}");
    Console.WriteLine($"Version: {depositOutput.Version}");
}

await DemoCommandsAsync();
```

**Explanation**:

1. **ICommand**: Extends `IUseCase` for write operations; its output type must extend `CqrsOutput<TOutput>`
2. **Input/Output**: Define contracts for command data
3. **Repository**: Commands use repository to persist changes
4. **Optimistic Locking**: Use `IVersionedInput` for concurrency control (settable `Version` property)
5. **Error Handling**: Throw `UseCaseFailureException` for business rule violations

**Output**:
```
=== CQRS Commands Demo ===

Command 1: CreateAccount
Result: Success
Message: Account ACC-CMD-001 created successfully
Version: 0

Command 2: DepositMoney
Result: Success
Message: Deposited 500.00 USD
New Balance: $1500.00
Version: 1
```

**Notes**:
- ✅ Commands modify state (write model)
- ✅ Use IVersionedInput for concurrency
- ✅ Return domain-meaningful output
- ✅ Commands may raise domain events
- ⚠️ Keep commands focused (single responsibility)

---

### Query Side (Read Model)

**Scenario**: Implement queries for read operations.

**Key Concepts**:
- IQuery interface
- Read model
- IArchive
- Optimized reads

**Complete Code**:

```csharp
using System.Collections.Concurrent;
using EzDdd.Cqrs;
using EzDdd.Cqrs.Query;
using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.In;

// Simple in-memory IArchive implementation for the demos
public sealed class InMemoryArchive<TData, TId>(Func<TData, TId> idSelector)
    : IArchive<TData, TId>
    where TData : class
    where TId : notnull
{
    private readonly ConcurrentDictionary<TId, TData> _store = new();

    public Task<TData?> FindByIdAsync(TId id)
    {
        _store.TryGetValue(id, out TData? data);
        return Task.FromResult(data);
    }

    public Task SaveAsync(TData data)
    {
        _store[idSelector(data)] = data;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TData data)
    {
        _store.TryRemove(idSelector(data), out _);
        return Task.CompletedTask;
    }
}

// Read model (denormalized for queries)
public sealed record AccountSummaryReadModel
(
    AccountId AccountId,
    string Owner,
    decimal Balance,
    DateTimeOffset CreatedOn,
    DateTimeOffset LastTransactionDate,
    int TransactionCount
) : IEntity<AccountId>
{
    AccountId IEntity<AccountId>.Id => AccountId;
}

// Query input (IQuery requires TInput : IInput)
public sealed record GetAccountSummaryInput
(
    AccountId AccountId
) : IInput;

// Query output (IQuery requires TOutput : CqrsOutput<TOutput>, new())
public sealed class GetAccountSummaryOutput : CqrsOutput<GetAccountSummaryOutput>
{
    public string AccountId { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset LastTransactionDate { get; set; }
    public int TransactionCount { get; set; }

    public GetAccountSummaryOutput SetAccountId(string accountId)
    {
        AccountId = accountId;
        return this;
    }

    public GetAccountSummaryOutput SetOwner(string owner)
    {
        Owner = owner;
        return this;
    }

    public GetAccountSummaryOutput SetBalance(decimal balance)
    {
        Balance = balance;
        return this;
    }

    public GetAccountSummaryOutput SetCreatedOn(DateTimeOffset createdOn)
    {
        CreatedOn = createdOn;
        return this;
    }

    public GetAccountSummaryOutput SetLastTransactionDate(DateTimeOffset date)
    {
        LastTransactionDate = date;
        return this;
    }

    public GetAccountSummaryOutput SetTransactionCount(int count)
    {
        TransactionCount = count;
        return this;
    }
}

// Query implementation
public sealed class GetAccountSummaryQuery
    (IArchive<AccountSummaryReadModel, AccountId> archive)
    : IQuery<GetAccountSummaryInput, GetAccountSummaryOutput>
{
    public async Task<GetAccountSummaryOutput> ExecuteAsync(GetAccountSummaryInput input)
    {
        var readModel = await archive.FindByIdAsync(input.AccountId);

        if (readModel == null)
        {
            throw new UseCaseFailureException(
                $"Account not found: {input.AccountId.Value}");
        }

        return GetAccountSummaryOutput.Create()
            .SetAccountId(readModel.AccountId.Value)
            .SetOwner(readModel.Owner)
            .SetBalance(readModel.Balance)
            .SetCreatedOn(readModel.CreatedOn)
            .SetLastTransactionDate(readModel.LastTransactionDate)
            .SetTransactionCount(readModel.TransactionCount)
            .Succeed()
            .SetMessage("Account summary retrieved successfully");
    }
}

// Additional query: GetAccountHistoryQuery
public sealed record GetAccountHistoryInput
(
    AccountId AccountId,
    int PageNumber,
    int PageSize
) : IInput;

public sealed class GetAccountHistoryOutput : CqrsOutput<GetAccountHistoryOutput>
{
    public List<TransactionHistoryItem> Transactions { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public sealed record TransactionHistoryItem
(
    string Type,
    decimal Amount,
    DateTimeOffset Date,
    decimal BalanceAfter
);

// Demo: Query execution
Console.WriteLine("=== CQRS Queries Demo ===\n");

// Setup archive with sample data
var archive = new InMemoryArchive<AccountSummaryReadModel, AccountId>(m => m.AccountId);

var accountId = new AccountId("ACC-QUERY-001");
var readModel = new AccountSummaryReadModel(
    accountId,
    "Query User",
    1500m,
    DateTimeOffset.Parse("2025-01-01T10:00:00Z"),
    DateTimeOffset.Parse("2025-01-15T14:30:00Z"),
    5);

await archive.SaveAsync(readModel);

// Execute query
var query = new GetAccountSummaryQuery(archive);
var input = new GetAccountSummaryInput(accountId);
var output = await query.ExecuteAsync(input);

Console.WriteLine("Query: GetAccountSummary");
Console.WriteLine($"Result: {output.ExitCode}");
Console.WriteLine($"Message: {output.Message}");
Console.WriteLine($"Account ID: {output.AccountId}");
Console.WriteLine($"Owner: {output.Owner}");
Console.WriteLine($"Balance: ${output.Balance:F2}");
Console.WriteLine($"Created: {output.CreatedOn:yyyy-MM-dd}");
Console.WriteLine($"Last Transaction: {output.LastTransactionDate:yyyy-MM-dd}");
Console.WriteLine($"Transactions: {output.TransactionCount}");
```

**Explanation**:

1. **IQuery**: Extends `IUseCase` for read operations; input must implement `IInput`, output must extend `CqrsOutput<TOutput>`
2. **Read Model**: Denormalized data structure optimized for queries
3. **IArchive**: Query database interface (query-side counterpart to IRepository)
4. **Fluent Output**: `CqrsOutput<T>` base class provides `Create()`/`SetId()`/`SetMessage()`/`Succeed()`/`Fail()`
5. **No Modifications**: Queries NEVER modify state

**Output**:
```
=== CQRS Queries Demo ===

Query: GetAccountSummary
Result: Success
Message: Account summary retrieved successfully
Account ID: ACC-QUERY-001
Owner: Query User
Balance: $1500.00
Created: 2025-01-01
Last Transaction: 2025-01-15
Transactions: 5
```

**Notes**:
- ✅ Queries are read-only (no state changes)
- ✅ Read models optimized for specific views
- ✅ Fast queries (no aggregates/business logic)
- ✅ Use IArchive for query database
- ⚠️ Read models eventually consistent

---

### Projection and Projector

**Scenario**: Build and maintain read models from domain events.

**Key Concepts**:
- IProjector<TInput> (a specialized IReactor<TInput>)
- ExecuteAsync for event handling
- Event-driven updates
- Eventually consistent reads

**Complete Code**:

```csharp
using EzDdd.Cqrs.Query;
using EzDdd.Entity;
using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.InOut;

// Uses BankAccount events, AccountSummaryReadModel and InMemoryArchive
// from the previous examples.

// Projector implementation (IProjector<TInput> inherits ExecuteAsync from IReactor<TInput>)
public sealed class AccountProjector : IProjector<DomainEventData>
{
    private readonly IArchive<AccountSummaryReadModel, AccountId> _archive;

    public AccountProjector(IArchive<AccountSummaryReadModel, AccountId> archive)
    {
        _archive = archive ?? throw new ArgumentNullException(nameof(archive));
    }

    public async Task ExecuteAsync(DomainEventData eventData)
    {
        try
        {
            var domainEvent = _DeserializeDomainEvent(eventData);

            switch (domainEvent)
            {
                case AccountCreated e:
                    await _HandleAccountCreatedAsync(e);
                    break;

                case MoneyDeposited e:
                    await _HandleMoneyDepositedAsync(e);
                    break;

                case MoneyWithdrawn e:
                    await _HandleMoneyWithdrawnAsync(e);
                    break;

                case AccountClosed e:
                    await _HandleAccountClosedAsync(e);
                    break;
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(
                $"Error processing event {eventData.Id}: {ex.Message}");
            throw;
        }
    }

    private async Task _HandleAccountCreatedAsync(AccountCreated @event)
    {
        var readModel = new AccountSummaryReadModel(
            @event.Source, // positional AccountId parameter of the event record
            @event.Owner,
            @event.InitialBalance.Amount,
            @event.OccurredOn,
            @event.OccurredOn,
            0);

        await _archive.SaveAsync(readModel);
    }

    private async Task _HandleMoneyDepositedAsync(MoneyDeposited @event)
    {
        var existing = await _archive.FindByIdAsync(@event.Source);
        if (existing == null) return;

        var updated = existing with
        {
            Balance = existing.Balance + @event.Amount.Amount,
            LastTransactionDate = @event.OccurredOn,
            TransactionCount = existing.TransactionCount + 1
        };

        await _archive.SaveAsync(updated);
    }

    private async Task _HandleMoneyWithdrawnAsync(MoneyWithdrawn @event)
    {
        var existing = await _archive.FindByIdAsync(@event.Source);
        if (existing == null) return;

        var updated = existing with
        {
            Balance = existing.Balance - @event.Amount.Amount,
            LastTransactionDate = @event.OccurredOn,
            TransactionCount = existing.TransactionCount + 1
        };

        await _archive.SaveAsync(updated);
    }

    private async Task _HandleAccountClosedAsync(AccountClosed @event)
    {
        var existing = await _archive.FindByIdAsync(@event.Source);
        if (existing == null) return;

        await _archive.DeleteAsync(existing);
    }

    private static IInternalDomainEvent _DeserializeDomainEvent(DomainEventData eventData)
    {
        return eventData.EventType switch
        {
            "AccountCreated" => DomainEventMapper.ToDomain<AccountCreated>(eventData),
            "MoneyDeposited" => DomainEventMapper.ToDomain<MoneyDeposited>(eventData),
            "MoneyWithdrawn" => DomainEventMapper.ToDomain<MoneyWithdrawn>(eventData),
            "AccountClosed" => DomainEventMapper.ToDomain<AccountClosed>(eventData),
            _ => throw new InvalidOperationException($"Unknown event type: {eventData.EventType}")
        };
    }
}

// Demo: Projector in action
public static async Task DemoProjectorAsync()
{
    // Setup
    DomainEventTypeMapper.Register<AccountCreated>("AccountCreated");
    DomainEventTypeMapper.Register<MoneyDeposited>("MoneyDeposited");

    var archive = new InMemoryArchive<AccountSummaryReadModel, AccountId>(m => m.AccountId);
    var projector = new AccountProjector(archive);

    Console.WriteLine("=== Projector Demo ===\n");

    // Simulate events from write side
    var accountId = new AccountId("ACC-PROJ-001");

    var event1 = new AccountCreated(
        Guid.NewGuid(),
        DateTimeOffset.UtcNow,
        accountId,
        "Projector User",
        new Money(1000m));

    var event2 = new MoneyDeposited(
        Guid.NewGuid(),
        DateTimeOffset.UtcNow,
        accountId,
        new Money(500m));

    // Process events through projector
    Console.WriteLine("Event 1: AccountCreated");
    await projector.ExecuteAsync(DomainEventMapper.ToData(event1));
    var readModel1 = await archive.FindByIdAsync(accountId);
    Console.WriteLine($"Read model created: Balance = ${readModel1?.Balance:F2}");

    Console.WriteLine("\nEvent 2: MoneyDeposited");
    await projector.ExecuteAsync(DomainEventMapper.ToData(event2));
    var readModel2 = await archive.FindByIdAsync(accountId);
    Console.WriteLine($"Read model updated: Balance = ${readModel2?.Balance:F2}");
    Console.WriteLine($"Transaction count: {readModel2?.TransactionCount}");
}

await DemoProjectorAsync();
```

**Explanation**:

1. **IProjector<DomainEventData>**: Projector interface, a specialized `IReactor<TInput>` (ADR-0028)
2. **ExecuteAsync**: Handles events delivered by infrastructure (e.g., an event store relay)
3. **Event Handlers**: Update read models based on events
4. **Immutable Updates**: Use `with` expressions for record updates
5. **Error Handling**: Log errors but don't crash projector

**Output**:
```
=== Projector Demo ===

Event 1: AccountCreated
Read model created: Balance = $1000.00

Event 2: MoneyDeposited
Read model updated: Balance = $1500.00
Transaction count: 1
```

**Notes**:
- ✅ Projectors listen to domain events
- ✅ Update read models asynchronously
- ✅ Eventually consistent with write model
- ✅ Can rebuild from event stream
- ⚠️ Handle events idempotently if possible

---

### Complete CQRS Flow

**Scenario**: End-to-end CQRS flow from command to query.

**Key Concepts**:
- Command → Aggregate → Events → Repository
- Events → Relay → Projector → Archive
- Archive → Query → Output
- Eventual consistency

**Complete Code**:

```csharp
using EzDdd.Cqrs.Command;
using EzDdd.Cqrs.Query;
using EzDdd.Entity;
using EzDdd.UseCase.Port.InOut;
using EzDdd.UseCase.Port.Out;

// Reuses BankAccount, the commands, the query, the projector and the
// in-memory peer/archive from the previous examples.

// Complete CQRS infrastructure setup
public static async Task DemoCompleteCqrsFlowAsync()
{
    Console.WriteLine("=== Complete CQRS Flow Demo ===\n");

    // Register event types
    DomainEventTypeMapper.Register<AccountCreated>("AccountCreated");
    DomainEventTypeMapper.Register<MoneyDeposited>("MoneyDeposited");
    DomainEventTypeMapper.Register<MoneyWithdrawn>("MoneyWithdrawn");

    // Setup write side (command)
    var eventStorePeer = new InMemoryEventStorePeer();
    var repository = new EsRepository<BankAccount, AccountId>(eventStorePeer);

    // Setup read side (query)
    var archive = new InMemoryArchive<AccountSummaryReadModel, AccountId>(m => m.AccountId);
    var projector = new AccountProjector(archive);

    // Create commands and queries
    var createCommand = new CreateAccountCommand(repository);
    var depositCommand = new DepositMoneyCommand(repository);
    var query = new GetAccountSummaryQuery(archive);

    var accountId = new AccountId("ACC-CQRS-001");

    // Simulated Relay: poll the event store and deliver new events to the
    // projector, tracking the delivery position. In production this is a
    // background service — see examples/EventInfrastructure/EventStoreRelay.cs.
    var relayPosition = 0;
    async Task RelayPendingEventsAsync()
    {
        var stored = await eventStorePeer.FindByIdAsync(accountId);
        var events = stored!.Events;
        for (; relayPosition < events.Count; relayPosition++)
        {
            var eventData = DomainEventMapper.ToData(
                (IInternalDomainEvent)events[relayPosition]);
            await projector.ExecuteAsync(eventData);
        }
    }

    // STEP 1: Execute CreateAccount command
    Console.WriteLine("STEP 1: Execute CreateAccount Command");
    var createInput = new CreateAccountInput(
        accountId,
        "CQRS User",
        new Money(2000m));

    var createOutput = await createCommand.ExecuteAsync(createInput);
    Console.WriteLine($"✅ Command executed: {createOutput.Message}");
    Console.WriteLine($"   Version: {createOutput.Version}");

    // Relay stored events to the read side
    await RelayPendingEventsAsync();

    // STEP 2: Query account (read model)
    Console.WriteLine("\nSTEP 2: Query Account (Read Model)");
    var queryInput1 = new GetAccountSummaryInput(accountId);
    var queryOutput1 = await query.ExecuteAsync(queryInput1);
    Console.WriteLine($"✅ Query executed: {queryOutput1.Message}");
    Console.WriteLine($"   Owner: {queryOutput1.Owner}");
    Console.WriteLine($"   Balance: ${queryOutput1.Balance:F2}");
    Console.WriteLine($"   Transactions: {queryOutput1.TransactionCount}");

    // STEP 3: Execute DepositMoney command
    Console.WriteLine("\nSTEP 3: Execute DepositMoney Command");
    var depositInput = new DepositMoneyInput(accountId, new Money(800m))
    {
        Version = createOutput.Version
    };

    var depositOutput = await depositCommand.ExecuteAsync(depositInput);
    Console.WriteLine($"✅ Command executed: {depositOutput.Message}");
    Console.WriteLine($"   New Balance (write model): ${depositOutput.NewBalance:F2}");
    Console.WriteLine($"   Version: {depositOutput.Version}");

    // Relay stored events to the read side
    await RelayPendingEventsAsync();

    // STEP 4: Query again (eventual consistency)
    Console.WriteLine("\nSTEP 4: Query Again (Eventually Consistent)");
    var queryInput2 = new GetAccountSummaryInput(accountId);
    var queryOutput2 = await query.ExecuteAsync(queryInput2);
    Console.WriteLine($"✅ Query executed: {queryOutput2.Message}");
    Console.WriteLine($"   Balance (read model): ${queryOutput2.Balance:F2}");
    Console.WriteLine($"   Transactions: {queryOutput2.TransactionCount}");

    // STEP 5: Verify write and read models match
    Console.WriteLine("\nSTEP 5: Verify Consistency");
    var finalAccount = await repository.FindByIdAsync(accountId);
    Console.WriteLine($"Write Model Balance: ${finalAccount!.Balance.Amount:F2}");
    Console.WriteLine($"Read Model Balance: ${queryOutput2.Balance:F2}");
    Console.WriteLine($"Consistent: {finalAccount.Balance.Amount == queryOutput2.Balance}");

    // STEP 6: Show CQRS benefits
    Console.WriteLine("\nSTEP 6: CQRS Benefits Demonstrated");
    Console.WriteLine("✅ Write and read models separated");
    Console.WriteLine("✅ Write model: Event-sourced BankAccount (business logic)");
    Console.WriteLine("✅ Read model: Denormalized AccountSummaryReadModel (queries)");
    Console.WriteLine("✅ Scalability: Read and write sides can scale independently");
    Console.WriteLine("✅ Optimization: Each side optimized for its purpose");
    Console.WriteLine("✅ Eventual consistency: Read model catches up asynchronously");
}

await DemoCompleteCqrsFlowAsync();
```

**Explanation**:

1. **Write Side**: Commands → Aggregates → Events → Repository
2. **Relay**: A background relay polls the event store and delivers stored events to reactors (Transactional Outbox; simulated inline here)
3. **Read Side**: Projectors → Read Models → Archive
4. **Query Side**: Queries read from archive (optimized read models)
5. **Eventual Consistency**: Read models updated asynchronously

**Output**:
```
=== Complete CQRS Flow Demo ===

STEP 1: Execute CreateAccount Command
✅ Command executed: Account ACC-CQRS-001 created successfully
   Version: 0

STEP 2: Query Account (Read Model)
✅ Query executed: Account summary retrieved successfully
   Owner: CQRS User
   Balance: $2000.00
   Transactions: 0

STEP 3: Execute DepositMoney Command
✅ Command executed: Deposited 800.00 USD
   New Balance (write model): $2800.00
   Version: 1

STEP 4: Query Again (Eventually Consistent)
✅ Query executed: Account summary retrieved successfully
   Balance (read model): $2800.00
   Transactions: 1

STEP 5: Verify Consistency
Write Model Balance: $2800.00
Read Model Balance: $2800.00
Consistent: True

STEP 6: CQRS Benefits Demonstrated
✅ Write and read models separated
✅ Write model: Event-sourced BankAccount (business logic)
✅ Read model: Denormalized AccountSummaryReadModel (queries)
✅ Scalability: Read and write sides can scale independently
✅ Optimization: Each side optimized for its purpose
✅ Eventual consistency: Read model catches up asynchronously
```

**Notes**:
- ✅ Write and read sides completely separated
- ✅ Each side optimized for purpose
- ✅ Scales independently
- ✅ Eventually consistent
- ⚠️ Requires relay infrastructure (event store polling; see examples/EventInfrastructure)
- ⚠️ Complexity cost vs. benefits

---

### CqrsOutput Fluent API

**Scenario**: Use CqrsOutput's self-referential fluent API for unified outputs.

**Key Concepts**:
- CqrsOutput<T> self-referential generic base class
- Fluent method chaining that preserves the concrete type
- Success/failure handling via ExitCode
- Domain-specific payload via subclass properties

**Complete Code**:

```csharp
using EzDdd.Cqrs;
using EzDdd.UseCase.Port.In;

// The generic parameter T is the concrete output type itself
// (self-referential constraint: T : CqrsOutput<T>, new()).
// Payload data lives in subclass properties, added with fluent setters.
public sealed class GetBalanceOutput : CqrsOutput<GetBalanceOutput>
{
    public decimal Balance { get; set; }

    public GetBalanceOutput SetBalance(decimal balance)
    {
        Balance = balance;
        return this;
    }
}

// Demo: CqrsOutput fluent API
public static void DemoCqrsOutputApi()
{
    Console.WriteLine("=== CqrsOutput Fluent API Demo ===\n");

    // Example 1: Simple success output
    Console.WriteLine("Example 1: Simple Success");
    var output1 = GetBalanceOutput.Create()
        .SetBalance(1500.00m)
        .Succeed()
        .SetMessage("Balance retrieved");

    Console.WriteLine($"ExitCode: {output1.ExitCode}");
    Console.WriteLine($"Message: {output1.Message}");
    Console.WriteLine($"Balance: ${output1.Balance:F2}");

    // Example 2: Success with ID
    Console.WriteLine("\nExample 2: Success with ID");
    var output2 = GetBalanceOutput.Create()
        .SetBalance(1500.00m)
        .Succeed()
        .SetMessage("Balance retrieved")
        .SetId("ACC-123");

    Console.WriteLine($"ExitCode: {output2.ExitCode}");
    Console.WriteLine($"ID: {output2.Id}");
    Console.WriteLine($"Balance: ${output2.Balance:F2}");

    // Example 3: Failure output
    Console.WriteLine("\nExample 3: Failure");
    var output3 = GetBalanceOutput.Create()
        .Fail()
        .SetMessage("Account not found");

    Console.WriteLine($"ExitCode: {output3.ExitCode}");
    Console.WriteLine($"Message: {output3.Message}");

    // Example 4: Conditional success/failure with SetExitCode
    Console.WriteLine("\nExample 4: Conditional Exit Code");
    bool operationSucceeded = true;
    var output4 = GetBalanceOutput.Create()
        .SetExitCode(operationSucceeded ? ExitCode.Success : ExitCode.Failure)
        .SetMessage(operationSucceeded ? "Success" : "Failed");
    Console.WriteLine($"Conditional: {output4.ExitCode}");
    Console.WriteLine($"Integer code: {output4.ExitCode.Code()}");

    // Example 5: Type-safe chaining
    Console.WriteLine("\nExample 5: Type-Safe Chaining");
    // Every fluent call - including the inherited SetId/SetMessage/Succeed -
    // returns GetBalanceOutput, not CqrsOutput, so custom setters can be
    // chained in any order:
    GetBalanceOutput chained = GetBalanceOutput.Create()
        .SetId("ACC-456")
        .SetBalance(42m)
        .Succeed();
    Console.WriteLine($"Chained: {chained.Id}, ${chained.Balance:F2}, {chained.ExitCode}");

    // Example 6: Used through the IOutput interface
    Console.WriteLine("\nExample 6: IOutput Interoperability");
    IOutput asInterface = chained; // CqrsOutput<T> implements IOutput
    Console.WriteLine($"IOutput.Message: '{asInterface.Message}'");
    Console.WriteLine($"IOutput.ExitCode: {asInterface.ExitCode}");
}

DemoCqrsOutputApi();
```

**Explanation**:

1. **Create()**: Static factory method to start building (requires `new()` constraint)
2. **Succeed()/Fail()**: Set exit code to Success/Failure
3. **SetExitCode()**: Set exit code explicitly
4. **SetMessage()/SetId()**: Add human-readable message and associated identifier
5. **Method Chaining**: All fluent methods return the concrete type `T` — subclass setters stay chainable
6. **Payload**: Domain data is expressed as subclass properties (there is no generic `Data` payload on `CqrsOutput`)

**Output**:
```
=== CqrsOutput Fluent API Demo ===

Example 1: Simple Success
ExitCode: Success
Message: Balance retrieved
Balance: $1500.00

Example 2: Success with ID
ExitCode: Success
ID: ACC-123
Balance: $1500.00

Example 3: Failure
ExitCode: Failure
Message: Account not found

Example 4: Conditional Exit Code
Conditional: Success
Integer code: 0

Example 5: Type-Safe Chaining
Chained: ACC-456, $42.00, Success

Example 6: IOutput Interoperability
IOutput.Message: ''
IOutput.ExitCode: Success
```

**Notes**:
- ✅ Fluent API preserves the concrete subclass type (self-referential generic)
- ✅ Exit code + message pattern (`ExitCode.Success` / `ExitCode.Failure` only)
- ✅ Optional ID field
- ✅ Implements `IOutput` for interface interoperability
- ⚠️ `T` must be the subclass itself: `class MyOutput : CqrsOutput<MyOutput>`
- ⚠️ No built-in `Data`/pagination members — model the payload as subclass properties

---

## Real-World Scenarios

### Banking System (Event Sourcing)

**Scenario**: Complete banking system with multiple aggregates.

**Key Concepts**:
- Event sourcing
- Multiple aggregates
- Use cases
- Repository pattern

**Complete Code**:

```csharp
using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.Out;

// === DOMAIN MODEL ===

// Account aggregate (from previous examples - BankAccount)
// Money value object (from previous examples)

// Transfer domain service
public sealed class TransferService
{
    public static void Transfer(BankAccount from, BankAccount to, Money amount)
    {
        if (from.IsClosed || to.IsClosed)
            throw new InvalidOperationException("Cannot transfer to/from closed account");

        if (amount.Amount <= 0)
            throw new InvalidOperationException("Transfer amount must be positive");

        if (from.Balance.Amount < amount.Amount)
            throw new InvalidOperationException("Insufficient funds");

        from.Withdraw(amount);
        to.Deposit(amount);
    }
}

// === USE CASES ===

// TransferMoney use case
public sealed record TransferMoneyInput
(
    AccountId FromAccountId,
    AccountId ToAccountId,
    Money Amount
) : IInput;

public sealed class TransferMoneyOutput : IOutput
{
    public Money AmountTransferred { get; init; } = null!;
    public decimal FromBalance { get; init; }
    public decimal ToBalance { get; init; }

    public string Message { get; private set; } = string.Empty;
    public ExitCode ExitCode { get; private set; }
    public string Id { get; private set; } = string.Empty;

    public IOutput SetMessage(string message) { Message = message; return this; }
    public IOutput SetExitCode(ExitCode exitCode) { ExitCode = exitCode; return this; }
    public IOutput SetId(string id) { Id = id; return this; }
    public IOutput Fail() { ExitCode = ExitCode.Failure; return this; }
    public IOutput Succeed() { ExitCode = ExitCode.Success; return this; }
}

public sealed class TransferMoneyUseCase
    (IRepository<BankAccount, AccountId, IInternalDomainEvent> repository)
    : IUseCase<TransferMoneyInput, TransferMoneyOutput>
{
    public async Task<TransferMoneyOutput> ExecuteAsync(TransferMoneyInput input)
    {
        // Load both accounts
        var fromAccount = await repository.FindByIdAsync(input.FromAccountId);
        if (fromAccount == null)
            throw new UseCaseFailureException($"From account not found: {input.FromAccountId.Value}");

        var toAccount = await repository.FindByIdAsync(input.ToAccountId);
        if (toAccount == null)
            throw new UseCaseFailureException($"To account not found: {input.ToAccountId.Value}");

        // Execute domain service
        TransferService.Transfer(fromAccount, toAccount, input.Amount);

        // Save both accounts
        await repository.SaveAsync(fromAccount);
        await repository.SaveAsync(toAccount);

        // Return output
        var output = new TransferMoneyOutput
        {
            AmountTransferred = input.Amount,
            FromBalance = fromAccount.Balance.Amount,
            ToBalance = toAccount.Balance.Amount
        };
        output.Succeed();
        output.SetMessage($"Transferred {input.Amount}");
        return output;
    }
}

// GetAccountBalance use case
public sealed record GetAccountBalanceInput(AccountId AccountId) : IInput;

public sealed class GetAccountBalanceOutput : IOutput
{
    public string AccountId { get; init; } = string.Empty;
    public string Owner { get; init; } = string.Empty;
    public Money Balance { get; init; } = null!;

    public string Message { get; private set; } = string.Empty;
    public ExitCode ExitCode { get; private set; }
    public string Id { get; private set; } = string.Empty;

    public IOutput SetMessage(string message) { Message = message; return this; }
    public IOutput SetExitCode(ExitCode exitCode) { ExitCode = exitCode; return this; }
    public IOutput SetId(string id) { Id = id; return this; }
    public IOutput Fail() { ExitCode = ExitCode.Failure; return this; }
    public IOutput Succeed() { ExitCode = ExitCode.Success; return this; }
}

public sealed class GetAccountBalanceUseCase
    (IRepository<BankAccount, AccountId, IInternalDomainEvent> repository)
    : IUseCase<GetAccountBalanceInput, GetAccountBalanceOutput>
{
    public async Task<GetAccountBalanceOutput> ExecuteAsync(GetAccountBalanceInput input)
    {
        var account = await repository.FindByIdAsync(input.AccountId);
        if (account == null)
            throw new UseCaseFailureException($"Account not found: {input.AccountId.Value}");

        var output = new GetAccountBalanceOutput
        {
            AccountId = account.Id.Value,
            Owner = account.Owner,
            Balance = account.Balance
        };
        output.Succeed();
        return output;
    }
}

// === DEMO ===

public static async Task DemoBankingSystemAsync()
{
    // Setup
    DomainEventTypeMapper.Register<AccountCreated>("AccountCreated");
    DomainEventTypeMapper.Register<MoneyDeposited>("MoneyDeposited");
    DomainEventTypeMapper.Register<MoneyWithdrawn>("MoneyWithdrawn");

    var peer = new InMemoryEventStorePeer();
    var repository = new EsRepository<BankAccount, AccountId>(peer);

    Console.WriteLine("=== Banking System Demo ===\n");

    // Step 1: Create two accounts
    Console.WriteLine("Step 1: Create two accounts");
    var account1 = new BankAccount(new AccountId("ACC-BANK-001"), "Alice", new Money(5000m));
    var account2 = new BankAccount(new AccountId("ACC-BANK-002"), "Bob", new Money(3000m));

    await repository.SaveAsync(account1);
    await repository.SaveAsync(account2);

    Console.WriteLine($"✅ Alice's account created: ${account1.Balance.Amount:F2}");
    Console.WriteLine($"✅ Bob's account created: ${account2.Balance.Amount:F2}");

    // Step 2: Check balances
    Console.WriteLine("\nStep 2: Check initial balances");
    var balanceUseCase = new GetAccountBalanceUseCase(repository);

    var aliceBalance = await balanceUseCase.ExecuteAsync(
        new GetAccountBalanceInput(new AccountId("ACC-BANK-001")));
    Console.WriteLine($"Alice's balance: {aliceBalance.Balance}");

    var bobBalance = await balanceUseCase.ExecuteAsync(
        new GetAccountBalanceInput(new AccountId("ACC-BANK-002")));
    Console.WriteLine($"Bob's balance: {bobBalance.Balance}");

    // Step 3: Transfer money
    Console.WriteLine("\nStep 3: Transfer $1,500 from Alice to Bob");
    var transferUseCase = new TransferMoneyUseCase(repository);

    var transferInput = new TransferMoneyInput(
        new AccountId("ACC-BANK-001"),
        new AccountId("ACC-BANK-002"),
        new Money(1500m));

    var transferOutput = await transferUseCase.ExecuteAsync(transferInput);
    Console.WriteLine($"✅ {transferOutput.Message}");
    Console.WriteLine($"   Alice's new balance: ${transferOutput.FromBalance:F2}");
    Console.WriteLine($"   Bob's new balance: ${transferOutput.ToBalance:F2}");

    // Step 4: Verify balances after transfer
    Console.WriteLine("\nStep 4: Verify balances after transfer");
    var aliceBalanceAfter = await balanceUseCase.ExecuteAsync(
        new GetAccountBalanceInput(new AccountId("ACC-BANK-001")));
    var bobBalanceAfter = await balanceUseCase.ExecuteAsync(
        new GetAccountBalanceInput(new AccountId("ACC-BANK-002")));

    Console.WriteLine($"Alice's final balance: {aliceBalanceAfter.Balance}");
    Console.WriteLine($"Bob's final balance: {bobBalanceAfter.Balance}");

    // Step 5: Show event sourcing audit trail
    Console.WriteLine("\nStep 5: Event Sourcing Audit Trail");
    var aliceAccount = await repository.FindByIdAsync(new AccountId("ACC-BANK-001"));
    Console.WriteLine($"Alice's aggregate version: {aliceAccount!.Version}");
    Console.WriteLine($"Alice's transaction history: reconstructable from events");

    var bobAccount = await repository.FindByIdAsync(new AccountId("ACC-BANK-002"));
    Console.WriteLine($"Bob's aggregate version: {bobAccount!.Version}");
    Console.WriteLine($"Bob's transaction history: reconstructable from events");
}

await DemoBankingSystemAsync();
```

**Output**:
```
=== Banking System Demo ===

Step 1: Create two accounts
✅ Alice's account created: $5000.00
✅ Bob's account created: $3000.00

Step 2: Check initial balances
Alice's balance: 5000.00 USD
Bob's balance: 3000.00 USD

Step 3: Transfer $1,500 from Alice to Bob
✅ Transferred 1500.00 USD
   Alice's new balance: $3500.00
   Bob's new balance: $4500.00

Step 4: Verify balances after transfer
Alice's final balance: 3500.00 USD
Bob's final balance: 4500.00 USD

Step 5: Event Sourcing Audit Trail
Alice's aggregate version: 1
Alice's transaction history: reconstructable from events
Bob's aggregate version: 1
Bob's transaction history: reconstructable from events
```

**Notes**:
- ✅ Complete banking domain
- ✅ Event sourcing for audit trail
- ✅ Use cases for operations
- ✅ Domain services for cross-aggregate logic
- ✅ Full transaction history

---

## System Reconciliation Examples

System reconciliation is used for periodic maintenance tasks, data consistency checks, and cleanup operations. Unlike use cases which are triggered by user actions, reconcilers are typically invoked by scheduled jobs or administrative tools.

### Cleanup Reconciler with Context

**Scenario**: Clean up expired draft orders that have been inactive for a specified number of days.

**Key Concepts**:
- IReconciler<TContext, TReport> interface
- Context provides input parameters
- Report describes reconciliation results
- Error handling with partial success

**Complete Code**:

```csharp
using EzDdd.UseCase.Port.In;

// Define reconciliation context
public record OrderCleanupContext(int ExpirationDays);

// Define reconciliation report
public record OrderCleanupReport(
    int TotalChecked,
    int DeletedCount,
    int ErrorCount,
    IReadOnlyList<string> Errors
);

// Repository interface for orders
public interface IOrderRepository
{
    Task<List<OrderId>> FindExpiredDraftOrdersAsync(DateTimeOffset cutoffDate);
    Task DeleteAsync(OrderId id);
}

// Implement reconciler
public class CleanUpExpiredOrdersReconciler : IReconciler<OrderCleanupContext, OrderCleanupReport>
{
    private readonly IOrderRepository _orderRepository;

    public CleanUpExpiredOrdersReconciler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderCleanupReport> ReconcileAsync(OrderCleanupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ExpirationDays <= 0)
        {
            throw new InvalidOperationException("Expiration days must be positive");
        }

        // 1. Find expired draft orders
        DateTimeOffset cutoffDate = DateTimeOffset.UtcNow.AddDays(-context.ExpirationDays);
        List<OrderId> expiredOrderIds = await _orderRepository.FindExpiredDraftOrdersAsync(cutoffDate);

        // 2. Delete expired orders (with error handling)
        int deletedCount = 0;
        int errorCount = 0;
        List<string> errors = [];

        foreach (OrderId orderId in expiredOrderIds)
        {
            try
            {
                await _orderRepository.DeleteAsync(orderId);
                deletedCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                errors.Add($"Failed to delete order {orderId}: {ex.Message}");
            }
        }

        // 3. Return detailed report
        return new OrderCleanupReport(
            TotalChecked: expiredOrderIds.Count,
            DeletedCount: deletedCount,
            ErrorCount: errorCount,
            Errors: errors
        );
    }
}

// Usage
var reconciler = new CleanUpExpiredOrdersReconciler(orderRepository);
var context = new OrderCleanupContext(ExpirationDays: 7);
var report = await reconciler.ReconcileAsync(context);

Console.WriteLine($"Checked: {report.TotalChecked}, Deleted: {report.DeletedCount}, Errors: {report.ErrorCount}");
```

**When to Use**:
- ✅ Periodic cleanup of old data
- ✅ Removing orphaned records
- ✅ Data hygiene maintenance
- ✅ Business rule enforcement across aggregates

---

### Global Reconciler with NullContext

**Scenario**: Perform system-wide cleanup that doesn't require specific input parameters.

**Key Concepts**:
- NullContext for reconcilers without input
- Singleton pattern (NullContext.Instance)
- Type safety instead of null or object

**Complete Code**:

```csharp
using EzDdd.UseCase.Port.In;

// Simple report for global cleanup
public record GlobalCleanupReport(
    int TempFilesCleaned,
    int ExpiredSessionsRemoved,
    int CachesCleared
);

// Global system cleanup reconciler
public class GlobalSystemCleanupReconciler : IReconciler<NullContext, GlobalCleanupReport>
{
    private readonly IFileCleanupService _fileService;
    private readonly ISessionManager _sessionManager;
    private readonly ICacheManager _cacheManager;

    public GlobalSystemCleanupReconciler(
        IFileCleanupService fileService,
        ISessionManager sessionManager,
        ICacheManager cacheManager)
    {
        _fileService = fileService;
        _sessionManager = sessionManager;
        _cacheManager = cacheManager;
    }

    public async Task<GlobalCleanupReport> ReconcileAsync(NullContext context)
    {
        // No context needed - perform global cleanup

        // 1. Clean up temporary files
        int tempFilesCleanedCount = await _fileService.CleanupTempFilesAsync();

        // 2. Remove expired sessions
        int expiredSessionsCount = await _sessionManager.RemoveExpiredSessionsAsync();

        // 3. Clear stale caches
        int cachesClearedCount = await _cacheManager.ClearStaleCachesAsync();

        return new GlobalCleanupReport(
            TempFilesCleaned: tempFilesCleanedCount,
            ExpiredSessionsRemoved: expiredSessionsCount,
            CachesCleared: cachesClearedCount
        );
    }
}

// Usage - note the use of NullContext.Instance
var reconciler = new GlobalSystemCleanupReconciler(fileService, sessionManager, cacheManager);
var report = await reconciler.ReconcileAsync(NullContext.Instance);

Console.WriteLine($"Global cleanup: {report.TempFilesCleaned} files, {report.ExpiredSessionsRemoved} sessions");
```

**When to Use**:
- ✅ System-wide maintenance tasks
- ✅ No specific input parameters needed
- ✅ Scheduled periodic cleanup
- ✅ Administrative operations

---

### Scheduling with BackgroundService

**Scenario**: Run reconciler periodically using ASP.NET Core BackgroundService.

**Key Concepts**:
- BackgroundService integration
- Periodic execution with Timer or Delay
- Graceful shutdown support
- Dependency injection

**Complete Code**:

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Background service that runs reconciler periodically
public class ReconcilerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReconcilerHostedService> _logger;
    private readonly TimeSpan _interval;

    public ReconcilerHostedService(
        IServiceProvider serviceProvider,
        ILogger<ReconcilerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _interval = TimeSpan.FromHours(24); // Run daily
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reconciler background service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunReconciliationAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running reconciliation");
            }

            // Wait for next execution
            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Reconciler background service stopping");
    }

    private async Task RunReconciliationAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var reconciler = scope.ServiceProvider
            .GetRequiredService<IReconciler<OrderCleanupContext, OrderCleanupReport>>();

        _logger.LogInformation("Starting order cleanup reconciliation");

        var context = new OrderCleanupContext(ExpirationDays: 7);
        var report = await reconciler.ReconcileAsync(context);

        _logger.LogInformation(
            "Reconciliation complete: Checked={Checked}, Deleted={Deleted}, Errors={Errors}",
            report.TotalChecked,
            report.DeletedCount,
            report.ErrorCount);

        if (report.ErrorCount > 0)
        {
            _logger.LogWarning("Reconciliation had {ErrorCount} errors", report.ErrorCount);
            foreach (var error in report.Errors)
            {
                _logger.LogWarning("  - {Error}", error);
            }
        }
    }
}

// Registration in Program.cs or Startup.cs
builder.Services.AddSingleton<IOrderRepository, OrderRepository>();
builder.Services.AddTransient<IReconciler<OrderCleanupContext, OrderCleanupReport>,
    CleanUpExpiredOrdersReconciler>();
builder.Services.AddHostedService<ReconcilerHostedService>();
```

**When to Use**:
- ✅ Simple periodic scheduling in ASP.NET Core
- ✅ No external scheduler dependency
- ✅ Graceful shutdown required
- ✅ Integrated with application lifecycle

---

### Scheduling with Hangfire

**Scenario**: Use Hangfire for advanced scheduling features (cron expressions, dashboard, retries).

**Key Concepts**:
- Hangfire recurring jobs
- Cron expressions for flexible scheduling
- Web dashboard for monitoring
- Automatic retry on failure

**Complete Code**:

```csharp
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

// Configure Hangfire in Program.cs
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage("your_connection_string"));

builder.Services.AddHangfireServer();

// Register reconcilers
builder.Services.AddScoped<IReconciler<OrderCleanupContext, OrderCleanupReport>,
    CleanUpExpiredOrdersReconciler>();
builder.Services.AddScoped<IReconciler<NullContext, GlobalCleanupReport>,
    GlobalSystemCleanupReconciler>();

// Configure recurring jobs after app build
var app = builder.Build();

// Daily order cleanup at 2 AM
RecurringJob.AddOrUpdate<IReconciler<OrderCleanupContext, OrderCleanupReport>>(
    "cleanup-expired-orders",
    reconciler => reconciler.ReconcileAsync(new OrderCleanupContext(ExpirationDays: 7)),
    Cron.Daily(2)); // Run at 2:00 AM every day

// Weekly global cleanup on Sunday at 3 AM
RecurringJob.AddOrUpdate<IReconciler<NullContext, GlobalCleanupReport>>(
    "global-system-cleanup",
    reconciler => reconciler.ReconcileAsync(NullContext.Instance),
    Cron.Weekly(DayOfWeek.Sunday, 3)); // Run at 3:00 AM every Sunday

// Hourly session cleanup
RecurringJob.AddOrUpdate<SessionCleanupReconciler>(
    "cleanup-expired-sessions",
    reconciler => reconciler.ReconcileAsync(new SessionCleanupContext(MaxIdleMinutes: 30)),
    Cron.Hourly); // Run every hour

app.UseHangfireDashboard("/hangfire"); // Access dashboard at /hangfire
app.Run();
```

**Advanced Cron Examples**:

```csharp
// Every 15 minutes
Cron.MinuteInterval(15)

// Every day at 2:30 AM
Cron.Daily(2, 30)

// Every Monday at 9:00 AM
Cron.Weekly(DayOfWeek.Monday, 9)

// First day of every month at midnight
Cron.Monthly(1, 0)

// Custom cron expression (every 6 hours)
"0 */6 * * *"
```

**When to Use**:
- ✅ Complex scheduling requirements (cron expressions)
- ✅ Need monitoring dashboard
- ✅ Automatic retry on failures
- ✅ Job history and statistics
- ✅ Multiple reconcilers with different schedules

**Alternative: Quartz.NET**:

```csharp
// Similar pattern with Quartz.NET
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("order-cleanup");
    q.AddJob<OrderCleanupJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("order-cleanup-trigger")
        .WithCronSchedule("0 0 2 * * ?") // 2:00 AM daily
    );
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
```

---

## See Also

- [API Reference](API_REFERENCE.md) - Complete API documentation
- [Architecture Decision Records](../adr/) - Design decisions
- [README.md](../../README.md) - Project overview

---

*Last updated: 2026-07-05*
