namespace EzDdd.UseCase.Exceptions;

/// <summary>
///     Exception thrown by RepositoryPeer implementations when persistence operations fail.
/// </summary>
/// <remarks>
///     <para>
///         This exception is thrown at the adapter layer (interface adapters layer) when
///         actual database persistence fails. It is typically caught by the Repository
///         layer and translated to <see cref="RepositorySaveException" />.
///     </para>
///     <para>
///         <strong>Exception Translation Flow:</strong>
///     </para>
///     <code>
/// Infrastructure Layer (Database)
///     SQLException, DbUpdateConcurrencyException
///          ↓ catch &amp; wrap
/// Adapter Layer (IRepositoryPeer)
///     RepositoryPeerSaveException
///          ↓ catch &amp; translate
/// Domain Layer (IRepository)
///     RepositorySaveException
///          ↓ propagate to
/// Use Case Layer
///     Handle or propagate to controller
/// </code>
///     <para>
///         <strong>Separation of Concerns:</strong>
///         This exception belongs to the adapter layer and should NOT contain domain-specific
///         constants (e.g., "Optimistic locking failure" belongs to <see cref="RepositorySaveException" />).
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // In IRepositoryPeer implementation (Adapter layer)
/// try
/// {
///     await _dbContext.SaveChangesAsync();
/// }
/// catch (DbUpdateConcurrencyException ex)
/// {
///     throw new RepositoryPeerSaveException("Database concurrency error", ex);
/// }
/// 
/// // In IRepository implementation (Domain layer)
/// try
/// {
///     await _repositoryPeer.SaveAsync(data);
/// }
/// catch (RepositoryPeerSaveException ex)
/// {
///     throw new RepositorySaveException(RepositorySaveException.OptimisticLockingFailure, ex);
/// }
/// </code>
/// </example>
public class RepositoryPeerSaveException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RepositoryPeerSaveException" /> class.
    /// </summary>
    public RepositoryPeerSaveException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RepositoryPeerSaveException" /> class
    ///     with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public RepositoryPeerSaveException(string message) : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RepositoryPeerSaveException" /> class
    ///     with a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="innerException">
    ///     The exception that is the cause of the current exception.
    ///     The inner exception's message is used as this exception's message.
    /// </param>
    public RepositoryPeerSaveException(Exception innerException)
        : base(innerException.Message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RepositoryPeerSaveException" /> class
    ///     with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">
    ///     The exception that is the cause of the current exception, or null if no inner exception is specified.
    /// </param>
    public RepositoryPeerSaveException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}