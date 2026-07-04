namespace EzDdd.UseCase.Port.In;

/// <summary>
///     <c>ExitCode</c> is an enum for representing the execution status of a use case.
/// </summary>
public enum ExitCode
{
    /// <summary>
    ///     Indicates successful execution.
    /// </summary>
    Success = 0,

    /// <summary>
    ///     Indicates failed execution.
    /// </summary>
    Failure = 1,
}
