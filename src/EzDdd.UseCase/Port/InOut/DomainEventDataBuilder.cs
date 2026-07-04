using System.Text.Json;

namespace EzDdd.UseCase.Port.InOut;

/// <summary>
///     Fluent builder for constructing <see cref="DomainEventData" /> instances.
/// </summary>
/// <remarks>
///     <para>
///         This builder provides a convenient and type-safe way to construct DomainEventData
///         with intelligent default value handling and automatic serialization.
///     </para>
///     <para>
///         <strong>Features:</strong>
///         <list type="bullet">
///             <item>
///                 <description>
///                     <strong>Factory Methods:</strong> <c>Json()</c> and <c>Binary()</c> for different payload
///                     types
///                 </description>
///             </item>
///             <item>
///                 <description><strong>Fluent API:</strong> Chain method calls for readable construction</description>
///             </item>
///             <item>
///                 <description>
///                     <strong>Optional Parameters:</strong> EventId and Metadata are optional with smart
///                     defaults
///                 </description>
///             </item>
///             <item>
///                 <description><strong>Automatic Serialization:</strong> Json() method automatically serializes payload</description>
///             </item>
///             <item>
///                 <description><strong>ContentType Management:</strong> Automatically sets based on factory method</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <strong>When to Use:</strong>
///         <list type="bullet">
///             <item>
///                 <description>In production code (e.g., DomainEventMapper) - more elegant than direct construction</description>
///             </item>
///             <item>
///                 <description>
///                     When you want optional parameters with defaults (eventId auto-generated, metadata defaults
///                     to empty)
///                 </description>
///             </item>
///             <item>
///                 <description>When you want automatic serialization (builder handles JSON conversion)</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <strong>Alternative:</strong>
///         You can also construct <see cref="DomainEventData" /> directly using its record constructor
///         if you need full control over all parameters (common in test code).
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Production code: Using builder (elegant, with defaults)
/// var eventData = DomainEventDataBuilder
///     .Json("OrderCreated", orderEvent)
///     // .EventId(...) optional - auto-generates Guid.NewGuid()
///     // .MetadataAsJson(...) optional - defaults to "{}"
///     .Build();
///
/// // Production code: With all options
/// var eventData = DomainEventDataBuilder
///     .Json("MoneyDeposited", depositEvent)
///     .EventId(Guid.Parse("..."))
///     .MetadataAsJson(new { CorrelationId = "123", UserId = "user@example.com" })
///     .Build();
///
/// // Binary payload support
/// var eventData = DomainEventDataBuilder
///     .Binary("LegacyEvent", avroBytes)
///     .MetadataAsBytes(metadataBytes)
///     .Build();
///
/// // Alternative: Direct construction (test code, full control)
/// var eventData = new DomainEventData(
///     Guid.NewGuid(),
///     "TestEvent",
///     "application/json",
///     eventBodyBytes,
///     metadataBytes
/// );
/// </code>
/// </example>
public class DomainEventDataBuilder
{
    private string _contentType = "application/json";
    private byte[]? _eventBody;
    private string? _eventType;
    private Guid? _id;
    private byte[]? _metadata;

    /// <summary>
    ///     Private constructor - use factory methods <see cref="Json{T}" /> or <see cref="Binary" />.
    /// </summary>
    private DomainEventDataBuilder() { }

    /// <summary>
    ///     Creates a builder for JSON-serialized event data.
    /// </summary>
    /// <typeparam name="T">The type of the event payload</typeparam>
    /// <param name="eventType">The event type identifier (e.g., "OrderCreated")</param>
    /// <param name="payload">The event payload to serialize as JSON</param>
    /// <returns>A builder instance with JSON payload and contentType set</returns>
    /// <exception cref="ArgumentNullException">Thrown when eventType or payload is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when JSON serialization fails</exception>
    /// <remarks>
    ///     This method automatically:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Serializes the payload to JSON using System.Text.Json</description>
    ///         </item>
    ///         <item>
    ///             <description>Sets ContentType to "application/json"</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static DomainEventDataBuilder Json<T>(string eventType, T payload)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(payload);

        DomainEventDataBuilder builder = new() { _eventType = eventType };

        try
        {
            builder._eventBody = JsonSerializer.SerializeToUtf8Bytes(payload);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to serialize payload to JSON: {ex.Message}", ex);
        }

        builder._contentType = "application/json";

        return builder;
    }

    /// <summary>
    ///     Creates a builder for binary event data (non-JSON formats like Avro, Protobuf).
    /// </summary>
    /// <param name="eventType">The event type identifier</param>
    /// <param name="payload">The pre-serialized binary payload</param>
    /// <returns>A builder instance with binary payload and contentType set to "application/octet-stream"</returns>
    /// <exception cref="ArgumentNullException">Thrown when eventType or payload is null</exception>
    public static DomainEventDataBuilder Binary(string eventType, byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(payload);

        DomainEventDataBuilder builder = new()
        {
            _eventType = eventType,
            _eventBody = payload,
            _contentType = "application/octet-stream",
        };

        return builder;
    }

    /// <summary>
    ///     Sets the event ID (optional - auto-generates if not set).
    /// </summary>
    /// <param name="id">The unique event identifier</param>
    /// <returns>This builder instance for fluent chaining</returns>
    public DomainEventDataBuilder EventId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    ///     Sets metadata by serializing an object to JSON.
    /// </summary>
    /// <typeparam name="T">The metadata object type</typeparam>
    /// <param name="metadata">The metadata object to serialize</param>
    /// <returns>This builder instance for fluent chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown when JSON serialization fails</exception>
    public DomainEventDataBuilder MetadataAsJson<T>(T metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        try
        {
            _metadata = JsonSerializer.SerializeToUtf8Bytes(metadata);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to serialize metadata to JSON: {ex.Message}", ex);
        }

        return this;
    }

    /// <summary>
    ///     Sets metadata as pre-serialized bytes.
    /// </summary>
    /// <param name="metadata">The pre-serialized metadata bytes</param>
    /// <returns>This builder instance for fluent chaining</returns>
    public DomainEventDataBuilder MetadataAsBytes(byte[] metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        _metadata = metadata;
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="DomainEventData" /> instance with smart defaults.
    /// </summary>
    /// <returns>A new DomainEventData instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when eventType or eventBody not set</exception>
    /// <remarks>
    ///     <para>
    ///         <strong>Default Values:</strong>
    ///         <list type="bullet">
    ///             <item>
    ///                 <description><strong>EventId:</strong> Auto-generates Guid.NewGuid() if not set</description>
    ///             </item>
    ///             <item>
    ///                 <description><strong>Metadata:</strong> Defaults to "{}" (empty JSON object) if not set</description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <strong>ContentType:</strong> Set by factory method (Json → "application/json", Binary →
    ///                     "application/octet-stream")
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public DomainEventData Build()
    {
        if (_eventType == null)
        {
            throw new InvalidOperationException("EventType is required. Call Json() or Binary() first.");
        }

        if (_eventBody == null)
        {
            throw new InvalidOperationException("EventBody is required. Call Json() or Binary() first.");
        }

        Guid eventId = _id ?? Guid.NewGuid();
        byte[] userMetadata = _metadata ?? "{}"u8.ToArray();

        return new DomainEventData(eventId, _eventType, _contentType, _eventBody, userMetadata);
    }
}
