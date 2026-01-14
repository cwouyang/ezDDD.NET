using System.Collections.ObjectModel;

using EzDdd.Entity;
using EzDdd.Integration.Tests.TestDomain;
using EzDdd.UseCase.Port.InOut;
using EzDdd.UseCase.Port.InOut.Messaging;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.Integration.Tests;

/// <summary>
///     Integration tests for complete CQRS flow with IDomainEvent.Metadata support.
///     Verifies that metadata is correctly propagated through the entire event lifecycle.
/// </summary>
/// <remarks>
///     <para>
///         Tests the full workflow:
///         Command → Aggregate → Events (with Metadata) → Repository → MessageProducer
///     </para>
///     <para>
///         Key aspects tested:
///         <list type="bullet">
///             <item>
///                 <description>Metadata preservation during event creation</description>
///             </item>
///             <item>
///                 <description>Metadata serialization/deserialization through repository</description>
///             </item>
///             <item>
///                 <description>Metadata propagation to message producer</description>
///             </item>
///             <item>
///                 <description>Idempotency detection using metadata (CorrelationId, CausationId)</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <strong>Java 4.1.0 Feature</strong>: These tests validate the IDomainEvent.Metadata property
///         introduced in Java ezddd 4.1.0 for idempotency and distributed tracing support.
///     </para>
/// </remarks>
public sealed class CqrsFlowWithMetadataTests
{
#region Event Replay with Metadata Tests

    [Fact]
    public async Task EventReplay_ShouldPreserveMetadata()
    {
        TestInfrastructure infra = _CreateInfrastructure();
        MetadataTestId id = new("AGG-004");
        string correlationId = Guid.NewGuid().ToString();
        IReadOnlyDictionary<string, string> metadata = _CreateMetadata(correlationId, userId: "charlie");

        // Act 1: Create and save aggregate
        MetadataTestAggregate aggregate = new(id, "Replay Test", 200, metadata);
        aggregate.UpdateValue(250, _CreateMetadata(correlationId, userId: "charlie"));
        await infra.SaveAndPublishAsync(aggregate);

        // Act 2: Load aggregate from repository (triggers event replay)
        MetadataTestAggregate? rehydrated = await infra.Repository.FindByIdAsync(id);

        // Assert: Rehydrated aggregate should have correct state
        Assert.NotNull(rehydrated);
        Assert.Equal("Replay Test", rehydrated.Name);
        Assert.Equal(250, rehydrated.Value);
        Assert.False(rehydrated.IsClosed);

        // Assert: All events with metadata should be published
        Assert.Equal(2, infra.EventProducer.PostedMessages.Count);
        List<DomainEventData> events = infra.EventProducer.PostedMessages.ToList();

        AggregateCreated createdEvent = DomainEventMapper.ToDomain<AggregateCreated>(events[0]);
        ValueUpdated updatedEvent = DomainEventMapper.ToDomain<ValueUpdated>(events[1]);

        Assert.Equal(correlationId, createdEvent.Metadata["CorrelationId"]);
        Assert.Equal(correlationId, updatedEvent.Metadata["CorrelationId"]);
    }

#endregion

#region Test Infrastructure Setup

    /// <summary>
    ///     Creates the CQRS infrastructure with metadata tracking support.
    /// </summary>
    private static TestInfrastructure _CreateInfrastructure()
    {
        // Register domain event types
        DomainEventTypeMapper.Register<AggregateCreated>("AggregateCreated");
        DomainEventTypeMapper.Register<ValueUpdated>("ValueUpdated");
        DomainEventTypeMapper.Register<AggregateClosed>("AggregateClosed");

        // Create infrastructure components
        InMemoryMessageProducer<DomainEventData> eventProducer = new();
        InMemoryMetadataTestEventStorePeer eventStorePeer = new();
        EsRepository<MetadataTestAggregate, MetadataTestId> repository = new(eventStorePeer);

        return new TestInfrastructure { Repository = repository, EventProducer = eventProducer, EventStorePeer = eventStorePeer };
    }

