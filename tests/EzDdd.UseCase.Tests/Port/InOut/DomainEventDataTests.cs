using System.Diagnostics;
using System.Text;

using EzDdd.UseCase.Port.InOut;

namespace EzDdd.UseCase.Tests.Port.InOut;

public class DomainEventDataTests
{
#region Test Fixtures

    private readonly Guid _eventId = Guid.NewGuid();
    private readonly byte[] _sampleEventBody = "{\"amount\":100}"u8.ToArray();
    private readonly byte[] _sampleMetadata = "{\"user\":\"admin\"}"u8.ToArray();

#endregion

#region Creation Tests

    [Fact]
    public void DomainEventData_Creation_ShouldSetAllProperties()
    {
        Guid id = _eventId;
        const string eventType = "MoneyDeposited";
        const string contentType = "application/json";
        byte[] eventBody = _sampleEventBody;
        byte[] metadata = _sampleMetadata;

        DomainEventData data = new(id, eventType, contentType, eventBody, metadata);

        Assert.Equal(id, data.Id);
        Assert.Equal(eventType, data.EventType);
        Assert.Equal(contentType, data.ContentType);
        Assert.Equal(eventBody, data.EventBody);
        Assert.Equal(metadata, data.UserMetadata);
    }

#endregion

#region Equality Tests - Basic Scenarios

    [Fact]
    public void DomainEventData_Equals_ShouldReturnTrueForSameData()
    {
        DomainEventData data1 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            _sampleEventBody,
            _sampleMetadata
        );

        DomainEventData data2 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            _sampleEventBody,
            _sampleMetadata
        );

        Assert.Equal(data1, data2);
        Assert.True(data1.Equals(data2));
    }

    [Fact]
    public void DomainEventData_Equals_ShouldReturnFalseForDifferentEventBody()
    {
        DomainEventData data1 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            "{\"amount\":100}"u8.ToArray(),
            _sampleMetadata
        );

        DomainEventData data2 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            "{\"amount\":200}"u8.ToArray(), // Different content
            _sampleMetadata
        );

        Assert.NotEqual(data1, data2);
        Assert.False(data1.Equals(data2));
    }

    [Fact]
    public void DomainEventData_Equals_ShouldReturnFalseForDifferentMetadata()
    {
        DomainEventData data1 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            _sampleEventBody,
            "{\"user\":\"admin\"}"u8.ToArray()
        );

        DomainEventData data2 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            _sampleEventBody,
            "{\"user\":\"guest\"}"u8.ToArray() // Different metadata
        );

        Assert.NotEqual(data1, data2);
        Assert.False(data1.Equals(data2));
    }

#endregion

#region HashCode Tests - Basic Scenarios

    [Fact]
    public void DomainEventData_HashCode_ShouldBeConsistentForSameData()
    {
        DomainEventData data1 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            _sampleEventBody,
            _sampleMetadata
        );

        DomainEventData data2 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            _sampleEventBody,
            _sampleMetadata
        );

        int hash1 = data1.GetHashCode();
        int hash2 = data2.GetHashCode();

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void DomainEventData_HashCode_ShouldBeDifferentForDifferentId()
    {
        DomainEventData data1 = new
        (
            Guid.NewGuid(),
            "MoneyDeposited",
            "application/json",
            "{\"amount\":100}"u8.ToArray(),
            _sampleMetadata
        );

        DomainEventData data2 = new
        (
            Guid.NewGuid(),
            "MoneyDeposited",
            "application/json",
            "{\"amount\":100}"u8.ToArray(),
            _sampleMetadata
        );

        int hash1 = data1.GetHashCode();
        int hash2 = data2.GetHashCode();

        Assert.NotEqual(hash1, hash2);
    }

#endregion

