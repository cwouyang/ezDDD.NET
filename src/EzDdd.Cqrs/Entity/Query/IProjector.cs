namespace EzDdd.Cqrs.Entity.Query;

/// <summary>
/// Builds or updates a read model from an input. A projector is a synchronous pure function and performs no I/O.
/// </summary>
/// <typeparam name="TInput">The type of the input, such as a domain event.</typeparam>
/// <typeparam name="TOutput">The type of the result, such as a read model.</typeparam>
public interface IProjector<in TInput, out TOutput>
{
    /// <summary>Projects the input.</summary>
    /// <param name="input">The input to project.</param>
    /// <returns>The projected result.</returns>
    TOutput Project(TInput input);
}
