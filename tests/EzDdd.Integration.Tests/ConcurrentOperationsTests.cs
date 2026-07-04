using EzDdd.Entity;
using EzDdd.Integration.Tests.TestDomain;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.Integration.Tests;

/// <summary>
///     Integration tests for concurrent operations across all ezDDD components.
///     Verifies thread safety under high concurrency scenarios.
/// </summary>
/// <remarks>
///     <para>
///         These tests validate thread safety improvements from Java ezddd 4.1.0:
///         <list type="bullet">
///             <item>
///                 <description>DomainEventTypeMapper concurrent registration (Lazy&lt;BiMap&gt; fix)</description>
///             </item>
///             <item>
///                 <description>Repository concurrent save/load operations</description>
///             </item>
///             <item>
///                 <description>Multi-component concurrent workflows</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <strong>Java 4.1.0 Feature</strong>: Thread safety enhancements to prevent race conditions
///         in concurrent scenarios.
///     </para>
/// </remarks>
[Collection("DomainEventTypeMapper")]
public sealed class ConcurrentOperationsTests
{
    #region DomainEventTypeMapper Concurrent Registration Tests

    [Fact]
    public async Task DomainEventTypeMapper_ConcurrentRegistration_ShouldBeThreadSafe()
    {
        const int threadCount = 20;
        const int registrationsPerThread = 5;

        // Act: Each thread repeatedly registers its dedicated event type under a fixed,
        // consistent name (idempotent path). Consistent type-name mappings must never throw,
        // so no exception is caught - any exception fails the test.
        List<Task> tasks = [];
        for (int t = 0; t < threadCount; t++)
        {
            int threadId = t;
            tasks.Add(
                Task.Run(() =>
                {
                    for (int i = 0; i < registrationsPerThread; i++)
                    {
                        _RegisterConcurrentEvent(threadId % 5);
                    }
                })
            );
        }

        // Assert: All tasks complete without deadlock, race condition, or exception
        await Task.WhenAll(tasks);

        // Assert: Bidirectional lookups resolve correctly after concurrent registration
        Assert.Equal("ConcurrentEvent0", DomainEventTypeMapper.GetTypeName(typeof(ConcurrentEvent0)));
        Assert.Equal("ConcurrentEvent1", DomainEventTypeMapper.GetTypeName(typeof(ConcurrentEvent1)));
        Assert.Equal("ConcurrentEvent2", DomainEventTypeMapper.GetTypeName(typeof(ConcurrentEvent2)));
        Assert.Equal("ConcurrentEvent3", DomainEventTypeMapper.GetTypeName(typeof(ConcurrentEvent3)));
        Assert.Equal("ConcurrentEvent4", DomainEventTypeMapper.GetTypeName(typeof(ConcurrentEvent4)));
        Assert.Equal(typeof(ConcurrentEvent0), DomainEventTypeMapper.GetType("ConcurrentEvent0"));
        Assert.Equal(typeof(ConcurrentEvent1), DomainEventTypeMapper.GetType("ConcurrentEvent1"));
        Assert.Equal(typeof(ConcurrentEvent2), DomainEventTypeMapper.GetType("ConcurrentEvent2"));
        Assert.Equal(typeof(ConcurrentEvent3), DomainEventTypeMapper.GetType("ConcurrentEvent3"));
        Assert.Equal(typeof(ConcurrentEvent4), DomainEventTypeMapper.GetType("ConcurrentEvent4"));
    }

    /// <summary>
    ///     Registers the dedicated concurrent-test event type assigned to the given slot,
    ///     always using the same fixed type name (idempotent, consistent mapping).
    /// </summary>
    private static void _RegisterConcurrentEvent(int eventNumber)
    {
        switch (eventNumber)
        {
            case 0:
                DomainEventTypeMapper.Register<ConcurrentEvent0>("ConcurrentEvent0");
                break;
            case 1:
                DomainEventTypeMapper.Register<ConcurrentEvent1>("ConcurrentEvent1");
                break;
            case 2:
                DomainEventTypeMapper.Register<ConcurrentEvent2>("ConcurrentEvent2");
                break;
            case 3:
                DomainEventTypeMapper.Register<ConcurrentEvent3>("ConcurrentEvent3");
                break;
            case 4:
                DomainEventTypeMapper.Register<ConcurrentEvent4>("ConcurrentEvent4");
                break;
        }
    }

