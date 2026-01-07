namespace EzDdd.UseCase.Exceptions;

/// <summary>
///     Exception thrown when a message producer fails to post a message to the message bus.
///     This exception typically indicates infrastructure-level failures (network, broker unavailable).
/// </summary>
/// <remarks>
///     <para>
///         This exception wraps lower-level exceptions from message broker clients (e.g., Kafka, RabbitMQ)
///         to provide a consistent error handling interface for application code.
///     </para>
///     <para>
///         <b>Example scenarios:</b>
///     </para>
///     <list type="bullet">
///         <item>
///             <description>Network connection to message broker lost</description>
///         </item>
///         <item>
///             <description>Message broker unavailable or overloaded</description>
///         </item>
///         <item>
///             <description>Serialization failure when converting message to broker format</description>
///         </item>
///         <item>
///             <description>Authentication or authorization failure with message broker</description>
///         </item>
///         <item>
///             <description>Message size exceeds broker limits</description>
///         </item>
///     </list>
///     <para>
///         <b>Usage Example:</b>
///     </para>
///     <code>
/// try
/// {
///     await eventProducer.PostAsync(eventData);
/// }
/// catch (PostEventFailureException ex)
/// {
///     _logger.LogError(ex, "Failed to publish event to message bus");
///     // Handle failure (retry, compensate, alert)
/// }
/// </code>
/// </remarks>
public class PostEventFailureException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PostEventFailureException" /> class.
    /// </summary>
    public PostEventFailureException()
        : base("Failed to post message to message producer.")
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PostEventFailureException" /> class
    ///     with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public PostEventFailureException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PostEventFailureException" /> class
    ///     with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">
    ///     The exception that is the cause of the current exception
    ///     (typically from the message broker client).
    /// </param>
    public PostEventFailureException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}