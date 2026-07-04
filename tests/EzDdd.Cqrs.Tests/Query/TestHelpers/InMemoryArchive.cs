using System.Collections.Concurrent;
using EzDdd.Cqrs.Query;

namespace EzDdd.Cqrs.Tests.Query.TestHelpers;

/// <summary>
///     In-memory implementation of <see cref="IArchive{TData,TId}" /> for testing purposes.
///     This implementation is thread-safe and provides idempotent operations.
/// </summary>
/// <typeparam name="TData">The read model type.</typeparam>
/// <typeparam name="TId">The identifier type.</typeparam>
public class InMemoryArchive<TData, TId> : IArchive<TData, TId>
    where TData : class
    where TId : notnull
{
    private readonly Func<TData, TId> _idExtractor;
    private readonly ConcurrentDictionary<TId, TData> _store = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryArchive{TData, TId}" /> class.
    /// </summary>
    /// <param name="idExtractor">Function to extract the ID from the data object.</param>
    public InMemoryArchive(Func<TData, TId> idExtractor)
    {
        _idExtractor = idExtractor ?? throw new ArgumentNullException(nameof(idExtractor));
    }

    /// <summary>
    ///     Gets the current count of items in the archive.
    /// </summary>
    public int Count => _store.Count;

    /// <summary>
    ///     Finds a read model by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the read model to find.</param>
    /// <returns>
    ///     A task containing the read model if found, or <c>null</c> if not found.
    /// </returns>
    public Task<TData?> FindByIdAsync(TId id)
    {
        _store.TryGetValue(id, out TData? data);
        return Task.FromResult(data);
    }

    /// <summary>
    ///     Saves (inserts or updates) a read model in the archive.
    ///     This operation is idempotent - saving the same data multiple times produces the same result.
    /// </summary>
    /// <param name="data">The read model to save.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    public Task SaveAsync(TData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        TId id = _idExtractor(data);
        _store[id] = data;

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Deletes a read model from the archive.
    ///     This operation is idempotent - deleting a non-existent item does not throw an exception.
    /// </summary>
    /// <param name="data">The read model to delete.</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    public Task DeleteAsync(TData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        TId id = _idExtractor(data);
        _store.TryRemove(id, out _);

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Clears all data from the archive. Useful for test cleanup.
    /// </summary>
    public void Clear()
    {
        _store.Clear();
    }

    /// <summary>
    ///     Gets all items in the archive. Useful for test assertions.
    /// </summary>
    /// <returns>A collection of all items currently in the archive.</returns>
    public IEnumerable<TData> GetAll()
    {
        return _store.Values;
    }
}
