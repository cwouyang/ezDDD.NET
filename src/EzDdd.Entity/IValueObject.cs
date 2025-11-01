namespace EzDdd.Entity;

/// <summary>
///     Marker interface for value objects.
/// </summary>
/// <remarks>
///     <para>
///         A value object is defined by its attributes, not by a unique identity.
///         Two value objects with identical attribute values are considered equal.
///         Value objects should be immutable.
///     </para>
///     <para>
///         In Domain-Driven Design (DDD), value objects have:
///         <list type="bullet">
///             <item>
///                 <description>No unique identifier</description>
///             </item>
///             <item>
///                 <description>Structural equality (same attributes = same value)</description>
///             </item>
///             <item>
///                 <description>Immutability (cannot change state after creation)</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <strong>Recommendation:</strong> Prefer using C# <see langword="record" /> types for
///         value object implementations, as records provide structural equality and immutability
///         by default through init-only properties.
///     </para>
///     <para>
///         This is a pure marker interface with zero methods, providing maximum flexibility
///         for implementations while clearly marking the semantic intent.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Recommended: Record-based value object (structural equality + immutability)
/// public record Money(decimal Amount, string Currency) : IValueObject;
/// 
/// // Usage:
/// var money1 = new Money(100m, "USD");
/// var money2 = new Money(100m, "USD");
/// Assert.Equal(money1, money2); // true - structural equality
/// 
/// // Alternative: Class-based value object (manual equality)
/// public class Email : IValueObject
/// {
///     public string Value { get; }
/// 
///     public Email(string value)
///     {
///         Value = value;
///     }
/// 
///     public override bool Equals(object? obj) =&gt;
///         obj is Email other &amp;&amp; Value == other.Value;
/// 
///     public override int GetHashCode() =&gt; Value.GetHashCode();
/// }
/// </code>
/// </example>
public interface IValueObject
{
    // Pure marker interface - zero methods
    // Implementations should provide structural equality and immutability
}