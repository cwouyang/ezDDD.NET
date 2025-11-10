# ADR-0015: Cross-Platform DTO Structure (InternalDomainEventDto)

## Status

**Accepted**

- **Date**: 2025-11-10
- **Deciders**: Project maintainers
- **Status Date**: 2025-11-10

---

## Context

### Problem Statement

`InternalDomainEventDto` is a Data Transfer Object (DTO) used to transmit domain events between backend services and frontend clients (REST APIs, WebSockets, SignalR). The DTO structure determines:

- **Cross-platform compatibility**: Can C# frontends consume Java backend events (and vice versa)?
- **Frontend developer experience**: How easily can frontend developers work with event data?
- **Event routing**: Can events be routed based on bounded context or event type?
- **Serialization efficiency**: JSON string vs. structured dictionary for event data?

**The question**: Should C# and Java ezddd use the **same DTO structure** for cross-platform compatibility, or optimize independently for each platform?

### Relevant Context

**Initial C# Implementation** (Phase 3 - Before F.3):
```csharp
// BEFORE: C#-optimized structure (6 properties)
public class InternalDomainEventDto
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredOn { get; set; }
    public string Source { get; set; }                        // Aggregate ID
    public Dictionary<string, object> Metadata { get; set; }  // Object values
    public string EventType { get; set; }                     // Full type name
    public Dictionary<string, object> EventData { get; set; } // Structured data
}
```

**Java ezddd Implementation**:
```java
// Java: Cross-platform structure (7 properties)
public class InternalDomainEventDto
{
    private UUID id;
    private Instant occurredOn;
    private String jsonEvent;              // ← Serialized JSON string
    private String eventType;              // ← Full class name
    private String eventSimpleName;        // ← Simple name (AccountCreated)
    private String boundedContext;         // ← Context identifier
    private Map<String, String> metadata;  // ← String-to-String map
}
```

**Critical Incompatibilities**:

| Property | Java | C# (Before) | Impact |
|----------|------|-------------|--------|
| `jsonEvent` | String (serialized) | **MISSING** | 🔴 Frontend cannot parse event data |
| `eventSimpleName` | String | **MISSING** | 🔴 No simple name for UI display |
| `boundedContext` | String | **MISSING** | 🔴 Cannot route events by context |
| `Source` | **MISSING** | String | 🟡 Different field |
| Event Data | `jsonEvent` (string) | `EventData` (Dictionary) | 🔴 Incompatible serialization |
| Metadata | `Map<String, String>` | `Dictionary<string, object>` | 🟡 Type mismatch |

**Real-World Scenario**:
```
System A: Java Backend (ezddd)
    ↓ Publishes InternalDomainEventDto via REST API
System B: C# Frontend (React + TypeScript)
    ↓ Consumes JSON and deserializes
    ❌ FAILURE: Missing boundedContext, eventSimpleName fields
    ❌ FAILURE: EventData structure incompatible
```

### Constraints