#region Equality Tests - Advanced Scenarios

    [Fact]
    public void DomainEventData_Equals_ShouldHandleNullCorrectly()
    {
        DomainEventData? data = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            _sampleEventBody,
            _sampleMetadata
        );

        Assert.False(data.Equals(null));
    }

    [Fact]
    public void DomainEventData_Equals_ShouldHandleSelfReference()
    {
        DomainEventData data = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            _sampleEventBody,
            _sampleMetadata
        );

        Assert.True(data.Equals(data));
        Assert.Equal(data, data);
    }

    [Fact]
    public void DomainEventData_Equals_ShouldCompareByteArrayContent_NotReference()
    {
        byte[] eventBody1 = "{\"amount\":100}"u8.ToArray();
        byte[] eventBody2 = "{\"amount\":100}"u8.ToArray();
        byte[] metadata1 = "{\"user\":\"admin\"}"u8.ToArray();
        byte[] metadata2 = "{\"user\":\"admin\"}"u8.ToArray();

        DomainEventData data1 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            eventBody1,
            metadata1
        );

        DomainEventData data2 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            eventBody2,
            metadata2
        );

        Assert.NotSame(eventBody1, eventBody2);
        Assert.NotSame(metadata1, metadata2);
        Assert.Equal(data1, data2); // Should be equal by content
    }

    [Fact]
    public void DomainEventData_Equals_ShouldReturnFalseForDifferentId()
    {
        DomainEventData data1 = new
        (
            Guid.NewGuid(),
            "MoneyDeposited",
            "application/json",
            _sampleEventBody,
            _sampleMetadata
        );

        DomainEventData data2 = new
        (
            Guid.NewGuid(),
            "MoneyDeposited",
            "application/json",
            _sampleEventBody,
            _sampleMetadata
        );

        Assert.NotEqual(data1, data2);
    }

    [Fact]
    public void DomainEventData_Equals_ShouldReturnFalseForDifferentEventType()
    {
        DomainEventData data1 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            _sampleEventBody,
            _sampleMetadata
        );

        DomainEventData data2 = new
        (
            _eventId,
            "MoneyWithdrawn", // Different event type
            "application/json",
            _sampleEventBody,
            _sampleMetadata
        );

        Assert.NotEqual(data1, data2);
    }

    [Fact]
    public void DomainEventData_WithEmptyByteArrays_ShouldWork()
    {
        DomainEventData data = new
        (
            _eventId,
            "EmptyEvent",
            "application/json",
            [],
            []
        );

        Assert.NotNull(data);
        Assert.Empty(data.EventBody);
        Assert.Empty(data.UserMetadata);
    }

#endregion

#region JSON-Aware Equality Tests

    [Fact]
    public void Equals_WithSameJsonDifferentKeyOrder_ShouldReturnTrue()
    {
        byte[] eventBody1 = "{\"amount\":100,\"currency\":\"USD\"}"u8.ToArray();
        byte[] eventBody2 = "{\"currency\":\"USD\",\"amount\":100}"u8.ToArray();

        DomainEventData data1 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            eventBody1,
            _sampleMetadata
        );

        DomainEventData data2 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            eventBody2,
            _sampleMetadata
        );

        Assert.Equal(data1, data2);
        Assert.True(data1.Equals(data2));
    }

    [Fact]
    public void Equals_WithSameJsonDifferentWhitespace_ShouldReturnTrue()
    {
        byte[] eventBody1 = "{\"amount\":100}"u8.ToArray();
        byte[] eventBody2 = "{ \"amount\" : 100 }"u8.ToArray();

        DomainEventData data1 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            eventBody1,
            _sampleMetadata
        );

        DomainEventData data2 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            eventBody2,
            _sampleMetadata
        );

        Assert.Equal(data1, data2);
        Assert.True(data1.Equals(data2));
    }

    [Fact]
    public void Equals_WithDifferentJsonValues_ShouldReturnFalse()
    {
        byte[] eventBody1 = "{\"amount\":100}"u8.ToArray();
        byte[] eventBody2 = "{\"amount\":200}"u8.ToArray();

        DomainEventData data1 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            eventBody1,
            _sampleMetadata
        );

        DomainEventData data2 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            eventBody2,
            _sampleMetadata
        );

        Assert.NotEqual(data1, data2);
        Assert.False(data1.Equals(data2));
    }

    [Fact]
    public void Equals_WithNestedJsonDifferentKeyOrder_ShouldReturnTrue()
    {
        byte[] eventBody1 = "{\"user\":{\"name\":\"John\",\"age\":30}}"u8.ToArray();
        byte[] eventBody2 = "{\"user\":{\"age\":30,\"name\":\"John\"}}"u8.ToArray();

        DomainEventData data1 = new
        (
            _eventId,
            "UserCreated",
            "application/json",
            eventBody1,
            _sampleMetadata
        );

        DomainEventData data2 = new
        (
            _eventId,
            "UserCreated",
            "application/json",
            eventBody2,
            _sampleMetadata
        );

        Assert.Equal(data1, data2);
        Assert.True(data1.Equals(data2));
    }

    [Fact]
    public void Equals_WithJsonArrayDifferentOrder_ShouldReturnFalse()
    {
        byte[] eventBody1 = "{\"items\":[1,2,3]}"u8.ToArray();
        byte[] eventBody2 = "{\"items\":[3,2,1]}"u8.ToArray();

        DomainEventData data1 = new
        (
            _eventId,
            "OrderCreated",
            "application/json",
            eventBody1,
            _sampleMetadata
        );

        DomainEventData data2 = new
        (
            _eventId,
            "OrderCreated",
            "application/json",
            eventBody2,
            _sampleMetadata
        );

        Assert.NotEqual(data1, data2);
        Assert.False(data1.Equals(data2));
    }

    [Fact]
    public void Equals_WithNonJsonBytes_ShouldFallbackToByteComparison()
    {
        byte[] binaryData1 = [0x01, 0x02, 0x03, 0x04];
        byte[] binaryData2 = [0x01, 0x02, 0x03, 0x04];

        DomainEventData data1 = new
        (
            _eventId,
            "BinaryEvent",
            "application/octet-stream",
            binaryData1,
            []
        );

        DomainEventData data2 = new
        (
            _eventId,
            "BinaryEvent",
            "application/octet-stream",
            binaryData2,
            []
        );

        Assert.Equal(data1, data2);
        Assert.True(data1.Equals(data2));
    }

    [Fact]
    public void Equals_WithMetadataDifferentKeyOrder_ShouldReturnTrue()
    {
        byte[] metadata1 = "{\"user\":\"admin\",\"timestamp\":\"2024-01-01\"}"u8.ToArray();
        byte[] metadata2 = "{\"timestamp\":\"2024-01-01\",\"user\":\"admin\"}"u8.ToArray();

        DomainEventData data1 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            _sampleEventBody,
            metadata1
        );

        DomainEventData data2 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            _sampleEventBody,
            metadata2
        );

        Assert.Equal(data1, data2);
        Assert.True(data1.Equals(data2));
    }

