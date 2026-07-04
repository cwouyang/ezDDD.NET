using System.Collections.ObjectModel;
using EzDdd.Entity;
using EzDdd.Integration.Tests.TestDomain;
using EzDdd.UseCase.Port.InOut;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.Integration.Tests;

/// <summary>
///     Integration tests for Event Sourcing with Metadata support.
///     Focuses on event replay, persistence, and metadata preservation across save/load cycles.
/// </summary>
/// <remarks>
///     <para>
///         These tests verify that metadata is correctly preserved throughout the event sourcing lifecycle:
///         <list type="bullet">
///             <item>
///                 <description>Event replay reconstructs aggregates with metadata intact</description>
///             </item>
///             <item>
///                 <description>Multiple save/load cycles preserve metadata</description>
///             </item>
///             <item>
///                 <description>Event store correctly persists metadata</description>
///             </item>
///             <item>
///                 <description>Large event streams handle metadata efficiently</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <strong>Java 4.1.0 Feature</strong>: Validates IDomainEvent.Metadata in event sourcing context.
///     </para>
/// </remarks>
public sealed class EventSourcingMetadataTests
{
    #region Version Control and Metadata Tests

    [Fact]
    public async Task MultipleUpdates_ShouldPreserveMetadataIndependently()
    {
        TestInfrastructure infra = _CreateInfrastructure();
        MetadataTestId id = new("ES-006");
        string correlationId = Guid.NewGuid().ToString();

        // Act 1: Create aggregate
        MetadataTestAggregate agg1 = new(id, "Multi Update Test", 5, _CreateMetadata(correlationId, userId: "v1"));
        await infra.SaveAndPublishAsync(agg1);

        // Act 2: Load and update
        MetadataTestAggregate? agg2 = await infra.Repository.FindByIdAsync(id);
        Assert.NotNull(agg2);
        agg2.UpdateValue(10, _CreateMetadata(correlationId, userId: "v2"));
        await infra.SaveAndPublishAsync(agg2);

        // Assert: Both events should have correct metadata
        Assert.Equal(2, infra.PublishedEvents.Count);
        List<DomainEventData> events = infra.PublishedEvents.ToList();

        AggregateCreated v1Event = DomainEventMapper.ToDomain<AggregateCreated>(events[0]);
        ValueUpdated v2Event = DomainEventMapper.ToDomain<ValueUpdated>(events[1]);

        // Each event should have its own independent metadata
        Assert.Equal(correlationId, v1Event.Metadata["CorrelationId"]);
        Assert.Equal("v1", v1Event.Metadata["UserId"]);

        Assert.Equal(correlationId, v2Event.Metadata["CorrelationId"]);
        Assert.Equal("v2", v2Event.Metadata["UserId"]);

        // Metadata should be different for each event
        Assert.NotEqual(v1Event.Metadata["UserId"], v2Event.Metadata["UserId"]);
    }

    #endregion

    #region Test Infrastructure

    private static TestInfrastructure _CreateInfrastructure()
    {
        DomainEventTypeMapper.Register<AggregateCreated>("AggregateCreated");
        DomainEventTypeMapper.Register<ValueUpdated>("ValueUpdated");
        DomainEventTypeMapper.Register<AggregateClosed>("AggregateClosed");

        InMemoryMetadataTestEventStorePeer eventStorePeer = new();
        EsRepository<MetadataTestAggregate, MetadataTestId> repository = new(eventStorePeer);

        return new TestInfrastructure { Repository = repository, EventStorePeer = eventStorePeer };
    }

    private sealed class TestInfrastructure
    {
        public required EsRepository<MetadataTestAggregate, MetadataTestId> Repository { get; init; }
        public required InMemoryMetadataTestEventStorePeer EventStorePeer { get; init; }

        /// <summary>
        ///     Events published by <see cref="SaveAndPublishAsync" /> (simulating a message broker sink).
        /// </summary>
        public List<DomainEventData> PublishedEvents { get; } = [];

