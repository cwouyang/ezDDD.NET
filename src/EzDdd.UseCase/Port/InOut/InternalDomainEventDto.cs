namespace EzDdd.UseCase.Port.InOut;

/// <summary>
///     Data Transfer Object for transmitting internal domain events to frontend or external systems.
///     Provides a JSON-friendly structure compatible with Java ezddd for cross-platform integration.
/// </summary>
/// <remarks>
///     <para>
///         <b>Design Rationale</b>:
///         <list type="bullet">
///             <item>
///                 <description><b>DTO pattern</b>: Pure data structure with no domain logic</description>
///             </item>
///             <item>
///                 <description><b>Mutable properties</b>: Enables easy serialization/deserialization by JSON frameworks</description>
///             </item>
///             <item>
///                 <description>
///                     <b>JSON string format</b>: Event data stored as JSON string for flexibility and
///                     cross-platform compatibility
///                 </description>
///             </item>
///             <item>
///                 <description><b>Cross-platform compatible</b>: Structure matches Java ezddd InternalDomainEventDto</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <b>Usage Scenarios</b>:
///         <list type="bullet">
///             <item>
///                 <description>REST API responses returning event history</description>
///             </item>
///             <item>
///                 <description>WebSocket/SignalR real-time event notifications</description>
///             </item>
///             <item>
///                 <description>Event log queries for user interfaces</description>
///             </item>
///             <item>
///                 <description>Cross-platform integration (C# frontend + Java backend, or vice versa)</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <b>Key Differences from Domain Events</b>:
///         <list type="bullet">
///             <item>
///                 <description>Domain events: Strongly-typed, immutable records with business semantics</description>
///             </item>
///             <item>
///                 <description>
///                     DTOs: Weakly-typed JSON strings optimized for serialization and cross-platform
///                     compatibility
///                 </description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <b>Cross-Platform Compatibility</b>:
///         This DTO structure matches Java ezddd's InternalDomainEventDto to ensure seamless integration
///         between C# and Java systems. The JsonEvent field contains the serialized event data as a JSON string,
///         which can be parsed by either platform.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Convert domain event to DTO for API response
/// var domainEvent = new MoneyDeposited(
///     Guid.NewGuid(),
///     DateTimeOffset.UtcNow,
///     "account-123",
///     new Dictionary&lt;string, string&gt; { ["userId"] = "user-1" },
///     500m
/// );
///
/// var eventData = new { aggregateId = "account-123", amount = domainEvent.Amount };
/// var dto = new InternalDomainEventDto
/// {
///     Id = domainEvent.Id,
///     OccurredOn = domainEvent.OccurredOn,
///     BoundedContext = "banking",
///     EventSimpleName = "MoneyDeposited",
///     JsonEvent = JsonSerializer.Serialize(eventData),
///     Metadata = domainEvent.Metadata
/// };
///
/// // Serialize to JSON for REST API
/// var json = JsonSerializer.Serialize(dto);
/// </code>
/// </example>
public class InternalDomainEventDto
{
    /// <summary>
    ///     Event unique identifier (not aggregate ID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Timestamp when the event occurred (UTC with timezone information).
    /// </summary>
    public DateTimeOffset OccurredOn { get; set; }

    /// <summary>
    ///     Bounded context name where this event originated (e.g., "banking", "inventory").
    ///     Used for routing and filtering events in cross-context scenarios.
    /// </summary>
    public string BoundedContext { get; set; } = string.Empty;

    /// <summary>
    ///     Simple event type name without namespace (e.g., "MoneyDeposited", "AccountCreated").
    ///     Corresponds to the domain event class name without fully-qualified namespace.
    /// </summary>
    public string EventSimpleName { get; set; } = string.Empty;

    /// <summary>
    ///     Event data serialized as JSON string.
    ///     Contains the business data relevant to this event type in JSON format.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The JSON string format allows for flexible cross-platform integration and
    ///         maintains compatibility with Java ezddd. Consumers can parse this JSON string
    ///         using their platform's JSON library (System.Text.Json for C#, Jackson for Java).
    ///     </para>
    /// </remarks>
    /// <example>
    ///     For MoneyDeposited: "{\"aggregateId\":\"account-123\",\"amount\":500,\"currency\":\"USD\"}"
    ///     For AccountCreated: "{\"aggregateId\":\"account-456\",\"owner\":\"John Doe\",\"initialBalance\":1000}"
    /// </example>
    public string JsonEvent { get; set; } = string.Empty;

    /// <summary>
    ///     Event metadata as key-value string pairs (e.g., userId, correlationId, causationId).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Uses string-to-string dictionary for cross-platform compatibility with Java ezddd.
    ///         Complex metadata values should be serialized to JSON strings.
    ///     </para>
    /// </remarks>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
