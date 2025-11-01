using EzDdd.Common;

using uContract;

namespace EzDdd.Entity;

/// <summary>
///     Static utility for mapping domain event types to string names and vice versa.
/// </summary>
/// <remarks>
///     <para>
///         This mapper is used for event serialization and deserialization when storing events
///         in an event store or database. Instead of storing fully-qualified type names (which
///         can change during refactoring), you register a stable string identifier for each
///         event type.
///     </para>
///     <para>
///         <strong>Thread Safety:</strong> This class is thread-safe. All operations use the
///         underlying <see cref="BiMap{TKey,TValue}" /> which provides lock-based synchronization.
///     </para>
///     <para>
///         <strong>Usage Pattern:</strong>
///         <list type="number">
///             <item>
///                 <description>Register event types at application startup</description>
///             </item>
///             <item>
///                 <description>Use <see cref="GetTypeName(Type)" /> when serializing events</description>
///             </item>
///             <item>
///                 <description>Use <see cref="GetType" /> when deserializing events</description>
///             </item>
///         </list>
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Application startup - register all event types
/// DomainEventTypeMapper.Register&lt;OrderCreated&gt;("OrderCreated");
/// DomainEventTypeMapper.Register&lt;OrderItemAdded&gt;("OrderItemAdded");
/// DomainEventTypeMapper.Register&lt;OrderCancelled&gt;("OrderCancelled");
/// 
/// // Serialization - get type name from event
/// var typeName = DomainEventTypeMapper.GetTypeName(orderCreatedEvent);
/// // Returns: "OrderCreated"
/// 
/// // Deserialization - get type from stored name
/// var eventType = DomainEventTypeMapper.GetType("OrderCreated");
/// // Returns: typeof(OrderCreated)
/// </code>
/// </example>
public static class DomainEventTypeMapper
{
    private static readonly BiMap<string, Type> Mapper = new();

    /// <summary>
    ///     Registers a domain event type with its string identifier.
    /// </summary>
    /// <typeparam name="TEvent">The domain event type to register</typeparam>
    /// <param name="typeName">The string identifier for this event type</param>
    /// <remarks>
    ///     <para>
    ///         <strong>Convention:</strong> Use the event class name as the type name for simplicity
    ///         (e.g., <c>"OrderCreated"</c> for <c>OrderCreated</c> event). This makes the mapping
    ///         intuitive while avoiding fully-qualified names that can break during refactoring.
    ///     </para>
    ///     <para>
    ///         <strong>When to Register:</strong> Register all event types at application startup,
    ///         typically in a startup/configuration class or module initializer.
    ///     </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="typeName" /> is null or empty
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>The <paramref name="typeName" /> is already registered to a different type</description>
    ///         </item>
    ///         <item>
    ///             <description>The type <typeparamref name="TEvent" /> is already registered with a different name</description>
    ///         </item>
    ///     </list>
    /// </exception>
    /// <example>
    ///     <code>
    /// // Register at startup
    /// DomainEventTypeMapper.Register&lt;OrderCreated&gt;("OrderCreated");
    /// DomainEventTypeMapper.Register&lt;MoneyDeposited&gt;("MoneyDeposited");
    /// </code>
    /// </example>
    public static void Register<TEvent>(string typeName) where TEvent : IInternalDomainEvent
    {
        Contract.Require("Type name cannot be null or empty", () => !string.IsNullOrWhiteSpace(typeName));

        Type eventType = typeof(TEvent);

        // Check if type name is already registered to a different type
        if (Mapper.TryGetValue(typeName, out Type? existingType))
        {
            if (existingType != eventType)
            {
                throw new ArgumentException
                (
                    $"Type name '{typeName}' is already registered to type '{existingType.Name}'. " +
                    $"Cannot register it to '{eventType.Name}'."
                );
            }

            // Same mapping already exists - idempotent operation, just return
            return;
        }

        // Check if type is already registered with a different name
        string? existingName = Mapper.GetKey(eventType);
        if (existingName != null)
        {
            if (existingName != typeName)
            {
                throw new ArgumentException
                (
                    $"Type '{eventType.Name}' is already registered with name '{existingName}'. " +
                    $"Cannot register it with name '{typeName}'."
                );
            }

            // Same mapping already exists - idempotent operation, just return
            return;
        }

        // Both type name and type are new - safe to add
        Mapper.Add(typeName, eventType);
    }

