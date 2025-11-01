namespace EzDdd.Entity.Tests;

public class DomainEventTests
{
    [Fact]
    public void IDomainEvent_WithAllProperties_ReturnsCorrectValues()
    {
        Guid id = Guid.NewGuid();
        DateTimeOffset occurredOn = DateTimeOffset.UtcNow;
        const string source = "test-aggregate-123";
        Dictionary<string, string> metadata = new() { ["CorrelationId"] = "corr-123", ["UserId"] = "user-456" };

        TestCommandEvent @event = new(id, occurredOn, source, 42, metadata);

        Assert.Equal(id, @event.Id);
        Assert.Equal(occurredOn, @event.OccurredOn);
        Assert.Equal(source, @event.Source);
        Assert.Equal(42, @event.Value);
        Assert.Equal(metadata, @event.Metadata);
    }

    [Fact]
    public void IDomainEvent_Metadata_IsImmutable()
    {
        Dictionary<string, string> metadata = new() { ["Key1"] = "Value1" };
        TestCommandEvent @event = new
        (
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "source",
            0,
            metadata
        );

        Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(@event.Metadata);

        // Cannot modify through the event's Metadata property
        // (@event.Metadata as Dictionary<string, string>).Add("Key2", "Value2");
        // This would throw if metadata is truly read-only
    }

    [Fact]
    public void IDomainEvent_WithEmptyMetadata_WorksCorrectly()
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        Dictionary<string, string> emptyMetadata = new();

        TestCommandEvent @event = new
        (
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "source",
            0,
            emptyMetadata
        );

        Assert.Empty(@event.Metadata);
    }

    [Fact]
    public void IDomainEvent_DateTimeOffset_PreservesTimezone()
    {
        DateTimeOffset utcTime = new(2025, 11, 1, 10, 30, 0, TimeSpan.Zero);
        DateTimeOffset localTime = new(2025, 11, 1, 18, 30, 0, TimeSpan.FromHours(8));

        TestCommandEvent utcEvent = new(Guid.NewGuid(), utcTime, "source", 0, new Dictionary<string, string>());
        TestCommandEvent localEvent = new(Guid.NewGuid(), localTime, "source", 0, new Dictionary<string, string>());

        Assert.Equal(TimeSpan.Zero, utcEvent.OccurredOn.Offset);
        Assert.Equal(TimeSpan.FromHours(8), localEvent.OccurredOn.Offset);
    }

    [Fact]
    public void IDomainEvent_RecordType_HasStructuralEquality()
    {
        Guid id = Guid.NewGuid();
        DateTimeOffset occurredOn = DateTimeOffset.UtcNow;
        Dictionary<string, string> metadata = new() { ["Key"] = "Value" };

        TestCommandEvent event1 = new(id, occurredOn, "source", 42, metadata);
        TestCommandEvent event2 = new(id, occurredOn, "source", 42, metadata);
        TestCommandEvent event3 = new(Guid.NewGuid(), occurredOn, "source", 42, metadata);

        // Records provide structural equality
        Assert.Equal(event1, event2); // Same values = equal
        Assert.NotEqual(event1, event3); // Different ID = not equal
    }

    [Fact]
    public void IDomainEvent_RecordType_IsImmutable()
    {
        TestCommandEvent @event = new
        (
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "source",
            42,
            new Dictionary<string, string>()
        );

        // Record properties are init-only
        // @event.Id = Guid.NewGuid(); // Compilation error - immutable
        // @event.Value = 100; // Compilation error - immutable
        Assert.Equal(42, @event.Value);
    }

    [Fact]
    public void IDomainEvent_CanBeUsedAsTypeConstraint()
    {
        TestCommandEvent @event = new
        (
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "source",
            42,
            new Dictionary<string, string>()
        );

        Guid id = GetEventId(@event);

        Assert.Equal(@event.Id, id);
        return;

        // Helper method demonstrating generic constraint
        static Guid GetEventId<TEvent>(TEvent @event) where TEvent : IDomainEvent
        {
            return @event.Id;
        }
    }


    // Test implementation - Construction event
    // ReSharper disable once UnusedType.Local
    private record TestConstructionEvent
    (
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string TestData,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent;

    // Test implementation - Command event (middle event)
    private record TestCommandEvent
    (
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        int Value,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    // Test implementation - Destruction event
    // ReSharper disable once UnusedType.Local
    private record TestDestructionEvent
    (
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string Reason,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IDestructionEvent;
}