    private sealed class TestInfrastructure : IDisposable
    {
        public required EsRepository<MetadataTestAggregate, MetadataTestId> Repository { get; init; }
        public required InMemoryMessageProducer<DomainEventData> EventProducer { get; init; }
        public required InMemoryMetadataTestEventStorePeer EventStorePeer { get; init; }

        public void Dispose()
        {
            EventProducer.Dispose();
        }

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
                await EventProducer.PostAsync(eventData);
            }
        }
    }

    /// <summary>
    ///     Creates metadata with correlation and causation IDs for distributed tracing.
    /// </summary>
    private static IReadOnlyDictionary<string, string> _CreateMetadata
    (
        string correlationId,
        string? causationId = null,
        string? userId = null
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

        return new ReadOnlyDictionary<string, string>(metadata);
    }

#endregion

#region Metadata Creation and Propagation Tests

    [Fact]
    public async Task CreateAggregate_WithMetadata_ShouldPreserveMetadataInMessageProducer()
    {
        TestInfrastructure infra = _CreateInfrastructure();
        MetadataTestId id = new("AGG-001");
        string correlationId = Guid.NewGuid().ToString();
        IReadOnlyDictionary<string, string> metadata = _CreateMetadata(correlationId, userId: "alice@example.com");

        // Act: Create aggregate with metadata
        MetadataTestAggregate aggregate = new(id, "Test Aggregate", 100, metadata);
        await infra.SaveAndPublishAsync(aggregate);

        // Assert: Metadata should be preserved in MessageProducer
        Assert.Single(infra.EventProducer.PostedMessages);
        DomainEventData publishedEvent = infra.EventProducer.PostedMessages.First();
        Assert.Equal("AggregateCreated", publishedEvent.EventType);

        // Deserialize and verify metadata
        AggregateCreated deserializedEvent = DomainEventMapper.ToDomain<AggregateCreated>(publishedEvent);
        Assert.NotNull(deserializedEvent.Metadata);
        Assert.Equal(correlationId, deserializedEvent.Metadata["CorrelationId"]);
        Assert.Equal("alice@example.com", deserializedEvent.Metadata["UserId"]);
    }

    [Fact]
    public async Task UpdateValue_WithMetadata_ShouldPreserveCausationChain()
    {
        TestInfrastructure infra = _CreateInfrastructure();
        MetadataTestId id = new("AGG-002");
        string correlationId = Guid.NewGuid().ToString();
        string creationEventId = Guid.NewGuid().ToString();

        // Act 1: Create aggregate
        IReadOnlyDictionary<string, string> createMetadata = _CreateMetadata(correlationId, userId: "system");
        MetadataTestAggregate aggregate = new(id, "Test", 50, createMetadata);
        await infra.SaveAndPublishAsync(aggregate);

        // Act 2: Update value with causation ID linking to creation
        IReadOnlyDictionary<string, string> updateMetadata = _CreateMetadata(correlationId, creationEventId, "bob@example.com");
        aggregate.UpdateValue(75, updateMetadata);
        await infra.SaveAndPublishAsync(aggregate);

        // Assert: Both events should have correct metadata chain
        Assert.Equal(2, infra.EventProducer.PostedMessages.Count);
        List<DomainEventData> publishedEvents = infra.EventProducer.PostedMessages.ToList();

        // Check creation event
        AggregateCreated createdEvent = DomainEventMapper.ToDomain<AggregateCreated>(publishedEvents[0]);
        Assert.Equal(correlationId, createdEvent.Metadata["CorrelationId"]);
        Assert.Equal("system", createdEvent.Metadata["UserId"]);
        Assert.False(createdEvent.Metadata.ContainsKey("CausationId"));

        // Check update event
        ValueUpdated updatedEvent = DomainEventMapper.ToDomain<ValueUpdated>(publishedEvents[1]);
        Assert.Equal(correlationId, updatedEvent.Metadata["CorrelationId"]);
        Assert.Equal(creationEventId, updatedEvent.Metadata["CausationId"]);
        Assert.Equal("bob@example.com", updatedEvent.Metadata["UserId"]);
    }

    [Fact]
    public async Task MetadataWithSpecialCharacters_ShouldSerializeCorrectly()
    {
        TestInfrastructure infra = _CreateInfrastructure();
        MetadataTestId id = new("AGG-003");

        Dictionary<string, string> complexMetadata = new()
        {
            ["CorrelationId"] = Guid.NewGuid().ToString(),
            ["UserEmail"] = "test@example.com",
            ["RequestPath"] = "/api/test?filter=active&sort=name",
            ["SpecialChars"] = "hello \"world\" with 'quotes' and \\ backslash",
            ["Unicode"] = "你好世界 🚀 Ñoño"
        };

        // Act: Create aggregate with complex metadata
        MetadataTestAggregate aggregate = new
        (
            id,
            "Complex Metadata Test",
            123,
            new ReadOnlyDictionary<string, string>(complexMetadata)
        );
        await infra.SaveAndPublishAsync(aggregate);

        // Assert: Special characters should survive serialization round-trip
        Assert.Single(infra.EventProducer.PostedMessages);
        AggregateCreated deserialized = DomainEventMapper.ToDomain<AggregateCreated>
        (
            infra.EventProducer.PostedMessages.First()
        );

        Assert.Equal(complexMetadata["UserEmail"], deserialized.Metadata["UserEmail"]);
        Assert.Equal(complexMetadata["RequestPath"], deserialized.Metadata["RequestPath"]);
        Assert.Equal(complexMetadata["SpecialChars"], deserialized.Metadata["SpecialChars"]);
        Assert.Equal(complexMetadata["Unicode"], deserialized.Metadata["Unicode"]);
    }

