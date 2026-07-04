namespace EzDdd.UseCase.Exceptions;

/// <summary>
///     Exception thrown when repository save operation fails.
/// </summary>
/// <remarks>
///     <para>
///         This exception is thrown at the domain layer (use case layer) when aggregate
///         persistence fails. Common causes include:
///     </para>
///     <list type="bullet">
///         <item>
///             <term>Optimistic Locking Failure</term>
///             <description>
///                 Concurrent modification detected. The aggregate was modified by another
///                 transaction since it was loaded. Use <see cref="OptimisticLockingFailure" />
///                 constant for this case.
///             </description>
///         </item>
///         <item>
///             <term>Database Constraint Violation</term>
///             <description>
///                 Unique constraint, foreign key constraint, or check constraint violated.
///             </description>
///         </item>
///         <item>
///             <term>Database Connection Error</term>
///             <description>
///                 Connection lost or transaction timeout.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
/// try
/// {
///     await _repository.SaveAsync(aggregate);
/// }
/// catch (RepositorySaveException ex) when (ex.Message == RepositorySaveException.OptimisticLockingFailure)
/// {
///     // Handle concurrent modification conflict
///     return new UseCaseOutput { ExitCode = ExitCode.ConflictFailure };
/// }
/// </code>
/// </example>
public class RepositorySaveException : Exception
{
    /// <summary>
    ///     Standard message for optimistic locking failures.
    /// </summary>
    /// <remarks>
    ///     Use this constant when throwing or catching optimistic locking conflicts:
    ///     <code>
    /// throw new RepositorySaveException(RepositorySaveException.OptimisticLockingFailure);
    /// </code>
    /// </remarks>
    public const string OptimisticLockingFailure = "Optimistic locking failure";

    /// <summary>
    ///     Initializes a new instance of the <see cref="RepositorySaveException" /> class.
    /// </summary>
    public RepositorySaveException() { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RepositorySaveException" /> class
    ///     with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public RepositorySaveException(string message)
        : base(message) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RepositorySaveException" /> class
    ///     with a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="innerException">
    ///     The exception that is the cause of the current exception.
    ///     The inner exception's message is used as this exception's message.
    /// </param>
    public RepositorySaveException(Exception innerException)
        : base(innerException.Message, innerException) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RepositorySaveException" /> class
    ///     with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">
    ///     The exception that is the cause of the current exception, or null if no inner exception is specified.
    /// </param>
    public RepositorySaveException(string message, Exception innerException)
        : base(message, innerException) { }
}
