namespace EzDdd.UseCase.Port.In;

/// <summary>
///     <c>DefaultOutput</c> is the ready-to-use concrete output for use cases that report only
///     an identifier, a message, and an <see cref="ExitCode" />, such as reactors and notifiers.
/// </summary>
/// <remarks>
///     It is not sealed so that an implementation can return a subclass carrying extra data.
///     Fluent setters inherited from <see cref="DefaultOutput{T}" /> return <see cref="DefaultOutput" />.
/// </remarks>
public class DefaultOutput : DefaultOutput<DefaultOutput>;