#endregion

#region Idempotency Detection Tests

    [Fact]
    public async Task DuplicateCorrelationId_ShouldBeDetectable()
    {
        TestInfrastructure infra = _CreateInfrastructure();
        string sharedCorrelationId = Guid.NewGuid().ToString();

        // Act: Process same operation twice with same CorrelationId
        MetadataTestAggregate agg1 = new
        (
            new MetadataTestId("AGG-IDEM-001"),
            "First",
            100,
            _CreateMetadata(sharedCorrelationId, userId: "user1")
        );
        await infra.SaveAndPublishAsync(agg1);

        MetadataTestAggregate agg2 = new
        (
            new MetadataTestId("AGG-IDEM-002"),
            "Second",
            200,
            _CreateMetadata(sharedCorrelationId, userId: "user1")
        );
        await infra.SaveAndPublishAsync(agg2);

        // Assert: Both events should be published (detection logic is application-specific)
        // But we can verify they share the same CorrelationId
        Assert.Equal(2, infra.EventProducer.PostedMessages.Count);
        List<DomainEventData> events = infra.EventProducer.PostedMessages.ToList();

        AggregateCreated event1 = DomainEventMapper.ToDomain<AggregateCreated>(events[0]);
        AggregateCreated event2 = DomainEventMapper.ToDomain<AggregateCreated>(events[1]);

        // Same CorrelationId indicates potentially duplicate operations
        Assert.Equal(sharedCorrelationId, event1.Metadata["CorrelationId"]);
        Assert.Equal(sharedCorrelationId, event2.Metadata["CorrelationId"]);
    }

    [Fact]
    public void IdempotencyCheck_UsingCorrelationId_ShouldWork()
    {
        string correlationId = Guid.NewGuid().ToString();
        HashSet<string> processedCorrelationIds = [];

        IReadOnlyDictionary<string, string> metadata = _CreateMetadata(correlationId);

        // Act 1: Process first event
        bool isFirstProcessing = processedCorrelationIds.Add(metadata["CorrelationId"]);
        Assert.True(isFirstProcessing, "First event should be new");

        // Act 2: Attempt to process duplicate (same CorrelationId)
        bool isSecondProcessing = processedCorrelationIds.Add(metadata["CorrelationId"]);
        Assert.False(isSecondProcessing, "Duplicate CorrelationId should be detected");

        // Assert: Only one unique CorrelationId processed
        Assert.Single(processedCorrelationIds);
        Assert.Contains(correlationId, processedCorrelationIds);
    }

