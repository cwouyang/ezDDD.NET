# ezDDD.NET API Reference

Complete reference for all public APIs in ezDDD.NET tactical Domain-Driven Design framework.

> **Version**: 1.0.0-alpha.1
> **Last Updated**: 2026-07-05

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
  - [IDomainEventSource<TEvent>](#idomaineventsource)
  - [AggregateRoot<TId, TEvent>](#aggregateroot)
  - [EsAggregateRoot<TId, TEvent>](#esaggregateroot)
  - [DomainEventTypeMapper](#domaineventtypemapper)
- [EzDdd.UseCase](#ezdddusеcase)
  - [Foundation Interfaces](#foundation-interfaces)
    - [IInput](#iinput)
    - [IOutput](#ioutput)
    - [IVersionedInput](#iversionedinput)
    - [ExitCode](#exitcode)
    - [ExitCodeExtensions](#exitcodeextensions)
    - [IReactor<TInput>](#ireactor)
    - [IReconciler<TContext, TReport>](#ireconciler)
    - [NullContext](#nullcontext)
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
    - [DomainEventDataBuilder](#domaineventdatabuilder)
    - [DomainEventMapper](#domaineventmapper)
    - [InternalDomainEventDto](#internaldomaineventdto)
  - [Event Sourcing](#event-sourcing)
    - [EventStoreData<TId>](#eventstoredata)
    - [EventStoreMapper](#eventstoremapper)
    - [EsRepository<TAggregate, TId>](#esrepository)
  - [State Sourcing](#state-sourcing)
    - [IOutboxData<TId>](#ioutboxdata)
    - [OutboxMapper<TAggregate, TData, TId>](#outboxmapper)
    - [OutboxRepository<TAggregate, TData, TId>](#outboxrepository)
  - [Messaging](#messaging)
    - [IExternalDomainEventPublisher<TEvent>](#iexternaldomaineventpublisher)
    - [PostEventFailureException](#posteventfailureexception)
- [EzDdd.Cqrs](#ezdddcqrs)
  - [Command Side](#command-side)
    - [ICommand<TInput, TOutput>](#icommand)
    - [IInquiry<TInput, TOutput>](#iinquiry)
    - [IInquiryInput](#iinquiryinput)
  - [Query Side](#query-side)
    - [IQuery<TInput, TOutput>](#iquery)
    - [IProjection<TInput, TOutput>](#iprojection)
    - [IProjector<TInput>](#iprojector)
    - [INotifier<TInput>](#inotifier)
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

### IDomainEventSource

**Namespace:** `EzDdd.Entity`

**Signature:**
```csharp
public interface IDomainEventSource<TEvent>
    where TEvent : class, IInternalDomainEvent
{
    void Apply(TEvent @event);
    IReadOnlyList<TEvent> GetDomainEvents();
    TEvent? GetLastDomainEvent();
    int GetDomainEventSize();
    void ClearDomainEvents();
}
```

**Description:**
Interface for objects that raise and collect internal domain events. Implemented by `AggregateRoot<TId, TEvent>`, which provides the event-collection behavior; most applications interact with these members through their aggregates rather than implementing this interface directly.

**Type Parameters:**
- `TEvent`: The type of internal domain events (must be a class implementing `IInternalDomainEvent`)

**Notes:**
- ✅ Defines the event sourcing capability contract used by repositories
- ✅ `AggregateRoot<TId, TEvent>` implements this interface

**Related:**
- [AggregateRoot](#aggregateroot)
- [IInternalDomainEvent](#iinternaldomainevent)

---

### AggregateRoot

**Namespace:** `EzDdd.Entity`

**Signature:**
```csharp
public abstract class AggregateRoot<TId, TEvent> : IEntity<TId>, IDomainEventSource<TEvent>
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

    protected void _AddDomainEvent(TEvent @event);
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
- After N events: `Version = N - 1`

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

#### _AddDomainEvent (protected)
Adds a domain event to the collection and increments `Version`. Non-virtual so subclasses cannot bypass event collection or version management; called by `Apply`.

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
    protected virtual void _ReplayEvents(IEnumerable<TEvent> events);

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

#### _ReplayEvents (virtual)
Replays a sequence of events (via `Apply`) to reconstruct aggregate state. Called by the event replay constructor; override to customize replay behavior.

**Parameters:**
- `events` (IEnumerable<TEvent>): The events to replay in chronological order

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
    long Version { get; set; }
}
```

**Description:**
Marker interface for inputs that carry version information for optimistic locking.

**Properties:**
- `Version`: Expected version number of the aggregate (read/write; the interface requires a `set` accessor, so implement it with a settable property, not an init-only positional record parameter)

**Example:**
```csharp
public record UpdateAccountInput(
    Guid AccountId,
    string NewOwner
) : IVersionedInput
{
    public long Version { get; set; }
}

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
    Failure = 1
}
```

**Description:**
Enumeration representing the execution status of a use case. Mirrors upstream Java ezddd's two-state result model; richer failure semantics (not-found, conflict, validation) are conveyed via `IOutput.Message` or by throwing `UseCaseFailureException`.

**Values:**
- `Success (0)`: Operation completed successfully
- `Failure (1)`: Operation failed

**Example:**
```csharp
var account = await _repository.FindByIdAsync(input.AccountId);
if (account == null)
{
    output.Fail();
    output.SetMessage("Account not found");
    return output;
}

try
{
    await _repository.SaveAsync(account);
}
catch (RepositorySaveException ex)
    when (ex.Message == RepositorySaveException.OptimisticLockingFailure)
{
    output.Fail();
    output.SetMessage("Concurrent modification detected");
    return output;
}

output.Succeed();
output.SetMessage("Operation completed");
return output;
```

**Notes:**
- ✅ Two-state result model (parity with Java ezddd)
- ✅ Use `IOutput.Message` to convey failure details
- ✅ `ExitCodeExtensions.Code()` yields the underlying integer value

**Related:**
- [ExitCodeExtensions](#exitcodeextensions)
- [IOutput](#ioutput)
- [UseCaseFailureException](#usecasefailureexception)

---

#### ExitCodeExtensions

**Namespace:** `EzDdd.UseCase.Port.In`

**Signature:**
```csharp
public static class ExitCodeExtensions
{
    public static int Code(this ExitCode exitCode);
}
```

**Description:**
Extension methods for `ExitCode`.

**Methods:**

##### Code
Gets the integer code value of the exit code.

**Returns:** int - `0` for `Success`, `1` for `Failure`

**Example:**
```csharp
int code = ExitCode.Failure.Code(); // 1
```

**Related:**
- [ExitCode](#exitcode)

---

#### IReactor

**Namespace:** `EzDdd.UseCase.Port.In`

**Signature:**
```csharp
public interface IReactor<in TInput>
{
    Task ExecuteAsync(TInput input);
}
```

**Description:**
In-port for services that take care of specific business rules whenever they receive a message. According to the received message, a reactor triggers a side effect such as notifying frontend clients or another bounded context. Reactors should handle messages idempotently. `IProjector<TInput>` and `INotifier<TInput>` in EzDdd.Cqrs are specialized reactors (see ADR-0028).

**Type Parameters:**
- `TInput`: The type of input message this reactor processes (contravariant)

**Example:**
```csharp
public class WelcomeEmailReactor : IReactor<DomainEventData>
{
    private readonly IEmailService _emailService;

    public WelcomeEmailReactor(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task ExecuteAsync(DomainEventData input)
    {
        var domainEvent = DomainEventMapper.ToDomain<AccountCreated>(input);
        await _emailService.SendWelcomeEmailAsync(domainEvent.Owner);
    }
}
```

**Notes:**
- ✅ Receives messages delivered by infrastructure (e.g., an event store relay)
- ✅ No return value (fire-and-forget semantics)
- ✅ Asynchronous execution
- ✅ Should be idempotent (same message processed twice yields same result)

**Related:**
- [IProjector<TInput>](#iprojector)
- [INotifier<TInput>](#inotifier)
- [DomainEventData](#domaineventdata)

---

#### IReconciler

**Namespace:** `EzDdd.UseCase.Port.In`

**Signature:**
```csharp
public interface IReconciler<in TContext, TReport>
{
    Task<TReport> ReconcileAsync(TContext context);
}
```

**Description:**
In-port for system reconciliation tasks: periodic maintenance, data-consistency checks, and cleanup operations. Unlike use cases (triggered by user actions), reconcilers are typically invoked by scheduled jobs or administrative tools.

**Type Parameters:**
- `TContext`: The input parameters for the reconciliation (contravariant); use [NullContext](#nullcontext) when no parameters are needed
- `TReport`: The report type describing the reconciliation results

**Example:**
```csharp
public record OrderCleanupContext(int ExpirationDays);
public record OrderCleanupReport(int TotalChecked, int DeletedCount);

public class CleanUpExpiredOrdersReconciler
    : IReconciler<OrderCleanupContext, OrderCleanupReport>
{
    public async Task<OrderCleanupReport> ReconcileAsync(OrderCleanupContext context)
    {
        // Find and delete expired draft orders, then report the results
        // ...
    }
}
```

**Notes:**
- ✅ Returns a report instead of an `IOutput` (administrative result, not a use case output)
- ✅ Schedule with `BackgroundService`, Hangfire, Quartz.NET, etc.

**Related:**
- [NullContext](#nullcontext)
- [IUseCase](#iusecase)

---

#### NullContext

**Namespace:** `EzDdd.UseCase.Port.In`

**Signature:**
```csharp
public sealed class NullContext
{
    public static readonly NullContext Instance;
}
```

**Description:**
Null-object context for reconcilers that need no input parameters. Provides type safety instead of `null` or `object`.

**Example:**
```csharp
public class GlobalSystemCleanupReconciler
    : IReconciler<NullContext, GlobalCleanupReport>
{
    public async Task<GlobalCleanupReport> ReconcileAsync(NullContext context)
    {
        // Perform system-wide cleanup (no parameters needed)
        // ...
    }
}

// Usage:
var report = await reconciler.ReconcileAsync(NullContext.Instance);
```

**Notes:**
- ✅ Singleton (`NullContext.Instance`)
- ✅ Counterpart of `IInput.NullInput` for the reconciliation side

**Related:**
- [IReconciler](#ireconciler)
- [IInput](#iinput)

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
        var output = new DepositOutput();

        // 1. Load aggregate
        var account = await _repository.FindByIdAsync(input.AccountId);
        if (account == null)
        {
            output.Fail();
            output.SetMessage("Account not found");
            return output;
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
            output.Fail();
            output.SetMessage(ex.Message);
            return output;
        }

        output.SetId(account.Id.ToString());
        output.SetMessage($"Deposited {input.Amount}");
        output.Succeed();
        return output;
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
    public UseCaseFailureException();
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
    output.Fail();
    output.SetMessage("Account not found");
    return output;
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
    output.Fail();
    output.SetMessage("Concurrent modification detected");
    return output;
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
public interface IStoreData<TId>
{
    TId Id { get; set; }
    long Version { get; set; }
    string StreamName { get; set; }
    IReadOnlyList<IDomainEvent> Events { get; set; }

    long GetOptimisticLockVersion(); // default implementation: Version + Events.Count
}
```

**Description:**
Base interface for persistence data structures used by IRepositoryPeer. Supports both event sourcing and state sourcing.

**Type Parameters:**
- `TId`: The type of identifier

**Properties:**
- `Id`: The identifier (read/write)
- `Version`: Version number for optimistic locking (`-1` for new aggregates, `0+` for existing)
- `StreamName`: Event stream name (convention: `{category}-{id}`)
- `Events`: Pending domain events to persist (Transactional Outbox pattern)

**Methods:**

#### GetOptimisticLockVersion
Gets the expected version after save. Default interface implementation returns `Version + Events.Count`; used for optimistic locking in database UPDATE operations.

**Example:**
```csharp
public class BankAccountData : IStoreData<Guid>
{
    public Guid Id { get; set; }
    public long Version { get; set; }
    public string StreamName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public IReadOnlyList<IDomainEvent> Events { get; set; }
        = Array.Empty<IDomainEvent>();
}
```

**Notes:**
- ✅ DTO for persistence layer
- ✅ Includes both state and events
- ✅ Default `GetOptimisticLockVersion()` implementation (override only if needed)

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

    public RepositorySaveException();
    public RepositorySaveException(string message);
    public RepositorySaveException(string message, Exception innerException);
    public RepositorySaveException(Exception innerException);
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
    output.Fail();
    output.SetMessage("Concurrent modification detected. Please retry.");
    return output;
}
catch (RepositorySaveException ex)
{
    // Handle other save failures
    output.Fail();
    output.SetMessage(ex.Message);
    return output;
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
    public RepositoryPeerSaveException();
    public RepositoryPeerSaveException(string message);
    public RepositoryPeerSaveException(string message, Exception innerException);
    public RepositoryPeerSaveException(Exception innerException);
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

// Publish through the out-port (typically from an INotifier implementation)
await _externalDomainEventPublisher.PublishAsync(integrationEvent);
```

**Notes:**
- ✅ Used for bounded context integration
- ✅ Published to external message brokers
- ✅ Separate from internal events (different purpose)

**Related:**
- [IInternalDomainEvent](#iinternaldomainevent)
- [IExternalDomainEventPublisher](#iexternaldomaineventpublisher)

---

#### DomainEventData

**Namespace:** `EzDdd.UseCase.Port.InOut`

**Signature:**
```csharp
public record DomainEventData(
    Guid Id,
    string EventType,
    string ContentType,
    byte[] EventBody,
    byte[] UserMetadata
)
{
    public virtual bool Equals(DomainEventData? other); // JSON-aware semantic equality
    public override int GetHashCode();
}
```

**Description:**
Immutable record for persisted domain events. Stores the event payload and metadata as byte arrays to support flexible serialization formats (JSON, Avro, Protobuf). Equality is JSON-aware for JSON content (key order independent), matching Java ezddd's `JSONObject.similar()` semantics; non-JSON content falls back to byte-level comparison.

**Properties:**
- `Id`: Event unique identifier (not aggregate ID)
- `EventType`: Mapped event type name (from DomainEventTypeMapper, e.g. `"AccountCreated"`)
- `ContentType`: Serialization format (e.g. `"application/json"`)
- `EventBody`: Serialized event payload as byte array
- `UserMetadata`: Serialized event metadata as byte array

**Example:**
```csharp
var @event = new AccountCreated(/* ... */);

// Recommended: convert via DomainEventMapper (uses the builder internally)
DomainEventData eventData = DomainEventMapper.ToData(@event);

// Or construct via the fluent builder
var built = DomainEventDataBuilder
    .Json("AccountCreated", @event)
    .EventId(@event.Id)
    .MetadataAsJson(@event.Metadata)
    .Build();

// Or construct directly (full control, common in test code)
var direct = new DomainEventData(
    Guid.NewGuid(),
    "AccountCreated",
    "application/json",
    JsonSerializer.SerializeToUtf8Bytes(@event),
    "{}"u8.ToArray()
);
```

**Notes:**
- ✅ Immutable record type
- ✅ Byte arrays support any serialization format
- ✅ JSON-aware equality (key order independent); hash code from `Id`/`EventType`/`ContentType`

**Related:**
- [DomainEventDataBuilder](#domaineventdatabuilder)
- [DomainEventMapper](#domaineventmapper)
- [EventStoreData](#eventstoredata)

---

#### DomainEventDataBuilder

**Namespace:** `EzDdd.UseCase.Port.InOut`

**Signature:**
```csharp
public class DomainEventDataBuilder
{
    public static DomainEventDataBuilder Json<T>(string eventType, T payload);
    public static DomainEventDataBuilder Binary(string eventType, byte[] payload);

    public DomainEventDataBuilder EventId(Guid id);
    public DomainEventDataBuilder MetadataAsJson<T>(T metadata);
    public DomainEventDataBuilder MetadataAsBytes(byte[] metadata);
    public DomainEventData Build();
}
```

**Description:**
Fluent builder for constructing [DomainEventData](#domaineventdata) instances with smart defaults and automatic serialization. Start with a factory method (`Json` or `Binary`), optionally set the event ID and metadata, then call `Build()`.

**Methods:**

##### Json<T> (static factory)
Creates a builder for a JSON-serialized payload; sets `ContentType` to `"application/json"` and serializes `payload` with System.Text.Json.

##### Binary (static factory)
Creates a builder for a pre-serialized binary payload (e.g., Avro, Protobuf); sets `ContentType` to `"application/octet-stream"`.

##### EventId
Sets the event ID (optional — defaults to `Guid.NewGuid()`).

##### MetadataAsJson<T> / MetadataAsBytes
Sets metadata as a JSON-serialized object or pre-serialized bytes (optional — defaults to `"{}"`).

##### Build
Builds the `DomainEventData`. Throws `InvalidOperationException` if no factory method was used.

**Example:**
```csharp
// Minimal (event ID auto-generated, metadata defaults to "{}")
var eventData = DomainEventDataBuilder
    .Json("OrderCreated", orderEvent)
    .Build();

// With all options
var eventData2 = DomainEventDataBuilder
    .Json("MoneyDeposited", depositEvent)
    .EventId(depositEvent.Id)
    .MetadataAsJson(new Dictionary<string, string> { ["CorrelationId"] = "123" })
    .Build();

// Binary payload
var eventData3 = DomainEventDataBuilder
    .Binary("LegacyEvent", avroBytes)
    .MetadataAsBytes(metadataBytes)
    .Build();
```

**Notes:**
- ✅ Auto-generates event ID and empty metadata when not provided
- ✅ `ContentType` managed by the factory methods
- ⚠️ Constructor is private — always start from `Json()` or `Binary()`

**Related:**
- [DomainEventData](#domaineventdata)
- [DomainEventMapper](#domaineventmapper)

---

#### DomainEventMapper

**Namespace:** `EzDdd.UseCase.Port.InOut`

**Signature:**
```csharp
public static class DomainEventMapper
{
    public static DomainEventData ToData(IInternalDomainEvent @event);
    public static IReadOnlyList<DomainEventData> ToData(IEnumerable<IInternalDomainEvent> events);

    public static T ToDomain<T>(DomainEventData data)
        where T : IInternalDomainEvent;
    public static IReadOnlyList<T> ToDomain<T>(IEnumerable<DomainEventData> datas)
        where T : IInternalDomainEvent;
}
```

**Description:**
Static utility for converting between domain events and [DomainEventData](#domaineventdata). Event types must be registered with `DomainEventTypeMapper` before use.

**Methods:**

#### ToData
Converts a domain event (or a collection of events) to `DomainEventData` for persistence. Serializes the event body and metadata as JSON via `DomainEventDataBuilder`.

**Exceptions:**
- `InvalidOperationException`: Serialization fails or event type not registered

**Example:**
```csharp
var @event = new AccountCreated(/* ... */);
DomainEventData eventData = DomainEventMapper.ToData(@event);

// Batch conversion
IReadOnlyList<DomainEventData> dataList =
    DomainEventMapper.ToData(aggregate.GetDomainEvents());
```

#### ToDomain<T>
Converts `DomainEventData` (or a collection) back to strongly-typed domain events. Resolves the CLR type via `DomainEventTypeMapper.GetType(data.EventType)`.

**Exceptions:**
- `InvalidOperationException`: Deserialization fails, event type not registered, or the deserialized event cannot be cast to `T`

**Example:**
```csharp
AccountCreated @event = DomainEventMapper.ToDomain<AccountCreated>(eventData);

// Deserialize a stream as the common interface type
IReadOnlyList<IInternalDomainEvent> events =
    DomainEventMapper.ToDomain<IInternalDomainEvent>(dataList);
var aggregate = new Account(events);
```

**Notes:**
- ✅ Uses DomainEventTypeMapper for type resolution
- ✅ System.Text.Json serialization (no third-party dependencies)
- ✅ Thread-safe static methods
- ⚠️ Register all event types at startup before mapping

**Related:**
- [DomainEventData](#domaineventdata)
- [DomainEventTypeMapper](#domaineventtypemapper)
- [EventStoreMapper](#eventstoremapper)

---

#### InternalDomainEventDto

**Namespace:** `EzDdd.UseCase.Port.InOut`

**Signature:**
```csharp
public class InternalDomainEventDto
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredOn { get; set; }
    public string BoundedContext { get; set; }
    public string EventSimpleName { get; set; }
    public string JsonEvent { get; set; }
    public IDictionary<string, string> Metadata { get; set; }
}
```

**Description:**
Mutable DTO for transmitting internal domain events to frontends or external systems (REST responses, WebSocket/SignalR notifications, event log queries). The structure matches Java ezddd's `InternalDomainEventDto` for cross-platform integration.

**Properties:**
- `Id`: Event unique identifier (not aggregate ID)
- `OccurredOn`: Timestamp when the event occurred (UTC with timezone)
- `BoundedContext`: Bounded context name where the event originated (e.g., `"banking"`)
- `EventSimpleName`: Simple event type name without namespace (e.g., `"MoneyDeposited"`)
- `JsonEvent`: Event data serialized as a JSON string
- `Metadata`: Key-value string metadata (userId, correlationId, etc.)

**Example:**
```csharp
var dto = new InternalDomainEventDto
{
    Id = domainEvent.Id,
    OccurredOn = domainEvent.OccurredOn,
    BoundedContext = "banking",
    EventSimpleName = "AccountCreated",
    JsonEvent = JsonUtil.AsString(domainEvent),
    Metadata = new Dictionary<string, string>
    {
        { "CorrelationId", correlationId }
    }
};
```

**Notes:**
- ✅ Mutable properties for easy JSON (de)serialization
- ✅ Cross-platform compatible with Java ezddd
- ⚠️ DTO for the boundary — not a substitute for strongly-typed domain events

**Related:**
- [DomainEventData](#domaineventdata)
- [OutboxMapper](#outboxmapper)

---

### Event Sourcing

#### EventStoreData

**Namespace:** `EzDdd.UseCase.Port.Out`

**Signature:**
```csharp
public class EventStoreData<TId> : IStoreData<TId>
{
    public TId Id { get; set; }
    public long Version { get; set; }
    public string StreamName { get; set; }
    public IReadOnlyList<IDomainEvent> Events { get; set; }

    public long GetOptimisticLockVersion(); // Version + Events.Count
}
```

**Description:**
Data structure for event sourcing persistence. Stores only events (no aggregate state); the aggregate is reconstructed by replaying the events through its event replay constructor.

**Type Parameters:**
- `TId`: The type of identifier

**Properties:**
- `Id`: Aggregate identifier
- `Version`: Number of events persisted before the current batch (used for optimistic locking)
- `StreamName`: Event stream name (format: `{category}-{id}`)
- `Events`: Domain events, in chronological order

**Methods:**

#### GetOptimisticLockVersion
Returns `Version + Events.Count` — the expected total number of persisted events after the current batch is saved. Used by the event store to detect concurrent modifications.

**Example:**
```csharp
var eventStoreData = new EventStoreData<Guid>
{
    Id = accountId,
    Version = 2, // 3 events already persisted (versions 0, 1, 2 → Version = 2)
    StreamName = "account-550e8400-e29b-41d4-a716-446655440000",
    Events = new List<IDomainEvent>
    {
        new MoneyDeposited(/* ... */), // new, not-yet-persisted event
    },
};

long expected = eventStoreData.GetOptimisticLockVersion(); // 3
```

**Notes:**
- ✅ Mutable data class with object-initializer construction
- ✅ Used by IRepositoryPeer<EventStoreData<TId>, TId>
- ✅ On save, `Events` contains the aggregate's *pending* events; the peer appends them to the stored stream

**Related:**
- [EventStoreMapper](#eventstoremapper)
- [EsRepository](#esrepository)
- [IStoreData](#istoredata)

---

#### EventStoreMapper

**Namespace:** `EzDdd.UseCase.Port.Out`

**Signature:**
```csharp
public static class EventStoreMapper
{
    public static EventStoreData<TId> ToData<TId>(
        EsAggregateRoot<TId, IInternalDomainEvent> aggregate);

    public static T ToDomain<T, TId>(EventStoreData<TId> data)
        where T : EsAggregateRoot<TId, IInternalDomainEvent>;
        // Always throws NotSupportedException — see below
}
```

**Description:**
Static utility for converting event-sourced aggregates to `EventStoreData` for persistence. The reverse direction is intentionally **not supported**: event-sourced aggregates are reconstructed through their event replay constructor (which enforces the R1/R2/R3 invariant rules), never through state hydration.

**Methods:**

#### ToData<TId>
Converts an aggregate to `EventStoreData` — takes a defensive copy of the aggregate's pending domain events plus `Id`, `Version`, and `GetStreamName()`.

**Example:**
```csharp
var account = new BankAccount(accountId, "Jane Doe", new Money(500m));
account.Deposit(new Money(200m));

EventStoreData<AccountId> data = EventStoreMapper.ToData(account);
// data.Events: [AccountCreated, MoneyDeposited]
// data.StreamName: "account-{accountId}"
```

#### ToDomain<T, TId>
**Always throws `NotSupportedException`.** Exists only for type-signature symmetry; use the aggregate's event replay constructor instead.

**Example:**
```csharp
// ❌ WRONG: throws NotSupportedException
var aggregate = EventStoreMapper.ToDomain<BankAccount, AccountId>(data);

// ✅ CORRECT: event replay constructor
var internalEvents = data.Events.Cast<IInternalDomainEvent>();
var reconstructed = new BankAccount(internalEvents);
```

**Notes:**
- ✅ One-way mapping by design (save direction only)
- ✅ Defensive copy of the event list
- ✅ Used internally by EsRepository (which reconstructs via the replay constructor)

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
- [EventStoreMapper](#eventstoremapper)
- [IRepository](#irepository)

---

### State Sourcing

#### IOutboxData

**Namespace:** `EzDdd.UseCase.Port.Out`

**Signature:**
```csharp
public interface IOutboxData<TId> : IStoreData<TId>
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
    public IReadOnlyList<IDomainEvent> Events { get; set; }
        = Array.Empty<IDomainEvent>();
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
public abstract class OutboxMapper<TAggregate, TData, TId>
    where TAggregate : AggregateRoot<TId, IInternalDomainEvent>
    where TData : IOutboxData<TId>
{
    public abstract TData ToData(TAggregate aggregate);
    public abstract TAggregate ToDomain(TData data);
}
```

**Description:**
Abstract base class for mapping between state-sourced aggregates and outbox data. Applications subclass it per aggregate and implement both directions; `OutboxRepository` receives the mapper via constructor injection.

**Type Parameters:**
- `TAggregate`: The aggregate type
- `TData`: The outbox data type (must implement `IOutboxData<TId>`)
- `TId`: The type of identifier

**Methods:**

#### ToData (abstract)
Converts an aggregate to outbox data: copy state, set `Id`/`Version`/`StreamName`, and include the pending events from `GetDomainEvents()`.

#### ToDomain (abstract)
Converts outbox data back to an aggregate: typically create the aggregate via a parameterless constructor and restore state (public setters, or reflection for protected members like `Id`/`Version`). Domain events are not restored.

**Example:**
```csharp
public sealed class BankAccountMapper
    : OutboxMapper<BankAccount, BankAccountOutboxData, Guid>
{
    public override BankAccountOutboxData ToData(BankAccount aggregate)
    {
        return new BankAccountOutboxData
        {
            Id = aggregate.Id,
            Version = aggregate.Version,
            StreamName = $"account-{aggregate.Id}",
            Owner = aggregate.Owner,
            Balance = aggregate.Balance,
            Events = aggregate.GetDomainEvents().ToList(),
        };
    }

    public override BankAccount ToDomain(BankAccountOutboxData data)
    {
        var account = new BankAccount(); // parameterless constructor
        // Restore state (use reflection for protected Id/Version if needed)
        // ...
        return account;
    }
}
```

**Notes:**
- ✅ Explicit, per-aggregate mapping (no hidden reflection magic in the framework)
- ✅ `ToDomain` returns an aggregate with an empty event collection
- ⚠️ Aggregate needs a parameterless constructor for reconstruction

**Related:**
- [IOutboxData](#ioutboxdata)
- [OutboxRepository](#outboxrepository)

---

#### OutboxRepository

**Namespace:** `EzDdd.UseCase.Port.Out`

**Signature:**
```csharp
public class OutboxRepository<TAggregate, TData, TId>
    : IRepository<TAggregate, TId, IInternalDomainEvent>
    where TAggregate : AggregateRoot<TId, IInternalDomainEvent>
    where TData : IOutboxData<TId>
{
    public OutboxRepository(
        IRepositoryPeer<TData, TId> peer,
        OutboxMapper<TAggregate, TData, TId> mapper);

    public async Task<TAggregate?> FindByIdAsync(TId id);
    public async Task SaveAsync(TAggregate aggregate);
    public async Task DeleteAsync(TAggregate aggregate);
}
```

**Description:**
Generic state sourcing repository with Transactional Outbox pattern. Persists both current aggregate state AND domain events atomically in the same transaction. Conversion between aggregate and data is delegated to the injected [OutboxMapper](#outboxmapper).

**Type Parameters:**
- `TAggregate`: The aggregate type
- `TData`: The outbox data type (must implement `IOutboxData<TId>`)
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
var mapper = new BankAccountMapper(); // OutboxMapper<BankAccount, BankAccountOutboxData, Guid>
var repository = new OutboxRepository<BankAccount, BankAccountOutboxData, Guid>(outboxPeer, mapper);

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
- ✅ Soft-delete filtering: `FindByIdAsync` returns `null` when the reconstructed aggregate has `IsDeleted == true` (the row stays in storage so its events can still be relayed)
- ✅ `DeleteAsync` performs a physical delete; use a destruction event + `SaveAsync` for soft delete

**Related:**
- [IOutboxData](#ioutboxdata)
- [OutboxMapper](#outboxmapper)
- [IRepository](#irepository)

---

### Messaging

> **Removed APIs**: The in-process message bus and producer types that earlier pre-release versions shipped (`IMessageBus<TMessage>`, `IMessageProducer`, `BlockingMessageBus<TMessage>`, `EventBusProducer`, `GenericReactor<TMessage>`) are **no longer part of the core packages**. Upstream Java ezddd 6.0.0 moved the producer abstraction to the external `ezddd-gateway` artifact; the .NET counterpart is deferred to the ezDDD.Gateway package (post-1.0). See [ADR-0029](../adr/0029-messageproducer-removal-gateway-deferral.md).
>
> Event publication now follows the **Relay pattern** (Transactional Outbox): a background relay polls the event store and forwards stored events to reactors or a message broker. A reference implementation, including a minimal example-scoped producer abstraction, lives in [`examples/EventInfrastructure`](../../examples/EventInfrastructure/EventStoreRelay.cs).

#### IExternalDomainEventPublisher

**Namespace:** `EzDdd.UseCase.Port.InOut.Messaging`

**Signature:**
```csharp
public interface IExternalDomainEventPublisher<in TEvent>
    where TEvent : IExternalDomainEvent
{
    Task PublishAsync(TEvent @event);
}
```

**Description:**
Out-port abstraction for publishing external domain events (integration events) to external systems, such as a message broker (e.g., Kafka), downstream bounded contexts, or front-ends. A typical publisher is invoked by an [INotifier<TInput>](#inotifier), which converts internal domain events into `IExternalDomainEvent` instances before dispatching them outward. Keeping publication behind this out-port upholds the cross-layer principle of Clean Architecture: use cases depend on the abstraction, while concrete messaging adapters live in the frameworks and drivers layer.

**Type Parameters:**
- `TEvent`: The type of external domain event this publisher publishes (contravariant, must implement `IExternalDomainEvent`)

**Methods:**

#### PublishAsync
Publishes the given external domain event to an external system asynchronously.

**Parameters:**
- `@event` (TEvent): The external domain event to publish

**Returns:** Task - Async operation

**Example:**
```csharp
// Adapter in the frameworks and drivers layer
public class KafkaAccountEventPublisher
    : IExternalDomainEventPublisher<AccountCreatedIntegrationEvent>
{
    private readonly IKafkaProducer _producer;

    public KafkaAccountEventPublisher(IKafkaProducer producer)
    {
        _producer = producer;
    }

    public async Task PublishAsync(AccountCreatedIntegrationEvent @event)
    {
        await _producer.SendAsync("banking.account-created", @event);
    }
}

// Used from a notifier in the use cases layer
public class AccountNotifier : INotifier<DomainEventData>
{
    private readonly IExternalDomainEventPublisher<AccountCreatedIntegrationEvent> _publisher;

    public AccountNotifier(
        IExternalDomainEventPublisher<AccountCreatedIntegrationEvent> publisher)
    {
        _publisher = publisher;
    }

    public async Task ExecuteAsync(DomainEventData input)
    {
        var internalEvent = DomainEventMapper.ToDomain<AccountCreated>(input);
        var integrationEvent = new AccountCreatedIntegrationEvent(
            internalEvent.Id,
            internalEvent.OccurredOn,
            internalEvent.Source,
            internalEvent.AccountNumber,
            internalEvent.Owner,
            internalEvent.Metadata);

        await _publisher.PublishAsync(integrationEvent);
    }
}
```

**Notes:**
- ✅ Asynchronous (publishing to external systems is I/O-bound)
- ✅ Corresponds to Java `ExternalDomainEventPublisher.publish(E)`
- ✅ Concrete adapters (Kafka, RabbitMQ, etc.) live outside the core packages

**Related:**
- [IExternalDomainEvent](#iexternaldomainevent)
- [INotifier<TInput>](#inotifier)
- [IReactor<TInput>](#ireactor)

---

#### PostEventFailureException

**Namespace:** `EzDdd.UseCase.Exceptions`

**Signature:**
```csharp
public class PostEventFailureException : Exception
{
    public PostEventFailureException();
    public PostEventFailureException(string message);
    public PostEventFailureException(string message, Exception innerException);
}
```

**Description:**
Exception thrown when a message producer fails to post a message to the message bus. Wraps infrastructure-level failures from message broker clients (network loss, broker unavailable, serialization or auth failures) behind a consistent exception type.

**Example:**
```csharp
try
{
    await eventProducer.PostAsync(eventData);
}
catch (PostEventFailureException ex)
{
    _logger.LogError(ex, "Failed to publish event to message bus");
    // Handle failure (retry, compensate, alert)
}
```

**Notes:**
- ✅ Infrastructure-level exception (relay / producer adapters)
- ✅ Wrap broker client exceptions as the inner exception

**Related:**
- [IExternalDomainEventPublisher](#iexternaldomaineventpublisher)
- [RepositoryPeerSaveException](#repositorypeersaveexception)

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
public interface IInquiry<in TInput, TOutput>
{
    Task<TOutput> QueryAsync(TInput input);
}
```

**Description:**
Validation queries usable within commands. Inquiries are read-only operations that validate conditions before command execution. Intentionally independent of `IUseCase` (no `ExecuteAsync`, no constraints) to avoid use case infrastructure overhead for quick validation checks; by convention inputs implement [IInquiryInput](#iinquiryinput).

**Type Parameters:**
- `TInput`: The input type (contravariant; conventionally an `IInquiryInput`)
- `TOutput`: The output type (unconstrained)

**Methods:**

#### QueryAsync
Executes the inquiry and returns the result.

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

    public async Task<CheckAccountExistsOutput> QueryAsync(
        CheckAccountExistsInput input)
    {
        var account = await _archive.FindByIdAsync(input.AccountNumber);

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
        var checkResult = await _checkExists.QueryAsync(
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
- ✅ Own `QueryAsync` contract — NOT an `IUseCase` (no `ExecuteAsync`)

**Related:**
- [IInquiryInput](#iinquiryinput)
- [ICommand](#icommand)
- [IArchive](#iarchive)

---

#### IInquiryInput

**Namespace:** `EzDdd.Cqrs.Command`

**Signature:**
```csharp
public interface IInquiryInput
{
    // Pure marker interface - no methods
}
```

**Description:**
Marker interface for inputs to validation inquiries. Standalone marker — it does **not** extend `IInput` (inquiries are independent of the use case infrastructure).

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
- ✅ Standalone marker (does not extend IInput)

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
public interface IProjection<in TInput, TOutput>
    where TInput : IProjectionInput
{
    Task<TOutput> QueryAsync(TInput input);
}
```

**Description:**
Read model builder that generates view models from the query database. Projections transform raw read model data into presentation-friendly formats. Independent of `IUseCase`: it declares its own `QueryAsync` method.

**Type Parameters:**
- `TInput`: The input type (must implement IProjectionInput)
- `TOutput`: The output type (unconstrained)

**Methods:**

#### QueryAsync
Executes the projection query to build a read model view.

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

    public async Task<AccountTransactionHistoryOutput> QueryAsync(
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
- ✅ Own `QueryAsync` contract — NOT an `IUseCase` (no `ExecuteAsync`)

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
public interface IProjector<in TInput> : IReactor<TInput>
{
    // Inherits Task ExecuteAsync(TInput input) from IReactor<TInput>
}
```

**Description:**
A kind of [IReactor<TInput>](#ireactor) that writes read models in a query database. Projectors receive domain events published by the write model and project them into denormalized read models in [IArchive](#iarchive), keeping the query side eventually consistent with the write side. Formerly a non-generic marker interface; genericized in ADR-0028 to mirror upstream `Projector<Input> extends Reactor<Input>`.

**Type Parameters:**
- `TInput`: The type of input message (typically domain event data) this projector processes (contravariant)

**CQRS Flow:**
```
Command → Aggregate → Events → Repository → Relay → Projector → Archive → Query
```

**Example:**
```csharp
public class AccountSummaryProjector : IProjector<DomainEventData>
{
    private readonly IArchive<AccountSummaryReadModel, Guid> _archive;

    public AccountSummaryProjector(
        IArchive<AccountSummaryReadModel, Guid> archive)
    {
        _archive = archive;
    }

    public async Task ExecuteAsync(DomainEventData input)
    {
        switch (input.EventType)
        {
            case "AccountCreated":
                var created = DomainEventMapper.ToDomain<AccountCreated>(input);
                await _archive.SaveAsync(new AccountSummaryReadModel
                {
                    Id = Guid.Parse(created.Source),
                    AccountNumber = created.AccountNumber,
                    Owner = created.Owner,
                    Balance = created.InitialBalance
                });
                break;

            case "MoneyDeposited":
                var deposited = DomainEventMapper.ToDomain<MoneyDeposited>(input);
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

// Startup configuration (events delivered by a relay; see
// examples/EventInfrastructure for a reference relay implementation):
services.AddSingleton<IProjector<DomainEventData>, AccountSummaryProjector>();
```

**Notes:**
- ✅ Reactor for read model maintenance (event handling contract inherited from IReactor<TInput>)
- ✅ Receives domain events from infrastructure (e.g., an event store relay)
- ✅ Updates read models asynchronously
- ✅ Maintains eventual consistency
- ✅ Lifecycle (Start/Stop) stays an infrastructure concern — combine with `IHostedService`/`BackgroundService`
- ⚠️ Handle events idempotently (same event processed twice yields same result)

**Related:**
- [IProjection](#iprojection)
- [IReactor<TInput>](#ireactor)
- [INotifier<TInput>](#inotifier)
- [IArchive](#iarchive)

---

#### INotifier

**Namespace:** `EzDdd.Cqrs.Query`

**Signature:**
```csharp
public interface INotifier<in TInput> : IReactor<TInput>
{
    // Inherits Task ExecuteAsync(TInput input) from IReactor<TInput>
}
```

**Description:**
A kind of [IReactor<TInput>](#ireactor) that receives internal domain events, converts them into external domain events (integration events), and dispatches them through an out-port to front-ends, downstream bounded contexts, or external systems (such as Kafka), in order to notify others of aggregate state changes. The notifier upholds the cross-layer principle of Clean Architecture: objects from the entities layer must not leave the use cases layer and travel outward directly.

**Type Parameters:**
- `TInput`: The type of input message (typically internal domain event data) this notifier processes (contravariant)

**Example:**
```csharp
public class AccountNotifier : INotifier<DomainEventData>
{
    private readonly IExternalDomainEventPublisher<AccountCreatedIntegrationEvent> _publisher;

    public AccountNotifier(
        IExternalDomainEventPublisher<AccountCreatedIntegrationEvent> publisher)
    {
        _publisher = publisher;
    }

    public async Task ExecuteAsync(DomainEventData input)
    {
        if (input.EventType != "AccountCreated")
        {
            return;
        }

        var internalEvent = DomainEventMapper.ToDomain<AccountCreated>(input);
        var integrationEvent = new AccountCreatedIntegrationEvent(
            internalEvent.Id,
            internalEvent.OccurredOn,
            internalEvent.Source,
            internalEvent.AccountNumber,
            internalEvent.Owner,
            internalEvent.Metadata);

        await _publisher.PublishAsync(integrationEvent);
    }
}
```

**Notes:**
- ✅ Converts internal domain events to integration events at the layer boundary
- ✅ Dispatches through IExternalDomainEventPublisher (out-port)
- ✅ Mirrors upstream `Notifier<Input>` (since Java ezddd 5.0.0)
- ⚠️ Handle events idempotently, like all reactors

**Related:**
- [IReactor<TInput>](#ireactor)
- [IProjector<TInput>](#iprojector)
- [IExternalDomainEventPublisher](#iexternaldomaineventpublisher)
- [IExternalDomainEvent](#iexternaldomainevent)

---

#### IProjectionInput

**Namespace:** `EzDdd.Cqrs.Query`

**Signature:**
```csharp
public interface IProjectionInput
{
    // Pure marker interface - no methods
}
```

**Description:**
Marker interface for inputs to projections. Standalone marker — it does **not** extend `IInput` (projections are independent of the use case infrastructure).

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
- ✅ Standalone marker (does not extend IInput)

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
- [ROADMAP.md](../../ROADMAP.md) - Development roadmap and progress tracking
- [README.md](../../README.md) - Project overview and quick start

---

*Last updated: 2026-07-05*
