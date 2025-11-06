using System.Collections.ObjectModel;
using System.Text.Json;

using EzDdd.Entity;
using EzDdd.UseCase.Port.InOut;
using EzDdd.UseCase.Port.Out;
using EzDdd.UseCase.Tests.Integration.TestDomain;

namespace EzDdd.UseCase.Tests.Integration;

public sealed class EventInfrastructureTests
{
    public EventInfrastructureTests()
    {
        // Register domain events for serialization
        DomainEventTypeMapper.Register<AccountCreated>("AccountCreated");
        DomainEventTypeMapper.Register<MoneyDeposited>("MoneyDeposited");
        DomainEventTypeMapper.Register<MoneyWithdrawn>("MoneyWithdrawn");
        DomainEventTypeMapper.Register<AccountClosed>("AccountClosed");
    }

    [Fact]
    public void CompleteEventSerializationRoundTrip_PreservesAllProperties()
    {
        AccountId accountId = new("acc-001");
        Dictionary<string, string> metadata = new() { ["TransactionId"] = "tx-12345", ["IpAddress"] = "192.168.1.1" };

        AccountCreated originalEvent = new
        (
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            accountId,
            "John Doe",
            new Money(1000m)
        ) { Metadata = new ReadOnlyDictionary<string, string>(metadata) };

        DomainEventData eventData = DomainEventMapper.ToData(originalEvent);

        // Serialize to JSON and back
        string json = JsonSerializer.Serialize(eventData);
        DomainEventData? deserializedData = JsonSerializer.Deserialize<DomainEventData>(json);
        Assert.NotNull(deserializedData);

        // Convert back to domain event
        AccountCreated reconstructedEvent = DomainEventMapper.ToDomain<AccountCreated>(deserializedData);

        Assert.Equal(originalEvent.Id, reconstructedEvent.Id);
        Assert.Equal(originalEvent.OccurredOn, reconstructedEvent.OccurredOn);
        Assert.Equal(originalEvent.Source, reconstructedEvent.Source);
        Assert.Equal(originalEvent.AccountId.Value, reconstructedEvent.AccountId.Value);
        Assert.Equal(originalEvent.Owner, reconstructedEvent.Owner);
        Assert.Equal(originalEvent.InitialBalance.Amount, reconstructedEvent.InitialBalance.Amount);

        // Verify metadata preserved
        Assert.Equal("tx-12345", reconstructedEvent.Metadata["TransactionId"]);
        Assert.Equal("192.168.1.1", reconstructedEvent.Metadata["IpAddress"]);
    }

    [Fact]
    public void ExternalDomainEvent_IsProperlyDistinguishedFromInternal()
    {
        AccountCreated internalEvent = new
        (
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            new AccountId("acc-001"),
            "John Doe",
            new Money(1000m)
        );

        PaymentReceived externalEvent = new
        (
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "payment-001",
            500m
        );

        Assert.IsAssignableFrom<IInternalDomainEvent>(internalEvent);
        Assert.IsNotAssignableFrom<IExternalDomainEvent>(internalEvent);

        Assert.IsAssignableFrom<IExternalDomainEvent>(externalEvent);
        Assert.IsNotAssignableFrom<IInternalDomainEvent>(externalEvent);

        // Verify both are IDomainEvent
        Assert.IsAssignableFrom<IDomainEvent>(internalEvent);
        Assert.IsAssignableFrom<IDomainEvent>(externalEvent);
    }

    [Fact]
    public void DomainEventData_WithMetadata_PreservesAllData()
    {
        Dictionary<string, string> metadata = new()
        {
            ["User"] = "admin@example.com", ["SessionId"] = "sess-99999", ["Platform"] = "Windows", ["Browser"] = "Chrome"
        };

        MoneyDeposited @event = new
        (
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            new AccountId("acc-002"),
            new Money(250m)
        ) { Metadata = new ReadOnlyDictionary<string, string>(metadata) };

        DomainEventData eventData = DomainEventMapper.ToData(@event);

        Assert.Equal(@event.Id, eventData.Id);
        Assert.NotNull(eventData.EventType);
        Assert.NotEmpty(eventData.EventType);
        Assert.Equal("application/json", eventData.ContentType);
        Assert.NotNull(eventData.EventBody);
        Assert.NotEmpty(eventData.EventBody);
        Assert.NotNull(eventData.UserMetadata);
        Assert.NotEmpty(eventData.UserMetadata);

        MoneyDeposited reconstructedEvent = DomainEventMapper.ToDomain<MoneyDeposited>(eventData);

        // Verify metadata preserved through round-trip
        Assert.Equal("admin@example.com", reconstructedEvent.Metadata["User"]);
        Assert.Equal("sess-99999", reconstructedEvent.Metadata["SessionId"]);
        Assert.Equal("Windows", reconstructedEvent.Metadata["Platform"]);
        Assert.Equal("Chrome", reconstructedEvent.Metadata["Browser"]);
    }

