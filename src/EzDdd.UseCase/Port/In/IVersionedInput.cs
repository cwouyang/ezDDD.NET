namespace EzDdd.UseCase.Port.In;

/// <summary>
///     <c>IVersionedInput</c> is an interface for representing the input with aggregate root version
///     of use case execution. This is used for optimistic locking scenarios.
/// </summary>
public interface IVersionedInput : IInput
{
    /// <summary>
    ///     Gets or sets the aggregate root version.
    /// </summary>
    long Version { get; set; }
}