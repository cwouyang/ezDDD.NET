namespace EzDdd.UseCase.Port.In;

/// <summary>
///     <c>IReactor</c> is an event-driven use case (an in-port) that takes care of specific business
///     rules whenever it receives a message. According to the received message, a reactor applies
///     the business rules and triggers a side effect, such as updating another aggregate or a read model.
///     It is a sibling of <c>INotifier</c> in EzDdd.Cqrs: a reactor applies business rules,
///     whereas a notifier publishes events outward.
/// </summary>
/// <typeparam name="TInput">The type of input message this reactor processes.</typeparam>
public interface IReactor<in TInput> : IUseCase<TInput, DefaultOutput>
    where TInput : IInput;
