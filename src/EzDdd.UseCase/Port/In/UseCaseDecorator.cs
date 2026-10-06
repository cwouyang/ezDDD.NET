namespace EzDdd.UseCase.Port.In;

/// <summary>
///     <c>UseCaseDecorator</c> is the base class for decorators of an in-port use case. A decorator
///     implements the same in-port as the use case it wraps and keeps that use case, so a concern that
///     cuts across many use cases (such as an idempotency check, a transaction, or logging) can be added
///     without touching them. Each decorator handles one concern, and it can act before, after, or
///     instead of delegating to the decorated use case. Decorators can be stacked. A subclass must either
///     delegate to the decorated use case or return an output that explains why it did not (for example
///     with an <c>Ignore</c> or <c>Reject</c> exit code), and it must not change the meaning of the
///     decorated use case.
/// </summary>
/// <typeparam name="TInput">The type parameter for representing a use case input.</typeparam>
/// <typeparam name="TOutput">The type parameter for representing a use case output.</typeparam>
public abstract class UseCaseDecorator<TInput, TOutput> : IUseCase<TInput, TOutput>
    where TInput : IInput
    where TOutput : IOutput
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UseCaseDecorator{TInput, TOutput}"/> class.
    /// </summary>
    /// <param name="useCase">The use case to decorate.</param>
    protected UseCaseDecorator(IUseCase<TInput, TOutput> useCase)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        DecoratedUseCase = useCase;
    }

    /// <summary>
    ///     Gets the decorated use case.
    /// </summary>
    protected IUseCase<TInput, TOutput> DecoratedUseCase { get; }

    /// <inheritdoc />
    public abstract Task<TOutput> ExecuteAsync(TInput input);
}
