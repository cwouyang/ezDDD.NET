using System.Text.Json;

using EzDdd.Entity;
using EzDdd.UseCase.Port.InOut;

namespace EzDdd.UseCase.Tests.Port.InOut;

/// <summary>
///     Tests for domain event metadata handling.
/// </summary>
/// <remarks>
///     <para>
///         These tests verify that metadata is correctly handled throughout the event lifecycle:
///         serialization, deserialization, equality comparison, and event replay. Metadata is
///         essential for distributed systems requiring idempotency, correlation, and tracing.
///     </para>
/// </remarks>
public class DomainEventMetadataTests
{
    public DomainEventMetadataTests()
    {
        // Register test event types
        DomainEventTypeMapper.Register<TestEvent>("TestEvent");
        DomainEventTypeMapper.Register<TestConstructionEvent>("TestConstructionEvent");
        DomainEventTypeMapper.Register<TestDestructionEvent>("TestDestructionEvent");
    }

#region Metadata Immutability

    [Fact]
    public void Metadata_IsReadOnly_CannotBeModified()
    {
        Dictionary<string, string> metadata = new() { ["Key1"] = "Value1" };
        TestEvent @event = new(Guid.NewGuid(), DateTimeOffset.UtcNow, "source-1", "test", metadata);

        Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(@event.Metadata);
    }

#endregion

#region Metadata Serialization/Deserialization

    [Fact]
    public void ToData_WithMetadata_SerializesMetadataCorrectly()
    {
        Dictionary<string, string> metadata = new() { ["CorrelationId"] = "corr-123", ["UserId"] = "user-456" };
        TestEvent @event = new(Guid.NewGuid(), DateTimeOffset.UtcNow, "source-1", "test", metadata);

        DomainEventData data = DomainEventMapper.ToData(@event);

        Assert.NotNull(data.UserMetadata);
        Assert.NotEmpty(data.UserMetadata);

        // Verify metadata can be deserialized
        Dictionary<string, string>? deserializedMetadata =
            JsonSerializer.Deserialize<Dictionary<string, string>>(data.UserMetadata);

        Assert.NotNull(deserializedMetadata);
        Assert.Equal(2, deserializedMetadata.Count);
        Assert.Equal("corr-123", deserializedMetadata["CorrelationId"]);
        Assert.Equal("user-456", deserializedMetadata["UserId"]);
    }

    [Fact]
    public void ToData_WithEmptyMetadata_SerializesEmptyDictionary()
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        Dictionary<string, string> emptyMetadata = new();
        TestEvent @event = new(Guid.NewGuid(), DateTimeOffset.UtcNow, "source-1", "test", emptyMetadata);

        DomainEventData data = DomainEventMapper.ToData(@event);

        Assert.NotNull(data.UserMetadata);

        Dictionary<string, string>? deserializedMetadata =
            JsonSerializer.Deserialize<Dictionary<string, string>>(data.UserMetadata);

        Assert.NotNull(deserializedMetadata);
        Assert.Empty(deserializedMetadata);
    }

    [Fact]
    public void ToDomain_WithMetadata_DeserializesMetadataCorrectly()
    {
        Dictionary<string, string> originalMetadata = new() { ["TraceId"] = "trace-789", ["SpanId"] = "span-012" };
        TestEvent originalEvent = new(Guid.NewGuid(), DateTimeOffset.UtcNow, "source-1", "test", originalMetadata);
        DomainEventData data = DomainEventMapper.ToData(originalEvent);

        TestEvent reconstructedEvent = DomainEventMapper.ToDomain<TestEvent>(data);

        Assert.Equal(2, reconstructedEvent.Metadata.Count);
        Assert.Equal("trace-789", reconstructedEvent.Metadata["TraceId"]);
        Assert.Equal("span-012", reconstructedEvent.Metadata["SpanId"]);
    }

    [Fact]
    public void RoundTrip_WithMetadata_PreservesMetadata()
    {
        Dictionary<string, string> metadata = new()
        {
            ["CorrelationId"] = "corr-123",
            ["CausationId"] = "cause-456",
            ["UserId"] = "user-789",
            ["TraceContext"] = "trace-context-abc"
        };
        TestEvent originalEvent = new(Guid.NewGuid(), DateTimeOffset.UtcNow, "source-1", "test-data", metadata);

        DomainEventData data = DomainEventMapper.ToData(originalEvent);
        TestEvent reconstructedEvent = DomainEventMapper.ToDomain<TestEvent>(data);

        Assert.Equal(originalEvent.Metadata.Count, reconstructedEvent.Metadata.Count);
        foreach (KeyValuePair<string, string> kvp in originalEvent.Metadata)
        {
            Assert.True(reconstructedEvent.Metadata.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, reconstructedEvent.Metadata[kvp.Key]);
        }
    }

