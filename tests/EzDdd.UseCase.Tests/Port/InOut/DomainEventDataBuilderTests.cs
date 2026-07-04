using System.Text.Json;
using EzDdd.UseCase.Port.InOut;

namespace EzDdd.UseCase.Tests.Port.InOut;

/// <summary>
///     Unit tests for <see cref="DomainEventDataBuilder" />.
/// </summary>
public class DomainEventDataBuilderTests
{
    /// <summary>
    ///     Test helper record for payload serialization.
    /// </summary>
    private record TestPayload(int Amount, string Currency);

    #region Factory Methods Tests

    [Fact]
    public void Json_ShouldCreateBuilderWithJsonPayload()
    {
        TestPayload payload = new(100, "USD");

        DomainEventData result = DomainEventDataBuilder.Json("TestEvent", payload).Build();

        Assert.Equal("TestEvent", result.EventType);
        Assert.NotEmpty(result.EventBody);
        // Verify payload can be deserialized back
        TestPayload? deserializedPayload = JsonSerializer.Deserialize<TestPayload>(result.EventBody);
        Assert.NotNull(deserializedPayload);
        Assert.Equal(100, deserializedPayload.Amount);
        Assert.Equal("USD", deserializedPayload.Currency);
    }

    [Fact]
    public void Json_ShouldAutomaticallySetContentTypeToApplicationJson()
    {
        var payload = new { Value = "test" };

        DomainEventData result = DomainEventDataBuilder.Json("TestEvent", payload).Build();

        Assert.Equal("application/json", result.ContentType);
    }

    [Fact]
    public void Binary_ShouldCreateBuilderWithBinaryPayload()
    {
        byte[] binaryPayload = [0x01, 0x02, 0x03, 0x04];

        DomainEventData result = DomainEventDataBuilder.Binary("BinaryEvent", binaryPayload).Build();

        Assert.Equal("BinaryEvent", result.EventType);
        Assert.Equal(binaryPayload, result.EventBody);
    }

    [Fact]
    public void Binary_ShouldSetContentTypeToOctetStream()
    {
        byte[] binaryPayload = [0xFF, 0xFE];

        DomainEventData result = DomainEventDataBuilder.Binary("BinaryEvent", binaryPayload).Build();

        Assert.Equal("application/octet-stream", result.ContentType);
    }

    #endregion

    #region Fluent API Tests

    [Fact]
    public void EventId_ShouldSetCustomEventId()
    {
        Guid customEventId = Guid.NewGuid();

        DomainEventData result = DomainEventDataBuilder
            .Json("TestEvent", new { Value = "test" })
            .EventId(customEventId)
            .Build();

        Assert.Equal(customEventId, result.Id);
    }

    [Fact]
    public void MetadataAsJson_ShouldSerializeMetadataToJson()
    {
        Dictionary<string, string> metadata = new() { ["CorrelationId"] = "123", ["UserId"] = "user@example.com" };

        DomainEventData result = DomainEventDataBuilder
            .Json("TestEvent", new { Value = "test" })
            .MetadataAsJson(metadata)
            .Build();

        Dictionary<string, string>? deserializedMetadata = JsonSerializer.Deserialize<Dictionary<string, string>>(
            result.UserMetadata
        );
        Assert.NotNull(deserializedMetadata);
        Assert.Equal("123", deserializedMetadata["CorrelationId"]);
        Assert.Equal("user@example.com", deserializedMetadata["UserId"]);
    }

    [Fact]
    public void MetadataAsBytes_ShouldSetRawMetadataBytes()
    {
        byte[] metadataBytes = "{\"key\":\"value\"}"u8.ToArray();

        DomainEventData result = DomainEventDataBuilder
            .Json("TestEvent", new { Value = "test" })
            .MetadataAsBytes(metadataBytes)
            .Build();

        Assert.Equal(metadataBytes, result.UserMetadata);
    }

    #endregion

    #region Build Method Tests

