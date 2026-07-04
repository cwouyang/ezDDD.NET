namespace EzDdd.Entity;

/// <summary>
///     Marker interface for entities with unique identity.
/// </summary>
/// <remarks>
///     <para>
///         An entity is defined by its unique identifier, not by its attributes.
///         Two entities with the same ID are considered the same entity,
///         regardless of attribute differences.
///     </para>
///     <para>
///         In Domain-Driven Design (DDD), entities have:
///         <list type="bullet">
///             <item>
///                 <description>A unique identifier that distinguishes them from other entities</description>
///             </item>
///             <item>
///                 <description>A lifecycle (they can change state over time)</description>
///             </item>
///             <item>
///                 <description>Identity-based equality (same ID = same entity)</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         This interface uses covariant type parameter (<c>out TId</c>) to enable
///         type variance for read-only scenarios. For example, <c>IEntity&lt;Guid&gt;</c>
///         can be assigned to <c>IEntity&lt;object&gt;</c>.
///     </para>
/// </remarks>
/// <typeparam name="TId">The type of the entity's unique identifier</typeparam>
/// <example>
///     <code>
/// public class Order : IEntity&lt;Guid&gt;
/// {
///     public Guid Id { get; private set; }
///     public string CustomerName { get; private set; }
///     public decimal TotalAmount { get; private set; }
///
///     // Entity equality based on ID
///     public override bool Equals(object? obj) =&gt;
///         obj is Order other &amp;&amp; Id == other.Id;
///
///     public override int GetHashCode() =&gt; Id.GetHashCode();
/// }
/// </code>
/// </example>
public interface IEntity<out TId>
{
    /// <summary>
    ///     Gets the unique identifier of this entity.
    /// </summary>
    /// <value>
    ///     The unique identifier that distinguishes this entity from all other entities
    ///     of the same type.
    /// </value>
    TId Id { get; }
}
