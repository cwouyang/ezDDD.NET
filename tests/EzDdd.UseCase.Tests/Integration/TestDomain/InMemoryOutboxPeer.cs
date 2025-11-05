using System.Collections.Concurrent;

using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.UseCase.Tests.Integration.TestDomain;

/// <summary>
///     In-memory implementation of IRepositoryPeer for outbox/state sourcing testing.
///     Thread-safe for concurrent operations.
/// </summary>
public sealed class InMemoryOutboxPeer : IRepositoryPeer<OrderData, OrderId>
{
    private readonly ConcurrentDictionary<string, OrderData> _storage = new();
    public int Count => _storage.Count;

    public Task<OrderData?> FindByIdAsync(OrderId id)
    {
        _storage.TryGetValue(id.Value, out OrderData? data);
        return Task.FromResult(data);
    }

    public Task SaveAsync(OrderData data)
    {
        // Simulate optimistic locking check
        if (_storage.TryGetValue(data.Id.Value, out OrderData? existing))
        {
            // State sourcing: Check that version increments correctly
            // Expected: existing.Version + newEvents.Count == data.Version
            int newEventsCount = data.Events.Count;
            long expectedVersion = existing.Version + newEventsCount;

            if (expectedVersion != data.Version)
            {
                throw new RepositoryPeerSaveException
                (
                    $"Optimistic lock failure: expected version {expectedVersion}, but got {data.Version}",
                    new InvalidOperationException("Version mismatch")
                );
            }
        }

        // State sourcing: Overwrite with current state (not append like event sourcing)
        // Events are stored separately for Transactional Outbox pattern
        _storage[data.Id.Value] = data;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(OrderData data)
    {
        _storage.TryRemove(data.Id.Value, out _);
        return Task.CompletedTask;
    }

    // Helper methods for testing
    public void Clear()
    {
        _storage.Clear();
    }

    public bool Contains(OrderId id)
    {
        return _storage.ContainsKey(id.Value);
    }
}