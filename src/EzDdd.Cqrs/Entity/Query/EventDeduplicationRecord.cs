using System.Text.Json.Serialization;

namespace EzDdd.Cqrs.Entity.Query;

/// <summary>
/// Remembers the ids of events already projected into the read model that owns it, so an event redelivered by an
/// at-least-once transport can be recognized and skipped.
/// </summary>
/// <remarks>
/// Only the most recent ids are kept: when the capacity is exceeded, the eldest ids are dropped first. A redelivery
/// older than the capacity is therefore no longer recognized. This type is not thread-safe.
/// </remarks>
public sealed class EventDeduplicationRecord
{
    /// <summary>
    /// The capacity used by the parameterless constructor. The upstream value 50 was sized for a gateway poll size of
    /// 20 that ezDDD.NET does not have, so callers should size the capacity above the number of events that can be
    /// redelivered for one read model.
    /// </summary>
    public const int DefaultMaxEventCapacity = 50;

    private readonly List<string> _eventIds = [];
    private int _maxEventCapacity;

    /// <summary>Creates a record with <see cref="DefaultMaxEventCapacity"/>.</summary>
    public EventDeduplicationRecord()
        : this(DefaultMaxEventCapacity) { }

    /// <summary>Creates a record that remembers at most <paramref name="maxEventCapacity"/> event ids.</summary>
    /// <param name="maxEventCapacity">The number of most recent event ids to remember.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxEventCapacity"/> is less than 1.</exception>
    public EventDeduplicationRecord(int maxEventCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxEventCapacity, 1);
        _maxEventCapacity = maxEventCapacity;
    }

    // JSON shape shared with the Java library. Non-public members keep the type free of public properties; the
    // setters are the tolerant read path, while the public constructor still rejects a capacity below 1.
    [JsonInclude]
    [JsonPropertyName("processedEventIds")]
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Replace)]
    private List<string?>? JsonEventIds
    {
        get => [.. _eventIds];
        set
        {
            _eventIds.Clear();
            foreach (string stored in value?.OfType<string>() ?? [])
            {
                string id = Guid.TryParse(stored, out Guid guid) ? guid.ToString("D") : stored;
                if (!_eventIds.Contains(id, StringComparer.Ordinal))
                {
                    _eventIds.Add(id);
                }
            }
        }
    }

    [JsonInclude]
    [JsonPropertyName("MAX_EVENT_CAPACITY")]
    private int? JsonMaxEventCapacity
    {
        get => _maxEventCapacity;
        set => _maxEventCapacity = value is >= 1 ? value.Value : DefaultMaxEventCapacity;
    }

    /// <summary>Tells whether the event id is still remembered.</summary>
    /// <param name="eventId">The id of the event to look up.</param>
    /// <returns><see langword="true"/> if the event id is remembered; otherwise <see langword="false"/>.</returns>
    public bool IsEventHandled(Guid eventId) => _eventIds.Contains(eventId.ToString("D"), StringComparer.Ordinal);

    /// <summary>Remembers the event id, dropping the eldest ids when the capacity is exceeded.</summary>
    /// <param name="eventId">The id of the handled event.</param>
    public void SetEventId(Guid eventId)
    {
        string id = eventId.ToString("D");
        if (!_eventIds.Contains(id, StringComparer.Ordinal))
        {
            _eventIds.Add(id);
        }

        while (_eventIds.Count > _maxEventCapacity)
        {
            _eventIds.RemoveAt(0);
        }
    }

    /// <summary>Creates an independent record with the same ids and capacity.</summary>
    internal EventDeduplicationRecord Copy()
    {
        EventDeduplicationRecord copy = new(_maxEventCapacity);
        copy._eventIds.AddRange(_eventIds);
        return copy;
    }
}
