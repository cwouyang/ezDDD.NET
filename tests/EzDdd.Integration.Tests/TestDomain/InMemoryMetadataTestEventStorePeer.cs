using System.Collections.Concurrent;
using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.Integration.Tests.TestDomain;

/// <summary>
///     In-memory event store peer for MetadataTestAggregate.
/// </summary>
public sealed class InMemoryMetadataTestEventStorePeer : IRepositoryPeer<EventStoreData<MetadataTestId>, MetadataTestId>
{
    private readonly ConcurrentDictionary<string, EventStoreData<MetadataTestId>> _storage = new();

    public int Count => _storage.Count;

    public Task<EventStoreData<MetadataTestId>?> FindByIdAsync(MetadataTestId id)
    {
        _storage.TryGetValue(id.Value, out EventStoreData<MetadataTestId>? data);
        return Task.FromResult(data);
    }

    public Task SaveAsync(EventStoreData<MetadataTestId> data)
    {
        ArgumentNullException.ThrowIfNull(data);

        // Simulate optimistic locking check
        if (_storage.TryGetValue(data.Id.Value, out EventStoreData<MetadataTestId>? existing))
        {
            if (existing.Version != data.Version - 1)
            {
                throw new RepositoryPeerSaveException(
                    $"Optimistic lock failure: expected version {existing.Version}, but got {data.Version - 1}",
                    new InvalidOperationException("Version mismatch")
                );
            }

            // Event sourcing: Append new events to existing events (accumulate history)
            List<IDomainEvent> allEvents = new(existing.Events);
            allEvents.AddRange(data.Events);

            _storage[data.Id.Value] = new EventStoreData<MetadataTestId>
            {
                Id = data.Id,
                Version = data.Version,
                Events = allEvents,
                StreamName = data.StreamName,
            };
        }
        else
        {
            // First save: Store as-is
            _storage[data.Id.Value] = data;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(EventStoreData<MetadataTestId> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _storage.TryRemove(data.Id.Value, out _);
        return Task.CompletedTask;
    }

    // Helper methods for testing
    public void Clear()
    {
        _storage.Clear();
    }

    public bool Contains(MetadataTestId id)
    {
        return _storage.ContainsKey(id.Value);
    }
}