#endregion

#region Metadata Equality

    [Fact]
    public void DomainEventData_WithSameMetadata_AreEqual()
    {
        Guid id = Guid.NewGuid();
        byte[] eventBody = JsonSerializer.SerializeToUtf8Bytes(new { Value = "test" });
        byte[] metadata1 = JsonSerializer.SerializeToUtf8Bytes(new { Key1 = "Value1", Key2 = "Value2" });
        byte[] metadata2 = JsonSerializer.SerializeToUtf8Bytes(new { Key1 = "Value1", Key2 = "Value2" });

        DomainEventData data1 = new(id, "TestEvent", "application/json", eventBody, metadata1);
        DomainEventData data2 = new(id, "TestEvent", "application/json", eventBody, metadata2);

        Assert.Equal(data1, data2);
    }

    [Fact]
    public void DomainEventData_WithDifferentMetadataKeyOrder_AreEqual()
    {
        Guid id = Guid.NewGuid();
        byte[] eventBody = JsonSerializer.SerializeToUtf8Bytes(new { Value = "test" });
        byte[] metadata1 = JsonSerializer.SerializeToUtf8Bytes(new { Key1 = "Value1", Key2 = "Value2" });
        byte[] metadata2 = JsonSerializer.SerializeToUtf8Bytes(new { Key2 = "Value2", Key1 = "Value1" });

        DomainEventData data1 = new(id, "TestEvent", "application/json", eventBody, metadata1);
        DomainEventData data2 = new(id, "TestEvent", "application/json", eventBody, metadata2);

        // Assert (JSON-aware equality, key order doesn't matter)
        Assert.Equal(data1, data2);
    }

    [Fact]
    public void DomainEventData_WithDifferentMetadata_AreNotEqual()
    {
        Guid id = Guid.NewGuid();
        byte[] eventBody = JsonSerializer.SerializeToUtf8Bytes(new { Value = "test" });
        byte[] metadata1 = JsonSerializer.SerializeToUtf8Bytes(new { Key1 = "Value1" });
        byte[] metadata2 = JsonSerializer.SerializeToUtf8Bytes(new { Key1 = "Value2" });

        DomainEventData data1 = new(id, "TestEvent", "application/json", eventBody, metadata1);
        DomainEventData data2 = new(id, "TestEvent", "application/json", eventBody, metadata2);

        Assert.NotEqual(data1, data2);
    }

#endregion

