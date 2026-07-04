using System.Text.Json;
using EzDdd.UseCase.Port.InOut;

namespace EzDdd.UseCase.Tests.Port.InOut;

public class InternalDomainEventDtoTests
{
    #region Construction and Properties

    [Fact]
    public void InternalDomainEventDto_Creation_ShouldSetAllProperties()
    {
        Guid id = Guid.NewGuid();
        DateTimeOffset occurredOn = DateTimeOffset.UtcNow;
        const string boundedContext = "banking";
        const string eventSimpleName = "MoneyDeposited";
        const string jsonEvent = "{\"aggregateId\":\"account-123\",\"amount\":100}";
        Dictionary<string, string> metadata = new() { ["userId"] = "user-1" };

        InternalDomainEventDto dto = new()
        {
            Id = id,
            OccurredOn = occurredOn,
            BoundedContext = boundedContext,
            EventSimpleName = eventSimpleName,
            JsonEvent = jsonEvent,
            Metadata = metadata,
        };

        Assert.Equal(id, dto.Id);
        Assert.Equal(occurredOn, dto.OccurredOn);
        Assert.Equal(boundedContext, dto.BoundedContext);
        Assert.Equal(eventSimpleName, dto.EventSimpleName);
        Assert.Equal(jsonEvent, dto.JsonEvent);
        Assert.Equal(metadata, dto.Metadata);
    }

    [Fact]
    public void InternalDomainEventDto_DefaultValues_ShouldBeInitialized()
    {
        InternalDomainEventDto dto = new();

        Assert.Equal(Guid.Empty, dto.Id);
        Assert.Equal(default, dto.OccurredOn);
        Assert.Equal(string.Empty, dto.BoundedContext);
        Assert.Equal(string.Empty, dto.EventSimpleName);
        Assert.Equal(string.Empty, dto.JsonEvent);
        Assert.NotNull(dto.Metadata);
        Assert.Empty(dto.Metadata);
    }

    [Fact]
    public void InternalDomainEventDto_PropertiesAreMutable()
    {
        InternalDomainEventDto dto = new()
        {
            Id = Guid.NewGuid(),
            EventSimpleName = "InitialType",
            BoundedContext = "initialContext",
        };

        Guid newId = Guid.NewGuid();

        dto.Id = newId;
        dto.EventSimpleName = "UpdatedType";
        dto.BoundedContext = "updatedContext";

        Assert.Equal(newId, dto.Id);
        Assert.Equal("UpdatedType", dto.EventSimpleName);
        Assert.Equal("updatedContext", dto.BoundedContext);
    }

    #endregion

    #region JSON Serialization

    [Fact]
    public void InternalDomainEventDto_JsonSerialization_ShouldWork()
    {
        InternalDomainEventDto dto = new()
        {
            Id = Guid.Parse("12345678-1234-1234-1234-123456789012"),
            OccurredOn = new DateTimeOffset(2025, 11, 5, 10, 30, 0, TimeSpan.Zero),
            BoundedContext = "banking",
            EventSimpleName = "AccountCreated",
            JsonEvent = "{\"owner\":\"John Doe\",\"balance\":1000}",
            Metadata = new Dictionary<string, string> { ["key"] = "value" },
        };

        string json = JsonSerializer.Serialize(dto);

        Assert.NotNull(json);
        Assert.Contains("\"Id\":", json);
        Assert.Contains("\"EventSimpleName\":\"AccountCreated\"", json);
        Assert.Contains("\"BoundedContext\":\"banking\"", json);
    }

    [Fact]
    public void InternalDomainEventDto_JsonDeserialization_ShouldWork()
    {
        string json =
            "{\n"
            + "    \"Id\": \"12345678-1234-1234-1234-123456789012\",\n"
            + "    \"OccurredOn\": \"2025-11-05T10:30:00+00:00\",\n"
            + "    \"BoundedContext\": \"banking\",\n"
            + "    \"EventSimpleName\": \"MoneyWithdrawn\",\n"
            + "    \"JsonEvent\": \"{\\\"aggregateId\\\": \\\"account-789\\\", \\\"amount\\\": 500}\",\n"
            + "    \"Metadata\": { \"userId\": \"user-1\" }\n"
            + "}";

        InternalDomainEventDto? dto = JsonSerializer.Deserialize<InternalDomainEventDto>(json);

        Assert.NotNull(dto);
        Assert.Equal(Guid.Parse("12345678-1234-1234-1234-123456789012"), dto.Id);
        Assert.Equal("banking", dto.BoundedContext);
        Assert.Equal("MoneyWithdrawn", dto.EventSimpleName);
        Assert.NotNull(dto.JsonEvent);
        Assert.Contains("account-789", dto.JsonEvent);
    }

