using EzDdd.Entity;
using EzDdd.UseCase.Port.InOut;
using EzDdd.UseCase.Port.InOut.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EzDdd.Examples.EventInfrastructure;

/// <summary>
///     Background service that polls an event store and publishes events to a message producer.
///     Implements the Relay pattern for Transactional Outbox.
/// </summary>
/// <remarks>
///     <para>
///         This is a reference implementation demonstrating the Relay pattern used in Java ezddd 4.1.0.
///         Production implementations should adapt this to their specific event store and infrastructure.
///     </para>
///     <para>
///         <strong>Outbox Pattern Guarantee:</strong>
///         By polling continuously and catching exceptions, this relay ensures that all events
///         saved to the event store will eventually be published to the message broker,
///         even if the broker experiences temporary failures.
///     </para>
///     <para>
///         <strong>Key Features:</strong>
///         <list type="bullet">
///             <item>
///                 <description>Automatic retry on message broker failures</description>
///             </item>
///             <item>
///                 <description>Guaranteed eventual consistency (at-least-once delivery)</description>
///             </item>
///             <item>
///                 <description>Configurable polling interval</description>
///             </item>
///             <item>
///                 <description>Graceful shutdown support</description>
///             </item>
///             <item>
///                 <description>Comprehensive logging for diagnostics</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <strong>Usage:</strong>
///         Register as a hosted service in ASP.NET Core:
///         <code>
/// services.AddHostedService&lt;EventStoreRelay&gt;();
/// </code>
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Startup.cs or Program.cs
/// public void ConfigureServices(IServiceCollection services)
/// {
///     // Register event store
///     services.AddSingleton&lt;IEventStore, SqlEventStore&gt;();
///
///     // Register message producer
///     services.AddSingleton&lt;IMessageProducer&lt;DomainEventData&gt;, KafkaMessageProducer&gt;();
///
///     // Register relay as hosted service
///     services.AddHostedService&lt;EventStoreRelay&gt;();
/// }
/// </code>
/// </example>
public class EventStoreRelay : BackgroundService
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<EventStoreRelay> _logger;
    private readonly IMessageProducer<DomainEventData> _messageProducer;
    private readonly int _pollingIntervalMs;
    private int _currentIndex = -1;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventStoreRelay" /> class.
    /// </summary>
    /// <param name="eventStore">The event store to poll for unpublished events</param>
    /// <param name="messageProducer">The message producer to publish events to</param>
    /// <param name="logger">Logger for diagnostics</param>
    /// <param name="pollingIntervalMs">Polling interval in milliseconds (default: 100ms)</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when eventStore, messageProducer, or logger is null
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when pollingIntervalMs is less than or equal to 0
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         The polling interval determines how frequently the relay checks for new events.
    ///         Lower values provide lower latency but increase database load. Recommended values:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>Production: 100-500ms (balance between latency and load)</description>
    ///             </item>
    ///             <item>
    ///                 <description>Testing: 10-50ms (faster feedback for tests)</description>
    ///             </item>
    ///             <item>
    ///                 <description>High-throughput: 50-100ms (minimize latency)</description>
    ///             </item>
    ///             <item>
    ///                 <description>Low-priority: 500-1000ms (reduce database load)</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public EventStoreRelay(
        IEventStore eventStore,
        IMessageProducer<DomainEventData> messageProducer,
        ILogger<EventStoreRelay> logger,
        int pollingIntervalMs = 100)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(messageProducer);
        ArgumentNullException.ThrowIfNull(logger);

        if (pollingIntervalMs <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pollingIntervalMs),
                pollingIntervalMs,
                "Polling interval must be greater than 0");
        }

        _eventStore = eventStore;
        _messageProducer = messageProducer;
        _logger = logger;
        _pollingIntervalMs = pollingIntervalMs;
    }

    /// <summary>
    ///     Executes the relay polling loop.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token for stopping the relay</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    /// <remarks>
    ///     <para>
    ///         This method runs continuously until cancellation is requested. In each iteration:
    ///         <list type="number">
    ///             <item>
    ///                 <description>Polls event store for new events after current index</description>
    ///             </item>
    ///             <item>
    ///                 <description>For each new event, converts to DomainEventData and publishes</description>
    ///             </item>
    ///             <item>
    ///                 <description>Increments current index on successful publish</description>
    ///             </item>
    ///             <item>
    ///                 <description>On publish failure, stops batch and retries on next poll</description>
    ///             </item>
    ///             <item>
    ///                 <description>Sleeps for polling interval before next iteration</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <strong>Error Handling:</strong>
    ///         All exceptions are caught and logged. The relay continues polling even after errors,
    ///         ensuring eventual consistency. Failed events will be retried on the next poll.
    ///     </para>
    /// </remarks>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "EventStoreRelay started with polling interval {Interval}ms",
            _pollingIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Get events after current index
                IReadOnlyList<IInternalDomainEvent> newEvents =
                    await _eventStore.GetEventsAfterAsync(_currentIndex, stoppingToken);

                if (newEvents.Count > 0)
                {
                    _logger.LogDebug(
                        "Polling event store: found {Count} new events after index {Index}",
                        newEvents.Count,
                        _currentIndex);

                    foreach (IInternalDomainEvent domainEvent in newEvents)
                    {
                        try
                        {
                            // Convert to DomainEventData
                            DomainEventData eventData = DomainEventMapper.ToData(domainEvent);

                            // Publish to message producer
                            await _messageProducer.PostAsync(eventData);

                            // Mark as published (increment index)
                            _currentIndex++;

                            _logger.LogDebug(
                                "Published event {EventType} with ID {EventId} (index: {Index})",
                                eventData.EventType,
                                eventData.EventId,
                                _currentIndex);
                        }
                        catch (Exception publishEx)
                        {
                            // Catch exception, will retry on next poll
                            _logger.LogError(
                                publishEx,
                                "Failed to publish event {EventType} at index {Index}, will retry on next poll",
                                domainEvent.GetType().Name,
                                _currentIndex + 1);

                            // Don't increment currentIndex - will retry this event next time
                            break; // Stop processing this batch, retry from this event
                        }
                    }
                }

                // Sleep until next poll
                await Task.Delay(_pollingIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                _logger.LogInformation("EventStoreRelay stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                // Catch all exceptions, relay continues
                _logger.LogError(
                    ex,
                    "EventStoreRelay encountered error at index {Index}, will retry on next poll",
                    _currentIndex);

                await Task.Delay(_pollingIntervalMs, stoppingToken);
            }
        }

        _logger.LogInformation(
            "EventStoreRelay stopped (published {Count} events total)",
            _currentIndex + 1);
    }

    /// <summary>
    ///     Performs cleanup when the service is stopping.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    /// <remarks>
    ///     <para>
    ///         This method is called when the application is shutting down. It logs a shutdown
    ///         message and allows the base class to perform cleanup.
    ///     </para>
    /// </remarks>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EventStoreRelay shutting down gracefully");
        await base.StopAsync(cancellationToken);
    }
}
