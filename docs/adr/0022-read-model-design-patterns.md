# ADR-0022: Read Model Design Patterns

## Status

**Accepted**

- **Date**: 2025-11-18
- **Deciders**: Development Team
- **Status Date**: 2025-11-18

---

## Context

### Problem Statement

What design patterns and C# language features should be used for read models in the CQRS query side to ensure immutability, performance, and maintainability?

### Relevant Context

- **CQRS pattern**: Read models are denormalized views optimized for queries, separate from write model
- **Projectors**: Build read models from domain events and store them in IArchive
- **Query requirements**: Read models must be fast to query, easy to serialize, and cache-friendly
- **C# features**: Records (C# 9+), classes, structs, and immutability options
- **Phase 4 implementation**: Iteration 6 integration tests implemented `AccountSummaryReadModel` as record
- **.NET platform**: Records provide built-in immutability, value equality, and with-expressions

### Constraints

- **Immutability preferred**: Read models should be immutable to prevent accidental modifications
- **Performance**: Read models are read frequently, mutations should be efficient (with-expressions)
- **Serialization**: Must serialize to/from JSON for potential caching or cross-process communication
- **Equality semantics**: Should support value equality for testing and caching

---

## Decision

**Use C# record types with positional parameters for all read models. Apply `[Entity]ReadModel` naming suffix for clarity.**

### Details

#### Read Model Pattern

```csharp
/// <summary>
///     Read model for account summary in the query side.
///     Immutable record type for efficient querying and caching.
/// </summary>
public record AccountSummaryReadModel(
    AccountId AccountId,
    string AccountNumber,
    decimal Balance,
    DateTimeOffset CreatedOn,
    DateTimeOffset LastTransactionDate
);
```

#### Key Characteristics

1. **Record Type**: Use `record` keyword for immutability and value equality
2. **Positional Parameters**: Primary constructor with positional parameters
3. **Naming Convention**: `[Entity]ReadModel` suffix (e.g., `AccountSummaryReadModel`, `OrderSummaryReadModel`)
4. **Immutability**: All properties are init-only (cannot be modified after construction)
5. **Value Equality**: Built-in structural equality (compares property values, not references)
6. **with-expressions**: Enable non-destructive mutations for projector updates

#### Projector Update Pattern

```csharp
public class AccountProjector : IProjector, IReactor, BackgroundService
{
    private readonly IArchive<AccountSummaryReadModel, AccountId> _archive;
    private readonly DomainEventMapper _eventMapper;

    public async Task UpdateAsync(DomainEventData eventData)
    {
        var domainEvent = _eventMapper.ToDomainEvent(eventData);

        switch (domainEvent)
        {
            case AccountCreated e:
                // Create new read model
                var readModel = new AccountSummaryReadModel(
                    AccountId: e.AccountId,
                    AccountNumber: e.AccountNumber,
                    Balance: e.InitialBalance,
                    CreatedOn: e.OccurredOn,
                    LastTransactionDate: e.OccurredOn
                );
                await _archive.SaveAsync(readModel);
                break;

            case MoneyDeposited e:
                // Update existing read model using with-expression
                var account = await _archive.FindByIdAsync(e.AccountId);
                if (account != null)
                {
                    var updated = account with
                    {
                        Balance = account.Balance + e.Amount,
                        LastTransactionDate = e.OccurredOn
                    };
                    await _archive.SaveAsync(updated);
                }
                break;

            case AccountClosed e:
                // Delete read model
                var toDelete = await _archive.FindByIdAsync(e.AccountId);
                if (toDelete != null)
                {
                    await _archive.DeleteAsync(toDelete);
                }
                break;
        }
    }
}
```

#### Serialization Support

```csharp
// System.Text.Json serialization works out-of-box with records
using System.Text.Json;

// Serialize
var json = JsonSerializer.Serialize(readModel);

// Deserialize
var restored = JsonSerializer.Deserialize<AccountSummaryReadModel>(json);

// Records automatically support JSON serialization
```

