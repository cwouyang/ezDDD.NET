using System.Collections.ObjectModel;
using EzDdd.Entity;

namespace EzDdd.UseCase.Tests.Integration.TestDomain;

/// <summary>
///     Order created event.
/// </summary>
public sealed record OrderCreated(
    Guid Id,
    DateTimeOffset OccurredOn,
    OrderId OrderId,
    string CustomerName,
    decimal TotalAmount
) : IInternalDomainEvent
{
    public string Source => OrderId.Value;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
}

/// <summary>
///     Order item added event.
/// </summary>
public sealed record OrderItemAdded(
    Guid Id,
    DateTimeOffset OccurredOn,
    OrderId OrderId,
    string ProductName,
    int Quantity,
    decimal Price
) : IInternalDomainEvent
{
    public string Source => OrderId.Value;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
}

/// <summary>
///     Order confirmed event.
/// </summary>
public sealed record OrderConfirmed(Guid Id, DateTimeOffset OccurredOn, OrderId OrderId) : IInternalDomainEvent
{
    public string Source => OrderId.Value;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
}

/// <summary>
///     Order cancelled event.
/// </summary>
public sealed record OrderCancelled(Guid Id, DateTimeOffset OccurredOn, OrderId OrderId, string Reason)
    : IInternalDomainEvent
{
    public string Source => OrderId.Value;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
}
