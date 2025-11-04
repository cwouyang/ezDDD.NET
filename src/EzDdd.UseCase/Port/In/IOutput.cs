namespace EzDdd.UseCase.Port.In;

/// <summary>
///     <c>IOutput</c> is an interface for representing the output after executing the use case.
/// </summary>
public interface IOutput
{
    /// <summary>
    ///     Gets the message associated with this output.
    /// </summary>
    string Message { get; }

    /// <summary>
    ///     Gets the exit code indicating the execution status.
    /// </summary>
    ExitCode ExitCode { get; }

    /// <summary>
    ///     Gets the identifier associated with this output.
    /// </summary>
    string Id { get; }

    /// <summary>
    ///     Sets the message for this output.
    /// </summary>
    /// <param name="message">The message to set.</param>
    /// <returns>This output instance for fluent API.</returns>
    IOutput SetMessage(string message);

    /// <summary>
    ///     Sets the exit code for this output.
    /// </summary>
    /// <param name="exitCode">The exit code to set.</param>
    /// <returns>This output instance for fluent API.</returns>
    IOutput SetExitCode(ExitCode exitCode);

    /// <summary>
    ///     Sets the exit code to <see cref="ExitCode.Failure" />.
    /// </summary>
    /// <returns>This output instance for fluent API.</returns>
    IOutput Fail();

    /// <summary>
    ///     Sets the exit code to <see cref="ExitCode.Success" />.
    /// </summary>
    /// <returns>This output instance for fluent API.</returns>
    IOutput Succeed();

    /// <summary>
    ///     Sets the identifier for this output.
    /// </summary>
    /// <param name="id">The identifier to set.</param>
    /// <returns>This output instance for fluent API.</returns>
    IOutput SetId(string id);
}