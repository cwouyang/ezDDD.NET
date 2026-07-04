using System.Text.Json;

namespace EzDdd.UseCase.Port.InOut;

/// <summary>
///     Immutable record for persisted domain events.
///     Stores event data as byte arrays to support flexible serialization formats.
/// </summary>
/// <param name="Id">Event ID (not aggregate ID)</param>
/// <param name="EventType">Mapped event class name (e.g., "BankAccountCreated")</param>
/// <param name="ContentType">Serialization format (e.g., "application/json")</param>
/// <param name="EventBody">Serialized event data as byte array</param>
/// <param name="UserMetadata">Event metadata as byte array</param>
/// <remarks>
///     <para>
///         <b>Design Rationale</b>:
///         <list type="bullet">
///             <item>
///                 <description><b>Byte arrays</b>: Supports any serialization format (JSON, Avro, Protobuf)</description>
///             </item>
///             <item>
///                 <description><b>Immutable</b>: Record type ensures thread-safe, value-based semantics</description>
///             </item>
///             <item>
///                 <description><b>JSON-aware equality</b>: Compares JSON content semantically (key order independent)</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <b>Persistence Format</b>: This record represents the storage format for domain events
///         in an event store or outbox table. The byte arrays contain serialized JSON (or other formats)
///         of the actual domain event and its metadata.
///     </para>
///     <para>
///         <b>Equality Semantics</b>: For JSON content, equality is JSON-aware (semantic comparison).
///         For non-JSON content, equality falls back to byte-level comparison. This ensures cross-platform
///         compatibility with Java ezddd (which uses JSONObject.similar() for semantic equality).
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Create event data
/// var eventJson = JsonSerializer.SerializeToUtf8Bytes(new { Amount = 100 });
/// var metadataJson = JsonSerializer.SerializeToUtf8Bytes(new { User = "admin" });
///
/// var data = new DomainEventData(
///     Guid.NewGuid(),
///     "MoneyDeposited",
///     "application/json",
///     eventJson,
///     metadataJson
/// );
///
/// // JSON-aware equality: key order doesn't matter
/// var data1 = new DomainEventData(..., UTF8("{\"a\":1,\"b\":2}"), ...);
/// var data2 = new DomainEventData(..., UTF8("{\"b\":2,\"a\":1}"), ...);
/// Assert.Equal(data1, data2); // true (same JSON content)
/// </code>
/// </example>
public record DomainEventData(Guid Id, string EventType, string ContentType, byte[] EventBody, byte[] UserMetadata)
{
    /// <summary>
    ///     Custom equality comparison that uses JSON-aware semantic comparison for JSON content.
    ///     Falls back to byte-level comparison for non-JSON content.
    /// </summary>
    /// <param name="other">The other DomainEventData to compare</param>
    /// <returns>True if all properties are equal, using JSON-aware comparison for byte arrays</returns>
    /// <remarks>
    ///     <para>
    ///         For JSON content (when byte arrays contain valid JSON), this method performs semantic
    ///         comparison where key order doesn't matter: {"a":1,"b":2} equals {"b":2,"a":1}.
    ///     </para>
    ///     <para>
    ///         For non-JSON content (when JSON parsing fails), this method falls back to byte-level
    ///         comparison using SequenceEqual().
    ///     </para>
    ///     <para>
    ///         This ensures cross-platform compatibility with Java ezddd, which uses JSONObject.similar()
    ///         for semantic JSON equality.
    ///     </para>
    /// </remarks>
    public virtual bool Equals(DomainEventData? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null)
        {
            return false;
        }

