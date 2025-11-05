using System.Collections.ObjectModel;

using EzDdd.Entity;

namespace EzDdd.UseCase.Tests.Integration.TestDomain;

/// <summary>
///     Account created event (construction event).
/// </summary>
public sealed record AccountCreated
(
    Guid Id,
    DateTimeOffset OccurredOn,
    AccountId AccountId,
    string Owner,
    Money InitialBalance
) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent
{
    public string Source => AccountId.Value;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
}

/// <summary>
///     Money deposited event.
/// </summary>
public sealed record MoneyDeposited
(
    Guid Id,
    DateTimeOffset OccurredOn,
    AccountId AccountId,
    Money Amount
) : IInternalDomainEvent
{
    public string Source => AccountId.Value;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
}

/// <summary>
///     Money withdrawn event.
/// </summary>
public sealed record MoneyWithdrawn
(
    Guid Id,
    DateTimeOffset OccurredOn,
    AccountId AccountId,
    Money Amount
) : IInternalDomainEvent
{
    public string Source => AccountId.Value;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
}

/// <summary>
///     Account closed event (destruction event).
/// </summary>
public sealed record AccountClosed
(
    Guid Id,
    DateTimeOffset OccurredOn,
    AccountId AccountId,
    string Reason
) : IInternalDomainEvent, IInternalDomainEvent.IDestructionEvent
{
    public string Source => AccountId.Value;

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
}