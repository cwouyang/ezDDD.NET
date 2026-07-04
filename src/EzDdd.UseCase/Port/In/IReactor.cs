namespace EzDdd.UseCase.Port.In;

/// <summary>
///     <c>IReactor</c> takes care of specific business rules whenever it receives a message.
///     According to the received message, a reactor triggers a side effect such as
///     notifying frontend clients, or notifying another bounded context.
///     In addition, <c>IReactor</c> ensures idempotent handling of messages.
/// </summary>
/// <typeparam name="TInput">The type of input message this reactor processes.</typeparam>
public interface IReactor<in TInput>
{
    /// <summary>
    ///     Executes the reactor logic asynchronously with the given input message.
    /// </summary>
    /// <param name="input">The input message to process.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(TInput input);
}