---

## Consequences

### Positive Consequences

- ✅ **Immutability**: Records are immutable by default, preventing accidental modifications
- ✅ **Value Equality**: Built-in structural equality simplifies testing and comparisons
- ✅ **Concise Syntax**: Positional parameters reduce boilerplate compared to classes
- ✅ **with-expressions**: Non-destructive mutations are efficient and readable
- ✅ **Serialization-friendly**: Works seamlessly with System.Text.Json
- ✅ **Cache-friendly**: Value equality makes records ideal for caching scenarios
- ✅ **Thread-safe**: Immutability eliminates thread-safety concerns
- ✅ **Clear Intent**: `ReadModel` suffix clearly identifies query-side objects

### Negative Consequences

- ❌ **C# 9+ Required**: Requires .NET 5+ (acceptable per ADR-0001, target is .NET 8)
- ❌ **Positional Parameter Verbosity**: Long parameter lists for complex read models
- ❌ **Learning Curve**: Teams unfamiliar with records need education

### Neutral Consequences

- ⚖️ **No Lazy Loading**: Records don't support lazy loading (acceptable for read models)
- ⚖️ **Limited Inheritance**: Records support inheritance but rarely needed for read models
- ⚖️ **Deconstruction**: Records support deconstruction, but rarely used in practice

---

## Alternatives Considered

### Alternative 1: Mutable Classes with Properties

```csharp
public class AccountSummaryReadModel
{
    public AccountId AccountId { get; set; }
    public string AccountNumber { get; set; }
    public decimal Balance { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset LastTransactionDate { get; set; }
}
```

**Pros**:
- Familiar to all C# developers
- Flexible - can add methods, private fields, etc.
- Supports lazy loading and computed properties

**Cons**:
- **Mutability risks** - properties can be modified accidentally
- **Reference equality** - default equality compares references, not values
- **More boilerplate** - requires explicit property definitions
- **Thread-safety concerns** - mutable objects require synchronization

**Why rejected**: Mutability is unnecessary for read models and introduces risks. Records provide immutability by default with less boilerplate.

---

### Alternative 2: Classes with Init-Only Properties

```csharp
public class AccountSummaryReadModel
{
    public AccountId AccountId { get; init; }
    public string AccountNumber { get; init; }
    public decimal Balance { get; init; }
    public DateTimeOffset CreatedOn { get; init; }
    public DateTimeOffset LastTransactionDate { get; init; }
}
```

**Pros**:
- Immutable like records
- More control over implementation details
- Can override equality if needed

**Cons**:
- **Reference equality** - doesn't provide value equality out-of-box
- **More boilerplate** - requires explicit property definitions
- **No with-expressions** - cannot use `with` for non-destructive mutations
- **Redundant** - records provide all benefits with less code

**Why rejected**: Records provide the same immutability with value equality and with-expressions. Classes with init-only properties are strictly inferior for this use case.

---

### Alternative 3: Readonly Structs

```csharp
public readonly struct AccountSummaryReadModel
{
    public AccountId AccountId { get; init; }
    public string AccountNumber { get; init; }
    public decimal Balance { get; init; }
    public DateTimeOffset CreatedOn { get; init; }
    public DateTimeOffset LastTransactionDate { get; init; }
}
```

**Pros**:
- Stack allocation for small structs
- Value semantics (value equality by default)
- Immutability via `readonly`

**Cons**:
- **Size limitations** - structs > 16 bytes should be heap-allocated (most read models are large)
- **Copying overhead** - structs are copied on assignment (expensive for large read models)
- **Nullability issues** - nullable structs (`AccountSummaryReadModel?`) are awkward
- **Not ideal for reference types** - AccountId is likely a class/record, embedding in struct is problematic

