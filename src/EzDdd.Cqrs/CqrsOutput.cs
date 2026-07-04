using EzDdd.UseCase.Port.In;

namespace EzDdd.Cqrs;

/// <summary>
///     <c>CqrsOutput</c> is a base class for CQRS command and query outputs.
///     <para>
///         This class provides a type-safe fluent API using self-referential generics,
///         allowing subclasses to maintain their concrete type when chaining methods.
///     </para>
/// </summary>
/// <typeparam name="T">
///     The concrete output type that extends CqrsOutput.
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
///         public class CreateAccountOutput : CqrsOutput&lt;CreateAccountOutput&gt;
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
///         always return the concrete subclass type, not the base CqrsOutput type.
///     </para>
///     <para>
///         <b>Extensibility</b>:
///     </para>
///     <list type="bullet">
///         <item>Subclass to add domain-specific fluent methods (e.g., <c>SetOrderTotal()</c>, <c>SetCustomerName()</c>)</item>
///         <item>Self-referential generic <c>TSelf</c> parameter preserves concrete type in fluent method chains</item>
///         <item>
///             Override <c>Self()</c> protected method to return concrete subclass instance (required for proper type
///             preservation)
///         </item>
///         <item>Use <c>new()</c> constraint to ensure parameterless constructor exists for <c>Create()</c> factory method</item>
///         <item>Compatible with both <c>ICommand</c> and <c>IQuery</c> output types</item>
///         <item>Can add validation logic in fluent methods before setting properties</item>
///     </list>
///     <para>
///         See ADR-0017 (CqrsOutput Implementation Strategy) for detailed explanation of the
///         self-referential generic pattern and design rationale.
///     </para>
/// </remarks>
public class CqrsOutput<T> : IOutput
    where T : CqrsOutput<T>, new()
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
    // These allow CqrsOutput to be used wherever IOutput is expected,
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
