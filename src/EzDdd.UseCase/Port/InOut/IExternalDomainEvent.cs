using EzDdd.Entity;

namespace EzDdd.UseCase.Port.InOut;

/// <summary>
///     Marker interface for external domain events that are published across bounded contexts.
///     External events represent state changes that are relevant to other bounded contexts
///     and are typically published to an external message bus or event stream.
///     <para>
///         Event Hierarchy:
///         <list type="bullet">
///             <item>
///                 <description>IDomainEvent (base interface)</description>
///             </item>
///             <item>
///                 <description>InternalDomainEvent (within bounded context)</description>
///             </item>
///             <item>
///                 <description>ExternalDomainEvent (cross bounded context)</description>
///             </item>
///         </list>
///     </para>
/// </summary>
/// <remarks>
///     <para><b>Usage</b>:</para>
///     <code>
/// // Internal event (aggregate lifecycle)
/// public record AccountCreated(...) : InternalDomainEvent, IConstructionEvent;
/// 
/// // External event (cross-context integration)
/// public record CustomerRegistered(...) : ExternalDomainEvent;
/// </code>
///     <para>
///         <b>Clean Architecture</b>: ExternalDomainEvent belongs to the Use Case layer,
///         not the Entity layer, because it represents integration concerns across bounded contexts.
///     </para>
/// </remarks>
public interface IExternalDomainEvent : IDomainEvent
{
    // Marker interface - no additional methods
    // Inherits: Id, OccurredOn, Source, Metadata from IDomainEvent
}