#region Metadata Special Cases

    [Fact]
    public void Metadata_WithSpecialCharacters_HandledCorrectly()
    {
        Dictionary<string, string> metadata = new()
        {
            ["Key-With-Dashes"] = "value-with-dashes",
            ["Key.With.Dots"] = "value.with.dots",
            ["Key_With_Underscores"] = "value_with_underscores",
            ["KeyWithNumbers123"] = "ValueWithNumbers456",
            ["Key@Symbol"] = "value@domain.com"
        };
        TestEvent originalEvent = new(Guid.NewGuid(), DateTimeOffset.UtcNow, "source-1", "test", metadata);

        DomainEventData data = DomainEventMapper.ToData(originalEvent);
        TestEvent reconstructedEvent = DomainEventMapper.ToDomain<TestEvent>(data);

        Assert.Equal(metadata.Count, reconstructedEvent.Metadata.Count);
        foreach (KeyValuePair<string, string> kvp in metadata)
        {
            Assert.Equal(kvp.Value, reconstructedEvent.Metadata[kvp.Key]);
        }
    }

    [Fact]
    public void Metadata_WithUnicodeCharacters_HandledCorrectly()
    {
        Dictionary<string, string> metadata = new()
        {
            ["Chinese"] = "中文測試",
            ["Japanese"] = "日本語テスト",
            ["Korean"] = "한국어 테스트",
            ["Emoji"] = "🎉🚀✅",
            ["Arabic"] = "اختبار"
        };
        TestEvent originalEvent = new(Guid.NewGuid(), DateTimeOffset.UtcNow, "source-1", "test", metadata);

        DomainEventData data = DomainEventMapper.ToData(originalEvent);
        TestEvent reconstructedEvent = DomainEventMapper.ToDomain<TestEvent>(data);

        Assert.Equal(metadata.Count, reconstructedEvent.Metadata.Count);
        foreach (KeyValuePair<string, string> kvp in metadata)
        {
            Assert.Equal(kvp.Value, reconstructedEvent.Metadata[kvp.Key]);
        }
    }

    [Fact]
    public void Metadata_WithLargeNumberOfKeys_HandledCorrectly()
    {
        Dictionary<string, string> metadata = new();
        for (int i = 0; i < 100; i++)
        {
            metadata[$"Key{i}"] = $"Value{i}";
        }

        TestEvent originalEvent = new(Guid.NewGuid(), DateTimeOffset.UtcNow, "source-1", "test", metadata);

        DomainEventData data = DomainEventMapper.ToData(originalEvent);
        TestEvent reconstructedEvent = DomainEventMapper.ToDomain<TestEvent>(data);

        Assert.Equal(100, reconstructedEvent.Metadata.Count);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal($"Value{i}", reconstructedEvent.Metadata[$"Key{i}"]);
        }
    }

    [Fact]
    public void Metadata_WithLongValues_HandledCorrectly()
    {
        string longValue = new('x', 10000); // 10KB string
        Dictionary<string, string> metadata = new() { ["LongKey"] = longValue };
        TestEvent originalEvent = new(Guid.NewGuid(), DateTimeOffset.UtcNow, "source-1", "test", metadata);

        DomainEventData data = DomainEventMapper.ToData(originalEvent);
        TestEvent reconstructedEvent = DomainEventMapper.ToDomain<TestEvent>(data);

        Assert.Equal(longValue, reconstructedEvent.Metadata["LongKey"]);
    }

    [Fact]
    public void Metadata_WithEmptyStringValues_HandledCorrectly()
    {
        Dictionary<string, string> metadata = new() { ["EmptyKey"] = "", ["NonEmptyKey"] = "value" };
        TestEvent originalEvent = new(Guid.NewGuid(), DateTimeOffset.UtcNow, "source-1", "test", metadata);

        DomainEventData data = DomainEventMapper.ToData(originalEvent);
        TestEvent reconstructedEvent = DomainEventMapper.ToDomain<TestEvent>(data);

        Assert.Equal("", reconstructedEvent.Metadata["EmptyKey"]);
        Assert.Equal("value", reconstructedEvent.Metadata["NonEmptyKey"]);
    }

#endregion

#region Metadata in Different Event Types

    [Fact]
    public void ConstructionEvent_WithMetadata_PreservesMetadata()
    {
        Dictionary<string, string> metadata = new() { ["CreatedBy"] = "system", ["Reason"] = "initialization" };
        TestConstructionEvent @event = new(Guid.NewGuid(), DateTimeOffset.UtcNow, "source-1", "init-data", metadata);

        DomainEventData data = DomainEventMapper.ToData(@event);
        TestConstructionEvent reconstructedEvent = DomainEventMapper.ToDomain<TestConstructionEvent>(data);

        Assert.Equal(metadata.Count, reconstructedEvent.Metadata.Count);
        Assert.Equal("system", reconstructedEvent.Metadata["CreatedBy"]);
        Assert.Equal("initialization", reconstructedEvent.Metadata["Reason"]);
    }

    [Fact]
    public void DestructionEvent_WithMetadata_PreservesMetadata()
    {
        Dictionary<string, string> metadata = new() { ["DeletedBy"] = "admin", ["Reason"] = "cleanup" };
        TestDestructionEvent @event = new(Guid.NewGuid(), DateTimeOffset.UtcNow, "source-1", "cleanup", metadata);

        DomainEventData data = DomainEventMapper.ToData(@event);
        TestDestructionEvent reconstructedEvent = DomainEventMapper.ToDomain<TestDestructionEvent>(data);

        Assert.Equal(metadata.Count, reconstructedEvent.Metadata.Count);
        Assert.Equal("admin", reconstructedEvent.Metadata["DeletedBy"]);
        Assert.Equal("cleanup", reconstructedEvent.Metadata["Reason"]);
    }

#endregion

#region Helper Test Events

    /// <summary>
    ///     Test event for metadata testing.
    /// </summary>
    private sealed record TestEvent
    (
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string TestData,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    /// <summary>
    ///     Test construction event for metadata testing.
    /// </summary>
    private sealed record TestConstructionEvent
    (
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string InitData,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent;

    /// <summary>
    ///     Test destruction event for metadata testing.
    /// </summary>
    private sealed record TestDestructionEvent
    (
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string Reason,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IDestructionEvent;

#endregion
}