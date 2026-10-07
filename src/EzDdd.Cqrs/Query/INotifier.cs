using EzDdd.UseCase.Port.In;

namespace EzDdd.Cqrs.Query;

/// <summary>
///     An <c>INotifier</c> is an event-driven use case (an in-port) and a sibling of
///     <see cref="IReactor{TInput}" />, not a subtype of it. It receives
///     internal domain events, converts them into external domain events (i.e., integration events),
///     and then dispatches them through an out-port to front-ends, downstream bounded contexts,
///     or external systems (such as Kafka), in order to notify others of aggregate state changes.
///     Where a reactor applies business rules, a notifier publishes events outward.
/// </summary>
/// <remarks>
///     When propagating internal domain events outward, the <c>INotifier</c> is responsible
///     for upholding the cross-layer principle of Clean Architecture: objects from the
///     entities layer must not leave the use cases layer and travel outward directly.
/// </remarks>
/// <typeparam name="TInput">The type of input message (typically internal domain event data) this notifier processes.</typeparam>
public interface INotifier<in TInput> : IUseCase<TInput, DefaultOutput>
    where TInput : IInput;
