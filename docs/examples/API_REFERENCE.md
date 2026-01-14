# ezDDD.NET API Reference

Complete reference for all public APIs in ezDDD.NET tactical Domain-Driven Design framework.

> **Version**: 1.0.0-alpha.1
> **Last Updated**: 2025-11-22

---

## Table of Contents

- [EzDdd.Common](#ezdddcommon)
  - [Converter<TSource, TTarget>](#converter)
  - [JsonUtil](#jsonutil)
  - [BiMap<TKey, TValue>](#bimap)
- [EzDdd.Entity](#ezdddentity)
  - [IEntity<TId>](#ientity)
  - [IValueObject](#ivalueobject)
  - [IDomainEvent](#idomainevent)
  - [IInternalDomainEvent](#iinternaldomainevent)
  - [AggregateRoot<TId, TEvent>](#aggregateroot)
  - [EsAggregateRoot<TId, TEvent>](#esaggregateroot)
  - [DomainEventTypeMapper](#domaineventtypemapper)
- [EzDdd.UseCase](#ezdddusеcase)
  - [Foundation Interfaces](#foundation-interfaces)
    - [IInput](#iinput)
    - [IOutput](#ioutput)
    - [IVersionedInput](#iversionedinput)
    - [ExitCode](#exitcode)
    - [IReactor](#ireactor)
  - [Use Case Pattern](#use-case-pattern)
    - [IUseCase<TInput, TOutput>](#iusecase)
    - [UseCaseFailureException](#usecasefailureexception)
  - [Repository Pattern](#repository-pattern)
    - [IRepository<TAggregate, TId, TEvent>](#irepository)
    - [IRepositoryPeer<TData, TId>](#irepositorypeer)
    - [IStoreData<TId>](#istoredata)
    - [RepositorySaveException](#repositorysaveexception)
    - [RepositoryPeerSaveException](#repositorypeersaveexception)
  - [Event Infrastructure](#event-infrastructure)
    - [IExternalDomainEvent](#iexternaldomainevent)
    - [DomainEventData](#domaineventdata)
    - [DomainEventMapper](#domaineventmapper)
    - [InternalDomainEventDto](#internaldomaineventdto)
  - [Event Sourcing](#event-sourcing)
    - [EventStoreData<TId>](#eventstoredata)
    - [EventStoreMapper](#eventstoremapper)
    - [EsRepository<TAggregate, TId>](#esrepository)
  - [State Sourcing](#state-sourcing)
    - [IOutboxData<TId>](#ioutboxdata)
    - [OutboxMapper](#outboxmapper)
    - [OutboxRepository<TAggregate, TId>](#outboxrepository)
  - [Message Bus](#message-bus)
    - [IMessageBus<TMessage>](#imessagebus)
    - [IMessageProducer](#imessageproducer)
    - [BlockingMessageBus<TMessage>](#blockingmessagebus)
    - [EventBusProducer](#eventbusproducer)
    - [GenericReactor<TMessage>](#genericreactor)
- [EzDdd.Cqrs](#ezdddcqrs)
  - [Command Side](#command-side)
    - [ICommand<TInput, TOutput>](#icommand)
    - [IInquiry<TInput, TOutput>](#iinquiry)
    - [IInquiryInput](#iinquiryinput)
  - [Query Side](#query-side)
    - [IQuery<TInput, TOutput>](#iquery)
    - [IProjection<TInput, TOutput>](#iprojection)
    - [IProjector](#iprojector)
    - [IProjectionInput](#iprojectioninput)
    - [IArchive<TData, TId>](#iarchive)
  - [CqrsOutput<T>](#cqrsoutput)

---

## EzDdd.Common

Foundation utilities for the entire ezDDD framework.

### Converter

**Namespace:** `EzDdd.Common`

**Signature:**
```csharp
public delegate TTarget Converter<in TSource, out TTarget>(TSource source);
```

**Description:**
Standard delegate for type conversion between layers. Provides a functional interface for converting values from one type to another, commonly used in Domain-Driven Design for mapping between entities, DTOs, and data models.

**Type Parameters:**
- `TSource`: The source type to convert from (contravariant)
- `TTarget`: The target type to convert to (covariant)

**Returns:** The converted target value

**Example:**
```csharp
// Lambda implementation
Converter<string, int> stringToInt = s => int.Parse(s);
int result = stringToInt("42"); // Returns 42

// Method reference
Converter<string, int> parser = int.Parse;
int value = parser("123"); // Returns 123

// Multi-line lambda for complex conversions
Converter<User, UserDto> converter = user =>
{
    return new UserDto(user.Id, user.Name, user.Email);
};
UserDto dto = converter(userEntity);
```

**Notes:**
- ✅ Generic variance enables flexible type relationships
- ✅ Supports both lambda expressions and method references
- ✅ Equivalent to Java's `@FunctionalInterface`

**Related:**
- [DomainEventMapper](#domaineventmapper)
- [EventStoreMapper](#eventstoremapper)

---

### JsonUtil

**Namespace:** `EzDdd.Common`

**Signature:**
```csharp
public static class JsonUtil
{
    public static readonly JsonSerializerOptions Options;
    public static string AsString(object value);
    public static T? ReadValue<T>(string json);
    public static T? ReadAs<T>(byte[] bytes);
    public static JsonDocument ReadTree(string json);
    public static JsonDocument ReadTree(byte[] bytes);
    public static T? DeepCopy<T>(T value);
}
```

**Description:**
JSON serialization utilities using System.Text.Json. Provides preconfigured options optimized for domain event serialization and deep copying of domain objects.

**Configuration:**
- Fields included in serialization (`IncludeFields = true`)
- Case-insensitive property names during deserialization
- ISO-8601 DateTime format (not timestamps)
- Compact output (no indentation)

**Methods:**

#### AsString
Serializes an object to a JSON string.

**Parameters:**
- `value` (object): The object to serialize

**Returns:** JSON string representation

**Exceptions:**
- `InvalidOperationException`: Thrown when serialization fails

**Example:**
```csharp
var user = new User { Id = 1, Name = "Alice" };
string json = JsonUtil.AsString(user);
// Result: {"Id":1,"Name":"Alice"}
```

#### ReadValue<T>
Deserializes a JSON string to an object of type T.

**Parameters:**
- `json` (string): The JSON string to deserialize

**Returns:** The deserialized object, or null if JSON is null

**Exceptions:**
- `InvalidOperationException`: Thrown when deserialization fails

**Example:**
```csharp
string json = "{\"Id\":1,\"Name\":\"Alice\"}";
User? user = JsonUtil.ReadValue<User>(json);
```

#### ReadAs<T>
Deserializes a UTF-8 encoded byte array to an object of type T.

**Parameters:**
- `bytes` (byte[]): The UTF-8 encoded byte array

**Returns:** The deserialized object, or null if bytes represent null

**Example:**
```csharp
byte[] bytes = Encoding.UTF8.GetBytes("{\"Id\":1,\"Name\":\"Alice\"}");
User? user = JsonUtil.ReadAs<User>(bytes);
```

#### ReadTree
Parses JSON into a JsonDocument for low-level DOM access.

**Parameters:**
- `json` (string) or `bytes` (byte[]): The JSON to parse

**Returns:** A JsonDocument (must be disposed after use)

**Example:**
```csharp
using var doc = JsonUtil.ReadTree("{\"name\":\"Alice\",\"age\":30}");
string name = doc.RootElement.GetProperty("name").GetString();
int age = doc.RootElement.GetProperty("age").GetInt32();
```

#### DeepCopy<T>
Creates a deep copy via JSON serialization/deserialization.

**Parameters:**
- `value` (T): The object to copy

**Returns:** A deep copy of the object

**Example:**
```csharp
var original = new User { Id = 1, Name = "Alice" };
var copy = JsonUtil.DeepCopy(original);
copy.Name = "Bob";
// original.Name is still "Alice"
```

**Notes:**
- ✅ Zero external dependencies (uses System.Text.Json)
- ✅ Field-based serialization matches Jackson configuration
- ⚠️ Standard JSON only (no unquoted field names)
- ⚠️ DeepCopy requires JSON-serializable types

**Related:**
- [DomainEventData](#domaineventdata)
- [EventStoreData](#eventstoredata)

---

### BiMap

**Namespace:** `EzDdd.Common`

**Signature:**
```csharp
public class BiMap<TKey, TValue> : IDictionary<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    public TKey? GetKey(TValue value);
    public bool TryGetKey(TValue value, out TKey key);
    public void PutAll(IDictionary<TKey, TValue> dictionary);
    public TValue? PutIfAbsent(TKey key, TValue value);
    public bool TryReplace(TKey key, TValue newValue, out TValue oldValue);
    public bool Replace(TKey key, TValue oldValue, TValue newValue);
}
```

**Description:**
A thread-safe bidirectional map that maintains mappings in both directions. Supports reverse lookups from value to key with O(1) performance.

**Type Parameters:**
- `TKey`: The type of keys in the map (must be non-null)
- `TValue`: The type of values in the map (must be non-null)

**Uniqueness Constraint:**
Each value can only be associated with one key. When a value is added that already exists with a different key, the old key is automatically removed.

**Methods:**

#### GetKey
Performs a reverse lookup to find the key associated with a value.

**Parameters:**
- `value` (TValue): The value to locate

**Returns:** The associated key, or null if not found

**Example:**
```csharp
var biMap = new BiMap<string, int>();
biMap.Add("one", 1);
biMap.Add("two", 2);

string? key = biMap.GetKey(2); // Returns "two"
```

#### TryGetKey
Attempts to get the key associated with a value.

**Parameters:**
- `value` (TValue): The value to locate
- `key` (out TKey): The associated key if found

**Returns:** `true` if value was found; otherwise `false`

#### PutAll
Adds all key-value pairs from a dictionary.

**Parameters:**
- `dictionary` (IDictionary<TKey, TValue>): The pairs to add

**Example:**
```csharp
var biMap = new BiMap<string, int>();
var items = new Dictionary<string, int>
{
    { "one", 1 },
    { "two", 2 }
};
biMap.PutAll(items);
```

#### PutIfAbsent
Adds a key-value pair only if the key is not already present.

**Parameters:**
- `key` (TKey): The key to add
- `value` (TValue): The value to associate

**Returns:** The existing value if key is present, otherwise the newly added value

**Example:**
```csharp
var biMap = new BiMap<string, int>();
biMap.PutIfAbsent("one", 1); // Returns 1 (added)
biMap.PutIfAbsent("one", 100); // Returns 1 (exists, not modified)
```

#### TryReplace
Replaces the value for a key only if the key exists.

**Parameters:**
- `key` (TKey): The key whose value should be replaced
- `newValue` (TValue): The new value
- `oldValue` (out TValue): The previous value if replacement succeeded

**Returns:** `true` if key was found and replaced; otherwise `false`

#### Replace
Replaces the value only if currently mapped to the expected old value (compare-and-swap).

**Parameters:**
- `key` (TKey): The key whose value should be replaced
- `oldValue` (TValue): The expected current value
- `newValue` (TValue): The new value

**Returns:** `true` if value was replaced; otherwise `false`

**Example:**
```csharp
var biMap = new BiMap<string, int>();
biMap.Add("one", 1);

// Optimistic locking pattern
if (biMap.Replace("one", 1, 100))
{
    Console.WriteLine("Updated successfully");
}
```

**Notes:**
- ✅ Thread-safe with lock-based synchronization
- ✅ O(1) reverse lookups via internal reverse dictionary
- ✅ Enforces bidirectional uniqueness constraint
- ⚠️ Snapshots returned for enumeration (no lock held during iteration)

**Related:**
- [DomainEventTypeMapper](#domaineventtypemapper)

---

## EzDdd.Entity

Core DDD building blocks for the entities layer.

### IEntity

**Namespace:** `EzDdd.Entity`

**Signature:**
```csharp
public interface IEntity<out TId>
{
    TId Id { get; }
}
```

**Description:**
Marker interface for entities with unique identity. An entity is defined by its unique identifier, not by its attributes. Two entities with the same ID are considered the same entity regardless of attribute differences.

**Type Parameters:**
- `TId`: The type of the entity's unique identifier (covariant)

**Properties:**
- `Id`: The unique identifier that distinguishes this entity

**Example:**
```csharp
public class Order : IEntity<Guid>
{
    public Guid Id { get; private set; }
    public string CustomerName { get; private set; }
    public decimal TotalAmount { get; private set; }

    // Entity equality based on ID
    public override bool Equals(object? obj) =>
        obj is Order other && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}
```

**Notes:**
- ✅ Identity-based equality (same ID = same entity)
- ✅ Entities have lifecycle (can change state over time)
- ✅ Covariant type parameter enables flexible assignments

**Related:**
- [IValueObject](#ivalueobject)
- [AggregateRoot](#aggregateroot)

---

### IValueObject

**Namespace:** `EzDdd.Entity`

**Signature:**
```csharp
public interface IValueObject
{
    // Pure marker interface - zero methods
}
```

**Description:**
Marker interface for value objects. A value object is defined by its attributes, not by a unique identity. Two value objects with identical attribute values are considered equal. Value objects should be immutable.

**Recommendation:**
Use C# `record` types for value object implementations, as records provide structural equality and immutability by default.

**Example:**
```csharp
// Recommended: Record-based value object
public record Money(decimal Amount, string Currency) : IValueObject;

// Usage:
var money1 = new Money(100m, "USD");
var money2 = new Money(100m, "USD");
Assert.Equal(money1, money2); // true - structural equality

// Alternative: Class-based value object (manual equality)
public class Email : IValueObject
{
    public string Value { get; }

    public Email(string value)
    {
        Value = value;
    }

    public override bool Equals(object? obj) =>
        obj is Email other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();
}
```

**Notes:**
- ✅ No unique identifier
- ✅ Structural equality (same attributes = same value)
- ✅ Immutability required
- ✅ Pure marker interface (maximum flexibility)

**Related:**
- [IEntity](#ientity)
- [IDomainEvent](#idomainevent)

---

### IDomainEvent

**Namespace:** `EzDdd.Entity`

**Signature:**
```csharp
public interface IDomainEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredOn { get; }
    string Source { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
}
```

**Description:**
Base interface for all domain events. A domain event represents something that happened in the domain that domain experts care about. Events are immutable facts that cannot be changed.

**Properties:**
- `Id`: Unique identifier for this specific event instance
- `OccurredOn`: Timestamp when the event occurred (with timezone)
- `Source`: Identifier of the aggregate that produced this event
- `Metadata`: Read-only dictionary of contextual information (CorrelationId, UserId, etc.)

**Example:**
```csharp
public record OrderCreated(
    Guid Id,
    DateTimeOffset OccurredOn,
    string Source,  // OrderId
    Guid CustomerId,
    decimal TotalAmount,
    IReadOnlyDictionary<string, string> Metadata
) : IInternalDomainEvent;

// Creating an event
var metadata = new Dictionary<string, string>
{
    { "CorrelationId", correlationId },
    { "UserId", userId }
};

var orderCreated = new OrderCreated(
    Id: Guid.NewGuid(),
    OccurredOn: DateTimeOffset.UtcNow,
    Source: orderId.ToString(),
    CustomerId: customerId,
    TotalAmount: 1250.00m,
    Metadata: metadata.AsReadOnly()
);
```

**Notes:**
- ✅ Events are immutable (the past cannot be altered)
- ✅ Use `record` types for automatic immutability
- ✅ `DateTimeOffset` preserves timezone information
- ✅ `IReadOnlyDictionary` enforces metadata immutability

**Related:**
- [IInternalDomainEvent](#iinternaldomainevent)
- [IExternalDomainEvent](#iexternaldomainevent)
- [AggregateRoot](#aggregateroot)

---

### IInternalDomainEvent

**Namespace:** `EzDdd.Entity`

**Signature:**
```csharp
public interface IInternalDomainEvent : IDomainEvent
{
    interface IConstructionEvent { }
    interface IDestructionEvent { }
}
```

**Description:**
Marker interface for domain events that occur within a single bounded context. Internal events are used for event sourcing and maintaining aggregate state. They form the event stream for event-sourced aggregates.

**Nested Interfaces:**

#### IConstructionEvent
Marks the first event in an aggregate's lifecycle (creation).

**Event Sourcing Rule R1:**
```
{pre₀} fun₀ {post₀ & INV}
```
No precondition invariant check, must satisfy postcondition invariants.

#### IDestructionEvent
Marks the last event in an aggregate's lifecycle (deletion).

**Event Sourcing Rule R3:**
```
{preᵤ & INV} funᵤ {postᵤ}
```
Must satisfy precondition invariants, no postcondition check.

**Example:**
```csharp
// Construction event (R1 rule - first event)
public record AccountCreated(
    Guid Id,
    DateTimeOffset OccurredOn,
    string Source,  // AccountId
    string Owner,
    decimal InitialBalance,
    IReadOnlyDictionary<string, string> Metadata
) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent;

// Command event (R2 rule - middle events)
public record MoneyDeposited(
    Guid Id,
    DateTimeOffset OccurredOn,
    string Source,  // AccountId
    decimal Amount,
    IReadOnlyDictionary<string, string> Metadata
) : IInternalDomainEvent;

// Destruction event (R3 rule - last event)
public record AccountClosed(
    Guid Id,
    DateTimeOffset OccurredOn,
    string Source,  // AccountId
    string Reason,
    IReadOnlyDictionary<string, string> Metadata
) : IInternalDomainEvent, IInternalDomainEvent.IDestructionEvent;
```

**Notes:**
- ✅ Used for event sourcing within bounded context
- ✅ Stored in event store (append-only)
- ✅ Not intended for cross-context integration
- ✅ Construction event must be first, destruction event must be last

**Related:**
- [EsAggregateRoot](#esaggregateroot)
- [IExternalDomainEvent](#iexternaldomainevent)
- [EsRepository](#esrepository)

---

### AggregateRoot

**Namespace:** `EzDdd.Entity`

**Signature:**
```csharp
public abstract class AggregateRoot<TId, TEvent> : IEntity<TId>
    where TEvent : class, IInternalDomainEvent
{
    public TId Id { get; protected set; }
    public long Version { get; protected set; } = -1;
    public bool IsDeleted { get; protected set; }

    public virtual void Apply(TEvent @event);
    public IReadOnlyList<TEvent> GetDomainEvents();
    public TEvent? GetLastDomainEvent();
    public int GetDomainEventSize();
    public void ClearDomainEvents();
}
```

**Description:**
Abstract base class for aggregate roots. An aggregate root is the entry point to an aggregate and maintains a collection of domain events representing state changes.

**Type Parameters:**
- `TId`: The type of the aggregate's unique identifier
- `TEvent`: The type of internal domain events this aggregate produces

**Properties:**
- `Id`: The unique identifier of this aggregate
- `Version`: Version number for optimistic concurrency control (starts at -1)
- `IsDeleted`: Soft delete flag

**Version Semantics:**
- Initial state: `Version = -1` (not yet persisted)
- After first event: `Version = 0`
- After second event: `Version = 1`
- Version = number of events applied

**Methods:**

#### Apply
Applies a domain event to this aggregate (template method).

**Parameters:**
- `@event` (TEvent): The domain event to apply

**Example:**
```csharp
// State sourcing aggregate
public class Order : AggregateRoot<Guid, IInternalDomainEvent>
{
    private OrderStatus _status = OrderStatus.Draft;
    private List<OrderItem> _items = new();

    public Order(Guid orderId, Guid customerId)
    {
        Id = orderId;

        var created = new OrderCreated(
            Id: Guid.NewGuid(),
            OccurredOn: DateTimeOffset.UtcNow,
            Source: orderId.ToString(),
            CustomerId: customerId,
            Metadata: new Dictionary<string, string>());

        Apply(created); // Adds event, increments version
        _status = OrderStatus.Created; // Manual state mutation
    }

    public void AddItem(string productId, int quantity)
    {
        var itemAdded = new OrderItemAdded(/* ... */);
        Apply(itemAdded);
        _items.Add(new OrderItem(productId, quantity));
    }
}
```

#### GetDomainEvents
Gets a read-only snapshot of all domain events.

**Returns:** IReadOnlyList<TEvent> - Defensive copy of event collection

#### GetLastDomainEvent
Gets the most recent domain event.

**Returns:** TEvent? - The last event, or null if no events

#### GetDomainEventSize
Gets the number of domain events currently in the collection.

**Returns:** int - Event count

#### ClearDomainEvents
Clears all domain events from the collection (called by repository after save).

**Notes:**
- ✅ Thread-safe event collection via lock-based synchronization
- ✅ Supports both state sourcing and event sourcing
- ✅ Template method pattern for customization
- ✅ Optimistic locking via version number

**Related:**
- [EsAggregateRoot](#esaggregateroot)
- [IRepository](#irepository)

---

### EsAggregateRoot

**Namespace:** `EzDdd.Entity`

**Signature:**
```csharp
public abstract class EsAggregateRoot<TId, TEvent> : AggregateRoot<TId, TEvent>
    where TEvent : class, IInternalDomainEvent
{
    protected EsAggregateRoot();
    public EsAggregateRoot(IEnumerable<TEvent> events);

    public sealed override void Apply(TEvent @event);
    protected abstract void _When(TEvent @event);
    protected virtual void _EnsureInvariant();

    public abstract string GetCategory();
    public string GetStreamName();
}
```

**Description:**
Abstract base class for event-sourced aggregate roots. Enforces event sourcing correctness rules (R1, R2, R3) through template method pattern with strategic invariant checking.

**Type Parameters:**
- `TId`: The type of the aggregate's unique identifier
- `TEvent`: The type of internal domain events

**Event Sourcing Correctness Rules:**

**R1 (Construction):** `{pre₀} fun₀ {post₀ & INV}`
- First event establishes initial state
- No precondition check, postcondition invariants checked

**R2 (Command):** `{preₜ & INV} funₜ {postₜ & INV}`
- Middle events maintain invariants
- Both precondition and postcondition invariants checked

**R3 (Destruction):** `{preᵤ & INV} funᵤ {postᵤ}`
- Last event finalizes deletion
- Precondition invariants checked, no postcondition check

**Constructors:**

#### Default Constructor
For creating new aggregates.

**Example:**
```csharp
public Account(Guid id, string owner, decimal initialBalance)
{
    var created = new AccountCreated(/* ... */);
    Apply(created); // Enforces R1
}
```

#### Event Replay Constructor
For reconstructing aggregates from event history.

**Parameters:**
- `events` (IEnumerable<TEvent>): The event history to replay

**Example:**
```csharp
// Constructor for event replay (REQUIRED)
public Account(IEnumerable<IInternalDomainEvent> events) : base(events) { }
```

**Methods:**

#### Apply (sealed)
Applies a domain event with invariant checking (cannot be overridden).

**Algorithm:**
1. If NOT construction event: Call `_EnsureInvariant()` (precondition)
2. Call `_When(@event)` to mutate state
3. If NOT destruction event: Call `_EnsureInvariant()` (postcondition)
4. Add event to collection via `_AddDomainEvent()`

#### _When (abstract)
Mutates aggregate state in response to events (must be implemented by subclass).

**Parameters:**
- `@event` (TEvent): The domain event to handle

**Pattern:** Use switch expressions for type-based dispatch

**Example:**
```csharp
protected override void _When(IInternalDomainEvent @event)
{
    switch (@event)
    {
        case AccountCreated e:
            Id = Guid.Parse(e.Source);
            _owner = e.Owner;
            _balance = e.InitialBalance;
            break;

        case MoneyDeposited e:
            _balance += e.Amount;
            break;

        case MoneyWithdrawn e:
            _balance -= e.Amount;
            break;

        case AccountClosed e:
            IsDeleted = true;
            break;

        default:
            throw new InvalidOperationException(
                $"Unknown event type: {@event.GetType().Name}");
    }
}
```

#### _EnsureInvariant (virtual)
Checks business invariants for this aggregate.

**Example:**
```csharp
protected override void _EnsureInvariant()
{
    // Skip invariant checks for deleted aggregates
    if (IsDeleted) return;

    // Check business rules
    if (Id == Guid.Empty)
        throw new InvalidOperationException("Account ID must be set");

    if (string.IsNullOrEmpty(_owner))
        throw new InvalidOperationException("Account must have owner");

    if (_balance < 0)
        throw new InvalidOperationException("Balance cannot be negative");
}
```

#### GetCategory (abstract)
Returns the category name for event stream naming.

**Returns:** string - Category (e.g., "order", "customer", "account")

**Example:**
```csharp
public override string GetCategory() => "account";
```

#### GetStreamName
Returns the event stream name in format `{category}-{id}`.

**Returns:** string - Stream name (e.g., "account-550e8400-e29b-41d4-a716-446655440000")

**Complete Example:**
```csharp
public class Account : EsAggregateRoot<Guid, IInternalDomainEvent>
{
    private string _owner = string.Empty;
    private decimal _balance;

    // Constructor for new aggregate
    public Account(Guid id, string owner, decimal initialBalance)
    {
        var created = new AccountCreated(
            Id: Guid.NewGuid(),
            OccurredOn: DateTimeOffset.UtcNow,
            Source: id.ToString(),
            Owner: owner,
            InitialBalance: initialBalance,
            Metadata: new Dictionary<string, string>());

        Apply(created); // Enforces R1
    }

    // Constructor for event replay (REQUIRED)
    public Account(IEnumerable<IInternalDomainEvent> events) : base(events) { }

    public void Deposit(decimal amount)
    {
        var deposited = new MoneyDeposited(
            Id: Guid.NewGuid(),
            OccurredOn: DateTimeOffset.UtcNow,
            Source: Id.ToString(),
            Amount: amount,
            Metadata: new Dictionary<string, string>());

        Apply(deposited); // Enforces R2
    }

    protected override void _When(IInternalDomainEvent @event)
    {
        switch (@event)
        {
            case AccountCreated e:
                Id = Guid.Parse(e.Source);
                _owner = e.Owner;
                _balance = e.InitialBalance;
                break;
            case MoneyDeposited e:
                _balance += e.Amount;
                break;
        }
    }

    protected override void _EnsureInvariant()
    {
        if (IsDeleted) return;
        if (_balance < 0)
            throw new InvalidOperationException("Balance cannot be negative");
    }

    public override string GetCategory() => "account";
}
```

**Notes:**
- ✅ Template method pattern enforces framework rules
- ✅ R1/R2/R3 correctness rules automatically enforced
- ✅ Subclasses focus on `_When()` and `_EnsureInvariant()`
- ⚠️ Must provide event replay constructor

**Related:**
- [IInternalDomainEvent](#iinternaldomainevent)
- [EsRepository](#esrepository)
- [AggregateRoot](#aggregateroot)

---

### DomainEventTypeMapper

**Namespace:** `EzDdd.Entity`

**Signature:**
```csharp
public static class DomainEventTypeMapper
{
    public static void Register<TEvent>(string typeName) where TEvent : IInternalDomainEvent;
    public static string GetTypeName(Type eventType);
    public static string GetTypeName(IInternalDomainEvent @event);
    public static Type GetType(string typeName);
    public static bool Contains(string typeName);
    public static IReadOnlyDictionary<string, Type> GetAllMappings();
    public static void Clear();
}
```

**Description:**
Static utility for mapping domain event types to string names and vice versa. Used for event serialization when storing events in an event store or database. Thread-safe via internal BiMap.

**Methods:**

#### Register<TEvent>
Registers a domain event type with its string identifier.

**Parameters:**
- `typeName` (string): The string identifier for this event type

**Exceptions:**
- `ArgumentNullException`: When typeName is null or empty
- `ArgumentException`: When typeName is already registered to a different type

**Example:**
```csharp
// Application startup - register all event types
DomainEventTypeMapper.Register<OrderCreated>("OrderCreated");
DomainEventTypeMapper.Register<OrderItemAdded>("OrderItemAdded");
DomainEventTypeMapper.Register<OrderCancelled>("OrderCancelled");
```

#### GetTypeName(Type)
Gets the string identifier for an event type.

**Parameters:**
- `eventType` (Type): The event type

**Returns:** string - The registered string identifier

**Exceptions:**
- `InvalidOperationException`: When the event type is not registered

**Example:**
```csharp
string typeName = DomainEventTypeMapper.GetTypeName(typeof(OrderCreated));
// Returns: "OrderCreated"
```

#### GetTypeName(IInternalDomainEvent)
Gets the string identifier for an event instance.

**Parameters:**
- `@event` (IInternalDomainEvent): The domain event instance

**Returns:** string - The registered string identifier

**Example:**
```csharp
var orderCreated = new OrderCreated(/* ... */);
string typeName = DomainEventTypeMapper.GetTypeName(orderCreated);
// Returns: "OrderCreated"
```

#### GetType
Gets the domain event type for a string identifier.

**Parameters:**
- `typeName` (string): The string identifier

**Returns:** Type - The registered event type

**Exceptions:**
- `InvalidOperationException`: When the type name is not registered

**Example:**
```csharp
Type eventType = DomainEventTypeMapper.GetType("OrderCreated");
// Returns: typeof(OrderCreated)

// Use with reflection to deserialize
var eventInstance = JsonSerializer.Deserialize(json, eventType);
```

#### Contains
Checks if a type name is registered.

**Parameters:**
- `typeName` (string): The string identifier to check

**Returns:** bool - True if registered; otherwise false

**Example:**
```csharp
if (DomainEventTypeMapper.Contains("OrderCreated"))
{
    var type = DomainEventTypeMapper.GetType("OrderCreated");
}
```

#### GetAllMappings
Gets all registered mappings as a read-only dictionary.

**Returns:** IReadOnlyDictionary<string, Type> - Snapshot of all mappings

**Example:**
```csharp
var mappings = DomainEventTypeMapper.GetAllMappings();
foreach (var (typeName, eventType) in mappings)
{
    Console.WriteLine($"{typeName} -> {eventType.Name}");
}
```

#### Clear
Clears all registered mappings (primarily for testing).

**Usage Pattern:**
```csharp
// 1. Register all event types at application startup
DomainEventTypeMapper.Register<AccountCreated>("AccountCreated");
DomainEventTypeMapper.Register<MoneyDeposited>("MoneyDeposited");

// 2. Use GetTypeName when serializing events
string eventType = DomainEventTypeMapper.GetTypeName(@event);
string json = JsonSerializer.Serialize(@event);
// Store: eventType, json

// 3. Use GetType when deserializing events
Type type = DomainEventTypeMapper.GetType(storedEventType);
var eventInstance = JsonSerializer.Deserialize(storedJson, type);
```

**Notes:**
- ✅ Thread-safe (uses BiMap internally)
- ✅ Convention: Use event class name as type name
- ✅ Avoids fully-qualified names (refactoring-friendly)
- ⚠️ Must register all event types at startup

**Related:**
- [BiMap](#bimap)
- [DomainEventMapper](#domaineventmapper)
- [EsRepository](#esrepository)

---

## EzDdd.UseCase

Use cases layer with persistence abstractions, event infrastructure, and messaging.

### Foundation Interfaces

#### IInput

**Namespace:** `EzDdd.UseCase.Port.In`

**Signature:**
```csharp
public interface IInput
{
    static NullInput OfNull();
    sealed record NullInput : IInput;
}
```

**Description:**
Marker interface for representing the input of a use case execution.

**Example:**
```csharp
public record CreateAccountInput(
    string AccountNumber,
    string Owner,
    decimal InitialBalance
) : IInput;

// Using NullInput for use cases without parameters
IInput nullInput = IInput.OfNull();
```

**Notes:**
- ✅ Pure marker interface (no methods)
- ✅ Provides NullInput for parameterless use cases
- ✅ Use `record` types for immutability

**Related:**
- [IOutput](#ioutput)
- [IUseCase](#iusecase)

---

#### IOutput

**Namespace:** `EzDdd.UseCase.Port.In`

**Signature:**
```csharp
public interface IOutput
{
    string Message { get; }
    ExitCode ExitCode { get; }
    string Id { get; }

    IOutput SetMessage(string message);
    IOutput SetExitCode(ExitCode exitCode);
    IOutput Fail();
    IOutput Succeed();
    IOutput SetId(string id);
}
```

**Description:**
Interface for representing the output after executing a use case. Provides fluent API for building output objects.

**Properties:**
- `Message`: Human-readable message
- `ExitCode`: Execution status code
- `Id`: Identifier associated with output

**Methods:**
All methods return `IOutput` for fluent API chaining.

**Example:**
```csharp
public class CreateAccountOutput : IOutput
{
    public string Message { get; private set; } = string.Empty;
    public ExitCode ExitCode { get; private set; } = ExitCode.Success;
    public string Id { get; private set; } = string.Empty;
    public string AccountNumber { get; private set; } = string.Empty;

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

    public IOutput SetId(string id)
    {
        Id = id;
        return this;
    }

    public CreateAccountOutput SetAccountNumber(string accountNumber)
    {
        AccountNumber = accountNumber;
        return this;
    }
}

// Usage:
var output = new CreateAccountOutput()
    .SetId(accountId.ToString())
    .SetAccountNumber("123456")
    .SetMessage("Account created successfully")
    .Succeed();
```

**Notes:**
- ✅ Fluent API for building outputs
- ✅ Standardized success/failure semantics
- ✅ Extensible for domain-specific properties

**Related:**
- [ExitCode](#exitcode)
- [CqrsOutput](#cqrsoutput)
- [IUseCase](#iusecase)

---

#### IVersionedInput

**Namespace:** `EzDdd.UseCase.Port.In`

**Signature:**
```csharp
public interface IVersionedInput : IInput
{
    long Version { get; }
}
```

**Description:**
Marker interface for inputs that carry version information for optimistic locking.

**Properties:**
- `Version`: Expected version number of the aggregate

**Example:**
```csharp
public record UpdateAccountInput(
    Guid AccountId,
    long Version,
    string NewOwner
) : IVersionedInput;

// Usage in command:
var account = await _repository.FindByIdAsync(input.AccountId);
if (account.Version != input.Version)
{
    return output.Fail().SetMessage("Optimistic locking conflict");
}
```

**Notes:**
- ✅ Supports optimistic locking in commands
- ✅ Version typically comes from read model
- ✅ Prevents lost updates in concurrent scenarios

**Related:**
- [IRepository](#irepository)
- [AggregateRoot](#aggregateroot)

---

#### ExitCode

**Namespace:** `EzDdd.UseCase.Port.In`

**Signature:**
```csharp
public enum ExitCode
{
    Success = 0,
    Failure = 1,
    ResourceNotFoundFailure = 404,
    ConflictFailure = 409,
    ValidationFailure = 422
}
```

**Description:**
Enumeration of standard exit codes for use case execution results.

**Values:**
- `Success (0)`: Operation completed successfully
- `Failure (1)`: General failure
- `ResourceNotFoundFailure (404)`: Requested resource not found
- `ConflictFailure (409)`: Conflict detected (e.g., optimistic locking)
- `ValidationFailure (422)`: Input validation failed

**Example:**
```csharp
var account = await _repository.FindByIdAsync(input.AccountId);
if (account == null)
{
    return output
        .SetExitCode(ExitCode.ResourceNotFoundFailure)
        .SetMessage("Account not found");
}

try
{
    await _repository.SaveAsync(account);
}
catch (RepositorySaveException ex)
    when (ex.Message == RepositorySaveException.OptimisticLockingFailure)
{
    return output
        .SetExitCode(ExitCode.ConflictFailure)
        .SetMessage("Concurrent modification detected");
}

return output
    .SetExitCode(ExitCode.Success)
    .SetMessage("Operation completed");
```

**Notes:**
- ✅ HTTP-aligned status codes (404, 409, 422)
- ✅ Standardized semantics across use cases
- ✅ Enables consistent error handling

**Related:**
- [IOutput](#ioutput)
- [UseCaseFailureException](#usecasefailureexception)

---

#### IReactor

**Namespace:** `EzDdd.UseCase.Port.In`

**Signature:**
```csharp
public interface IReactor
{
    Task ExecuteAsync();
}
```

**Description:**
Marker interface for reactors that respond to domain events without input.

**Example:**
```csharp
public class AccountCreatedReactor : IReactor
{
    private readonly IExternalDomainEvent _event;
    private readonly IEmailService _emailService;

    public AccountCreatedReactor(
        IExternalDomainEvent @event,
        IEmailService emailService)
    {
        _event = @event;
        _emailService = emailService;
    }

    public async Task ExecuteAsync()
    {
        var accountCreated = (AccountCreated)_event;
        await _emailService.SendWelcomeEmailAsync(accountCreated.Owner);
    }
}
```

**Notes:**
- ✅ Used with IMessageBus for event-driven reactions
- ✅ No return value (fire-and-forget semantics)
- ✅ Asynchronous execution

**Related:**
- [IMessageBus](#imessagebus)
- [GenericReactor](#genericreactor)

---

### Use Case Pattern

#### IUseCase

**Namespace:** `EzDdd.UseCase.Port.In`

**Signature:**
```csharp
public interface IUseCase<in TInput, TOutput>
    where TInput : IInput
    where TOutput : IOutput
{
    Task<TOutput> ExecuteAsync(TInput input);
}
```

**Description:**
Interface for representing a use case in Clean Architecture. Use cases encapsulate application-specific business rules.

**Type Parameters:**
- `TInput`: The input type (contravariant)
- `TOutput`: The output type

**Methods:**

#### ExecuteAsync
Executes the use case with the given input.

**Parameters:**
- `input` (TInput): The input for the use case

**Returns:** Task<TOutput> - The output result

**Exceptions:**
- `UseCaseFailureException`: When the use case cannot fulfill its specifications

**Example:**
```csharp
public class DepositMoneyUseCase : IUseCase<DepositInput, DepositOutput>
{
    private readonly IRepository<BankAccount, Guid, IInternalDomainEvent> _repository;

    public DepositMoneyUseCase(
        IRepository<BankAccount, Guid, IInternalDomainEvent> repository)
    {
        _repository = repository;
    }

    public async Task<DepositOutput> ExecuteAsync(DepositInput input)
    {
        // 1. Load aggregate
        var account = await _repository.FindByIdAsync(input.AccountId);
        if (account == null)
        {
            return new DepositOutput()
                .SetExitCode(ExitCode.ResourceNotFoundFailure)
                .SetMessage("Account not found");
        }

        // 2. Execute domain logic
        account.Deposit(input.Amount);

        // 3. Save aggregate
        try
        {
            await _repository.SaveAsync(account);
        }
        catch (RepositorySaveException ex)
        {
            return new DepositOutput()
                .SetExitCode(ExitCode.Failure)
                .SetMessage(ex.Message);
        }

        return new DepositOutput()
            .SetId(account.Id.ToString())
            .SetMessage($"Deposited {input.Amount}")
            .Succeed();
    }
}
```

**Notes:**
- ✅ Clean Architecture application layer
- ✅ Generic variance for flexible composition
- ✅ Async/await throughout

**Related:**
- [ICommand](#icommand)
- [IQuery](#iquery)
- [IRepository](#irepository)

---

#### UseCaseFailureException

**Namespace:** `EzDdd.UseCase.Exceptions`

**Signature:**
```csharp
public class UseCaseFailureException : Exception
{
    public UseCaseFailureException(string message);
    public UseCaseFailureException(string message, Exception innerException);
}
```

**Description:**
Exception thrown when a use case cannot fulfill its specifications. Represents business-level failures.

**Example:**
```csharp
public async Task<TransferOutput> ExecuteAsync(TransferInput input)
{
    var account = await _repository.FindByIdAsync(input.AccountId);
    if (account == null)
    {
        throw new UseCaseFailureException(
            $"Account {input.AccountId} not found");
    }

    if (account.Balance < input.Amount)
    {
        throw new UseCaseFailureException(
            $"Insufficient funds. Balance: {account.Balance}, Required: {input.Amount}");
    }

    // Continue with transfer...
}
```

**Notes:**
- ✅ Business-level exception (not technical failure)
- ✅ Should be caught by application controllers
- ✅ Contains user-friendly error messages

**Related:**
- [IUseCase](#iusecase)
- [RepositorySaveException](#repositorysaveexception)

---

### Repository Pattern

#### IRepository

**Namespace:** `EzDdd.UseCase.Port.Out`

**Signature:**
```csharp
public interface IRepository<TAggregate, in TId, TEvent>
    where TAggregate : AggregateRoot<TId, TEvent>
    where TEvent : class, IInternalDomainEvent
{
    Task<TAggregate?> FindByIdAsync(TId id);
    Task SaveAsync(TAggregate aggregate);
    Task DeleteAsync(TAggregate aggregate);
}
```

**Description:**
Repository abstraction for persisting and retrieving aggregates. Belongs to the use case layer (domain layer) in Clean Architecture. Works with domain objects and provides infrastructure-agnostic persistence.

**Type Parameters:**
- `TAggregate`: The type of aggregate root
- `TId`: The type of the aggregate identifier
- `TEvent`: The type of domain events

**Bridge Pattern:**
```
Domain Layer (Use Case)
    IRepository<TAggregate, TId, TEvent> (Abstraction)
         ↓ uses
Adapter Layer (Interface Adapters)
    IRepositoryPeer<TData, TId> (Implementor)
         ↓ implements
Infrastructure Layer
    SqlRepositoryPeer, MongoRepositoryPeer, etc.
```

**Methods:**

#### FindByIdAsync
Finds an aggregate by its identifier.

**Parameters:**
- `id` (TId): The aggregate identifier

**Returns:** Task<TAggregate?> - The aggregate if found, or null

**Example:**
```csharp
var account = await _repository.FindByIdAsync(accountId);
if (account == null)
{
    return new DepositOutput()
        .SetExitCode(ExitCode.ResourceNotFoundFailure)
        .SetMessage("Account not found");
}
```

#### SaveAsync
Saves an aggregate (create or update).

**Parameters:**
- `aggregate` (TAggregate): The aggregate to save

**Returns:** Task - Async save operation

**Exceptions:**
- `RepositorySaveException`: Thrown when save fails (optimistic locking, constraints, etc.)

**Example:**
```csharp
try
{
    await _repository.SaveAsync(account);
}
catch (RepositorySaveException ex)
    when (ex.Message == RepositorySaveException.OptimisticLockingFailure)
{
    return new DepositOutput()
        .SetExitCode(ExitCode.ConflictFailure)
        .SetMessage("Concurrent modification detected");
}
```

#### DeleteAsync
Deletes an aggregate.

**Parameters:**
- `aggregate` (TAggregate): The aggregate to delete

**Returns:** Task - Async delete operation

**Example:**
```csharp
var account = await _repository.FindByIdAsync(accountId);
if (account != null)
{
    account.Close(); // Domain logic for deletion
    await _repository.DeleteAsync(account);
}
```

**Transaction Boundary:**
- ❌ IRepository implementations MUST NOT contain transaction logic
- ✅ Transaction boundaries MUST be at IRepositoryPeer layer only

**Notes:**
- ✅ Works with domain objects
- ✅ Infrastructure agnostic
- ✅ No transaction management at this layer
- ✅ Bridge pattern for layer separation

**Related:**
- [IRepositoryPeer](#irepositorypeer)
- [EsRepository](#esrepository)
- [OutboxRepository](#outboxrepository)

---

#### IRepositoryPeer

**Namespace:** `EzDdd.UseCase.Port.Out`

**Signature:**
```csharp
public interface IRepositoryPeer<TData, in TId>
    where TData : IStoreData<TId>
{
    Task<TData?> FindByIdAsync(TId id);
    Task SaveAsync(TData data);
    Task DeleteAsync(TData data);
}
```

**Description:**
Repository Service Provider Interface (SPI) for actual persistence implementation. Belongs to the adapter layer (interface adapters) in Clean Architecture. This is the implementor role in the Bridge pattern.

**Type Parameters:**
- `TData`: The type of persistence data structure
- `TId`: The type of the identifier

**Architecture Layers:**
```
Domain Layer (Use Case)
    IRepository<TAggregate, TId, TEvent> (Abstraction)
         ↓ depends on
Adapter Layer
    IRepositoryPeer<TData, TId> (Implementor) ← YOU ARE HERE
         ↓ implemented by
Infrastructure Layer
    SqlRepositoryPeer, MongoRepositoryPeer, etc.
```

**Transaction Boundary:**
- ✅ IRepositoryPeer implementations MUST manage transactions
- ✅ Ensures atomic persistence of aggregate state AND events
- ✅ Rollback on failure

**Transaction Strategies:**
- **EF Core**: `await _dbContext.Database.BeginTransactionAsync()`
- **ADO.NET**: `await connection.BeginTransactionAsync()`
- **TransactionScope**: `new TransactionScope(TransactionScopeAsyncFlowOption.Enabled)`

**Methods:**

#### FindByIdAsync
Loads data from persistence by identifier.

**Parameters:**
- `id` (TId): The data identifier

**Returns:** Task<TData?> - The data if found, or null

#### SaveAsync
Persists data to storage (MUST use transactions).

**Parameters:**
- `data` (TData): The data to save

**Returns:** Task - Async save operation

**Exceptions:**
- `RepositoryPeerSaveException`: Database-level errors

**Example:**
```csharp
public class SqlBankAccountRepositoryPeer
    : IRepositoryPeer<BankAccountData, Guid>
{
    private readonly ApplicationDbContext _dbContext;

    public async Task<BankAccountData?> FindByIdAsync(Guid id)
    {
        return await _dbContext.BankAccounts
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task SaveAsync(BankAccountData data)
    {
        using var transaction = await _dbContext.Database
            .BeginTransactionAsync();
        try
        {
            // 1. Upsert aggregate state
            if (data.Version == -1)
                _dbContext.BankAccounts.Add(data);
            else
                _dbContext.BankAccounts.Update(data);

            // 2. Store events in outbox (Transactional Outbox pattern)
            foreach (var @event in data.Events)
            {
                _dbContext.OutboxEvents.Add(new OutboxEvent
                {
                    EventId = @event.Id,
                    EventType = @event.GetType().Name,
                    EventData = JsonSerializer.Serialize(@event),
                    OccurredOn = @event.OccurredOn
                });
            }

            // 3. Commit atomically
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            throw new RepositoryPeerSaveException(
                "Optimistic locking failure", ex);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new RepositoryPeerSaveException(
                "Database save failed", ex);
        }
    }

    public async Task DeleteAsync(BankAccountData data)
    {
        _dbContext.BankAccounts.Remove(data);
        await _dbContext.SaveChangesAsync();
    }
}
```

#### DeleteAsync
Removes data from storage.

**Parameters:**
- `data` (TData): The data to delete

**Returns:** Task - Async delete operation

**Notes:**
- ✅ Works with persistence DTOs (not domain objects)
- ✅ Throws infrastructure exceptions
- ✅ MUST manage transactions
- ✅ Database technology specific

**Related:**
- [IRepository](#irepository)
- [IStoreData](#istoredata)
- [RepositoryPeerSaveException](#repositorypeersaveexception)

---

#### IStoreData

**Namespace:** `EzDdd.UseCase.Port.Out`

**Signature:**
```csharp
public interface IStoreData<out TId>
{
    TId Id { get; }
    long Version { get; }
    string StreamName { get; }
    IReadOnlyList<IInternalDomainEvent> Events { get; }
}
```

**Description:**
Base interface for persistence data structures used by IRepositoryPeer.

**Type Parameters:**
- `TId`: The type of identifier (covariant)

**Properties:**
- `Id`: The identifier
- `Version`: Version number for optimistic locking
- `StreamName`: Event stream name (for event sourcing)
- `Events`: Domain events to persist

**Example:**
```csharp
public class BankAccountData : IStoreData<Guid>
{
    public Guid Id { get; set; }
    public long Version { get; set; }
    public string StreamName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public IReadOnlyList<IInternalDomainEvent> Events { get; set; }
        = Array.Empty<IInternalDomainEvent>();
}
```

**Notes:**
- ✅ DTO for persistence layer
- ✅ Covariant identifier for flexibility
- ✅ Includes both state and events

**Related:**
- [IRepositoryPeer](#irepositorypeer)
- [EventStoreData](#eventstoredata)
- [IOutboxData](#ioutboxdata)

---

#### RepositorySaveException

**Namespace:** `EzDdd.UseCase.Exceptions`

**Signature:**
```csharp
public class RepositorySaveException : Exception
{
    public const string OptimisticLockingFailure = "Optimistic locking failure";

    public RepositorySaveException(string message);
    public RepositorySaveException(string message, Exception innerException);
}
```

**Description:**
Domain-level exception for repository save failures. Thrown by IRepository implementations.

**Constants:**
- `OptimisticLockingFailure`: Standard message for concurrent modification conflicts

**Example:**
```csharp
try
{
    await _repository.SaveAsync(account);
}
catch (RepositorySaveException ex)
    when (ex.Message == RepositorySaveException.OptimisticLockingFailure)
{
    // Handle optimistic locking conflict
    return output
        .SetExitCode(ExitCode.ConflictFailure)
        .SetMessage("Concurrent modification detected. Please retry.");
}
catch (RepositorySaveException ex)
{
    // Handle other save failures
    return output
        .SetExitCode(ExitCode.Failure)
        .SetMessage(ex.Message);
}
```

**Notes:**
- ✅ Domain-level exception
- ✅ Should be caught by use cases
- ✅ Contains user-friendly error messages

**Related:**
- [IRepository](#irepository)
- [RepositoryPeerSaveException](#repositorypeersaveexception)

---

#### RepositoryPeerSaveException

**Namespace:** `EzDdd.UseCase.Exceptions`

**Signature:**
```csharp
public class RepositoryPeerSaveException : Exception
{
    public RepositoryPeerSaveException(string message);
    public RepositoryPeerSaveException(string message, Exception innerException);
}
```

**Description:**
Infrastructure-level exception for repository peer save failures. Thrown by IRepositoryPeer implementations and typically caught/translated by IRepository.

**Example:**
```csharp
// In IRepositoryPeer implementation:
catch (DbUpdateConcurrencyException ex)
{
    throw new RepositoryPeerSaveException(
        "Optimistic locking failure", ex);
}

// In IRepository implementation:
try
{
    await _peer.SaveAsync(data);
}
catch (RepositoryPeerSaveException ex)
{
    throw new RepositorySaveException(ex.Message, ex);
}
```

**Notes:**
- ✅ Infrastructure-level exception
- ✅ Typically wrapped by RepositorySaveException
- ✅ Contains technical error details

**Related:**
- [IRepositoryPeer](#irepositorypeer)
- [RepositorySaveException](#repositorysaveexception)

---

### Event Infrastructure

#### IExternalDomainEvent

**Namespace:** `EzDdd.UseCase.Port.InOut`

**Signature:**
```csharp
public interface IExternalDomainEvent : IDomainEvent
{
    // Marker interface for cross-context integration events
}
```

**Description:**
Marker interface for domain events intended for cross-bounded-context integration. These events are published to external message brokers (RabbitMQ, Kafka, etc.).

**Example:**
```csharp
public record AccountCreatedIntegrationEvent(
    Guid Id,
    DateTimeOffset OccurredOn,
    string Source,
    string AccountNumber,
    string Owner,
    IReadOnlyDictionary<string, string> Metadata
) : IExternalDomainEvent;

// Publishing external events
var integrationEvent = new AccountCreatedIntegrationEvent(
    Id: Guid.NewGuid(),
    OccurredOn: DateTimeOffset.UtcNow,
    Source: accountId.ToString(),
    AccountNumber: account.AccountNumber,
    Owner: account.Owner,
    Metadata: new Dictionary<string, string>
    {
        { "BoundedContext", "Banking" }
    });

await _eventBusProducer.ProduceAsync(integrationEvent);
```

**Notes:**
- ✅ Used for bounded context integration
- ✅ Published to external message brokers
- ✅ Separate from internal events (different purpose)

**Related:**
- [IInternalDomainEvent](#iinternaldomainevent)
- [EventBusProducer](#eventbusproducer)

---

#### DomainEventData

**Namespace:** `EzDdd.UseCase.Port.InOut`

**Signature:**
```csharp
public record DomainEventData(
    Guid Id,
    string EventType,
    DateTimeOffset OccurredOn,
    string Source,
    string EventData,
    IReadOnlyDictionary<string, string> Metadata
);
```

**Description:**
Serializable record representing a domain event for persistence or transmission. Contains event metadata and JSON-serialized payload.

**Properties:**
- `Id`: Event unique identifier
- `EventType`: Event type name (from DomainEventTypeMapper)
- `OccurredOn`: Timestamp when event occurred
- `Source`: Aggregate identifier that produced the event
- `EventData`: JSON-serialized event payload
- `Metadata`: Event metadata dictionary

**Example:**
```csharp
var @event = new AccountCreated(/* ... */);

// Convert to DomainEventData for persistence
var eventData = new DomainEventData(
    Id: @event.Id,
    EventType: DomainEventTypeMapper.GetTypeName(@event),
    OccurredOn: @event.OccurredOn,
    Source: @event.Source,
    EventData: JsonUtil.AsString(@event),
    Metadata: @event.Metadata
);

// Store eventData in database...
```

**Notes:**
- ✅ Immutable record type
- ✅ JSON-serialized payload
- ✅ Database-friendly structure

**Related:**
- [DomainEventMapper](#domaineventmapper)
- [EventStoreData](#eventstoredata)

---

#### DomainEventMapper

**Namespace:** `EzDdd.UseCase.Port.InOut`

**Signature:**
```csharp
public static class DomainEventMapper
{
    public static Converter<IInternalDomainEvent, DomainEventData> ToData { get; }
    public static Converter<DomainEventData, IInternalDomainEvent> ToDomain { get; }
}
```

**Description:**
Static mapper providing converters between domain events and serialized event data.

**Converters:**

#### ToData
Converts an IInternalDomainEvent to DomainEventData.

**Example:**
```csharp
var @event = new AccountCreated(/* ... */);
DomainEventData eventData = DomainEventMapper.ToData(@event);
```

#### ToDomain
Converts a DomainEventData back to IInternalDomainEvent.

**Example:**
```csharp
IInternalDomainEvent @event = DomainEventMapper.ToDomain(eventData);
```

**Usage:**
```csharp
// Serialization (for persistence)
var events = aggregate.GetDomainEvents();
var eventDataList = events.Select(DomainEventMapper.ToData).ToList();

// Deserialization (for replay)
var eventDataList = await LoadEventsFromDatabaseAsync(streamName);
var events = eventDataList.Select(DomainEventMapper.ToDomain).ToList();
var aggregate = new Account(events);
```

**Notes:**
- ✅ Uses DomainEventTypeMapper for type resolution
- ✅ Uses JsonUtil for serialization
- ✅ Thread-safe static converters

**Related:**
- [DomainEventData](#domaineventdata)
- [DomainEventTypeMapper](#domaineventtypemapper)
- [EventStoreMapper](#eventstorem apper)

---

#### InternalDomainEventDto

**Namespace:** `EzDdd.UseCase.Port.InOut`

**Signature:**
```csharp
public record InternalDomainEventDto(
    Guid Id,
    string EventType,
    DateTimeOffset OccurredOn,
    string Source,
    string EventData,
    Dictionary<string, string> Metadata
);
```

**Description:**
DTO record for internal domain event serialization. Similar to DomainEventData but with mutable Metadata for deserialization compatibility.

**Properties:**
- `Id`: Event unique identifier
- `EventType`: Event type name
- `OccurredOn`: Timestamp
- `Source`: Aggregate identifier
- `EventData`: JSON-serialized payload
- `Metadata`: Mutable metadata dictionary

**Example:**
```csharp
var dto = new InternalDomainEventDto(
    Id: Guid.NewGuid(),
    EventType: "AccountCreated",
    OccurredOn: DateTimeOffset.UtcNow,
    Source: "account-123",
    EventData: jsonPayload,
    Metadata: new Dictionary<string, string>
    {
        { "CorrelationId", correlationId }
    }
);
```

**Notes:**
- ✅ Record type for immutability
- ✅ Mutable Metadata for deserialization
- ⚠️ Internal use (not exposed to domain)

**Related:**
- [DomainEventData](#domaineventdata)
- [OutboxMapper](#outboxmapper)

---

### Event Sourcing

#### EventStoreData

**Namespace:** `EzDdd.UseCase.Port.Out`

**Signature:**
```csharp
public sealed record EventStoreData<TId>(
    TId Id,
    long Version,
    string StreamName,
    IReadOnlyList<IInternalDomainEvent> Events
) : IStoreData<TId>;
```

**Description:**
Immutable record representing event stream data for event sourcing. Contains all events for a single aggregate instance.

**Type Parameters:**
- `TId`: The type of identifier

**Properties:**
- `Id`: Aggregate identifier
- `Version`: Current version (number of events - 1)
- `StreamName`: Event stream name (format: `{category}-{id}`)
- `Events`: All events in the stream

**Example:**
```csharp
var eventStoreData = new EventStoreData<Guid>(
    Id: accountId,
    Version: 2, // 3 events total (0, 1, 2)
    StreamName: "account-550e8400-e29b-41d4-a716-446655440000",
    Events: new List<IInternalDomainEvent>
    {
        new AccountCreated(/* ... */),
        new MoneyDeposited(/* ... */),
        new MoneyDeposited(/* ... */)
    }
);
```

**Notes:**
- ✅ Immutable record type
- ✅ Used by IRepositoryPeer<EventStoreData<TId>, TId>
- ✅ Contains complete event history for aggregate

**Related:**
- [EventStoreMapper](#eventstorem apper)
- [EsRepository](#esrepository)
- [IStoreData](#istoredata)

---

#### EventStoreMapper

**Namespace:** `EzDdd.UseCase.Port.Out`

**Signature:**
```csharp
public static class EventStoreMapper
{
    public static Converter<EventStoreData<TId>, TAggregate> ToDomain<TAggregate, TId>()
        where TAggregate : EsAggregateRoot<TId, IInternalDomainEvent>;

    public static Converter<TAggregate, EventStoreData<TId>> ToData<TAggregate, TId>()
        where TAggregate : EsAggregateRoot<TId, IInternalDomainEvent>;
}
```

**Description:**
Static mapper providing generic converters between event-sourced aggregates and event store data.

**Methods:**

#### ToDomain<TAggregate, TId>
Creates a converter from EventStoreData to aggregate (via event replay).

**Returns:** Converter<EventStoreData<TId>, TAggregate>

**Example:**
```csharp
var converter = EventStoreMapper.ToDomain<Account, Guid>();
Account account = converter(eventStoreData);
```

#### ToData<TAggregate, TId>
Creates a converter from aggregate to EventStoreData (extracts events).

**Returns:** Converter<TAggregate, EventStoreData<TId>>

**Example:**
```csharp
var converter = EventStoreMapper.ToData<Account, Guid>();
EventStoreData<Guid> data = converter(account);
```

**Usage:**
```csharp
// In EsRepository:
private readonly Converter<EventStoreData<TId>, TAggregate> _toDomain
    = EventStoreMapper.ToDomain<TAggregate, TId>();
private readonly Converter<TAggregate, EventStoreData<TId>> _toData
    = EventStoreMapper.ToData<TAggregate, TId>();

public async Task<TAggregate?> FindByIdAsync(TId id)
{
    var data = await _peer.FindByIdAsync(id);
    return data == null ? null : _toDomain(data);
}

public async Task SaveAsync(TAggregate aggregate)
{
    var data = _toData(aggregate);
    await _peer.SaveAsync(data);
    aggregate.ClearDomainEvents();
}
```

**Notes:**
- ✅ Generic converters for any aggregate type
- ✅ Handles event replay automatically
- ✅ Used internally by EsRepository

**Related:**
- [EventStoreData](#eventstoredata)
- [EsRepository](#esrepository)
- [EsAggregateRoot](#esaggregateroot)

---

#### EsRepository

**Namespace:** `EzDdd.UseCase.Port.Out`

**Signature:**
```csharp
public class EsRepository<TAggregate, TId>
    : IRepository<TAggregate, TId, IInternalDomainEvent>
    where TAggregate : EsAggregateRoot<TId, IInternalDomainEvent>
{
    public EsRepository(IRepositoryPeer<EventStoreData<TId>, TId> peer);

    public async Task<TAggregate?> FindByIdAsync(TId id);
    public async Task SaveAsync(TAggregate aggregate);
    public async Task DeleteAsync(TAggregate aggregate);
}
```

**Description:**
Generic event sourcing repository implementation. Reconstructs aggregates from event streams using reflection with cached constructor information for performance.

**Type Parameters:**
- `TAggregate`: The aggregate type (must extend EsAggregateRoot)
- `TId`: The type of identifier

**Constructor:**
**Parameters:**
- `peer` (IRepositoryPeer<EventStoreData<TId>, TId>): The repository peer for event store persistence

**Example:**
```csharp
// Setup
var eventStorePeer = new PostgresEventStorePeer();
var repository = new EsRepository<BankAccount, Guid>(eventStorePeer);

// Save aggregate (stores events)
var account = new BankAccount(accountId, "John Doe", 1000m);
account.Deposit(500m);
await repository.SaveAsync(account);

// Load aggregate (reconstructs from events)
var loaded = await repository.FindByIdAsync(accountId);
// loaded.Balance == 1500m (reconstructed by replaying events)
```

**Event Sourcing Flow:**

**Save:**
1. Extract events from aggregate using EventStoreMapper
2. Persist via peer (IRepositoryPeer)
3. Clear aggregate's domain events

**Load:**
1. Retrieve event stream from peer
2. Reconstruct aggregate by invoking event replay constructor via reflection
3. Return reconstructed aggregate

**Performance:**
Constructor reflection information is cached per aggregate type using ConcurrentDictionary to avoid repeated reflection overhead.

**Notes:**
- ✅ Generic implementation for any event-sourced aggregate
- ✅ Automatic event replay via reflection
- ✅ Constructor caching for performance
- ⚠️ Requires aggregate to have public constructor accepting IEnumerable<TEvent>

**Related:**
- [EsAggregateRoot](#esaggregateroot)
- [EventStoreData](#eventstoredata)
- [EventStoreMapper](#eventstorem apper)
- [IRepository](#irepository)

---

### State Sourcing

#### IOutboxData

**Namespace:** `EzDdd.UseCase.Port.Out`

**Signature:**
```csharp
public interface IOutboxData<out TId> : IStoreData<TId>
{
    // Marker interface extending IStoreData for state sourcing
}
```

**Description:**
Marker interface for state sourcing data with Transactional Outbox pattern. Contains both current aggregate state and domain events.

**Example:**
```csharp
public class BankAccountOutboxData : IOutboxData<Guid>
{
    public Guid Id { get; set; }
    public long Version { get; set; }
    public string StreamName { get; set; } = string.Empty;

    // Aggregate state
    public string Owner { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public bool IsDeleted { get; set; }

    // Domain events (Transactional Outbox)
    public IReadOnlyList<IInternalDomainEvent> Events { get; set; }
        = Array.Empty<IInternalDomainEvent>();
}
```

**Notes:**
- ✅ Extends IStoreData
- ✅ Contains both state and events
- ✅ Used with OutboxRepository

**Related:**
- [IStoreData](#istoredata)
- [OutboxRepository](#outboxrepository)
- [OutboxMapper](#outboxmapper)

---

#### OutboxMapper

**Namespace:** `EzDdd.UseCase.Port.Out`

**Signature:**
```csharp
public static class OutboxMapper
{
    public static Converter<TData, TAggregate> ToDomain<TData, TAggregate, TId>()
        where TData : IOutboxData<TId>
        where TAggregate : AggregateRoot<TId, IInternalDomainEvent>;

    public static Converter<TAggregate, TData> ToData<TData, TAggregate, TId>()
        where TData : IOutboxData<TId>, new()
        where TAggregate : AggregateRoot<TId, IInternalDomainEvent>;
}
```

**Description:**
Static mapper providing generic converters between state-sourced aggregates and outbox data.

**Methods:**

#### ToDomain<TData, TAggregate, TId>
Creates a converter from outbox data to aggregate (via parameterless constructor + reflection).

**Returns:** Converter<TData, TAggregate>

#### ToData<TData, TAggregate, TId>
Creates a converter from aggregate to outbox data (extracts state + events).

**Returns:** Converter<TAggregate, TData>

**Example:**
```csharp
// In OutboxRepository:
private readonly Converter<BankAccountOutboxData, BankAccount> _toDomain
    = OutboxMapper.ToDomain<BankAccountOutboxData, BankAccount, Guid>();
private readonly Converter<BankAccount, BankAccountOutboxData> _toData
    = OutboxMapper.ToData<BankAccountOutboxData, BankAccount, Guid>();

public async Task<BankAccount?> FindByIdAsync(Guid id)
{
    var data = await _peer.FindByIdAsync(id);
    return data == null ? null : _toDomain(data);
}

public async Task SaveAsync(BankAccount aggregate)
{
    var data = _toData(aggregate);
    await _peer.SaveAsync(data);
    aggregate.ClearDomainEvents();
}
```

**Notes:**
- ✅ Generic converters for any aggregate type
- ✅ Uses reflection to copy state
- ✅ Extracts events for outbox pattern

**Related:**
- [IOutboxData](#ioutboxdata)
- [OutboxRepository](#outboxrepository)

---

#### OutboxRepository

**Namespace:** `EzDdd.UseCase.Port.Out`

**Signature:**
```csharp
public class OutboxRepository<TAggregate, TId>
    : IRepository<TAggregate, TId, IInternalDomainEvent>
    where TAggregate : AggregateRoot<TId, IInternalDomainEvent>
{
    public OutboxRepository(IRepositoryPeer<TData, TId> peer)
        where TData : IOutboxData<TId>;

    public async Task<TAggregate?> FindByIdAsync(TId id);
    public async Task SaveAsync(TAggregate aggregate);
    public async Task DeleteAsync(TAggregate aggregate);
}
```

**Description:**
Generic state sourcing repository with Transactional Outbox pattern. Persists both current aggregate state AND domain events atomically in the same transaction.

**Type Parameters:**
- `TAggregate`: The aggregate type
- `TId`: The type of identifier

**Transactional Outbox Pattern:**
1. Store current aggregate state (snapshot)
2. Store domain events in outbox table
3. Both operations in same database transaction
4. Background process publishes events from outbox

**Example:**
```csharp
// Setup
var outboxPeer = new SqlBankAccountOutboxPeer();
var repository = new OutboxRepository<BankAccount, Guid>(outboxPeer);

// Save aggregate (stores state + events)
var account = new BankAccount(accountId, "Jane Doe", 2000m);
account.Withdraw(300m);
await repository.SaveAsync(account);

// Database contains:
// - bank_accounts table: { id, owner, balance, version }
// - outbox_events table: { AccountCreated, MoneyWithdrawn }

// Load aggregate (loads from state table)
var loaded = await repository.FindByIdAsync(accountId);
// loaded.Balance == 1700m (loaded from state, not events)
```

**Advantages:**
- ✅ Faster reads than pure event sourcing (current state readily available)
- ✅ Guaranteed event publishing (outbox pattern)
- ✅ Atomic persistence of state and events

**Trade-offs:**
- ⚠️ Dual write (state + events)
- ⚠️ Requires background worker to publish events from outbox

**Notes:**
- ✅ Generic implementation for any aggregate
- ✅ Atomic persistence via transaction at IRepositoryPeer layer
- ✅ Compatible with both event-sourced and state-sourced aggregates

**Related:**
- [IOutboxData](#ioutboxdata)
- [OutboxMapper](#outboxmapper)
- [IRepository](#irepository)

---

### Message Bus

#### IMessageBus

**Namespace:** `EzDdd.UseCase.Port.InOut.Messaging`

**Signature:**
```csharp
public interface IMessageBus<TMessage> : IDisposable
{
    Task ProduceAsync(TMessage message);
    void Subscribe(Func<TMessage, Task> reactor);
}
```

**Description:**
Intra-process message bus for domain event distribution. Supports asynchronous message production and subscription-based consumption.

**Type Parameters:**
- `TMessage`: The type of messages (typically IInternalDomainEvent or IExternalDomainEvent)

**Methods:**

#### ProduceAsync
Publishes a message to all subscribers asynchronously.

**Parameters:**
- `message` (TMessage): The message to publish

**Returns:** Task - Async operation

#### Subscribe
Registers a reactor function to receive messages.

**Parameters:**
- `reactor` (Func<TMessage, Task>): Async function that processes messages

**Example:**
```csharp
// Setup message bus
IMessageBus<IInternalDomainEvent> messageBus =
    new BlockingMessageBus<IInternalDomainEvent>();

// Subscribe reactors
messageBus.Subscribe(async @event =>
{
    if (@event is AccountCreated accountCreated)
    {
        await SendWelcomeEmail(accountCreated.Owner);
    }
});

messageBus.Subscribe(async @event =>
{
    if (@event is MoneyDeposited deposited)
    {
        await UpdateBalanceProjection(deposited);
    }
});

// Produce events
await messageBus.ProduceAsync(new AccountCreated(/* ... */));
```

**Notes:**
- ✅ In-process pub/sub (not distributed)
- ✅ Asynchronous message handling
- ✅ Multiple subscribers supported
- ✅ Observer pattern

**Related:**
- [BlockingMessageBus](#blockingmessagebus)
- [IReactor](#ireactor)
- [GenericReactor](#genericreactor)

---

#### IMessageProducer

**Namespace:** `EzDdd.UseCase.Port.InOut.Messaging`

**Signature:**
```csharp
public interface IMessageProducer : IDisposable
{
    Task ProduceAsync(IInternalDomainEvent @event);
}
```

**Description:**
Interface for producing internal domain events (for IMessageBus) or external events (for external message brokers).

**Methods:**

#### ProduceAsync
Produces a domain event.

**Parameters:**
- `@event` (IInternalDomainEvent): The event to produce

**Returns:** Task - Async operation

**Example:**
```csharp
public class AccountEventProducer : IMessageProducer
{
    private readonly IMessageBus<IInternalDomainEvent> _messageBus;

    public async Task ProduceAsync(IInternalDomainEvent @event)
    {
        await _messageBus.ProduceAsync(@event);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
```

**Notes:**
- ✅ Abstraction for event production
- ✅ Supports both internal and external messaging
- ✅ IDisposable for resource cleanup

**Related:**
- [IMessageBus](#imessagebus)
- [EventBusProducer](#eventbusproducer)

---

#### BlockingMessageBus

**Namespace:** `EzDdd.UseCase.Port.InOut.Messaging`

**Signature:**
```csharp
public sealed class BlockingMessageBus<TMessage> : IMessageBus<TMessage>
{
    public async Task ProduceAsync(TMessage message);
    public void Subscribe(Func<TMessage, Task> reactor);
    public void Dispose();
}
```

**Description:**
Blocking implementation of IMessageBus. Messages are delivered to all subscribers before ProduceAsync returns. Thread-safe via snapshot-based subscription list.

**Type Parameters:**
- `TMessage`: The type of messages

**Characteristics:**
- **Blocking**: ProduceAsync waits for all reactors to complete
- **Thread-safe**: Subscription list snapshot prevents concurrent modification
- **Order guaranteed**: Reactors execute in subscription order
- **Error handling**: Reactor exceptions logged but don't stop other reactors

**Example:**
```csharp
var messageBus = new BlockingMessageBus<IInternalDomainEvent>();

// Subscribe multiple reactors
messageBus.Subscribe(async @event =>
{
    Console.WriteLine($"Reactor 1: {@event.GetType().Name}");
    await Task.Delay(100);
});

messageBus.Subscribe(async @event =>
{
    Console.WriteLine($"Reactor 2: {@event.GetType().Name}");
    await Task.Delay(50);
});

// Produce event - blocks until both reactors complete
await messageBus.ProduceAsync(new AccountCreated(/* ... */));
Console.WriteLine("All reactors completed");
```

**Notes:**
- ✅ Synchronous delivery (blocking)
- ✅ Thread-safe subscription management
- ✅ Exception isolation per reactor
- ⚠️ Performance scales with number of reactors

**Related:**
- [IMessageBus](#imessagebus)
- [GenericReactor](#genericreactor)

---

#### EventBusProducer

**Namespace:** `EzDdd.UseCase.Port.InOut.Messaging`

**Signature:**
```csharp
public sealed class EventBusProducer : IMessageProducer
{
    public EventBusProducer(IMessageBus<IExternalDomainEvent> messageBus);

    public async Task ProduceAsync(IInternalDomainEvent @event);
    public void Dispose();
}
```

**Description:**
Adapter that converts internal domain events to external domain events and publishes them to an external message bus. Bridges internal and external event systems.

**Constructor:**
**Parameters:**
- `messageBus` (IMessageBus<IExternalDomainEvent>): The external event message bus

**Example:**
```csharp
// Setup
var externalMessageBus = new RabbitMqMessageBus();
var eventBusProducer = new EventBusProducer(externalMessageBus);

// Produce internal event (automatically converted to external)
var accountCreated = new AccountCreated(/* internal event */);
await eventBusProducer.ProduceAsync(accountCreated);

// External systems receive AccountCreatedIntegrationEvent
```

**Conversion Strategy:**
- Maps internal events to external events (integration events)
- Preserves event metadata
- Suitable for cross-bounded-context communication

**Notes:**
- ✅ Adapter pattern for internal → external event conversion
- ✅ Used with OutboxRepository for reliable event publishing
- ✅ IDisposable for cleanup

**Related:**
- [IMessageProducer](#imessageproducer)
- [IExternalDomainEvent](#iexternaldomainevent)
- [IInternalDomainEvent](#iinternaldomainevent)

---

#### GenericReactor

**Namespace:** `EzDdd.UseCase.Port.InOut.Messaging`

**Signature:**
```csharp
public sealed class GenericReactor<TMessage> : IReactor
{
    public GenericReactor(
        TMessage message,
        Func<TMessage, Task> handler);

    public async Task ExecuteAsync();
}
```

**Description:**
Generic implementation of IReactor that wraps a message and handler function. Simplifies creating reactors from lambda expressions.

**Type Parameters:**
- `TMessage`: The type of message

**Constructor:**
**Parameters:**
- `message` (TMessage): The message to react to
- `handler` (Func<TMessage, Task>): Async handler function

**Example:**
```csharp
// Create reactor from lambda
var accountCreated = new AccountCreated(/* ... */);
var reactor = new GenericReactor<AccountCreated>(
    accountCreated,
    async evt => await SendWelcomeEmail(evt.Owner));

// Execute reactor
await reactor.ExecuteAsync();

// Or subscribe to message bus
messageBus.Subscribe(async @event =>
{
    if (@event is AccountCreated created)
    {
        var reactor = new GenericReactor<AccountCreated>(
            created,
            async evt => await SendWelcomeEmail(evt.Owner));
        await reactor.ExecuteAsync();
    }
});
```

**Notes:**
- ✅ Simplifies reactor creation
- ✅ Generic for any message type
- ✅ Wraps lambda expressions as IReactor

**Related:**
- [IReactor](#ireactor)
- [IMessageBus](#imessagebus)

---

## EzDdd.Cqrs

CQRS pattern separation of command (write) and query (read) operations.

### Command Side

#### ICommand

**Namespace:** `EzDdd.Cqrs.Command`

**Signature:**
```csharp
public interface ICommand<in TInput, TOutput> : IUseCase<TInput, TOutput>
    where TInput : IInput
    where TOutput : CqrsOutput<TOutput>, new()
{
    // Marker interface - inherits ExecuteAsync from IUseCase
}
```

**Description:**
Marker interface for command operations in CQRS (write side). Commands modify system state by creating, updating, or deleting aggregates.

**Type Parameters:**
- `TInput`: The input type
- `TOutput`: The output type (must extend CqrsOutput<TOutput>)

**Example:**
```csharp
public record CreateAccountInput(
    Guid AccountId,
    string AccountNumber,
    string Owner,
    decimal InitialBalance
) : IInput;

public class CreateAccountOutput : CqrsOutput<CreateAccountOutput>
{
    public string AccountNumber { get; set; } = string.Empty;

    public CreateAccountOutput SetAccountNumber(string accountNumber)
    {
        AccountNumber = accountNumber;
        return this;
    }
}

public class CreateAccountCommand
    : ICommand<CreateAccountInput, CreateAccountOutput>
{
    private readonly IRepository<BankAccount, Guid, IInternalDomainEvent> _repository;

    public CreateAccountCommand(
        IRepository<BankAccount, Guid, IInternalDomainEvent> repository)
    {
        _repository = repository;
    }

    public async Task<CreateAccountOutput> ExecuteAsync(CreateAccountInput input)
    {
        // 1. Create aggregate
        var account = new BankAccount(
            input.AccountId,
            input.Owner,
            input.InitialBalance);

        // 2. Save aggregate
        await _repository.SaveAsync(account);

        // 3. Return output
        return CreateAccountOutput.Create()
            .SetId(input.AccountId.ToString())
            .SetAccountNumber(input.AccountNumber)
            .SetMessage("Account created successfully")
            .Succeed();
    }
}
```

**Key Characteristics:**
- Modifies system state
- Uses IRepository for persistence
- Returns CqrsOutput with operation result
- May use IInquiry for validation

**Notes:**
- ✅ Write side of CQRS
- ✅ Semantic marker for commands
- ✅ Inherits ExecuteAsync from IUseCase

**Related:**
- [IQuery](#iquery)
- [IInquiry](#iinquiry)
- [CqrsOutput](#cqrsoutput)
- [IRepository](#irepository)

---

#### IInquiry

**Namespace:** `EzDdd.Cqrs.Command`

**Signature:**
```csharp
public interface IInquiry<in TInput, TOutput> : IUseCase<TInput, TOutput>
    where TInput : IInquiryInput
    where TOutput : CqrsOutput<TOutput>, new()
{
    // Marker interface - inherits ExecuteAsync from IUseCase
}
```

**Description:**
Validation queries usable within commands. Inquiries are read-only operations that validate conditions before command execution.

**Type Parameters:**
- `TInput`: The input type (must extend IInquiryInput)
- `TOutput`: The output type

**Example:**
```csharp
public record CheckAccountExistsInput(
    string AccountNumber
) : IInquiryInput;

public class CheckAccountExistsOutput : CqrsOutput<CheckAccountExistsOutput>
{
    public bool Exists { get; set; }

    public CheckAccountExistsOutput SetExists(bool exists)
    {
        Exists = exists;
        return this;
    }
}

public class CheckAccountExistsInquiry
    : IInquiry<CheckAccountExistsInput, CheckAccountExistsOutput>
{
    private readonly IArchive<AccountReadModel, string> _archive;

    public async Task<CheckAccountExistsOutput> ExecuteAsync(
        CheckAccountExistsInput input)
    {
        var account = await _archive.FindByAccountNumberAsync(
            input.AccountNumber);

        return CheckAccountExistsOutput.Create()
            .SetExists(account != null)
            .Succeed();
    }
}

// Usage in command:
public class CreateAccountCommand : ICommand<CreateAccountInput, CreateAccountOutput>
{
    private readonly IInquiry<CheckAccountExistsInput, CheckAccountExistsOutput> _checkExists;

    public async Task<CreateAccountOutput> ExecuteAsync(CreateAccountInput input)
    {
        // Validate before creating aggregate
        var checkResult = await _checkExists.ExecuteAsync(
            new CheckAccountExistsInput(input.AccountNumber));

        if (checkResult.Exists)
        {
            return CreateAccountOutput.Create()
                .Fail()
                .SetMessage("Account number already exists");
        }

        // Proceed with creation...
    }
}
```

**Notes:**
- ✅ Read-only validation queries
- ✅ Used within commands for validation
- ✅ Access read models via IArchive

**Related:**
- [IInquiryInput](#iinquiryinput)
- [ICommand](#icommand)
- [IArchive](#iarchive)

---

#### IInquiryInput

**Namespace:** `EzDdd.Cqrs.Command`

**Signature:**
```csharp
public interface IInquiryInput : IInput
{
    // Marker interface for inquiry inputs
}
```

**Description:**
Marker interface for inputs to validation inquiries.

**Example:**
```csharp
public record CheckAccountExistsInput(
    string AccountNumber
) : IInquiryInput;

public record ValidateTransferInput(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount
) : IInquiryInput;
```

**Notes:**
- ✅ Semantic marker for inquiry inputs
- ✅ Extends IInput

**Related:**
- [IInquiry](#iinquiry)
- [IInput](#iinput)

---

### Query Side

#### IQuery

**Namespace:** `EzDdd.Cqrs.Query`

**Signature:**
```csharp
public interface IQuery<in TInput, TOutput> : IUseCase<TInput, TOutput>
    where TInput : IInput
    where TOutput : CqrsOutput<TOutput>, new()
{
    // Marker interface - inherits ExecuteAsync from IUseCase
}
```

**Description:**
Marker interface for query operations (read side). Queries retrieve system state without modifying it, typically from optimized read models.

**Type Parameters:**
- `TInput`: The input type
- `TOutput`: The output type

**Example:**
```csharp
public record GetAccountSummaryInput(
    Guid AccountId
) : IInput;

public class GetAccountSummaryOutput : CqrsOutput<GetAccountSummaryOutput>
{
    public string AccountNumber { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public decimal Balance { get; set; }

    public GetAccountSummaryOutput SetAccountNumber(string accountNumber)
    {
        AccountNumber = accountNumber;
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
}

public class GetAccountSummaryQuery
    : IQuery<GetAccountSummaryInput, GetAccountSummaryOutput>
{
    private readonly IArchive<AccountSummaryReadModel, Guid> _archive;

    public async Task<GetAccountSummaryOutput> ExecuteAsync(
        GetAccountSummaryInput input)
    {
        var readModel = await _archive.FindByIdAsync(input.AccountId);

        if (readModel == null)
        {
            throw new UseCaseFailureException("Account not found");
        }

        return GetAccountSummaryOutput.Create()
            .SetAccountNumber(readModel.AccountNumber)
            .SetOwner(readModel.Owner)
            .SetBalance(readModel.Balance)
            .Succeed();
    }
}
```

**Key Characteristics:**
- Read-only operations (no state modification)
- Access read models from IArchive
- May use IProjection for complex views
- Returns data without side effects

**Notes:**
- ✅ Read side of CQRS
- ✅ Optimized for query performance
- ✅ Eventually consistent with write model

**Related:**
- [ICommand](#icommand)
- [IProjection](#iprojection)
- [IArchive](#iarchive)

---

#### IProjection

**Namespace:** `EzDdd.Cqrs.Query`

**Signature:**
```csharp
public interface IProjection<in TInput, TOutput> : IUseCase<TInput, TOutput>
    where TInput : IProjectionInput
    where TOutput : CqrsOutput<TOutput>, new()
{
    // Marker interface - inherits ExecuteAsync from IUseCase
}
```

**Description:**
Read model builder that generates view models from query database. Projections transform raw read model data into presentation-friendly formats.

**Type Parameters:**
- `TInput`: The input type (must extend IProjectionInput)
- `TOutput`: The output type

**Example:**
```csharp
public record AccountTransactionHistoryInput(
    Guid AccountId,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate
) : IProjectionInput;

public class AccountTransactionHistoryOutput
    : CqrsOutput<AccountTransactionHistoryOutput>
{
    public List<TransactionDto> Transactions { get; set; } = new();

    public AccountTransactionHistoryOutput SetTransactions(
        List<TransactionDto> transactions)
    {
        Transactions = transactions;
        return this;
    }
}

public class AccountTransactionHistoryProjection
    : IProjection<AccountTransactionHistoryInput, AccountTransactionHistoryOutput>
{
    private readonly IArchive<TransactionReadModel, Guid> _archive;

    public async Task<AccountTransactionHistoryOutput> ExecuteAsync(
        AccountTransactionHistoryInput input)
    {
        // Query raw read model data
        var transactions = await _archive.FindByAccountIdAndDateRangeAsync(
            input.AccountId,
            input.StartDate,
            input.EndDate);

        // Transform to presentation model
        var dtos = transactions
            .Select(t => new TransactionDto
            {
                Date = t.OccurredOn,
                Description = t.Description,
                Amount = t.Amount,
                Balance = t.BalanceAfter
            })
            .OrderByDescending(t => t.Date)
            .ToList();

        return AccountTransactionHistoryOutput.Create()
            .SetTransactions(dtos)
            .Succeed();
    }
}
```

**Notes:**
- ✅ Builds complex view models
- ✅ Transforms read model data
- ✅ Used for reporting and analytics

**Related:**
- [IProjectionInput](#iprojectioninput)
- [IQuery](#iquery)
- [IArchive](#iarchive)
- [IProjector](#iprojector)

---

#### IProjector

**Namespace:** `EzDdd.Cqrs.Query`

**Signature:**
```csharp
public interface IProjector
{
    // Marker interface for background projector services
}
```

**Description:**
Marker interface for background services that build and maintain read models. Projectors listen to domain events and update read models accordingly.

**Example:**
```csharp
public class AccountSummaryProjector : IProjector
{
    private readonly IMessageBus<IInternalDomainEvent> _messageBus;
    private readonly IArchive<AccountSummaryReadModel, Guid> _archive;

    public AccountSummaryProjector(
        IMessageBus<IInternalDomainEvent> messageBus,
        IArchive<AccountSummaryReadModel, Guid> archive)
    {
        _messageBus = messageBus;
        _archive = archive;

        // Subscribe to domain events
        _messageBus.Subscribe(async @event => await HandleEventAsync(@event));
    }

    private async Task HandleEventAsync(IInternalDomainEvent @event)
    {
        switch (@event)
        {
            case AccountCreated created:
                await _archive.SaveAsync(new AccountSummaryReadModel
                {
                    Id = Guid.Parse(created.Source),
                    AccountNumber = created.AccountNumber,
                    Owner = created.Owner,
                    Balance = created.InitialBalance
                });
                break;

            case MoneyDeposited deposited:
                var account = await _archive.FindByIdAsync(
                    Guid.Parse(deposited.Source));
                if (account != null)
                {
                    account.Balance += deposited.Amount;
                    await _archive.SaveAsync(account);
                }
                break;
        }
    }
}

// Startup configuration:
services.AddSingleton<IProjector, AccountSummaryProjector>();
```

**Notes:**
- ✅ Background service for read model maintenance
- ✅ Listens to domain events
- ✅ Updates read models asynchronously
- ✅ Maintains eventual consistency

**Related:**
- [IProjection](#iprojection)
- [IMessageBus](#imessagebus)
- [IArchive](#iarchive)

---

#### IProjectionInput

**Namespace:** `EzDdd.Cqrs.Query`

**Signature:**
```csharp
public interface IProjectionInput : IInput
{
    // Marker interface for projection inputs
}
```

**Description:**
Marker interface for inputs to projections.

**Example:**
```csharp
public record AccountTransactionHistoryInput(
    Guid AccountId,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate
) : IProjectionInput;

public record CustomerOrderSummaryInput(
    Guid CustomerId,
    int PageNumber,
    int PageSize
) : IProjectionInput;
```

**Notes:**
- ✅ Semantic marker for projection inputs
- ✅ Extends IInput

**Related:**
- [IProjection](#iprojection)
- [IInput](#iinput)

---

#### IArchive

**Namespace:** `EzDdd.Cqrs.Query`

**Signature:**
```csharp
public interface IArchive<TData, in TId>
{
    Task<TData?> FindByIdAsync(TId id);
    Task SaveAsync(TData data);
    Task DeleteAsync(TData data);
}
```

**Description:**
Query database interface (query-side counterpart to IRepository). Provides access to optimized read models.

**Type Parameters:**
- `TData`: The type of read model
- `TId`: The type of identifier

**Methods:**

#### FindByIdAsync
Finds a read model by identifier.

**Parameters:**
- `id` (TId): The identifier

**Returns:** Task<TData?> - The read model if found, or null

#### SaveAsync
Saves a read model (create or update).

**Parameters:**
- `data` (TData): The read model to save

**Returns:** Task - Async operation

#### DeleteAsync
Deletes a read model.

**Parameters:**
- `data` (TData): The read model to delete

**Returns:** Task - Async operation

**Example:**
```csharp
public class AccountSummaryReadModel
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
}

public class SqlAccountSummaryArchive
    : IArchive<AccountSummaryReadModel, Guid>
{
    private readonly QueryDbContext _dbContext;

    public async Task<AccountSummaryReadModel?> FindByIdAsync(Guid id)
    {
        return await _dbContext.AccountSummaries
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task SaveAsync(AccountSummaryReadModel data)
    {
        var existing = await FindByIdAsync(data.Id);
        if (existing == null)
            _dbContext.AccountSummaries.Add(data);
        else
            _dbContext.AccountSummaries.Update(data);

        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(AccountSummaryReadModel data)
    {
        _dbContext.AccountSummaries.Remove(data);
        await _dbContext.SaveChangesAsync();
    }
}
```

**Notes:**
- ✅ Query-side persistence (read models)
- ✅ Optimized for read performance
- ✅ Eventually consistent with write model
- ✅ Separate database (optional)

**Related:**
- [IQuery](#iquery)
- [IProjection](#iprojection)
- [IProjector](#iprojector)
- [IRepository](#irepository)

---

### CqrsOutput

**Namespace:** `EzDdd.Cqrs`

**Signature:**
```csharp
public class CqrsOutput<T> : IOutput
    where T : CqrsOutput<T>, new()
{
    public string Id { get; set; }
    public string Message { get; set; }
    public ExitCode ExitCode { get; set; }

    public static T Create();
    public T SetId(string id);
    public T SetMessage(string message);
    public T SetExitCode(ExitCode exitCode);
    public T Succeed();
    public T Fail();
}
```

**Description:**
Base class for CQRS command and query outputs. Provides type-safe fluent API using self-referential generics, allowing subclasses to maintain their concrete type when chaining methods.

**Type Parameters:**
- `T`: The concrete output type (self-referential constraint)

**Properties:**
- `Id`: Identifier associated with output
- `Message`: Human-readable message
- `ExitCode`: Execution status code

**Methods:**

#### Create (static)
Creates a new instance of the concrete output type.

**Returns:** T - New instance

#### SetId
Sets the identifier and returns this instance.

**Parameters:**
- `id` (string): The identifier

**Returns:** T - This instance for chaining

#### SetMessage
Sets the message and returns this instance.

**Parameters:**
- `message` (string): The message

**Returns:** T - This instance for chaining

#### SetExitCode
Sets the exit code and returns this instance.

**Parameters:**
- `exitCode` (ExitCode): The exit code

**Returns:** T - This instance for chaining

#### Succeed
Sets exit code to Success and returns this instance.

**Returns:** T - This instance for chaining

#### Fail
Sets exit code to Failure and returns this instance.

**Returns:** T - This instance for chaining

**Example:**
```csharp
public class CreateAccountOutput : CqrsOutput<CreateAccountOutput>
{
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }

    public CreateAccountOutput SetAccountNumber(string accountNumber)
    {
        AccountNumber = accountNumber;
        return this;
    }

    public CreateAccountOutput SetBalance(decimal balance)
    {
        Balance = balance;
        return this;
    }
}

// Usage with fluent API:
var output = CreateAccountOutput.Create()
    .SetId("ACC-001")
    .SetAccountNumber("1234567890")
    .SetBalance(1000m)
    .SetMessage("Account created successfully")
    .Succeed();

// All methods return CreateAccountOutput, not CqrsOutput
// This enables type-safe chaining with domain-specific methods
```

**Design Pattern:**
Self-referential generic with fluent builder API. The `T` parameter ensures that fluent methods always return the concrete subclass type, not the base `CqrsOutput` type.

**Notes:**
- ✅ Type-safe method chaining preserves concrete type
- ✅ Static factory method for creating instances
- ✅ Explicit IOutput implementation for interface compatibility
- ✅ Extensible with domain-specific fluent methods

**Related:**
- [IOutput](#ioutput)
- [ICommand](#icommand)
- [IQuery](#iquery)
- [ExitCode](#exitcode)

---

## See Also

- [USAGE_EXAMPLES.md](USAGE_EXAMPLES.md) - Practical examples for common scenarios
- [TRANSACTION_BOUNDARY_GUIDE.md](../TRANSACTION_BOUNDARY_GUIDE.md) - Transaction management best practices
- [Architecture Decision Records](../adr/) - Design decisions and rationale
- [ROADMAP.md](../ROADMAP.md) - Development roadmap and progress tracking
- [README.md](../../README.md) - Project overview and quick start

---

*Last updated: 2025-11-22*
