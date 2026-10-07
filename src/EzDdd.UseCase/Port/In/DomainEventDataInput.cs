using EzDdd.UseCase.Port.InOut;

namespace EzDdd.UseCase.Port.In;

/// <summary>
///     <c>DomainEventDataInput</c> is an <see cref="IInput" /> that carries a <see cref="DomainEventData" />,
///     the input of event-driven use cases such as <see cref="IReactor{TInput}" />.
/// </summary>
/// <remarks>
///     It is not sealed so that an input can derive from it to carry additional data besides the event.
/// </remarks>
public record DomainEventDataInput : IInput
{
    /// <summary>
    ///     Gets the domain event data this input carries.
    /// </summary>
    public required DomainEventData Event { get; init; }
}
