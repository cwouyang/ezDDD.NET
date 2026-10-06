using System.Text.Json.Serialization;

namespace EzDdd.Cqrs.Entity.Query;

/// <summary>
/// The product of a projection, and the unit an archive stores. It carries the <see cref="EventDeduplicationRecord"/>
/// of the events already projected into it.
/// </summary>
/// <remarks>
/// Call <see cref="UpdateEventDeduplicationRecord"/> before the single save of the read model, also on the first event
/// that creates the model, so the model and its record are written in one atomic write. An archive must return a copy
/// from its find operation: otherwise a failure between the update and the save marks an event as handled that was never
/// persisted, and its redelivery is ignored.
/// <para>
/// The capacity is persisted per document: a stored capacity wins over the constructor's, so raising it in code affects
/// only new documents. A redelivery older than the capacity is projected again. Each remembered id adds about 39 bytes
/// to the document.
/// </para>
/// <para>
/// Rolling back to an older version that drops the record property and saving again loses the record. The JSON form is
/// produced by reflection-based System.Text.Json only: a source-generated <c>JsonSerializerContext</c> cannot see the
/// private members involved and would silently drop the record. This type is not thread-safe.
/// </para>
/// </remarks>
public abstract record ReadModel
{
    private EventDeduplicationRecord _eventDeduplicationRecord;

    /// <summary>Creates a read model with the default deduplication capacity.</summary>
    protected ReadModel() => _eventDeduplicationRecord = new();

    /// <summary>Creates a read model that remembers at most <paramref name="maxEventCapacity"/> event ids.</summary>
    /// <param name="maxEventCapacity">The number of most recent event ids to remember.</param>
    protected ReadModel(int maxEventCapacity) => _eventDeduplicationRecord = new(maxEventCapacity);

    /// <summary>Creates a copy that owns an independent deduplication record.</summary>
    /// <param name="original">The read model to copy.</param>
    protected ReadModel(ReadModel original)
    {
        ArgumentNullException.ThrowIfNull(original);
        _eventDeduplicationRecord = original.EventDeduplicationRecord.Copy();
    }

    /// <summary>The ids of the events already projected into this model.</summary>
    [JsonInclude]
    [JsonPropertyName("eventDeduplicationRecord")]
    public EventDeduplicationRecord EventDeduplicationRecord
    {
        get => _eventDeduplicationRecord;
        // Only for deserialization: a document without a record (null) keeps the record the constructor created.
        private set => _eventDeduplicationRecord = value ?? _eventDeduplicationRecord;
    }

    /// <summary>Remembers the event id as handled.</summary>
    /// <param name="eventId">The id of the handled event.</param>
    public void UpdateEventDeduplicationRecord(Guid eventId) => EventDeduplicationRecord.SetEventId(eventId);

    /// <summary>Compares the runtime type only; the deduplication record never takes part.</summary>
    /// <param name="other">The read model to compare with.</param>
    /// <returns><see langword="true"/> if both are of the same runtime type.</returns>
    public virtual bool Equals(ReadModel? other) =>
        ReferenceEquals(this, other) || (other is not null && EqualityContract == other.EqualityContract);

    /// <inheritdoc/>
    public override int GetHashCode() => EqualityContract.GetHashCode();
}
