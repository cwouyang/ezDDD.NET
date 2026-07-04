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

/// <summary>
///     Extension methods for <see cref="ExitCode" />.
/// </summary>
public static class ExitCodeExtensions
{
    /// <summary>
    ///     Gets the integer code value of the exit code.
    /// </summary>
    /// <param name="exitCode">The exit code.</param>
    /// <returns>The integer code value.</returns>
    public static int Code(this ExitCode exitCode)
    {
        return (int)exitCode;
    }
}
