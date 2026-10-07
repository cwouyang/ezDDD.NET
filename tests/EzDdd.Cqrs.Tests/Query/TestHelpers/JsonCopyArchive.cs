using System.Collections.Concurrent;
using EzDdd.Common;
using EzDdd.Cqrs.Query;

namespace EzDdd.Cqrs.Tests.Query.TestHelpers;

/// <summary>
///     In-memory <see cref="IArchive{TData,TId}" /> that stores JSON and copies through it on both find and save,
///     so a caller never shares an instance with the store (like a real database).
/// </summary>
/// <typeparam name="TData">The read model type.</typeparam>
/// <typeparam name="TId">The identifier type.</typeparam>
public sealed class JsonCopyArchive<TData, TId> : IArchive<TData, TId>
    where TData : class
    where TId : notnull
{
    private readonly Func<TData, TId> _idExtractor;
    private readonly ConcurrentDictionary<TId, string> _store = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="JsonCopyArchive{TData, TId}" /> class.
    /// </summary>
    /// <param name="idExtractor">Function to extract the ID from the data object.</param>
    public JsonCopyArchive(Func<TData, TId> idExtractor)
    {
        _idExtractor = idExtractor ?? throw new ArgumentNullException(nameof(idExtractor));
    }

    /// <inheritdoc />
    public Task<TData?> FindByIdAsync(TId id)
    {
        TData? copy = _store.TryGetValue(id, out string? json) ? JsonUtil.ReadValue<TData>(json) : null;
        return Task.FromResult(copy);
    }

    /// <inheritdoc />
    public Task SaveAsync(TData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        _store[_idExtractor(data)] = JsonUtil.AsString(data);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(TData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        _store.TryRemove(_idExtractor(data), out _);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Gets the stored JSON of a read model, or <c>null</c> when none is stored.
    /// </summary>
    /// <param name="id">The identifier of the read model.</param>
    /// <returns>The stored JSON, or <c>null</c>.</returns>
    public string? GetStoredJson(TId id)
    {
        return _store.TryGetValue(id, out string? json) ? json : null;
    }
}
