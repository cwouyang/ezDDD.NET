using System.Text.Json;
using System.Text.Json.Serialization;

namespace EzDdd.Common;

/// <summary>
///     JSON serialization utilities using System.Text.Json.
///     Provides a preconfigured <see cref="JsonSerializerOptions" /> optimized for domain event serialization
///     and conversion of domain objects.
/// </summary>
/// <remarks>
///     <para>
///         This utility is designed to serialize domain events, entities, and value objects with field-based
///         serialization, similar to Jackson's field visibility configuration in the Java version.
///     </para>
///     <para>
///         <b>Configuration:</b>
///         <list type="bullet">
///             <item>
///                 <description>Fields are included in serialization (IncludeFields = true)</description>
///             </item>
///             <item>
///                 <description>Property names are case-insensitive during deserialization</description>
///             </item>
///             <item>
///                 <description>DateTime values are serialized in ISO-8601 format (not timestamps)</description>
///             </item>
///             <item>
///                 <description>Indentation is disabled for compact output</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <b>Error Handling:</b>
///         All JSON processing exceptions are wrapped in <see cref="InvalidOperationException" /> for consistent error
///         handling,
///         similar to the Java version which wraps exceptions in RuntimeException.
///     </para>
///     <para>
///         <b>Difference from Java Version:</b>
///         The Java version (using Jackson) supports <c>ALLOW_UNQUOTED_FIELD_NAMES: true</c>, which permits
///         non-standard JSON with unquoted field names (e.g., <c>{name:"Alice"}</c>).
///         System.Text.Json does not support this feature and requires standard JSON per RFC 8259
///         (e.g., <c>{"name":"Alice"}</c>).
///     </para>
///     <para>
///         <b>Impact:</b>
///         This difference has minimal practical impact because:
///         <list type="bullet">
///             <item>
///                 <description>
///                     Standard JSON (with quoted field names) works identically in both versions
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     Domain events and DTOs in production systems use standard JSON format
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     Unquoted field names violate the JSON specification (RFC 8259)
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     If non-standard JSON support is required, custom <c>JsonConverter</c> implementations can be added
///                 </description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public static class JsonUtil
{
    /// <summary>
    ///     Gets the preconfigured JSON serializer options used by all methods in this utility.
    /// </summary>
    public static readonly JsonSerializerOptions Options;

    static JsonUtil()
    {
        Options = new JsonSerializerOptions
        {
            // Serialize both properties and fields (equivalent to Jackson's setVisibility(FIELD, ANY))
            IncludeFields = true,

            // Property name comparison is case-insensitive during deserialization
            PropertyNameCaseInsensitive = true,

            // Preserve original property names (no camelCase conversion)
            PropertyNamingPolicy = null,

            // Compact JSON output (no indentation)
            WriteIndented = false,

            // Include all fields, even if null (no ignore conditions)
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,

            // DateTime serialization: ISO-8601 format (default, not timestamps)
            // Equivalent to Jackson's WRITE_DATES_AS_TIMESTAMPS: false
            // System.Text.Json defaults to ISO-8601, so no additional configuration needed
        };
    }

    /// <summary>
    ///     Serializes an object to a JSON string.
    /// </summary>
    /// <param name="value">The object to serialize</param>
    /// <returns>JSON string representation of the object</returns>
    /// <exception cref="InvalidOperationException">Thrown when serialization fails</exception>
    /// <remarks>
    ///     Equivalent to Java's <c>Json.asString(Object value)</c>.
    /// </remarks>
    /// <example>
    ///     <code>
    /// var user = new User { Id = 1, Name = "Alice" };
    /// string json = JsonUtil.AsString(user);
    /// // Result: {"Id":1,"Name":"Alice"}
    /// </code>
    /// </example>
    public static string AsString(object value)
    {
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            return JsonSerializer.Serialize(value, Options);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to serialize object to JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Deserializes a JSON string to an object of the specified type.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to</typeparam>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>The deserialized object, or null if the JSON is null or the type is nullable</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails</exception>
    /// <remarks>
    ///     Equivalent to Java's <c>Json.readValue(String content, Class&lt;T&gt; valueType)</c>.
    /// </remarks>
    /// <example>
    ///     <code>
    /// string json = "{\"Id\":1,\"Name\":\"Alice\"}";
    /// User? user = JsonUtil.ReadValue&lt;User&gt;(json);
    /// </code>
    /// </example>
    public static T? ReadValue<T>(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to deserialize JSON to {typeof(T).Name}: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Deserializes a byte array to an object of the specified type.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to</typeparam>
    /// <param name="bytes">The UTF-8 encoded byte array to deserialize</param>
    /// <returns>The deserialized object, or null if the bytes represent null or the type is nullable</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails</exception>
    /// <remarks>
    ///     Equivalent to Java's <c>Json.readAs(byte[] content, Class&lt;A&gt; clazz)</c>.
    ///     The byte array must be UTF-8 encoded JSON.
    /// </remarks>
    /// <example>
    ///     <code>
    /// byte[] bytes = Encoding.UTF8.GetBytes("{\"Id\":1,\"Name\":\"Alice\"}");
    /// User? user = JsonUtil.ReadAs&lt;User&gt;(bytes);
    /// </code>
    /// </example>
    public static T? ReadAs<T>(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        try
        {
            return JsonSerializer.Deserialize<T>(bytes, Options);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize byte array to {typeof(T).Name}: {ex.Message}",
                ex
            );
        }
    }

    /// <summary>
    ///     Parses a JSON string into a JsonDocument for low-level DOM access.
    /// </summary>
    /// <param name="json">The JSON string to parse</param>
    /// <returns>A JsonDocument representing the parsed JSON structure</returns>
    /// <exception cref="InvalidOperationException">Thrown when parsing fails</exception>
    /// <remarks>
    ///     <para>
    ///         Equivalent to Java's <c>Json.readTree(String content)</c>.
    ///         The returned JsonDocument must be disposed after use to free resources.
    ///     </para>
    ///     <para>
    ///         <b>Important:</b> The caller is responsible for disposing the returned JsonDocument.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// using var doc = JsonUtil.ReadTree("{\"name\":\"Alice\",\"age\":30}");
    /// string name = doc.RootElement.GetProperty("name").GetString();
    /// int age = doc.RootElement.GetProperty("age").GetInt32();
    /// </code>
    /// </example>
    public static JsonDocument ReadTree(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            return JsonDocument.Parse(json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse JSON string: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Parses a byte array into a JsonDocument for low-level DOM access.
    /// </summary>
    /// <param name="bytes">The UTF-8 encoded byte array to parse</param>
    /// <returns>A JsonDocument representing the parsed JSON structure</returns>
    /// <exception cref="InvalidOperationException">Thrown when parsing fails</exception>
    /// <remarks>
    ///     <para>
    ///         Equivalent to Java's <c>Json.readTree(byte[] content)</c>.
    ///         The byte array must be UTF-8 encoded JSON.
    ///         The returned JsonDocument must be disposed after use to free resources.
    ///     </para>
    ///     <para>
    ///         <b>Important:</b> The caller is responsible for disposing the returned JsonDocument.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// byte[] bytes = Encoding.UTF8.GetBytes("{\"name\":\"Alice\"}");
    /// using var doc = JsonUtil.ReadTree(bytes);
    /// string name = doc.RootElement.GetProperty("name").GetString();
    /// </code>
    /// </example>
    public static JsonDocument ReadTree(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        try
        {
            return JsonDocument.Parse(bytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse JSON byte array: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Creates a deep copy of an object by serializing and deserializing it.
    /// </summary>
    /// <typeparam name="T">The type of the object to copy</typeparam>
    /// <param name="value">The object to copy</param>
    /// <returns>A deep copy of the object, or default(T) if the value is null or serialization fails</returns>
    /// <exception cref="InvalidOperationException">Thrown when deep copy fails</exception>
    /// <remarks>
    ///     <para>
    ///         This method is useful for capturing object state in Design by Contract postconditions
    ///         (e.g., uContract.NET's <c>Old&lt;T&gt;()</c> method).
    ///     </para>
    ///     <para>
    ///         <b>Note:</b> This method performs a full serialization-deserialization cycle,
    ///         which may have performance implications for large objects. Use judiciously.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var original = new User { Id = 1, Name = "Alice" };
    /// var copy = JsonUtil.DeepCopy(original);
    /// copy.Name = "Bob";
    /// // original.Name is still "Alice"
    /// </code>
    /// </example>
    public static T? DeepCopy<T>(T value)
    {
        if (value == null)
        {
            return default;
        }

        try
        {
            string json = JsonSerializer.Serialize(value, Options);
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to deep copy object of type {typeof(T).Name}: {ex.Message}",
                ex
            );
        }
    }
}
