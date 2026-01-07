using System.Collections.Concurrent;

namespace EzDdd.UseCase.Port.InOut.Messaging;

/// <summary>
///     In-memory message producer implementation for testing and development.
///     Stores posted messages in a thread-safe queue for verification.
/// </summary>
/// <typeparam name="TMessage">The type of messages this producer handles.</typeparam>
/// <remarks>
///     <para>
///         This implementation is designed for testing and local development scenarios.
///         For production use, implement <see cref="IMessageProducer{TMessage}" /> with actual
///         message broker clients (e.g., Kafka, RabbitMQ, Azure Service Bus).
///     </para>
///     <para>
///         <b>Thread Safety:</b>
///     </para>
///     <para>
///         Multiple threads can call <see cref="PostAsync" /> concurrently. The underlying
///         <see cref="ConcurrentQueue{T}" /> ensures thread-safe message storage without locking.
///     </para>
///     <para>
///         <b>Usage Example:</b>
///     </para>
///     <code>
/// // Use in tests
/// using var producer = new InMemoryMessageProducer&lt;DomainEventData&gt;();
/// await producer.PostAsync(eventData1);
/// await producer.PostAsync(eventData2);
/// 
/// // Verify posted messages
/// Assert.Equal(2, producer.PostedMessages.Count);
/// Assert.Contains(eventData1, producer.PostedMessages);
/// </code>
/// </remarks>
public class InMemoryMessageProducer<TMessage> : IMessageProducer<TMessage>
{
    private readonly ConcurrentQueue<TMessage> _postedMessages;
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryMessageProducer{TMessage}" /> class.
    /// </summary>
    public InMemoryMessageProducer()
    {
        _postedMessages = new ConcurrentQueue<TMessage>();
    }

    /// <summary>
    ///     Gets the collection of all messages posted to this producer.
    ///     Useful for testing and verification.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property returns a snapshot of the currently posted messages as an array.
    ///         Modifications to the returned array do not affect the internal queue.
    ///     </para>
    ///     <para>
    ///         After disposal, this property returns an empty array.
    ///     </para>
    /// </remarks>
    public IReadOnlyCollection<TMessage> PostedMessages =>
        _disposed ? [] : _postedMessages.ToArray();

    /// <summary>
    ///     Posts a message to the in-memory queue.
    /// </summary>
    /// <param name="message">The message to post.</param>
    /// <returns>A completed task.</returns>
    /// <exception cref="ArgumentNullException">Thrown if message is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if this producer has been disposed.</exception>
    /// <remarks>
    ///     <para>
    ///         This method completes immediately (synchronously) after adding the message to the queue.
    ///         The returned task is always completed.
    ///     </para>
    ///     <para>
    ///         Thread-safe: Multiple threads can call this method concurrently without locking.
    ///     </para>
    /// </remarks>
    public Task PostAsync(TMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _ThrowIfDisposed();

        _postedMessages.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Disposes the producer and clears all posted messages.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method clears the internal message queue and marks the producer as disposed.
    ///         Subsequent calls to <see cref="PostAsync" /> will throw <see cref="ObjectDisposedException" />.
    ///     </para>
    ///     <para>
    ///         This method is idempotent and can be called multiple times safely.
    ///     </para>
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Clear all messages
        while (_postedMessages.TryDequeue(out _))
        {
            // Discard
        }

        _disposed = true;
    }

    private void _ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException
            (
                nameof(InMemoryMessageProducer<TMessage>),
                "Cannot post messages to a disposed message producer."
            );
        }
    }
}