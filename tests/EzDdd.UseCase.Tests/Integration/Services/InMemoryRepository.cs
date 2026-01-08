using System.Collections.Concurrent;
using EzDdd.Entity;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.UseCase.Tests.Integration.Services;

/// <summary>
/// Simple in-memory repository for testing Service Layer examples.
/// Uses ConcurrentDictionary for thread-safe storage.
/// </summary>
public sealed class InMemoryRepository<TAggregate, TId> : IRepository<TAggregate, TId, IInternalDomainEvent>
    where TAggregate : AggregateRoot<TId, IInternalDomainEvent>
    where TId : notnull
{
    private readonly ConcurrentDictionary<TId, TAggregate> _storage = new();

    public Task<TAggregate?> FindByIdAsync(TId id)
    {
        _storage.TryGetValue(id, out var aggregate);
        return Task.FromResult(aggregate);
    }

    public Task SaveAsync(TAggregate aggregate)
    {
        _storage[aggregate.Id] = aggregate;

        // Clear domain events after successful save
        aggregate.ClearDomainEvents();

        return Task.CompletedTask;
    }

    public Task DeleteAsync(TAggregate aggregate)
    {
        _storage.TryRemove(aggregate.Id, out _);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<TAggregate>> FindAllAsync()
    {
        return Task.FromResult<IEnumerable<TAggregate>>(_storage.Values.ToList());
    }

    public void Clear()
    {
        _storage.Clear();
    }

    public int Count => _storage.Count;
}
