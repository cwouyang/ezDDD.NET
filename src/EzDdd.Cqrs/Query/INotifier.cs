using EzDdd.UseCase.Port.In;

namespace EzDdd.Cqrs.Query;

/// <summary>
///     An <c>INotifier</c> is a kind of <see cref="IReactor{TInput}" /> (an in-port) that receives
///     internal domain events, converts them into external domain events (i.e., integration events),
///     and then dispatches them through an out-port to front-ends, downstream bounded contexts,
///     or external systems (such as Kafka), in order to notify others of aggregate state changes.
/// </summary>
/// <remarks>
///     <para>
///         When propagating internal domain events outward, the <c>INotifier</c> is responsible
///         for upholding the cross-layer principle of Clean Architecture: objects from the
///         entities layer must not leave the use cases layer and travel outward directly.
///     </para>
///     <para>
///         The event handling contract is inherited from <see cref="IReactor{TInput}" />:
///         <c>ExecuteAsync(TInput)</c>. Like all reactors, notifiers should handle messages
///         idempotently.
///     </para>
/// </remarks>
/// <typeparam name="TInput">The type of input message (typically internal domain event data) this notifier processes.</typeparam>
public interface INotifier<in TInput> : IReactor<TInput>
{
    // Inherits Task ExecuteAsync(TInput input) from IReactor<TInput>
}
