using System.Text.Json;

using EzDdd.Entity;

namespace EzDdd.UseCase.Port.InOut;

/// <summary>
///     Static utility class for converting between domain events and <see cref="DomainEventData" />.
/// </summary>
/// <remarks>
///     <para>
///         <b>Design Rationale</b>:
///         <list type="bullet">
///             <item>
///                 <description><b>Static class</b>: Utility class with no instance state</description>
///             </item>
///             <item>
///                 <description><b>Thread-safe</b>: Inherits thread-safety from DomainEventTypeMapper</description>
///             </item>
///             <item>
///                 <description><b>Bidirectional conversion</b>: Domain event ↔ DomainEventData</description>
///             </item>
///             <item>
///                 <description><b>Batch operations</b>: Efficiently converts collections of events</description>
///             </item>
///             <item>
///                 <description><b>System.Text.Json</b>: Uses .NET built-in serialization (no third-party dependencies)</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <b>Usage</b>: Used by repositories and event infrastructure to persist domain events
///         to an event store or outbox table, and to reconstruct domain events from persisted data.
///     </para>
///     <para>
///         <b>Event Type Registration</b>: Event types must be registered with DomainEventTypeMapper
///         at application startup before using this mapper.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Application startup - register event types
/// DomainEventTypeMapper.Register&lt;AccountCreated&gt;("AccountCreated");
/// DomainEventTypeMapper.Register&lt;MoneyDeposited&gt;("MoneyDeposited");
/// 
/// // Convert event to data (for persistence)
/// var @event = new AccountCreated(...);
/// var data = DomainEventMapper.ToData(@event);
/// 
/// // Convert data back to event (from persistence)
/// var reconstructedEvent = DomainEventMapper.ToDomain&lt;AccountCreated&gt;(data);
/// </code>
/// </example>
public static class DomainEventMapper
{
    /// <summary>
    ///     Converts a domain event to <see cref="DomainEventData" /> for persistence.
    /// </summary>
    /// <param name="event">The domain event to convert</param>
    /// <returns>DomainEventData ready for persistence</returns>
    /// <exception cref="InvalidOperationException">Thrown when serialization fails or event type not registered</exception>
    public static DomainEventData ToData(IInternalDomainEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        string eventType = DomainEventTypeMapper.GetTypeName(@event);
        byte[] eventBody = _SerializeToJson(@event);
        byte[] metadata = _SerializeToJson(@event.Metadata);

        return new DomainEventData
        (
            @event.Id,
            eventType,
            "application/json",
            eventBody,
            metadata
        );
    }

    /// <summary>
    ///     Converts <see cref="DomainEventData" /> back to a domain event.
    /// </summary>
    /// <typeparam name="T">The domain event type</typeparam>
    /// <param name="data">The persisted event data</param>
    /// <returns>The reconstructed domain event</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails or event type is not registered</exception>
    public static T ToDomain<T>(DomainEventData data) where T : IInternalDomainEvent
    {
        ArgumentNullException.ThrowIfNull(data);

        Type eventClass = DomainEventTypeMapper.GetType(data.EventType);
        object @event = _DeserializeFromJson(data.EventBody, eventClass);

        if (@event is not T typedEvent)
        {
            throw new InvalidOperationException
            (
                $"Failed to cast event of type {eventClass.Name} to {typeof(T).Name}"
            );
        }

        return typedEvent;
    }

    /// <summary>
    ///     Batch conversion of domain events to <see cref="DomainEventData" />.
    /// </summary>
    /// <param name="events">Collection of domain events</param>
    /// <returns>List of DomainEventData</returns>
    public static IReadOnlyList<DomainEventData> ToData(IEnumerable<IInternalDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        return events.Select(ToData).ToList();
    }

    /// <summary>
    ///     Batch conversion of <see cref="DomainEventData" /> to domain events.
    /// </summary>
    /// <typeparam name="T">The domain event type</typeparam>
    /// <param name="datas">Collection of event data</param>
    /// <returns>List of domain events</returns>
    public static IReadOnlyList<T> ToDomain<T>(IEnumerable<DomainEventData> datas)
        where T : IInternalDomainEvent
    {
        ArgumentNullException.ThrowIfNull(datas);

        return datas.Select(ToDomain<T>).ToList();
    }

    /// <summary>
    ///     Serializes an object to JSON byte array.
    /// </summary>
    /// <param name="obj">The object to serialize</param>
    /// <returns>UTF-8 JSON byte array</returns>
    /// <exception cref="InvalidOperationException">Thrown when serialization fails</exception>
    private static byte[] _SerializeToJson(object obj)
    {
        try
        {
            return JsonSerializer.SerializeToUtf8Bytes(obj);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Serialization failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Deserializes a JSON byte array to an object.
    /// </summary>
    /// <param name="json">UTF-8 JSON byte array</param>
    /// <param name="type">The target type</param>
    /// <returns>Deserialized object</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails</exception>
    private static object _DeserializeFromJson(byte[] json, Type type)
    {
        try
        {
            object? result = JsonSerializer.Deserialize(json, type);
            return result ?? throw new InvalidOperationException($"Deserialization returned null for type {type.Name}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Deserialization failed: {ex.Message}", ex);
        }
    }
}