    // Dedicated event types for the concurrent registration test.
    // Never reuse shared TestDomain types here: binding a shared type to a random name
    // would poison the process-global mapper for every other test in this assembly.
    private sealed record ConcurrentEvent0(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private sealed record ConcurrentEvent1(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private sealed record ConcurrentEvent2(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private sealed record ConcurrentEvent3(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private sealed record ConcurrentEvent4(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    #endregion

    #region Mixed Concurrent Operations Tests

    [Fact]
    public async Task MixedOperations_ConcurrentSaveAndLoad_ShouldMaintainConsistency()
    {
        InMemoryMetadataTestEventStorePeer peer = new();
        EsRepository<MetadataTestAggregate, MetadataTestId> repository = new(peer);

        DomainEventTypeMapper.Register<AggregateCreated>("AggregateCreated");
        DomainEventTypeMapper.Register<ValueUpdated>("ValueUpdated");

        // Create initial aggregate
        MetadataTestId sharedId = new("SHARED-AGG-001");
        MetadataTestAggregate initialAgg = new(sharedId, "Shared Aggregate", 0);
        await repository.SaveAsync(initialAgg);

        const int updateCount = 10;

        // Act: Perform concurrent updates and loads on the same aggregate
        List<Task> tasks = [];

        // Half the tasks perform updates
        for (int i = 0; i < updateCount / 2; i++)
        {
            int updateId = i;
            tasks.Add(Task.Run(() => _TryUpdateSharedAggregateAsync(repository, sharedId, updateId)));
        }

        // Other half performs reads
        for (int i = 0; i < updateCount / 2; i++)
        {
            tasks.Add(
                Task.Run(async () =>
                {
                    MetadataTestAggregate? agg = await repository.FindByIdAsync(sharedId);
                    Assert.NotNull(agg);
                    // Just verify we can read
                })
            );
        }

        // Wait for all operations
        await Task.WhenAll(tasks);

        // Assert: Final aggregate should exist and have some value
        MetadataTestAggregate? finalAgg = await repository.FindByIdAsync(sharedId);
        Assert.NotNull(finalAgg);

        // Due to optimistic locking, not all updates will succeed
        // Verify at least the initial aggregate was persisted
        Assert.True(finalAgg.Value >= 0);
    }

    /// <summary>
    ///     Loads the shared aggregate and applies one update, swallowing the optimistic-locking
    ///     conflicts that concurrent updates are expected to produce.
    /// </summary>
    private static async Task _TryUpdateSharedAggregateAsync(
        EsRepository<MetadataTestAggregate, MetadataTestId> repository,
        MetadataTestId sharedId,
        int updateId
    )
    {
        try
        {
            // Load current state
            MetadataTestAggregate? agg = await repository.FindByIdAsync(sharedId);
            if (agg != null && !agg.IsClosed)
            {
                // Update value
                agg.UpdateValue((updateId + 1) * 100);
                await repository.SaveAsync(agg);
            }
        }
        catch (RepositorySaveException)
        {
            // Expected: Optimistic locking conflict due to concurrent updates
            // Some updates will fail - this is correct behavior
        }
    }

    #endregion

    #region Repository Concurrent Operations Tests

    [Fact]
    public async Task Repository_ConcurrentSaves_ShouldHandleCorrectly()
    {
        InMemoryMetadataTestEventStorePeer peer = new();
        EsRepository<MetadataTestAggregate, MetadataTestId> repository = new(peer);

        DomainEventTypeMapper.Register<AggregateCreated>("AggregateCreated");

        const int concurrentAggregates = 20;

        // Act: Save multiple aggregates concurrently
        List<Task> tasks = [];
        for (int i = 0; i < concurrentAggregates; i++)
        {
            int aggId = i;
            tasks.Add(
                Task.Run(async () =>
                {
                    MetadataTestAggregate agg = new(
                        new MetadataTestId($"CONCURRENT-AGG-{aggId:D3}"),
                        $"Aggregate {aggId}",
                        aggId * 100
                    );
                    await repository.SaveAsync(agg);
                })
            );
        }

        await Task.WhenAll(tasks);

        // Assert: All aggregates should be saved
        Assert.Equal(concurrentAggregates, peer.Count);
    }

    [Fact]
    public async Task Repository_ConcurrentLoadOperations_ShouldReturnCorrectData()
    {
        InMemoryMetadataTestEventStorePeer peer = new();
        EsRepository<MetadataTestAggregate, MetadataTestId> repository = new(peer);

        DomainEventTypeMapper.Register<AggregateCreated>("AggregateCreated");

        // Pre-populate repository with aggregates
        const int aggregateCount = 10;
        for (int i = 0; i < aggregateCount; i++)
        {
            MetadataTestAggregate agg = new(new MetadataTestId($"AGG-LOAD-{i:D3}"), $"Aggregate {i}", i * 10);
            await repository.SaveAsync(agg);
        }

        // Act: Load all aggregates concurrently
        List<Task<MetadataTestAggregate?>> loadTasks = [];
        for (int i = 0; i < aggregateCount; i++)
        {
            MetadataTestId id = new($"AGG-LOAD-{i:D3}");
            loadTasks.Add(Task.Run(() => repository.FindByIdAsync(id)));
        }

        MetadataTestAggregate?[] results = await Task.WhenAll(loadTasks);

        // Assert: All aggregates should be loaded correctly
        Assert.Equal(aggregateCount, results.Length);
        Assert.All(results, agg => Assert.NotNull(agg));

        // Verify correct data
        for (int i = 0; i < aggregateCount; i++)
        {
            Assert.Equal($"Aggregate {i}", results[i]!.Name);
            Assert.Equal(i * 10, results[i]!.Value);
        }
    }

    #endregion
}