    [Fact]
    public void InternalDomainEventDto_ConvertsToJsonFriendlyFormat()
    {
        Dictionary<string, string> metadata = new() { ["Channel"] = "Web" };

        AccountCreated @event = new
        (
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            new AccountId("acc-003"),
            "Jane Doe",
            new Money(5000m)
        ) { Metadata = new ReadOnlyDictionary<string, string>(metadata) };

        var eventData = new { AccountId = @event.AccountId.Value, @event.Owner, InitialBalance = @event.InitialBalance.Amount };
        string jsonEvent = JsonSerializer.Serialize(eventData);

        InternalDomainEventDto dto = new()
        {
            Id = @event.Id,
            OccurredOn = @event.OccurredOn,
            BoundedContext = "banking", // Bounded context for event routing
            EventSimpleName = DomainEventTypeMapper.GetTypeName(@event),
            JsonEvent = jsonEvent,
            Metadata = @event.Metadata.ToDictionary(kv => kv.Key, kv => kv.Value) // String-to-string dictionary
        };

        Assert.Equal(@event.Id, dto.Id);
        Assert.Equal(@event.OccurredOn, dto.OccurredOn);
        Assert.Equal("banking", dto.BoundedContext);
        Assert.NotNull(dto.EventSimpleName);
        Assert.NotEmpty(dto.EventSimpleName);

        // Verify metadata (string-to-string)
        Assert.NotNull(dto.Metadata);
        Assert.Equal("Web", dto.Metadata["Channel"]);

        // Verify JsonEvent is string containing JSON
        Assert.NotNull(dto.JsonEvent);
        Assert.Contains("AccountId", dto.JsonEvent);
        Assert.Contains("Jane Doe", dto.JsonEvent);
        Assert.Contains("5000", dto.JsonEvent);

        // Verify JSON serialization works (cross-platform compatible)
        string json = JsonSerializer.Serialize(dto);
        Assert.NotNull(json);
        Assert.Contains("\"Id\":", json);
        Assert.Contains("\"BoundedContext\":\"banking\"", json);
        Assert.Contains("\"EventSimpleName\":", json);
        Assert.Contains("\"JsonEvent\":", json);

        // Verify no C#-only fields present
        Assert.DoesNotContain("\"Source\":", json);
        Assert.DoesNotContain("\"EventData\":", json);
        Assert.DoesNotContain("\"EventType\":", json);
    }

    [Fact]
    public void EventStoreData_HasCorrectPersistenceFormat()
    {
        AccountId accountId = new("acc-004");
        BankAccount account = new(accountId, "Bob Smith", new Money(1000m));
        account.Deposit(new Money(500m));
        account.Withdraw(new Money(200m));

        List<IInternalDomainEvent> events = account.GetDomainEvents().ToList();
        EventStoreData<AccountId> eventStoreData = new()
        {
            Id = accountId, StreamName = $"{account.GetCategory()}-{accountId.Value}", Events = events, Version = account.Version
        };

        Assert.Equal(accountId, eventStoreData.Id);
        Assert.Equal("account-acc-004", eventStoreData.StreamName);
        Assert.Equal(3, eventStoreData.Events.Count); // Created + Deposited + Withdrawn
        Assert.Equal(2L, eventStoreData.Version); // Version after 3 events (starts at -1)

        // Verify events in order
        Assert.IsType<AccountCreated>(eventStoreData.Events[0]);
        Assert.IsType<MoneyDeposited>(eventStoreData.Events[1]);
        Assert.IsType<MoneyWithdrawn>(eventStoreData.Events[2]);
    }

    [Fact]
    public void DomainEventTypeMapper_BidirectionalMapping_WorksCorrectly()
    {
        Type accountCreatedType = typeof(AccountCreated);
        Type moneyDepositedType = typeof(MoneyDeposited);

        string accountCreatedName = DomainEventTypeMapper.GetTypeName(accountCreatedType);
        string moneyDepositedName = DomainEventTypeMapper.GetTypeName(moneyDepositedType);

        Assert.NotNull(accountCreatedName);
        Assert.NotNull(moneyDepositedName);
        Assert.NotEmpty(accountCreatedName);
        Assert.NotEmpty(moneyDepositedName);

        Type retrievedAccountCreatedType = DomainEventTypeMapper.GetType(accountCreatedName);
        Type retrievedMoneyDepositedType = DomainEventTypeMapper.GetType(moneyDepositedName);

        Assert.Equal(accountCreatedType, retrievedAccountCreatedType);
        Assert.Equal(moneyDepositedType, retrievedMoneyDepositedType);
    }

    // Test domain: External domain event example
    private sealed record PaymentReceived
    (
        Guid Id,
        DateTimeOffset OccurredOn,
        string PaymentId,
        decimal Amount
    ) : IExternalDomainEvent
    {
        public string Source => "PaymentService";

        public IReadOnlyDictionary<string, string> Metadata { get; } =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
    }
}