- Must support frontend integration (React, Angular, Vue, TypeScript)
- Must support cross-platform scenarios (C# backend + Java frontend, or vice versa)
- Must enable event routing by bounded context (microservices)
- Must provide simple event names for UI display (without namespaces)
- Must remain serialization-agnostic (System.Text.Json, Newtonsoft.Json, Jackson)
- Breaking changes acceptable for alpha version

---

## Decision

**We will adopt the Java ezddd structure for `InternalDomainEventDto` to ensure 100% cross-platform compatibility. This is a breaking change from the initial C# implementation.**

### Details

**Updated C# Structure** (After F.3):
```csharp
public class InternalDomainEventDto
{
    /// <summary>Event unique identifier (not aggregate ID).</summary>
    public Guid Id { get; set; }

    /// <summary>Timestamp when the event occurred (UTC with timezone).</summary>
    public DateTimeOffset OccurredOn { get; set; }

    /// <summary>
    /// Bounded context name where this event originated (e.g., "banking", "inventory").
    /// Used for routing and filtering events in cross-context scenarios.
    /// </summary>
    public string BoundedContext { get; set; } = string.Empty;

    /// <summary>
    /// Simple event type name without namespace (e.g., "MoneyDeposited").
    /// Corresponds to the domain event class name without fully-qualified namespace.
    /// </summary>
    public string EventSimpleName { get; set; } = string.Empty;

    /// <summary>
    /// Full event type name including namespace (e.g., "BankingContext.MoneyDeposited").
    /// Used for backend event type mapping and deserialization.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Event data serialized as JSON string.
    /// Contains the business data relevant to this event type in JSON format.
    /// </summary>
    /// <example>
    /// For MoneyDeposited: "{\"aggregateId\":\"account-123\",\"amount\":500}"
    /// </example>
    public string JsonEvent { get; set; } = string.Empty;

    /// <summary>
    /// Event metadata as key-value string pairs (e.g., userId, correlationId).
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
```

**Key Properties**:

1. **BoundedContext** (NEW):
   - Purpose: Event routing in microservices/multi-tenant systems
   - Example: `"banking"`, `"inventory"`, `"shipping"`
   - Use case: Frontend can filter events by context

2. **EventSimpleName** (NEW):
   - Purpose: Human-readable display in UI
   - Example: `"MoneyDeposited"` (not `"BankingContext.Events.MoneyDeposited"`)
   - Use case: Event log displays, notifications

3. **EventType** (ADDED):
   - Purpose: Full type name for backend deserialization
   - Example: `"BankingContext.Events.MoneyDeposited"`
   - Use case: Backend event type mapping

4. **JsonEvent** (NEW):
   - Purpose: Serialized event data as opaque JSON string
   - Example: `"{\"aggregateId\":\"account-123\",\"amount\":500}"`
   - Use case: Frontend parses JSON without backend deserialization

5. **Metadata** (CHANGED):
   - Before: `Dictionary<string, object>`
   - After: `Dictionary<string, string>`
   - Reason: Cross-platform compatibility (complex objects → JSON strings)

**Properties Removed**:
- `Source` (aggregate ID) → Included in `JsonEvent` as `aggregateId` field
- `EventData` (Dictionary) → Replaced by `JsonEvent` (string)

**Serialization Example**:
```csharp
// Domain Event
var domainEvent = new MoneyDeposited(
    id: Guid.NewGuid(),
    occurredOn: DateTimeOffset.UtcNow,
    source: "account-123",
    metadata: new Dictionary<string, string> { ["userId"] = "user-1" },
    amount: 500m
);

// Convert to DTO
var dto = new InternalDomainEventDto
{
    Id = domainEvent.Id,
    OccurredOn = domainEvent.OccurredOn,
    BoundedContext = "banking",
    EventSimpleName = "MoneyDeposited",
    EventType = "BankingContext.Events.MoneyDeposited",
    JsonEvent = JsonSerializer.Serialize(new
    {
        aggregateId = domainEvent.Source,
        amount = domainEvent.Amount
    }),
    Metadata = domainEvent.Metadata
};

// Serialize to JSON for REST API
var json = JsonSerializer.Serialize(dto);
/*
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "occurredOn": "2025-11-10T12:34:56Z",
  "boundedContext": "banking",
  "eventSimpleName": "MoneyDeposited",
  "eventType": "BankingContext.Events.MoneyDeposited",
  "jsonEvent": "{\"aggregateId\":\"account-123\",\"amount\":500}",
  "metadata": { "userId": "user-1" }
}
*/
```

**Frontend Consumption** (TypeScript):
```typescript
interface InternalDomainEventDto {
  id: string;
  occurredOn: string;
  boundedContext: string;
  eventSimpleName: string;
  eventType: string;
  jsonEvent: string;
  metadata: Record<string, string>;
}

// Parse event
const dto: InternalDomainEventDto = await response.json();

// Display in UI
console.log(`Event: ${dto.eventSimpleName} from ${dto.boundedContext}`);

// Parse event data
const eventData = JSON.parse(dto.jsonEvent);
console.log(`Amount: ${eventData.amount}`);
```

---

## Consequences

### Positive Consequences

- ✅ **100% Cross-Platform Compatibility**: C# and Java systems can exchange events seamlessly
- ✅ **Event Routing**: BoundedContext enables filtering and routing in microservices
- ✅ **Frontend Developer Experience**: EventSimpleName provides human-readable names
- ✅ **Opaque Event Data**: JsonEvent avoids backend deserialization (pass-through)
- ✅ **Metadata Consistency**: String-to-string map works across all platforms
- ✅ **Future-Proof**: Compatible with Java ezddd future versions
- ✅ **Microservices Ready**: Supports multi-context event distribution

### Negative Consequences

- ❌ **Breaking Change**: Incompatible with initial C# implementation (alpha version only)
- ❌ **Double Serialization**: Event data serialized twice (domain event → JsonEvent → DTO JSON)
- ❌ **String-Only Metadata**: Complex metadata must be JSON-encoded as strings
- ❌ **Less Type-Safe**: JsonEvent is opaque string (frontend must parse)

### Neutral Consequences

- ⚖️ **JSON String Trade-off**: More flexible (any structure) but less structured (requires parsing)
- ⚖️ **Backend Complexity**: Backend must serialize event data to JsonEvent string
- ⚖️ **Frontend Responsibility**: Frontend must parse JsonEvent for event-specific data

---

## Alternatives Considered

### Alternative 1: Keep C# Structure (Independent Design)

**Description**: Keep the original C# structure with `EventData` dictionary and `Source` field

**C# Structure**:
```csharp
public class InternalDomainEventDto
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredOn { get; set; }
    public string Source { get; set; }                        // Aggregate ID
    public Dictionary<string, object> Metadata { get; set; }  // Object values
    public string EventType { get; set; }                     // Full type name
    public Dictionary<string, object> EventData { get; set; } // Structured data
}
```

**Pros**:
- More type-safe (Dictionary vs JSON string)
- Single serialization (no JsonEvent intermediate step)
- Better IDE support (IntelliSense for structured data)
- More .NET-idiomatic (POCO with properties)

**Cons**:
- **Cross-platform incompatibility**: C# frontend CANNOT consume Java backend events
- **No event routing**: Missing `boundedContext` field
- **No simple name**: Missing `eventSimpleName` for UI display
- **Ecosystem fragmentation**: C# and Java ezddd diverge
- **Migration burden**: Users must write custom converters for cross-platform scenarios

**Why rejected**: Breaks cross-platform compatibility, which is a core value proposition of ezDDD.NET. Organizations using both C# and Java need seamless event exchange. The ergonomic benefits of structured data don't outweigh the interoperability loss.

---

### Alternative 2: Support Both Structures (Dual Mode)

**Description**: Support both C# and Java structures with converters

**Implementation**:
```csharp
// Option 1: C#-native structure
public class InternalDomainEventDto { /* ... C# structure ... */ }

// Option 2: Java-compatible structure
public class InternalDomainEventDtoJava { /* ... Java structure ... */ }

// Converter
public static class DtoConverter
{
    public static InternalDomainEventDtoJava ToJavaFormat(InternalDomainEventDto csDto) { /* ... */ }
    public static InternalDomainEventDto FromJavaFormat(InternalDomainEventDtoJava javaDto) { /* ... */ }
}
```

**Pros**:
- Both C# and Java structures available
- Users choose based on scenario
- Backward compatible with initial C# structure

**Cons**:
- **API fragmentation**: Two DTOs for same concept
- **Converter maintenance**: Must maintain bidirectional conversion
- **Documentation complexity**: Users must choose correct DTO
- **Test burden**: Must test both structures and converters
- **Ambiguous default**: Which structure is primary?
- **Performance overhead**: Conversion adds latency

**Why rejected**: Over-engineering. Adds significant complexity without clear benefit. Users would be confused about which DTO to use. Converter bugs could introduce subtle serialization issues. Simpler to have one canonical structure.

---

### Alternative 3: Minimal Cross-Platform (Add Missing Fields Only)

**Description**: Keep C# structure but add only essential Java fields (boundedContext, eventSimpleName)

**Implementation**:
```csharp
public class InternalDomainEventDto
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredOn { get; set; }
    public string Source { get; set; }                        // C# field
    public string BoundedContext { get; set; }                // Java field
    public string EventSimpleName { get; set; }               // Java field
    public Dictionary<string, object> Metadata { get; set; }  // C# type
    public string EventType { get; set; }
    public Dictionary<string, object> EventData { get; set; } // C# structure
}
```

**Pros**:
- Partial compatibility (routing and display work)
- Keeps C# structured data benefits
- Smaller breaking change

**Cons**:
- **Hybrid inconsistency**: Mix of C# and Java conventions
- **Metadata incompatibility**: Dictionary<string, object> vs Map<String, String>
- **EventData incompatibility**: Dictionary vs jsonEvent string
- **Partial cross-platform**: Works for some fields, fails for others
- **Confusing**: Neither fully C# nor fully Java

**Why rejected**: Half-measure that doesn't solve the core problem. Frontend would still fail to parse EventData correctly. Metadata type mismatch would cause serialization errors. Better to fully commit to one structure than create a confusing hybrid.

---

## Related Decisions

- **Related to**: [ADR-0005](0005-complete-reimplementation-approach.md) - Complete reimplementation allows breaking changes for correctness
- **Related to**: [ADR-0014](0014-domaineventdata-equality-semantics.md) - JSON-aware semantics also important for cross-platform compatibility
- **Influences**: Frontend integration patterns, event routing architecture, microservices event distribution

---

## Implementation Notes

### Implementation Checklist (Phase F.3 - Completed 2025-11-10)

- ✅ Updated `InternalDomainEventDto.cs` with Java-compatible structure (7 properties)
- ✅ Added `BoundedContext`, `EventSimpleName`, `EventType`, `JsonEvent` properties
- ✅ Removed `Source`, `EventData` (Dictionary) properties
- ✅ Changed `Metadata` from `Dictionary<string, object>` to `Dictionary<string, string>`
- ✅ Updated XML documentation with cross-platform compatibility notes
- ✅ Rewrote all 12 unit tests in `InternalDomainEventDtoTests.cs`:
  - Property getters/setters tests
  - Serialization roundtrip tests
  - JsonEvent parsing tests
  - BoundedContext routing tests
  - EventSimpleName display tests
  - Metadata string-to-string tests
- ✅ Updated 1 integration test in `EventInfrastructureTests.cs`
- ✅ All 426 tests passing (no regressions)
- ✅ Zero new compiler warnings

### Migration Guide (For Alpha Users)

**Breaking Changes**:
1. `Source` property removed → Use `aggregateId` field in `JsonEvent`
2. `EventData` (Dictionary) removed → Use `JsonEvent` (string)
3. `Metadata` changed from `Dictionary<string, object>` to `Dictionary<string, string>`

**Migration Steps**:
```csharp
// BEFORE (C# structure)
var dto = new InternalDomainEventDto
{
    Source = "account-123",
    EventData = new Dictionary<string, object>
    {
        ["amount"] = 500,
        ["currency"] = "USD"
    },
    Metadata = new Dictionary<string, object>
    {
        ["userId"] = 42  // int value
    }
};

// AFTER (Java-compatible structure)
var dto = new InternalDomainEventDto
{
    BoundedContext = "banking",
    EventSimpleName = "MoneyDeposited",
    EventType = "BankingContext.Events.MoneyDeposited",
    JsonEvent = JsonSerializer.Serialize(new
    {
        aggregateId = "account-123",  // Source moved here
        amount = 500,
        currency = "USD"
    }),
    Metadata = new Dictionary<string, string>
    {
        ["userId"] = "42"  // Convert to string
    }
};
```

### Usage Examples

**Backend (C# REST API)**:
```csharp
[HttpGet("events")]
public async Task<IEnumerable<InternalDomainEventDto>> GetEvents(string context)
{
    var events = await _repository.GetEventsAsync();

    return events
        .Where(e => e.BoundedContext == context)  // Filter by context
        .Select(e => new InternalDomainEventDto
        {
            Id = e.Id,
            OccurredOn = e.OccurredOn,
            BoundedContext = context,
            EventSimpleName = e.GetType().Name,
            EventType = e.GetType().FullName,
            JsonEvent = JsonSerializer.Serialize(e),
            Metadata = e.Metadata
        });
}
```

**Frontend (TypeScript + React)**:
```typescript
// Event log component
function EventLog() {
  const [events, setEvents] = useState<InternalDomainEventDto[]>([]);

  useEffect(() => {
    fetch('/api/events?context=banking')
      .then(res => res.json())
      .then(setEvents);
  }, []);

  return (
    <ul>
      {events.map(event => (
        <li key={event.id}>
          <strong>{event.eventSimpleName}</strong>
          {' from '}
          <em>{event.boundedContext}</em>
          {' at '}
          {new Date(event.occurredOn).toLocaleString()}
          <pre>{event.jsonEvent}</pre>
        </li>
      ))}
    </ul>
  );
}
```

---

## References

- [Phase 3 Final Review Report](../review/PHASE3_FINAL_REVIEW_REPORT.md) - Identified DTO incompatibility as critical issue
- [GROUP_4_REVIEW.md](../review/GROUP_4_REVIEW.md) - Critical Issue #2: Structure Incompatibility (lines 102-170)
- [InternalDomainEventDto.cs](../../src/EzDdd.UseCase/Port/InOut/InternalDomainEventDto.cs) - Implementation (lines 66-117)
- [Java InternalDomainEventDto.java](../../../../ezddd/ezddd-usecase/src/main/java/tw/teddysoft/ezddd/usecase/port/inout/domainevent/InternalDomainEventDto.java) - Java reference
- [PHASE3_POST_REVIEW_SESSION_STATE.md](../../PHASE3_POST_REVIEW_SESSION_STATE.md) - F.3 implementation record (lines 106-144)

---

## Revision History

| Date       | Status   | Notes                          |
|------------|----------|--------------------------------|
| 2025-11-10 | Accepted | Decision finalized, F.3 implementation complete |

---
