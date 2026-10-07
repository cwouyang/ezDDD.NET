using System.Diagnostics.CodeAnalysis;

namespace EzDdd.UseCase.Port.In;

/// <summary>
///     <c>DefaultOutput</c> is the default implementation of <see cref="IOutput" /> used by use cases,
///     commands, and queries.
///     <para>
///         This class provides a type-safe fluent API using self-referential generics,
///         allowing subclasses to maintain their concrete type when chaining methods.
///     </para>
/// </summary>
/// <typeparam name="T">
///     The concrete output type that extends DefaultOutput.
///     Must be the same type as the subclass (self-referential constraint).
/// </typeparam>
/// <remarks>
///     <para>
///         <b>Design Pattern</b>: Self-referential generic with fluent builder API
///     </para>
///     <para>
///         <b>Key Features</b>:
///         <list type="bullet">
///             <item>Type-safe method chaining that preserves concrete type</item>
///             <item>Static factory method for creating instances</item>
///             <item>Fluent setter methods returning concrete type T</item>
///             <item>Explicit IOutput implementation for interface compatibility</item>
///         </list>
///     </para>
///     <para>
///         <b>Example</b>:
///         <code>
///         public class CreateAccountOutput : DefaultOutput&lt;CreateAccountOutput&gt;
///         {
///             public string AccountNumber { get; set; } = string.Empty;
///
///             public CreateAccountOutput SetAccountNumber(string accountNumber)
///             {
///                 AccountNumber = accountNumber;
///                 return this;
///             }
///         }
///
///         // Usage with fluent API:
///         var output = CreateAccountOutput.Create()
///             .SetId("ACC-001")
///             .SetAccountNumber("1234567890")
///             .SetMessage("Account created successfully")
///             .Succeed();
///         </code>
///     </para>
///     <para>
///         <b>Type Safety</b>: The self-referential constraint ensures that fluent methods
///         always return the concrete subclass type, not the base DefaultOutput type.
///     </para>
///     <para>
///         <b>Extensibility</b>:
///     </para>
///     <list type="bullet">
///         <item>Subclass to add domain-specific fluent methods (e.g., <c>SetOrderTotal()</c>, <c>SetCustomerName()</c>)</item>
///         <item>Self-referential generic <c>T</c> parameter preserves concrete type in fluent method chains</item>
///         <item>
///             Fluent methods cast <c>this</c> to <c>T</c>, so <c>T</c> must be the subclass itself
///         </item>
///         <item>Use <c>new()</c> constraint to ensure parameterless constructor exists for <c>Create()</c> factory method</item>
///         <item>Compatible with both <c>ICommand</c> and <c>IQuery</c> output types</item>
///         <item>Can add validation logic in fluent methods before setting properties</item>
///     </list>
///     <para>
///         See ADR-0017 (CqrsOutput Implementation Strategy) for the self-referential generic pattern and its
///         rationale, and ADR-0031 (Alignment with Java ezddd 9.0.1), which supersedes it, for the rename to
///         <c>DefaultOutput</c> and the move to EzDdd.UseCase.
///     </para>
/// </remarks>
public class DefaultOutput<T> : IOutput
    where T : DefaultOutput<T>, new()
{
    /// <summary>
    ///     Gets or sets the identifier associated with this output.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the message associated with this output.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the exit code indicating the execution status.
    /// </summary>
    public ExitCode ExitCode { get; set; } = ExitCode.Success;

    // Explicit IOutput interface implementations
    // These allow DefaultOutput to be used wherever IOutput is expected,
    // while the public methods return the concrete type T

    /// <inheritdoc />
    IOutput IOutput.SetMessage(string message)
    {
        return SetMessage(message);
    }

    /// <inheritdoc />
    IOutput IOutput.SetExitCode(ExitCode exitCode)
    {
        return SetExitCode(exitCode);
    }

    /// <inheritdoc />
    IOutput IOutput.Fail()
    {
        return Fail();
    }

    /// <inheritdoc />
    IOutput IOutput.Succeed()
    {
        return Succeed();
    }

    /// <inheritdoc />
    IOutput IOutput.Ignore()
    {
        return Ignore();
    }

    /// <inheritdoc />
    IOutput IOutput.Reject()
    {
        return Reject();
    }

    /// <inheritdoc />
    IOutput IOutput.SetId(string id)
    {
        return SetId(id);
    }

    /// <summary>
    ///     Creates a new instance of the concrete output type.
    /// </summary>
    /// <returns>A new instance of type T.</returns>
    /// <remarks>
    ///     This static factory method requires that T has a parameterless constructor.
    ///     The <c>new()</c> constraint at the class level enables instantiation.
    /// </remarks>
    [SuppressMessage(
        "Design",
        "CA1000:Do not declare static members on generic types",
        Justification = "DefaultOutput<T>.Create() is the designed fluent entry point of the self-referential generic builder, matching Java ezddd's DefaultOutput.create() API; the type argument is always inferred from the concrete subclass."
    )]
    public static T Create()
    {
        return new T();
    }

    /// <summary>
    ///     Sets the identifier for this output.
    /// </summary>
    /// <param name="id">The identifier to set.</param>
    /// <returns>This output instance as type T for fluent API.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="id" /> is null.
    /// </exception>
    public T SetId(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
        return _Self();
    }

    /// <summary>
    ///     Sets the message for this output.
    /// </summary>
    /// <param name="message">The message to set.</param>
    /// <returns>This output instance as type T for fluent API.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="message" /> is null.
    /// </exception>
    public T SetMessage(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Message = message;
        return _Self();
    }

    /// <summary>
    ///     Sets the exit code for this output.
    /// </summary>
    /// <param name="exitCode">The exit code to set.</param>
    /// <returns>This output instance as type T for fluent API.</returns>
    public T SetExitCode(ExitCode exitCode)
    {
        ExitCode = exitCode;
        return _Self();
    }

    /// <summary>
    ///     Sets the exit code to <see cref="ExitCode.Failure" />.
    /// </summary>
    /// <returns>This output instance as type T for fluent API.</returns>
    public T Fail()
    {
        ExitCode = ExitCode.Failure;
        return _Self();
    }

    /// <summary>
    ///     Sets the exit code to <see cref="ExitCode.Success" />.
    /// </summary>
    /// <returns>This output instance as type T for fluent API.</returns>
    public T Succeed()
    {
        ExitCode = ExitCode.Success;
        return _Self();
    }

    /// <summary>
    ///     Sets the exit code to <see cref="ExitCode.Ignore" />.
    /// </summary>
    /// <returns>This output instance as type T for fluent API.</returns>
    public T Ignore()
    {
        ExitCode = ExitCode.Ignore;
        return _Self();
    }

    /// <summary>
    ///     Sets the exit code to <see cref="ExitCode.Reject" />.
    /// </summary>
    /// <returns>This output instance as type T for fluent API.</returns>
    public T Reject()
    {
        ExitCode = ExitCode.Reject;
        return _Self();
    }

    /// <summary>
    ///     Casts this instance to the concrete type T for type-safe method chaining.
    /// </summary>
    /// <returns>This instance as type T.</returns>
    /// <remarks>
    ///     This private method enables the fluent API to return the concrete type
    ///     while maintaining type safety through the self-referential constraint.
    /// </remarks>
    private T _Self()
    {
        return (T)this;
    }
}
