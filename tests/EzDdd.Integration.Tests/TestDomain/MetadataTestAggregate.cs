using System.Collections.ObjectModel;
using EzDdd.Entity;

namespace EzDdd.Integration.Tests.TestDomain;

/// <summary>
///     Test aggregate specifically designed to verify metadata propagation throughout the CQRS flow.
/// </summary>
/// <remarks>
///     <para>
///         This aggregate is intentionally simple, focusing on metadata handling rather than complex business logic.
///         All command methods accept a <c>metadata</c> parameter to allow tests to verify metadata preservation
///         through the entire event lifecycle.
///     </para>
///     <para>
///         <strong>Usage in tests</strong>:
///         <code>
///         var metadata = new Dictionary&lt;string, string&gt; { ["CorrelationId"] = "123" };
///         var agg = new MetadataTestAggregate(id, "Test", 100, metadata);
///         agg.UpdateValue(200, metadata);
///         </code>
///     </para>
/// </remarks>
public sealed class MetadataTestAggregate : EsAggregateRoot<MetadataTestId, IInternalDomainEvent>
{
    /// <summary>
    ///     Creates a new aggregate with metadata.
    /// </summary>
    /// <param name="id">Aggregate ID</param>
    /// <param name="name">Aggregate name</param>
    /// <param name="initialValue">Initial value</param>
    /// <param name="metadata">Metadata to attach to the creation event</param>
    public MetadataTestAggregate(
        MetadataTestId id,
        string name,
        int initialValue,
        IReadOnlyDictionary<string, string>? metadata = null
    )
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(name);

        Id = id;
        AggregateCreated @event = new(Guid.NewGuid(), DateTimeOffset.UtcNow, id, name, initialValue)
        {
            Metadata = metadata ?? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()),
        };
        Apply(@event);
    }

    /// <summary>
    ///     Constructor for event replay.
    /// </summary>
    public MetadataTestAggregate(IEnumerable<IInternalDomainEvent> events)
        : base(events) { }

    // Properties
    public string Name { get; private set; } = string.Empty;
    public int Value { get; private set; }
    public bool IsClosed { get; private set; }

    /// <summary>
    ///     Updates the aggregate value with metadata.
    /// </summary>
    /// <param name="newValue">New value to set</param>
    /// <param name="metadata">Metadata to attach to the update event</param>
    public void UpdateValue(int newValue, IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (IsClosed)
        {
            throw new InvalidOperationException("Cannot update a closed aggregate");
        }

        ValueUpdated @event = new(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, Value, newValue)
        {
            Metadata = metadata ?? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()),
        };
        Apply(@event);
    }

    /// <summary>
    ///     Closes the aggregate with metadata.
    /// </summary>
    /// <param name="reason">Reason for closing</param>
    /// <param name="metadata">Metadata to attach to the close event</param>
    public void Close(string reason, IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (IsClosed)
        {
            throw new InvalidOperationException("Aggregate is already closed");
        }

        ArgumentNullException.ThrowIfNull(reason);

        AggregateClosed @event = new(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, reason)
        {
            Metadata = metadata ?? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()),
        };
        Apply(@event);
    }

    protected override void _When(IInternalDomainEvent @event)
    {
        switch (@event)
        {
            case AggregateCreated created:
                Id = created.AggregateId;
                Name = created.Name;
                Value = created.InitialValue;
                IsClosed = false;
                break;

            case ValueUpdated updated:
                Value = updated.NewValue;
                break;

            case AggregateClosed closed:
                IsClosed = true;
                break;

            default:
                throw new InvalidOperationException($"Unknown event type: {@event.GetType().Name}");
        }
    }

    protected override void _EnsureInvariant()
    {
        // Skip invariant checks for closed aggregates
        if (IsClosed)
        {
            return;
        }

        // Business rule: Name must not be empty
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException("Aggregate name cannot be empty");
        }
    }

    public override string GetCategory()
    {
        return "metadata-test";
    }
}