**Why rejected**: Structs are best for small, simple value types. Read models are typically larger and contain reference types, making structs inappropriate.

---

### Alternative 4: Anonymous Types (in projectors)

```csharp
// Create read model as anonymous type
var readModel = new
{
    AccountId = e.AccountId,
    AccountNumber = e.AccountNumber,
    Balance = e.InitialBalance,
    CreatedOn = e.OccurredOn,
    LastTransactionDate = e.OccurredOn
};
await _archive.SaveAsync(readModel); // ❌ Won't compile - wrong type
```

**Pros**:
- Very concise
- No explicit type definition needed

**Cons**:
- **Type incompatibility** - IArchive requires specific type, not anonymous
- **No reusability** - each projector must define structure inline
- **No serialization control** - cannot customize serialization
- **No type safety** - no compile-time checking across boundaries

**Why rejected**: Anonymous types are not compatible with IArchive<TData, TId> and provide no benefits over records. Completely impractical for this use case.

---

## Related Decisions

- **Related to**: [ADR-0018](0018-iarchive-async-method-design.md) (IArchive stores read models)
- **Related to**: [ADR-0020](0020-iprojector-lifecycle-management.md) (Projectors create and update read models)
- **Related to**: [ADR-0023](0023-archive-idempotency-requirements.md) (Read model updates must be idempotent)
- **Depends on**: [ADR-0001](0001-target-framework.md) (.NET 8 supports records)

---

## Implementation Notes

### Naming Convention

Follow this pattern for read model names:

```csharp
// Pattern: [Entity]ReadModel or [Entity][Purpose]ReadModel
AccountSummaryReadModel      // Summary of account
OrderDetailsReadModel        // Detailed view of order
ProductListItemReadModel     // Item in product list
CustomerProfileReadModel     // Customer profile view
```

### Complex Read Models

For read models with many properties, consider grouping:

```csharp
public record OrderDetailsReadModel(
    OrderId OrderId,
    string OrderNumber,
    OrderStatus Status,

    // Customer info group
    CustomerId CustomerId,
    string CustomerName,
    string CustomerEmail,

    // Financial info group
    decimal Subtotal,
    decimal Tax,
    decimal Total,

    // Metadata
    DateTimeOffset CreatedOn,
    DateTimeOffset? CompletedOn
);
```

### Computed Properties

Add computed properties as expression-bodied members:

```csharp
public record AccountSummaryReadModel(
    AccountId AccountId,
    string AccountNumber,
    decimal Balance,
    DateTimeOffset CreatedOn,
    DateTimeOffset LastTransactionDate
)
{
    // Computed property (not stored)
    public bool IsActive => Balance > 0;

    // Computed property with logic
    public int DaysSinceCreation => (int)(DateTimeOffset.UtcNow - CreatedOn).TotalDays;
}
```

### Collection Properties

Use immutable collections for properties containing collections:

```csharp
using System.Collections.Immutable;

public record OrderSummaryReadModel(
    OrderId OrderId,
    string OrderNumber,
    ImmutableList<OrderLineReadModel> Lines,  // Immutable collection
    decimal Total
);

public record OrderLineReadModel(
    ProductId ProductId,
    string ProductName,
    int Quantity,
    decimal Price
);
```

---

## References

- **C# Records**: [Microsoft Docs - Records](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
- **with-expressions**: [Microsoft Docs - with expressions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/with-expression)
- **Value Equality**: [Microsoft Docs - Equality comparisons](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/equality-comparisons)
- Phase 4 implementation plan - Iteration 6 integration tests (AccountSummaryReadModel) (internal working note, not retained in the repository)
- Phase 4 session notes - Implementation evidence (Iteration 6 complete; internal working notes, not retained in the repository)

---

## Revision History

| Date       | Status      | Notes                          |
|------------|-------------|--------------------------------|
| 2025-11-18 | Accepted    | Initial decision after Iteration 6 integration tests |

---
