using EzDdd.Entity;
using EzDdd.Integration.Tests.TestDomain;
using EzDdd.UseCase.Port.InOut;
using EzDdd.UseCase.Port.InOut.Messaging;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.Integration.Tests;

/// <summary>
///     Integration tests for MessageProducer resource management.
///     Verifies IDisposable implementation and proper resource cleanup.
/// </summary>
/// <remarks>
///     <para>
///         These tests validate the MessageProducer pattern introduced in Java ezddd 4.1.0:
///         <list type="bullet">
///             <item>
///                 <description>IDisposable contract compliance</description>
///             </item>
///             <item>
///                 <description>Resource cleanup after disposal</description>
///             </item>
///             <item>
///                 <description>Exception handling when using disposed producers</description>
///             </item>
///             <item>
///                 <description>Idempotent disposal (multiple Dispose calls)</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <strong>Java 4.1.0 Feature</strong>: MessageProducer implements IDisposable for
///         proper resource management (replaces the old MessageBus pattern).
///     </para>
/// </remarks>
public sealed class MessageProducerResourceTests
{
#region Concurrent Disposal Tests

    [Fact]
    public async Task MessageProducer_ConcurrentPostingBeforeDisposal_ShouldAllSucceed()
    {
        InMemoryMessageProducer<DomainEventData> producer = new();
        const int concurrentTasks = 10;

        // Act: Post messages concurrently
        List<Task> tasks = [];
        for (int i = 0; i < concurrentTasks; i++)
        {
            int taskId = i;
            tasks.Add
            (
                Task.Run
                (async () =>
                    {
                        DomainEventData evt = _CreateTestEvent($"CONCURRENT-{taskId:D3}");
                        await producer.PostAsync(evt);
                    }
                )
            );
        }

        await Task.WhenAll(tasks);

        // Assert: All messages should be posted
        Assert.Equal(concurrentTasks, producer.PostedMessages.Count);

        // Dispose
        producer.Dispose();
        Assert.Empty(producer.PostedMessages);
    }

#endregion

#region Helper Methods

    /// <summary>
    ///     Creates a test DomainEventData for resource management testing.
    /// </summary>
    private static DomainEventData _CreateTestEvent(string eventType)
    {
        byte[] emptyJson = "{}"u8.ToArray();
        return new DomainEventData
        (
            Guid.NewGuid(),
            eventType,
            "application/json",
            emptyJson,
            emptyJson
        );
    }

#endregion

#region Disposal Behavior Tests

    [Fact]
    public async Task MessageProducer_AfterDisposal_ShouldThrowObjectDisposedException()
    {
        InMemoryMessageProducer<DomainEventData> producer = new();
        DomainEventData testEvent = _CreateTestEvent("TEST-001");

        // Act: Post a message successfully
        await producer.PostAsync(testEvent);
        Assert.Single(producer.PostedMessages);

        // Dispose the producer
        producer.Dispose();

        // Assert: PostAsync should throw ObjectDisposedException after disposal
        await Assert.ThrowsAsync<ObjectDisposedException>(() => producer.PostAsync(testEvent));
    }

    [Fact]
    public async Task MessageProducer_AfterDisposal_PostedMessages_ShouldReturnEmpty()
    {
        InMemoryMessageProducer<DomainEventData> producer = new();
        DomainEventData event1 = _CreateTestEvent("EVENT-001");
        DomainEventData event2 = _CreateTestEvent("EVENT-002");
        DomainEventData event3 = _CreateTestEvent("EVENT-003");

        // Act: Post multiple messages
        await producer.PostAsync(event1);
        await producer.PostAsync(event2);
        await producer.PostAsync(event3);
        Assert.Equal(3, producer.PostedMessages.Count);

        // Dispose the producer
        producer.Dispose();

        // Assert: PostedMessages should return empty collection after disposal
        Assert.Empty(producer.PostedMessages);
    }

    [Fact]
    public async Task MessageProducer_MultipleDisposeCalls_ShouldBeIdempotent()
    {
        InMemoryMessageProducer<DomainEventData> producer = new();
        DomainEventData testEvent = _CreateTestEvent("MULTI-DISPOSE-001");
        await producer.PostAsync(testEvent);

        // Act: Call Dispose multiple times
        producer.Dispose();
        producer.Dispose();
        producer.Dispose();

        // Assert: No exceptions should be thrown, and state should be consistent
        Assert.Empty(producer.PostedMessages);

        // Further PostAsync should still throw ObjectDisposedException
        await Assert.ThrowsAsync<ObjectDisposedException>(() => producer.PostAsync(testEvent));
    }

#endregion

#region Using Statement Pattern Tests

    [Fact]
    public async Task MessageProducer_WithUsingStatement_ShouldDisposeAutomatically()
    {
        InMemoryMessageProducer<DomainEventData> producer;
        DomainEventData event1 = _CreateTestEvent("USING-001");
        DomainEventData event2 = _CreateTestEvent("USING-002");

        // Act: Use producer within using block
        using (producer = new InMemoryMessageProducer<DomainEventData>())
        {
            await producer.PostAsync(event1);
            await producer.PostAsync(event2);
            Assert.Equal(2, producer.PostedMessages.Count);
        }

        // Assert: After using block, producer should be disposed
        Assert.Empty(producer.PostedMessages);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => producer.PostAsync(event1));
    }

    [Fact]
    public async Task MessageProducer_WithUsingDeclaration_ShouldDisposeAtScopeEnd()
    {
        InMemoryMessageProducer<DomainEventData>? producerRef = null;

        // Act: Use producer with using declaration
        {
            using InMemoryMessageProducer<DomainEventData> producer = new();
            producerRef = producer;

            DomainEventData testEvent = _CreateTestEvent("USING-DECL-001");
            await producer.PostAsync(testEvent);
            Assert.Single(producer.PostedMessages);

            // Producer is still usable within scope
            await producer.PostAsync(_CreateTestEvent("USING-DECL-002"));
            Assert.Equal(2, producer.PostedMessages.Count);
        } // Dispose called here automatically

        // Assert: After scope, producer should be disposed
        Assert.NotNull(producerRef);
        Assert.Empty(producerRef.PostedMessages);
    }

#endregion

#region Resource Cleanup Tests

    [Fact]
    public async Task MessageProducer_Disposal_ShouldClearAllMessages()
    {
        InMemoryMessageProducer<DomainEventData> producer = new();

        // Add many messages
        for (int i = 1; i <= 100; i++)
        {
            await producer.PostAsync(_CreateTestEvent($"BULK-{i:D3}"));
        }

        Assert.Equal(100, producer.PostedMessages.Count);

        // Act: Dispose producer
        producer.Dispose();

        // Assert: All messages should be cleared
        Assert.Empty(producer.PostedMessages);
    }

    [Fact]
    public async Task MessageProducer_InRepository_ShouldManageLifecycleProperly()
    {
        using InMemoryMessageProducer<DomainEventData> producer = new();
        InMemoryMetadataTestEventStorePeer peer = new();
        EsRepository<MetadataTestAggregate, MetadataTestId> repository = new(peer, producer);

        DomainEventTypeMapper.Register<AggregateCreated>("AggregateCreated");

        // Act: Use repository to save aggregate
        MetadataTestAggregate aggregate = new
        (
            new MetadataTestId("REPO-DISPOSE-001"),
            "Test",
            100
        );
        await repository.SaveAsync(aggregate);

        // Assert: Message should be posted
        Assert.Single(producer.PostedMessages);

        // When producer is disposed, it should clear messages
        // (Repository doesn't own the producer, so it doesn't dispose it)
    }

#endregion
}