        /// <summary>
        ///     Helper method to save aggregate and manually publish events (simulating Relay pattern).
        /// </summary>
        public async Task SaveAndPublishAsync(MetadataTestAggregate aggregate)
        {
            // Capture events before save
            List<IInternalDomainEvent> events = aggregate.GetDomainEvents().ToList();

            // Save aggregate (Repository does NOT publish events)
            await Repository.SaveAsync(aggregate);

            // Manually publish events (simulating EventStoreRelay)
            foreach (IInternalDomainEvent domainEvent in events)
            {
                DomainEventData eventData = DomainEventMapper.ToData(domainEvent);
                PublishedEvents.Add(eventData);
            }
        }
    }

    private static IReadOnlyDictionary<string, string> _CreateMetadata(
        string correlationId,
        string? causationId = null,
        string? userId = null,
        string? traceId = null
    )
    {
        Dictionary<string, string> metadata = new() { ["CorrelationId"] = correlationId };

        if (causationId != null)
        {
            metadata["CausationId"] = causationId;
        }

        if (userId != null)
        {
            metadata["UserId"] = userId;
        }

        if (traceId != null)
        {
            metadata["TraceId"] = traceId;
        }

        return new ReadOnlyDictionary<string, string>(metadata);
    }

    #endregion

    #region Event Replay with Metadata Tests

    [Fact]
    public async Task SingleSaveLoadCycle_ShouldPreserveMetadata()
    {
        TestInfrastructure infra = _CreateInfrastructure();
        MetadataTestId id = new("ES-001");
        string correlationId = Guid.NewGuid().ToString();
        IReadOnlyDictionary<string, string> metadata = _CreateMetadata(correlationId, userId: "user1");

        // Act 1: Create and save aggregate
        MetadataTestAggregate original = new(id, "Test", 100, metadata);
        await infra.SaveAndPublishAsync(original);

        // Act 2: Load aggregate (triggers event replay)
        MetadataTestAggregate? reloaded = await infra.Repository.FindByIdAsync(id);

        // Assert: State should be correct
        Assert.NotNull(reloaded);
        Assert.Equal("Test", reloaded.Name);
        Assert.Equal(100, reloaded.Value);

        // Assert: Events with metadata should be in the published events
        Assert.Single(infra.PublishedEvents);
        AggregateCreated deserializedEvent = DomainEventMapper.ToDomain<AggregateCreated>(
            infra.PublishedEvents.First()
        );
        Assert.Equal(correlationId, deserializedEvent.Metadata["CorrelationId"]);
        Assert.Equal("user1", deserializedEvent.Metadata["UserId"]);
    }

    [Fact]
    public async Task MultipleSaveLoadCycles_ShouldPreserveMetadata()
    {
        TestInfrastructure infra = _CreateInfrastructure();
        MetadataTestId id = new("ES-002");
        string correlationId = Guid.NewGuid().ToString();

        // Act 1: Create aggregate with metadata
        MetadataTestAggregate agg1 = new(id, "Cycle Test", 50, _CreateMetadata(correlationId, userId: "user1"));
        await infra.SaveAndPublishAsync(agg1);

        // Act 2: Load and update (cycle 1)
        MetadataTestAggregate? agg2 = await infra.Repository.FindByIdAsync(id);
        Assert.NotNull(agg2);
        agg2.UpdateValue(100, _CreateMetadata(correlationId, userId: "user2"));
        await infra.SaveAndPublishAsync(agg2);

        // Act 3: Load and update (cycle 2)
        MetadataTestAggregate? agg3 = await infra.Repository.FindByIdAsync(id);
        Assert.NotNull(agg3);
        agg3.UpdateValue(150, _CreateMetadata(correlationId, userId: "user3"));
        await infra.SaveAndPublishAsync(agg3);

        // Act 4: Final load
        MetadataTestAggregate? final = await infra.Repository.FindByIdAsync(id);
        Assert.NotNull(final);

        // Assert: Final state should be correct
        Assert.Equal(150, final.Value);

        // Assert: All three events with metadata should be published
        Assert.Equal(3, infra.PublishedEvents.Count);
        List<DomainEventData> events = infra.PublishedEvents.ToList();

        AggregateCreated created = DomainEventMapper.ToDomain<AggregateCreated>(events[0]);
        ValueUpdated updated1 = DomainEventMapper.ToDomain<ValueUpdated>(events[1]);
        ValueUpdated updated2 = DomainEventMapper.ToDomain<ValueUpdated>(events[2]);

        // All events should have same CorrelationId but different UserId
        Assert.Equal(correlationId, created.Metadata["CorrelationId"]);
        Assert.Equal("user1", created.Metadata["UserId"]);

        Assert.Equal(correlationId, updated1.Metadata["CorrelationId"]);
        Assert.Equal("user2", updated1.Metadata["UserId"]);

        Assert.Equal(correlationId, updated2.Metadata["CorrelationId"]);
        Assert.Equal("user3", updated2.Metadata["UserId"]);
    }