    [Fact]
    public void InternalDomainEventDto_RoundTripJsonConversion_ShouldPreserveData()
    {
        InternalDomainEventDto original = new()
        {
            Id = Guid.NewGuid(),
            OccurredOn = DateTimeOffset.UtcNow,
            BoundedContext = "inventory",
            EventSimpleName = "AccountClosed",
            JsonEvent = "{\"aggregateId\":\"account-999\",\"reason\":\"Customer request\"}",
            Metadata = new Dictionary<string, string> { ["correlationId"] = "corr-123" },
        };

        string json = JsonSerializer.Serialize(original);
        InternalDomainEventDto? deserialized = JsonSerializer.Deserialize<InternalDomainEventDto>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.BoundedContext, deserialized.BoundedContext);
        Assert.Equal(original.EventSimpleName, deserialized.EventSimpleName);
        Assert.Equal(original.JsonEvent, deserialized.JsonEvent);
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void InternalDomainEventDto_WithNestedJsonInJsonEvent_ShouldSerialize()
    {
        var eventData = new
        {
            aggregateId = "order-123",
            items = new[] { new { productId = "p1", quantity = 2 }, new { productId = "p2", quantity = 1 } },
        };
        string jsonEvent = JsonSerializer.Serialize(eventData);

        InternalDomainEventDto dto = new()
        {
            Id = Guid.NewGuid(),
            OccurredOn = DateTimeOffset.UtcNow,
            BoundedContext = "sales",
            EventSimpleName = "OrderCreated",
            JsonEvent = jsonEvent,
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal),
        };

        string json = JsonSerializer.Serialize(dto);
        InternalDomainEventDto? deserialized = JsonSerializer.Deserialize<InternalDomainEventDto>(json);

        Assert.NotNull(json);
        Assert.NotNull(deserialized);
        Assert.Equal(dto.EventSimpleName, deserialized.EventSimpleName);
        Assert.Contains("order-123", deserialized.JsonEvent);
    }

    [Fact]
    public void InternalDomainEventDto_WithEmptyCollections_ShouldWork()
    {
        InternalDomainEventDto dto = new()
        {
            Id = Guid.NewGuid(),
            OccurredOn = DateTimeOffset.UtcNow,
            BoundedContext = "test",
            EventSimpleName = "TestEvent",
            JsonEvent = "{}",
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal),
        };

        Assert.NotNull(dto);
        Assert.Empty(dto.Metadata);
        Assert.Equal("{}", dto.JsonEvent);

        // Serialize and deserialize
        string json = JsonSerializer.Serialize(dto);
        InternalDomainEventDto? deserialized = JsonSerializer.Deserialize<InternalDomainEventDto>(json);

        Assert.NotNull(deserialized);
        Assert.Empty(deserialized.Metadata);
        Assert.Equal("{}", deserialized.JsonEvent);
    }

    [Fact]
    public void InternalDomainEventDto_WithComplexJsonEvent_ShouldPreserveStructure()
    {
        var complexEvent = new
        {
            aggregateId = "account-456",
            amount = 1000m,
            currency = "USD",
            timestamp = "2025-11-10T10:00:00Z",
            metadata = new { source = "mobile-app", version = "1.0.0" },
        };
        string jsonEvent = JsonSerializer.Serialize(complexEvent);

        InternalDomainEventDto dto = new()
        {
            Id = Guid.NewGuid(),
            OccurredOn = DateTimeOffset.UtcNow,
            BoundedContext = "banking",
            EventSimpleName = "MoneyDeposited",
            JsonEvent = jsonEvent,
            Metadata = new Dictionary<string, string> { ["userId"] = "user-123" },
        };

        string serialized = JsonSerializer.Serialize(dto);
        InternalDomainEventDto? deserialized = JsonSerializer.Deserialize<InternalDomainEventDto>(serialized);

        Assert.NotNull(deserialized);
        Assert.Equal(dto.JsonEvent, deserialized.JsonEvent);

        // Parse JsonEvent to verify structure preserved
        Dictionary<string, JsonElement>? parsedEvent = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            deserialized.JsonEvent
        );
        Assert.NotNull(parsedEvent);
        Assert.Equal("account-456", parsedEvent["aggregateId"].GetString());
        Assert.Equal(1000m, parsedEvent["amount"].GetDecimal());
    }

    #endregion

    #region Specific Features

    [Fact]
    public void InternalDomainEventDto_Metadata_ShouldOnlyAcceptStringValues()
    {
        InternalDomainEventDto dto = new()
        {
            Id = Guid.NewGuid(),
            OccurredOn = DateTimeOffset.UtcNow,
            BoundedContext = "test",
            EventSimpleName = "TestEvent",
            JsonEvent = "{}",
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = "user-1",
                ["correlationId"] = "corr-123",
                ["causationId"] = "cause-456",
            },
        };

        Assert.All(dto.Metadata.Values, value => Assert.IsType<string>(value));
    }

    [Fact]
    public void InternalDomainEventDto_CrossPlatformCompatibility_ShouldMatchJavaStructure()
    {
        InternalDomainEventDto dto = new()
        {
            Id = Guid.NewGuid(),
            OccurredOn = DateTimeOffset.UtcNow,
            BoundedContext = "banking",
            EventSimpleName = "AccountCreated",
            JsonEvent = "{\"aggregateId\":\"account-123\",\"owner\":\"John Doe\"}",
            Metadata = new Dictionary<string, string> { ["userId"] = "user-1" },
        };

        string json = JsonSerializer.Serialize(dto);

        Assert.Contains("\"Id\":", json);
        Assert.Contains("\"OccurredOn\":", json);
        Assert.Contains("\"BoundedContext\":", json);
        Assert.Contains("\"EventSimpleName\":", json);
        Assert.Contains("\"JsonEvent\":", json);
        Assert.Contains("\"Metadata\":", json);

        // Verify no C#-only fields present
        Assert.DoesNotContain("\"Source\":", json);
        Assert.DoesNotContain("\"EventData\":", json);
        Assert.DoesNotContain("\"EventType\":", json);
    }

    #endregion
}
