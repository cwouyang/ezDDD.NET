using System.Collections.ObjectModel;
using EzDdd.Entity;

namespace EzDdd.Integration.Tests.TestDomain;

/// <summary>
///     Aggregate created event with metadata support (construction event).
/// </summary>
public sealed record AggregateCreated(
    Guid Id,
    DateTimeOffset OccurredOn,
    MetadataTestId AggregateId,
    string Name,
    int InitialValue
) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent
{
    public string Source => AggregateId.Value;

    /// <summary>
    ///     Metadata dictionary. Can be set via init property.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
}

/// <summary>
///     Value updated event with metadata support.
/// </summary>
public sealed record ValueUpdated(
    Guid Id,
    DateTimeOffset OccurredOn,
    MetadataTestId AggregateId,
    int OldValue,
    int NewValue
) : IInternalDomainEvent
{
    public string Source => AggregateId.Value;

    /// <summary>
    ///     Metadata dictionary. Can be set via init property.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
}

/// <summary>
///     Aggregate closed event with metadata support (destruction event).
/// </summary>
public sealed record AggregateClosed(Guid Id, DateTimeOffset OccurredOn, MetadataTestId AggregateId, string Reason)
    : IInternalDomainEvent,
        IInternalDomainEvent.IDestructionEvent
{
    public string Source => AggregateId.Value;

    /// <summary>
    ///     Metadata dictionary. Can be set via init property.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
}
