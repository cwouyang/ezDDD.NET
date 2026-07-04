namespace EzDdd.UseCase.Port.In;

/// <summary>
///     <c>IInput</c> is a marker interface for representing the input of a use case execution.
/// </summary>
public interface IInput
{
    /// <summary>
    ///     Creates a null object instance of <see cref="IInput" />.
    /// </summary>
    /// <returns>A <see cref="NullInput" /> instance.</returns>
    static NullInput OfNull()
    {
        return new NullInput();
    }

    /// <summary>
    ///     Null object implementation of <see cref="IInput" />.
    /// </summary>
    sealed record NullInput : IInput;
}