    [Fact]
    public void Build_ShouldAutoGenerateEventIdIfNotSet()
    {
        DomainEventData result = DomainEventDataBuilder.Json("TestEvent", new { Value = "test" }).Build();

        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public void Build_ShouldUseEmptyJsonForMetadataIfNotSet()
    {
        DomainEventData result = DomainEventDataBuilder.Json("TestEvent", new { Value = "test" }).Build();

        Assert.Equal("{}"u8.ToArray(), result.UserMetadata);
    }

    [Fact]
    public void Build_ShouldGenerateUniqueEventIdsForMultipleCalls()
    {
        DomainEventData result1 = DomainEventDataBuilder.Json("TestEvent", new { Value = "test1" }).Build();

        DomainEventData result2 = DomainEventDataBuilder.Json("TestEvent", new { Value = "test2" }).Build();

        // Each build should generate unique event IDs
        Assert.NotEqual(Guid.Empty, result1.Id);
        Assert.NotEqual(Guid.Empty, result2.Id);
        Assert.NotEqual(result1.Id, result2.Id);
    }

    [Fact]
    public void Build_ShouldConstructValidDomainEventData()
    {
        Guid customEventId = Guid.NewGuid();
        TestPayload payload = new(500, "EUR");
        Dictionary<string, object> metadata = new() { ["TraceId"] = "trace-123" };

        DomainEventData result = DomainEventDataBuilder
            .Json("PaymentReceived", payload)
            .EventId(customEventId)
            .MetadataAsJson(metadata)
            .Build();

        Assert.Equal(customEventId, result.Id);
        Assert.Equal("PaymentReceived", result.EventType);
        Assert.Equal("application/json", result.ContentType);
        Assert.NotEmpty(result.EventBody);
        Assert.NotEmpty(result.UserMetadata);
        // Verify all fields are populated
        TestPayload? deserializedPayload = JsonSerializer.Deserialize<TestPayload>(result.EventBody);
        Assert.NotNull(deserializedPayload);
        Assert.Equal(500, deserializedPayload.Amount);
        Assert.Equal("EUR", deserializedPayload.Currency);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Builder_ShouldSupportFullFluentChain()
    {
        Guid eventId = Guid.NewGuid();
        var payload = new { OrderId = 1, Amount = 250.50m };
        Dictionary<string, string> metadata = new() { ["CorrelationId"] = "corr-456", ["UserId"] = "user-789" };

        DomainEventData result = DomainEventDataBuilder
            .Json("OrderCreated", payload)
            .EventId(eventId)
            .MetadataAsJson(metadata)
            .Build();

        Assert.Equal(eventId, result.Id);
        Assert.Equal("OrderCreated", result.EventType);
        Assert.Equal("application/json", result.ContentType);
        Assert.NotEmpty(result.EventBody);

        Dictionary<string, string>? deserializedMetadata = JsonSerializer.Deserialize<Dictionary<string, string>>(
            result.UserMetadata
        );
        Assert.NotNull(deserializedMetadata);
        Assert.Equal("corr-456", deserializedMetadata["CorrelationId"]);
        Assert.Equal("user-789", deserializedMetadata["UserId"]);
    }

    [Fact]
    public void Builder_ShouldProduceSameResultAsDirectConstruction()
    {
        Guid eventId = Guid.NewGuid();
        TestPayload payload = new(1000, "GBP");
        Dictionary<string, string> metadata = new() { ["key"] = "value" };

        // Act - using builder
        DomainEventData builderResult = DomainEventDataBuilder
            .Json("TestEvent", payload)
            .EventId(eventId)
            .MetadataAsJson(metadata)
            .Build();

        // Act - using direct construction (old way)
        DomainEventData directResult = new(
            eventId,
            "TestEvent",
            "application/json",
            JsonSerializer.SerializeToUtf8Bytes(payload),
            JsonSerializer.SerializeToUtf8Bytes(metadata)
        );

        // should be equal
        Assert.Equal(directResult.Id, builderResult.Id);
        Assert.Equal(directResult.EventType, builderResult.EventType);
        Assert.Equal(directResult.ContentType, builderResult.ContentType);
        Assert.Equal(directResult.EventBody, builderResult.EventBody);
        Assert.Equal(directResult.UserMetadata, builderResult.UserMetadata);
        // Record equality should work
        Assert.Equal(directResult, builderResult);
    }

    #endregion

    #region Null Safety Tests

    [Fact]
    public void Json_ShouldThrowArgumentNullExceptionWhenEventTypeIsNull()
    {
        string? nullEventType = null;
        var payload = new { Value = "test" };

        Assert.Throws<ArgumentNullException>(() => DomainEventDataBuilder.Json(nullEventType!, payload));
    }

    [Fact]
    public void Json_ShouldThrowArgumentNullExceptionWhenPayloadIsNull()
    {
        TestPayload? nullPayload = null;

        Assert.Throws<ArgumentNullException>(() => DomainEventDataBuilder.Json("TestEvent", nullPayload!));
    }

    [Fact]
    public void Binary_ShouldThrowArgumentNullExceptionWhenEventTypeIsNull()
    {
        string? nullEventType = null;
        byte[] payload = [0x01, 0x02];

        Assert.Throws<ArgumentNullException>(() => DomainEventDataBuilder.Binary(nullEventType!, payload));
    }

    [Fact]
    public void Binary_ShouldThrowArgumentNullExceptionWhenPayloadIsNull()
    {
        byte[]? nullPayload = null;

        Assert.Throws<ArgumentNullException>(() => DomainEventDataBuilder.Binary("TestEvent", nullPayload!));
    }

    [Fact]
    public void MetadataAsJson_ShouldThrowArgumentNullExceptionWhenMetadataIsNull()
    {
        DomainEventDataBuilder builder = DomainEventDataBuilder.Json("TestEvent", new { Value = "test" });
        Dictionary<string, string>? nullMetadata = null;

        Assert.Throws<ArgumentNullException>(() => builder.MetadataAsJson(nullMetadata!));
    }

    [Fact]
    public void MetadataAsBytes_ShouldThrowArgumentNullExceptionWhenMetadataIsNull()
    {
        DomainEventDataBuilder builder = DomainEventDataBuilder.Json("TestEvent", new { Value = "test" });
        byte[]? nullMetadata = null;

        Assert.Throws<ArgumentNullException>(() => builder.MetadataAsBytes(nullMetadata!));
    }

    #endregion
}