#endregion

#region Complete Lifecycle Tests

    [Fact]
    public async Task CompleteLifecycle_WithMetadata_ShouldPreserveMetadataThroughAllOperations()
    {
        TestInfrastructure infra = _CreateInfrastructure();
        MetadataTestId id = new("AGG-COMPLETE-001");
        string correlationId = Guid.NewGuid().ToString();
        const string userId = "integration-test-user";

        // Act 1: Create aggregate
        MetadataTestAggregate aggregate = new
        (
            id,
            "Complete Lifecycle Test",
            1000,
            _CreateMetadata(correlationId, userId: userId)
        );
        await infra.SaveAndPublishAsync(aggregate);

        // Act 2: Update value
        aggregate.UpdateValue(1500, _CreateMetadata(correlationId, userId: userId));
        await infra.SaveAndPublishAsync(aggregate);

        // Act 3: Close aggregate
        aggregate.Close("Test complete", _CreateMetadata(correlationId, userId: userId));
        await infra.SaveAndPublishAsync(aggregate);

        // Assert: All three events should have metadata
        Assert.Equal(3, infra.EventProducer.PostedMessages.Count);
        List<DomainEventData> events = infra.EventProducer.PostedMessages.ToList();

        AggregateCreated createdEvent = DomainEventMapper.ToDomain<AggregateCreated>(events[0]);
        ValueUpdated updatedEvent = DomainEventMapper.ToDomain<ValueUpdated>(events[1]);
        AggregateClosed closedEvent = DomainEventMapper.ToDomain<AggregateClosed>(events[2]);

        // All events should have the same CorrelationId and UserId
        Assert.Equal(correlationId, createdEvent.Metadata["CorrelationId"]);
        Assert.Equal(userId, createdEvent.Metadata["UserId"]);

        Assert.Equal(correlationId, updatedEvent.Metadata["CorrelationId"]);
        Assert.Equal(userId, updatedEvent.Metadata["UserId"]);

        Assert.Equal(correlationId, closedEvent.Metadata["CorrelationId"]);
        Assert.Equal(userId, closedEvent.Metadata["UserId"]);

        // Verify final state
        MetadataTestAggregate? rehydrated = await infra.Repository.FindByIdAsync(id);
        Assert.NotNull(rehydrated);
        Assert.True(rehydrated.IsClosed);
        Assert.Equal(1500, rehydrated.Value);
    }

    [Fact]
    public async Task EmptyMetadata_ShouldStillSerializeCorrectly()
    {
        TestInfrastructure infra = _CreateInfrastructure();
        MetadataTestId id = new("AGG-EMPTY-001");

        // Act: Create aggregate without metadata (empty dictionary)
        MetadataTestAggregate aggregate = new(id, "No Metadata", 42);
        await infra.SaveAndPublishAsync(aggregate);

        // Assert: Event should be published with empty metadata
        Assert.Single(infra.EventProducer.PostedMessages);
        AggregateCreated deserializedEvent = DomainEventMapper.ToDomain<AggregateCreated>
        (
            infra.EventProducer.PostedMessages.First()
        );

        Assert.NotNull(deserializedEvent.Metadata);
        Assert.Empty(deserializedEvent.Metadata);
    }

#endregion
}