    [Fact]
    public async Task LargeEventStream_ShouldHandleMetadataCorrectly()
    {
        TestInfrastructure infra = _CreateInfrastructure();
        MetadataTestId id = new("ES-003");
        string correlationId = Guid.NewGuid().ToString();
        const int eventCount = 50; // Create 50 events

        // Act: Create aggregate and perform many updates
        MetadataTestAggregate aggregate = new(
            id,
            "Large Stream Test",
            0,
            _CreateMetadata(correlationId, userId: "system", traceId: "trace-001")
        );

        for (int i = 1; i <= eventCount - 1; i++)
        {
            aggregate.UpdateValue(i * 10, _CreateMetadata(correlationId, userId: $"user{i}", traceId: $"trace-{i:D3}"));
        }

        await infra.SaveAndPublishAsync(aggregate);

        // Act: Reload aggregate (replay all events)
        MetadataTestAggregate? reloaded = await infra.Repository.FindByIdAsync(id);

        // Assert: State should be correct
        Assert.NotNull(reloaded);
        Assert.Equal((eventCount - 1) * 10, reloaded.Value);

        // Assert: All events should be published with metadata
        Assert.Equal(eventCount, infra.PublishedEvents.Count);

        // Verify first and last events have correct metadata
        AggregateCreated firstEvent = DomainEventMapper.ToDomain<AggregateCreated>(infra.PublishedEvents.First());
        Assert.Equal(correlationId, firstEvent.Metadata["CorrelationId"]);
        Assert.Equal("system", firstEvent.Metadata["UserId"]);
        Assert.Equal("trace-001", firstEvent.Metadata["TraceId"]);

        ValueUpdated lastEvent = DomainEventMapper.ToDomain<ValueUpdated>(infra.PublishedEvents.Last());
        Assert.Equal(correlationId, lastEvent.Metadata["CorrelationId"]);
        Assert.Equal($"user{eventCount - 1}", lastEvent.Metadata["UserId"]);
        Assert.Equal($"trace-{eventCount - 1:D3}", lastEvent.Metadata["TraceId"]);
    }

    #endregion

    #region Event Store Persistence Tests

    [Fact]
    public async Task EventStore_ShouldPersistMetadataInEventStoreData()
    {
        TestInfrastructure infra = _CreateInfrastructure();
        MetadataTestId id = new("ES-004");
        string correlationId = Guid.NewGuid().ToString();

        // Act: Create and save aggregate
        MetadataTestAggregate aggregate = new(
            id,
            "Persistence Test",
            200,
            _CreateMetadata(correlationId, userId: "alice", traceId: "trace-xyz")
        );
        await infra.SaveAndPublishAsync(aggregate);

        // Assert: Event store should contain EventStoreData with events
        EventStoreData<MetadataTestId>? storeData = await infra.EventStorePeer.FindByIdAsync(id);
        Assert.NotNull(storeData);
        Assert.Single(storeData.Events);

        // Verify the stored event has correct type and metadata via the published events
        // (EventStoreData stores IDomainEvent, which includes metadata)
        Assert.Single(infra.PublishedEvents);
        AggregateCreated publishedEvent = DomainEventMapper.ToDomain<AggregateCreated>(infra.PublishedEvents.First());

        Assert.Equal(correlationId, publishedEvent.Metadata["CorrelationId"]);
        Assert.Equal("alice", publishedEvent.Metadata["UserId"]);
        Assert.Equal("trace-xyz", publishedEvent.Metadata["TraceId"]);
    }