    /// <summary>
    ///     Gets the string identifier for a domain event type.
    /// </summary>
    /// <param name="eventType">The event type</param>
    /// <returns>The registered string identifier</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="eventType" /> is null
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the event type is not registered
    /// </exception>
    /// <example>
    ///     <code>
    /// var typeName = DomainEventTypeMapper.GetTypeName(typeof(OrderCreated));
    /// // Returns: "OrderCreated"
    /// </code>
    /// </example>
    public static string GetTypeName(Type eventType)
    {
        Contract.Require("Event type cannot be null", () => eventType != null);

        string? typeName = Mapper.GetKey(eventType);
        if (typeName == null)
        {
            throw new InvalidOperationException
            (
                $"Event type '{eventType.Name}' is not registered. " +
                $"Register it using DomainEventTypeMapper.Register<{eventType.Name}>(\"TypeName\") at application startup."
            );
        }

        return typeName;
    }

    /// <summary>
    ///     Gets the string identifier for a domain event instance.
    /// </summary>
    /// <param name="event">The domain event instance</param>
    /// <returns>The registered string identifier</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="event" /> is null
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the event type is not registered
    /// </exception>
    /// <example>
    ///     <code>
    /// var orderCreated = new OrderCreated(/* ... */);
    /// var typeName = DomainEventTypeMapper.GetTypeName(orderCreated);
    /// // Returns: "OrderCreated"
    /// </code>
    /// </example>
    public static string GetTypeName(IInternalDomainEvent @event)
    {
        Contract.Require("Event cannot be null", () => @event != null);

        return GetTypeName(@event.GetType());
    }

    /// <summary>
    ///     Gets the domain event type for a string identifier.
    /// </summary>
    /// <param name="typeName">The string identifier</param>
    /// <returns>The registered event type</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="typeName" /> is null or empty
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the type name is not registered
    /// </exception>
    /// <example>
    ///     <code>
    /// var eventType = DomainEventTypeMapper.GetType("OrderCreated");
    /// // Returns: typeof(OrderCreated)
    /// 
    /// // Use with reflection to deserialize
    /// var eventInstance = JsonSerializer.Deserialize(json, eventType);
    /// </code>
    /// </example>
    public static Type GetType(string typeName)
    {
        Contract.Require("Type name cannot be null or empty", () => !string.IsNullOrWhiteSpace(typeName));

        if (!Mapper.TryGetValue(typeName, out Type? type))
        {
            throw new InvalidOperationException
            (
                $"Event type name '{typeName}' is not registered. " +
                $"Available types: {string.Join(", ", Mapper.Keys)}"
            );
        }

        return type;
    }

    /// <summary>
    ///     Checks if a type name is registered.
    /// </summary>
    /// <param name="typeName">The string identifier to check</param>
    /// <returns><c>true</c> if the type name is registered; otherwise, <c>false</c></returns>
    /// <remarks>
    ///     Use this method to check if an event type is registered before attempting
    ///     to retrieve it, to avoid exceptions.
    /// </remarks>
    /// <example>
    ///     <code>
    /// if (DomainEventTypeMapper.Contains("OrderCreated"))
    /// {
    ///     var type = DomainEventTypeMapper.GetType("OrderCreated");
    ///     // Use type...
    /// }
    /// </code>
    /// </example>
    public static bool Contains(string typeName)
    {
        return !string.IsNullOrWhiteSpace(typeName) && Mapper.ContainsKey(typeName);
    }

    /// <summary>
    ///     Gets all registered mappings.
    /// </summary>
    /// <returns>
    ///     A read-only dictionary containing all type name to type mappings
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is useful for:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>Diagnostics and logging (verify all types are registered)</description>
    ///             </item>
    ///             <item>
    ///                 <description>Testing (verify registration completeness)</description>
    ///             </item>
    ///             <item>
    ///                 <description>Documentation generation (list all event types)</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The returned dictionary is a snapshot - modifications to the mapper after
    ///         this call will not affect the returned dictionary.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var mappings = DomainEventTypeMapper.GetAllMappings();
    /// foreach (var (typeName, eventType) in mappings)
    /// {
    ///     Console.WriteLine($"{typeName} -> {eventType.Name}");
    /// }
    /// </code>
    /// </example>
    public static IReadOnlyDictionary<string, Type> GetAllMappings()
    {
        // BiMap implements IDictionary, so we can enumerate it
        // Create a defensive copy as a read-only dictionary
        return new Dictionary<string, Type>(Mapper);
    }

    /// <summary>
    ///     Clears all registered mappings.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <strong>Use Case:</strong> Primarily for testing to ensure test isolation.
    ///         In production, registrations are typically made once at startup and never cleared.
    ///     </para>
    ///     <para>
    ///         <strong>Thread Safety:</strong> This operation is thread-safe but may cause
    ///         concurrent operations to fail if they expect previously registered types.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // In test setup
    /// [Fact]
    /// public void MyTest()
    /// {
    ///     DomainEventTypeMapper.Clear(); // Ensure clean state
    ///     DomainEventTypeMapper.Register&lt;TestEvent&gt;("TestEvent");
    ///     // ... test code ...
    /// }
    /// </code>
    /// </example>
    public static void Clear()
    {
        Mapper.Clear();
    }
}