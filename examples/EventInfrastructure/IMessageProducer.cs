namespace EzDdd.Examples.EventInfrastructure;

/// <summary>
///     Minimal message producer abstraction used by the <see cref="EventStoreRelay" /> example.
/// </summary>
/// <typeparam name="TMessage">The type of message to produce.</typeparam>
/// <remarks>
///     <para>
///         <strong>Why this interface lives in the example, not in the core library:</strong>
///         Java ezddd 6.0.0 (commit <c>67686ac</c>) moved the <c>MessageProducer</c> interface
///         out of the core library into the external <c>ezddd-gateway</c> artifact. ezDDD.NET
///         mirrors that module boundary: the core packages no longer ship a message producer
///         abstraction. The official counterpart will be provided by the <c>ezDDD.Gateway</c>
///         package (planned post-1.0). Until then, applications that need a producer port
///         declare a minimal abstraction like this one in their own composition root -
///         see ADR-0029 for the full decision record.
///     </para>
///     <para>
///         Implementations wrap a concrete message broker client (Kafka, RabbitMQ, Azure
///         Service Bus, ...). <see cref="IDisposable" /> allows deterministic release of the
///         underlying broker connection.
///     </para>
/// </remarks>
public interface IMessageProducer<in TMessage> : IDisposable
{
    /// <summary>
    ///     Posts a message to the underlying message broker.
    /// </summary>
    /// <param name="message">The message to post.</param>
    /// <returns>A task that represents the asynchronous post operation.</returns>
    Task PostAsync(TMessage message);
}