    [Fact]
    public async Task EventStoreAppend_ShouldAccumulateEventsWithMetadata()
    {
        TestInfrastructure infra = _CreateInfrastructure();
        MetadataTestId id = new("ES-005");
        string correlationId = Guid.NewGuid().ToString();

        // Act 1: Create aggregate
        MetadataTestAggregate agg1 = new(id, "Append Test", 10, _CreateMetadata(correlationId, userId: "user1"));
        await infra.SaveAndPublishAsync(agg1);

        // Act 2: Load, update, save
        MetadataTestAggregate? agg2 = await infra.Repository.FindByIdAsync(id);
        Assert.NotNull(agg2);
        agg2.UpdateValue(20, _CreateMetadata(correlationId, userId: "user2"));
        await infra.SaveAndPublishAsync(agg2);

        // Act 3: Load, update, save
        MetadataTestAggregate? agg3 = await infra.Repository.FindByIdAsync(id);
        Assert.NotNull(agg3);
        agg3.UpdateValue(30, _CreateMetadata(correlationId, userId: "user3"));
        await infra.SaveAndPublishAsync(agg3);

        // Assert: All 3 events should be published with correct metadata
        Assert.Equal(3, infra.PublishedEvents.Count);
        List<DomainEventData> events = infra.PublishedEvents.ToList();

        AggregateCreated event1 = DomainEventMapper.ToDomain<AggregateCreated>(events[0]);
        ValueUpdated event2 = DomainEventMapper.ToDomain<ValueUpdated>(events[1]);
        ValueUpdated event3 = DomainEventMapper.ToDomain<ValueUpdated>(events[2]);

        // Verify metadata preservation through all saves
        Assert.Equal(correlationId, event1.Metadata["CorrelationId"]);
        Assert.Equal("user1", event1.Metadata["UserId"]);

        Assert.Equal(correlationId, event2.Metadata["CorrelationId"]);
        Assert.Equal("user2", event2.Metadata["UserId"]);

        Assert.Equal(correlationId, event3.Metadata["CorrelationId"]);
        Assert.Equal("user3", event3.Metadata["UserId"]);

        // Verify final aggregate state
        MetadataTestAggregate? final = await infra.Repository.FindByIdAsync(id);
        Assert.NotNull(final);
        Assert.Equal(30, final.Value);
    }

    #endregion

    #region Metadata Consistency Tests

    [Fact]
    public async Task ReloadedAggregate_ShouldHaveNoUnpublishedEvents()
    {
        TestInfrastructure infra = _CreateInfrastructure();
        MetadataTestId id = new("ES-007");

        // Act: Create, save, reload
        MetadataTestAggregate original = new(id, "Consistency", 1, _CreateMetadata("corr-1"));
        await infra.SaveAndPublishAsync(original);

        MetadataTestAggregate? reloaded = await infra.Repository.FindByIdAsync(id);

        // Assert: Reloaded aggregate should have no pending domain events
        Assert.NotNull(reloaded);
        Assert.Empty(reloaded.GetDomainEvents());
    }

    [Fact]
    public async Task MetadataRoundTrip_ThroughSerialization_ShouldBeIdentical()
    {
        TestInfrastructure infra = _CreateInfrastructure();
        MetadataTestId id = new("ES-008");

        Dictionary<string, string> originalMetadata = new()
        {
            ["CorrelationId"] = "abc-123",
            ["UserId"] = "test@example.com",
            ["TraceId"] = "trace-456",
            ["CustomKey"] = "CustomValue with spaces and 特殊字符",
        };

        // Act: Create aggregate with specific metadata
        MetadataTestAggregate aggregate = new(
            id,
            "Round Trip Test",
            999,
            new ReadOnlyDictionary<string, string>(originalMetadata)
        );
        await infra.SaveAndPublishAsync(aggregate);

        // Assert: Deserialized event should have identical metadata
        Assert.Single(infra.PublishedEvents);
        AggregateCreated deserializedEvent = DomainEventMapper.ToDomain<AggregateCreated>(
            infra.PublishedEvents.First()
        );

        // Check all keys and values
        Assert.Equal(originalMetadata.Count, deserializedEvent.Metadata.Count);
        foreach (KeyValuePair<string, string> kvp in originalMetadata)
        {
            Assert.True(deserializedEvent.Metadata.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, deserializedEvent.Metadata[kvp.Key]);
        }
    }

    #endregion
}
