using System.Collections.Concurrent;

using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.UseCase.Tests.Integration.TestDomain;

/// <summary>
///     In-memory implementation of IRepositoryPeer for event store testing.
///     Thread-safe for concurrent operations.
/// </summary>
public sealed class InMemoryEventStorePeer : IRepositoryPeer<EventStoreData<AccountId>, AccountId>
{
    private readonly ConcurrentDictionary<string, EventStoreData<AccountId>> _storage = new();
    public int Count => _storage.Count;

    public Task<EventStoreData<AccountId>?> FindByIdAsync(AccountId id)
    {
        _storage.TryGetValue(id.Value, out EventStoreData<AccountId>? data);
        return Task.FromResult(data);
    }

    public Task SaveAsync(EventStoreData<AccountId> data)
    {
        // Simulate optimistic locking check
        if (_storage.TryGetValue(data.Id.Value, out EventStoreData<AccountId>? existing))
        {
            if (existing.Version != data.Version - 1)
            {
                throw new RepositoryPeerSaveException
                (
                    $"Optimistic lock failure: expected version {existing.Version}, but got {data.Version - 1}",
                    new InvalidOperationException("Version mismatch")
                );
            }

            // Event sourcing: Append new events to existing events (accumulate history)
            List<IDomainEvent> allEvents = new(existing.Events);
            allEvents.AddRange(data.Events);

            _storage[data.Id.Value] = new EventStoreData<AccountId>
            {
                Id = data.Id, Version = data.Version, Events = allEvents, StreamName = data.StreamName
            };
        }
        else
        {
            // First save: Store as-is
            _storage[data.Id.Value] = data;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(EventStoreData<AccountId> data)
    {
        _storage.TryRemove(data.Id.Value, out _);
        return Task.CompletedTask;
    }

    // Helper methods for testing
    public void Clear()
    {
        _storage.Clear();
    }

    public bool Contains(AccountId id)
    {
        return _storage.ContainsKey(id.Value);
    }
}