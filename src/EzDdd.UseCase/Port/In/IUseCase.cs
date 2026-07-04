namespace EzDdd.UseCase.Port.In;

/// <summary>
///     <c>IUseCase</c> is an interface for representing a use case in Clean Architecture.
/// </summary>
/// <typeparam name="TInput">The type parameter for representing a use case input.</typeparam>
/// <typeparam name="TOutput">The type parameter for representing a use case output.</typeparam>
public interface IUseCase<in TInput, TOutput>
    where TInput : IInput
    where TOutput : IOutput
{
    /// <summary>
    ///     Executes the use case with the given input.
    /// </summary>
    /// <param name="input">The input for the use case.</param>
    /// <returns>A task representing the asynchronous operation, containing the output.</returns>
    /// <exception cref="Exceptions.UseCaseFailureException">
    ///     Thrown when the use case cannot fulfill its specifications.
    /// </exception>
    Task<TOutput> ExecuteAsync(TInput input);
}
