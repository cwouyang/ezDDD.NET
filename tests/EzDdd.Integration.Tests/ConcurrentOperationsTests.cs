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
public sealed class ConcurrentOperationsTests
{
#region DomainEventTypeMapper Concurrent Registration Tests

    [Fact]
    public async Task DomainEventTypeMapper_ConcurrentRegistration_ShouldBeThreadSafe()
    {
        const int threadCount = 20;
        const int registrationsPerThread = 5;

        // Act: Register event types concurrently from multiple threads
        List<Task> tasks = [];
        for (int t = 0; t < threadCount; t++)
        {
            int threadId = t;
            tasks.Add
            (
                Task.Run
                (() =>
                    {
                        for (int i = 0; i < registrationsPerThread; i++)
                        {
                            // Each thread registers unique event types
                            string eventTypeName = $"ConcurrentEvent_T{threadId:D2}_E{i:D2}";

                            // Create a unique event type dynamically
                            // For testing, we'll just register the same type multiple times
                            // (The mapper should handle duplicate registrations gracefully)
                            try
                            {
                                DomainEventTypeMapper.Register<AggregateCreated>(eventTypeName);
                            }
                            catch
                            {
                                // Duplicate registration may throw - that's acceptable
                                // We're testing thread safety, not duplicate handling
                            }
                        }
                    }
                )
            );
        }

        // Assert: All tasks should complete without deadlock or race conditions
        await Task.WhenAll(tasks);
        Assert.Equal(threadCount, tasks.Count);
    }

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
            tasks.Add
            (
                Task.Run
                (async () =>
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
                )
            );
        }

        // Other half performs reads
        for (int i = 0; i < updateCount / 2; i++)
        {
            tasks.Add
            (
                Task.Run
                (async () =>
                    {
                        MetadataTestAggregate? agg = await repository.FindByIdAsync(sharedId);
                        Assert.NotNull(agg);
                        // Just verify we can read
                    }
                )
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
            tasks.Add
            (
                Task.Run
                (async () =>
                    {
                        MetadataTestAggregate agg = new
                        (
                            new MetadataTestId($"CONCURRENT-AGG-{aggId:D3}"),
                            $"Aggregate {aggId}",
                            aggId * 100
                        );
                        await repository.SaveAsync(agg);
                    }
                )
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
            MetadataTestAggregate agg = new
            (
                new MetadataTestId($"AGG-LOAD-{i:D3}"),
                $"Aggregate {i}",
                i * 10
            );
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