#endregion

#region JSON-Aware HashCode Tests

    [Fact]
    public void GetHashCode_WithDifferentJsonKeyOrder_ShouldBeStable()
    {
        byte[] eventBody1 = "{\"amount\":100,\"currency\":\"USD\"}"u8.ToArray();
        byte[] eventBody2 = "{\"currency\":\"USD\",\"amount\":100}"u8.ToArray();

        DomainEventData data1 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            eventBody1,
            _sampleMetadata
        );

        DomainEventData data2 = new
        (
            _eventId,
            "MoneyDeposited",
            "application/json",
            eventBody2,
            _sampleMetadata
        );

        int hash1 = data1.GetHashCode();
        int hash2 = data2.GetHashCode();

        Assert.Equal(hash1, hash2);
    }

#endregion

#region Performance Tests

    [Fact]
    public void Equals_Performance_ShouldCompleteWithin50ms()
    {
        const string json1 = "{\"amount\":1000,\"currency\":\"USD\",\"timestamp\":\"2024-01-01T12:00:00Z\",\"metadata\":{\"source\":\"mobile-app\",\"version\":\"1.0.0\",\"userId\":\"user-123\"},\"items\":[{\"id\":1,\"name\":\"Item 1\",\"price\":100},{\"id\":2,\"name\":\"Item 2\",\"price\":200}]}";
        const string json2 = "{\"currency\":\"USD\",\"amount\":1000,\"metadata\":{\"userId\":\"user-123\",\"version\":\"1.0.0\",\"source\":\"mobile-app\"},\"timestamp\":\"2024-01-01T12:00:00Z\",\"items\":[{\"name\":\"Item 1\",\"id\":1,\"price\":100},{\"name\":\"Item 2\",\"id\":2,\"price\":200}]}";

        byte[] eventBody1 = Encoding.UTF8.GetBytes(json1);
        byte[] eventBody2 = Encoding.UTF8.GetBytes(json2);

        DomainEventData data1 = new
        (
            _eventId,
            "OrderCreated",
            "application/json",
            eventBody1,
            _sampleMetadata
        );

        DomainEventData data2 = new
        (
            _eventId,
            "OrderCreated",
            "application/json",
            eventBody2,
            _sampleMetadata
        );

        _ = data1.Equals(data2);

        // Measure time on second call (warmed up)
        Stopwatch stopwatch = Stopwatch.StartNew();
        bool result = data1.Equals(data2);
        stopwatch.Stop();

        Assert.True(result, "JSON equality should return true for same content with different key order");
        Assert.True
        (
            stopwatch.ElapsedMilliseconds < 50,
            $"Equality check took {stopwatch.ElapsedMilliseconds}ms (expected < 50ms)"
        );
    }

#endregion
}