        return Id == other.Id
            && EventType == other.EventType
            && ContentType == other.ContentType
            && _JsonEquals(EventBody, other.EventBody)
            && _JsonEquals(UserMetadata, other.UserMetadata);
    }

    /// <summary>
    ///     Custom hash code based on Id only (stable, independent of JSON structure).
    /// </summary>
    /// <returns>Hash code based on Id</returns>
    /// <remarks>
    ///     <para>
    ///         The hash code is based on Id only, not on the JSON content. This ensures:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Stable hash code regardless of JSON key ordering</description>
    ///         </item>
    ///         <item>
    ///             <description>Fast hash code calculation (no JSON parsing needed)</description>
    ///         </item>
    ///         <item>
    ///             <description>Consistent hash code for equal objects with different JSON formatting</description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Since Id is unique per event, this provides sufficient hash distribution for collections.
    ///     </para>
    /// </remarks>
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, EventType, ContentType);
    }

    /// <summary>
    ///     Compares two byte arrays using JSON-aware semantic comparison.
    ///     Falls back to byte-level comparison if JSON parsing fails.
    /// </summary>
    /// <param name="left">First byte array</param>
    /// <param name="right">Second byte array</param>
    /// <returns>True if arrays are semantically equal (as JSON) or byte-equal</returns>
    private static bool _JsonEquals(byte[] left, byte[] right)
    {
        // Fast path: same reference or both empty
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left.Length == 0 && right.Length == 0)
        {
            return true;
        }

        // Try JSON-aware comparison
        try
        {
            using JsonDocument leftDoc = JsonDocument.Parse(left);
            using JsonDocument rightDoc = JsonDocument.Parse(right);
            return _JsonElementEquals(leftDoc.RootElement, rightDoc.RootElement);
        }
        catch (JsonException)
        {
            // Not valid JSON, fall back to byte comparison
            return left.SequenceEqual(right);
        }
    }

    /// <summary>
    ///     Recursively compares two JSON elements for semantic equality.
    ///     Handles objects (key order independent), arrays (order sensitive), and primitives.
    /// </summary>
    /// <param name="left">First JSON element</param>
    /// <param name="right">Second JSON element</param>
    /// <returns>True if elements are semantically equal</returns>
    private static bool _JsonElementEquals(JsonElement left, JsonElement right)
    {
        // Different value kinds are not equal
        if (left.ValueKind != right.ValueKind)
        {
            return false;
        }

        switch (left.ValueKind)
        {
            case JsonValueKind.Object:
                // Compare objects: key order doesn't matter
                return _JsonObjectEquals(left, right);

            case JsonValueKind.Array:
                // Compare arrays: order matters
                return _JsonArrayEquals(left, right);

            case JsonValueKind.String:
                return left.GetString() == right.GetString();

            case JsonValueKind.Number:
                // Compare as raw text to preserve precision
                return left.GetRawText() == right.GetRawText();

            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                return true; // Same kind means equal

            default:
                return false;
        }
    }

    /// <summary>
    ///     Compares two JSON objects for semantic equality (key order independent).
    /// </summary>
    private static bool _JsonObjectEquals(JsonElement left, JsonElement right)
    {
        List<JsonProperty> leftProperties = left.EnumerateObject().ToList();
        List<JsonProperty> rightProperties = right.EnumerateObject().ToList();

        // Must have same number of properties
        if (leftProperties.Count != rightProperties.Count)
        {
            return false;
        }

        // Build dictionary for efficient lookup (key order independent)
        Dictionary<string, JsonElement> rightDict = rightProperties.ToDictionary(p => p.Name, p => p.Value);

        // Check all properties match
        foreach (JsonProperty leftProp in leftProperties)
        {
            if (!rightDict.TryGetValue(leftProp.Name, out JsonElement rightValue))
            {
                return false; // Property missing in right
            }

            if (!_JsonElementEquals(leftProp.Value, rightValue))
            {
                return false; // Property values differ
            }
        }

        return true;
    }

    /// <summary>
    ///     Compares two JSON arrays for semantic equality (order sensitive).
    /// </summary>
    private static bool _JsonArrayEquals(JsonElement left, JsonElement right)
    {
        List<JsonElement> leftArray = left.EnumerateArray().ToList();
        List<JsonElement> rightArray = right.EnumerateArray().ToList();

        // Must have same length
        if (leftArray.Count != rightArray.Count)
        {
            return false;
        }

        // Compare element by element (order matters)
        for (int i = 0; i < leftArray.Count; i++)
        {
            if (!_JsonElementEquals(leftArray[i], rightArray[i]))
            {
                return false;
            }
        }

        return true;
    }
}
