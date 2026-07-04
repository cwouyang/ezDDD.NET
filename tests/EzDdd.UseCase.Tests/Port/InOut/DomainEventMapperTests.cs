using System.Text.Json;
using EzDdd.Entity;
using EzDdd.UseCase.Port.InOut;

namespace EzDdd.UseCase.Tests.Port.InOut;

public class DomainEventMapperTests
{
    #region Setup

    public DomainEventMapperTests()
    {
        // Register event types for testing
        // Note: DomainEventTypeMapper.Register is idempotent
        DomainEventTypeMapper.Register<TestAccountCreated>("TestAccountCreated");
        DomainEventTypeMapper.Register<TestMoneyDeposited>("TestMoneyDeposited");
    }

    #endregion

    #region ToData Conversion Tests (Domain → Data)

    [Fact]
    public void ToData_ShouldConvertDomainEventToDomainEventData()
    {
        TestAccountCreated @event = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "account-123",
            new Dictionary<string, string> { ["userId"] = "user-1" },
            "John Doe",
            1000m
        );

        DomainEventData data = DomainEventMapper.ToData(@event);

        Assert.NotNull(data);
        Assert.Equal(@event.Id, data.Id);
        Assert.Equal("TestAccountCreated", data.EventType);
        Assert.Equal("application/json", data.ContentType);
        Assert.NotEmpty(data.EventBody);
        Assert.NotEmpty(data.UserMetadata);
    }

    [Fact]
    public void ToData_ShouldSerializeEventBodyCorrectly()
    {
        TestMoneyDeposited @event = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "account-123",
            new Dictionary<string, string>(),
            500m
        );

        DomainEventData data = DomainEventMapper.ToData(@event);
        TestMoneyDeposited? deserializedEvent = JsonSerializer.Deserialize<TestMoneyDeposited>(data.EventBody);

        Assert.NotNull(deserializedEvent);
        Assert.Equal(@event.Id, deserializedEvent.Id);
        Assert.Equal(@event.Amount, deserializedEvent.Amount);
        Assert.Equal(@event.Source, deserializedEvent.Source);
    }

    [Fact]
    public void ToData_ShouldSerializeMetadataCorrectly()
    {
        Dictionary<string, string> metadata = new() { ["userId"] = "user-1", ["correlationId"] = "corr-123" };

        TestAccountCreated @event = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "account-123",
            metadata,
            "Jane Doe",
            2000m
        );

        DomainEventData data = DomainEventMapper.ToData(@event);
        Dictionary<string, string>? deserializedMetadata = JsonSerializer.Deserialize<Dictionary<string, string>>(
            data.UserMetadata
        );

        Assert.NotNull(deserializedMetadata);
        Assert.Equal(2, deserializedMetadata.Count);
        Assert.Equal("user-1", deserializedMetadata["userId"]);
        Assert.Equal("corr-123", deserializedMetadata["correlationId"]);
    }

    [Fact]
    public void ToData_WithEmptyMetadata_ShouldWork()
    {
        TestMoneyDeposited @event = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "account-123",
            new Dictionary<string, string>(),
            100m
        );

        DomainEventData data = DomainEventMapper.ToData(@event);

        Assert.NotNull(data);
        Assert.NotEmpty(data.UserMetadata); // Empty dict still serializes to "{}"
    }

    [Fact]
    public void ToData_WithComplexNestedObject_ShouldSerializeCorrectly()
    {
        TestAccountCreated @event = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "account-789",
            new Dictionary<string, string> { ["region"] = "US-WEST" },
            "Charlie Brown",
            10000m
        );

        DomainEventData data = DomainEventMapper.ToData(@event);
        TestAccountCreated reconstructedEvent = DomainEventMapper.ToDomain<TestAccountCreated>(data);

        Assert.NotNull(reconstructedEvent);
        Assert.Equal(@event.Owner, reconstructedEvent.Owner);
        Assert.Equal(@event.InitialBalance, reconstructedEvent.InitialBalance);
    }

    [Fact]
    public void ToData_EmptyCollection_ShouldReturnEmptyList()
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        List<IInternalDomainEvent> emptyEvents = [];

        IReadOnlyList<DomainEventData> dataList = DomainEventMapper.ToData(emptyEvents);

        Assert.NotNull(dataList);
        Assert.Empty(dataList);
    }

    #endregion

    #region ToDomain Conversion Tests (Data → Domain)

    [Fact]
    public void ToDomain_ShouldConvertDomainEventDataToDomainEvent()
    {
        TestMoneyDeposited originalEvent = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "account-123",
            new Dictionary<string, string> { ["userId"] = "user-1" },
            750m
        );

        DomainEventData data = DomainEventMapper.ToData(originalEvent);

        TestMoneyDeposited reconstructedEvent = DomainEventMapper.ToDomain<TestMoneyDeposited>(data);

        Assert.NotNull(reconstructedEvent);
        Assert.Equal(originalEvent.Id, reconstructedEvent.Id);
        Assert.Equal(originalEvent.Amount, reconstructedEvent.Amount);
        Assert.Equal(originalEvent.Source, reconstructedEvent.Source);
    }

    [Fact]
    public void ToDomain_EmptyCollection_ShouldReturnEmptyList()
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        List<DomainEventData> emptyDataList = [];

        IReadOnlyList<TestMoneyDeposited> events = DomainEventMapper.ToDomain<TestMoneyDeposited>(emptyDataList);

        Assert.NotNull(events);
        Assert.Empty(events);
    }

    #endregion

    #region Batch Conversion Tests

    [Fact]
    public void ToData_BatchConversion_ShouldConvertMultipleEvents()
    {
        List<IInternalDomainEvent> events =
        [
            new TestAccountCreated(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "account-1",
                new Dictionary<string, string>(),
                "Alice",
                1000m
            ),
            new TestMoneyDeposited(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "account-1",
                new Dictionary<string, string>(),
                500m
            ),
            new TestMoneyDeposited(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "account-1",
                new Dictionary<string, string>(),
                300m
            ),
        ];

        IReadOnlyList<DomainEventData> dataList = DomainEventMapper.ToData(events);

        Assert.NotNull(dataList);
        Assert.Equal(3, dataList.Count);
        Assert.Equal("TestAccountCreated", dataList[0].EventType);
        Assert.Equal("TestMoneyDeposited", dataList[1].EventType);
        Assert.Equal("TestMoneyDeposited", dataList[2].EventType);
    }

    [Fact]
    public void ToDomain_BatchConversion_ShouldConvertMultipleEventData()
    {
        List<IInternalDomainEvent> originalEvents =
        [
            new TestMoneyDeposited(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "account-1",
                new Dictionary<string, string>(),
                100m
            ),
            new TestMoneyDeposited(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                "account-1",
                new Dictionary<string, string>(),
                200m
            ),
        ];

        IReadOnlyList<DomainEventData> dataList = DomainEventMapper.ToData(originalEvents);

        IReadOnlyList<TestMoneyDeposited> reconstructedEvents = DomainEventMapper.ToDomain<TestMoneyDeposited>(
            dataList
        );

        Assert.NotNull(reconstructedEvents);
        Assert.Equal(2, reconstructedEvents.Count);
        Assert.Equal(100m, reconstructedEvents[0].Amount);
        Assert.Equal(200m, reconstructedEvents[1].Amount);
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_ShouldPreserveEventData()
    {
        TestAccountCreated originalEvent = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "account-456",
            new Dictionary<string, string> { ["key"] = "value" },
            "Bob Smith",
            5000m
        );

        DomainEventData data = DomainEventMapper.ToData(originalEvent);
        TestAccountCreated reconstructedEvent = DomainEventMapper.ToDomain<TestAccountCreated>(data);

        Assert.Equal(originalEvent.Id, reconstructedEvent.Id);
        Assert.Equal(originalEvent.Source, reconstructedEvent.Source);
        Assert.Equal(originalEvent.Owner, reconstructedEvent.Owner);
        Assert.Equal(originalEvent.InitialBalance, reconstructedEvent.InitialBalance);
        Assert.Equal(originalEvent.Metadata["key"], reconstructedEvent.Metadata["key"]);
    }

    #endregion

    #region Test Event Definitions

    // Test internal domain event
    private sealed record TestAccountCreated(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata,
        string Owner,
        decimal InitialBalance
    ) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent;

    private sealed record TestMoneyDeposited(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata,
        decimal Amount
    ) : IInternalDomainEvent;

    #endregion
}
