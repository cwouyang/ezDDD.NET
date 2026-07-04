namespace EzDdd.UseCase.Port.In;

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
