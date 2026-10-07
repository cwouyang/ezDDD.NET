namespace EzDdd.Cqrs;

/// <summary>
///     <c>IIdempotentIdParser</c> tells an <see cref="IdempotentDecorator{TInput, TOutput}"/> which data
///     a message would touch, so that the decorator can ask whether the message was already applied to it.
/// </summary>
/// <typeparam name="TInput">The type of the message to parse.</typeparam>
public interface IIdempotentIdParser<in TInput>
{
    /// <summary>
    ///     Returns the id of the data that handling the message would touch.
    /// </summary>
    /// <param name="input">The message to parse.</param>
    /// <returns>
    ///     The data id, or <c>null</c> when the message names no data this use case handles. An empty
    ///     string is a valid id.
    /// </returns>
    string? Parse(TInput input);
}
