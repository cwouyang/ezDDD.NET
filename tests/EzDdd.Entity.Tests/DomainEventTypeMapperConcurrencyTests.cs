using System.Collections.Concurrent;

namespace EzDdd.Entity.Tests;

/// <summary>
///     Thread safety and concurrency tests for DomainEventTypeMapper.
/// </summary>
/// <remarks>
///     These tests verify that DomainEventTypeMapper handles concurrent access correctly
///     using Lazy&lt;BiMap&gt; for thread-safe initialization (Java 4.1.0 alignment).
/// </remarks>
[Collection("DomainEventTypeMapper")]
public class DomainEventTypeMapperConcurrencyTests : IDisposable
{
    public DomainEventTypeMapperConcurrencyTests()
    {
        // Ensure clean state for each test
        DomainEventTypeMapper.Clear();
    }

    public void Dispose()
    {
        // Clean up after each test
        DomainEventTypeMapper.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Test that multiple threads can concurrently register different event types without conflicts.
    /// </summary>
    [Fact]
    public void ConcurrentRegistration_DifferentTypes_ShouldSucceed()
    {
        const int THREAD_COUNT = 10;
        ConcurrentBag<Exception> exceptions = [];

        // Act - Launch multiple threads registering different types
        Parallel.For(
            0,
            THREAD_COUNT,
            i =>
            {
                try
                {
                    switch (i)
                    {
                        case 0:
                            DomainEventTypeMapper.Register<TestEvent1>($"TestEvent{i}");
                            break;
                        case 1:
                            DomainEventTypeMapper.Register<TestEvent2>($"TestEvent{i}");
                            break;
                        case 2:
                            DomainEventTypeMapper.Register<TestEvent3>($"TestEvent{i}");
                            break;
                        case 3:
                            DomainEventTypeMapper.Register<TestEvent4>($"TestEvent{i}");
                            break;
                        case 4:
                            DomainEventTypeMapper.Register<TestEvent5>($"TestEvent{i}");
                            break;
                        case 5:
                            DomainEventTypeMapper.Register<TestEvent6>($"TestEvent{i}");
                            break;
                        case 6:
                            DomainEventTypeMapper.Register<TestEvent7>($"TestEvent{i}");
                            break;
                        case 7:
                            DomainEventTypeMapper.Register<TestEvent8>($"TestEvent{i}");
                            break;
                        case 8:
                            DomainEventTypeMapper.Register<TestEvent9>($"TestEvent{i}");
                            break;
                        case 9:
                            DomainEventTypeMapper.Register<TestEvent10>($"TestEvent{i}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        );

        Assert.Empty(exceptions);
        Assert.Equal(THREAD_COUNT, DomainEventTypeMapper.GetAllMappings().Count);
    }

    /// <summary>
    ///     Test that multiple threads attempting to register the same type with the same name
    ///     succeed idempotently (no exceptions thrown).
    /// </summary>
    [Fact]
    public void ConcurrentRegistration_SameTypeAndName_ShouldBeIdempotent()
    {
        const int THREAD_COUNT = 20;
        ConcurrentBag<Exception> exceptions = [];

        // Act - Launch multiple threads registering the same type/name
        Parallel.For(
            0,
            THREAD_COUNT,
            _ =>
            {
                try
                {
                    DomainEventTypeMapper.Register<TestEvent1>("TestEvent1");
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        );

        // Assert - All threads should succeed (idempotent operation)
        Assert.Empty(exceptions);
        Assert.Single(DomainEventTypeMapper.GetAllMappings());
        Assert.Equal(typeof(TestEvent1), DomainEventTypeMapper.GetType("TestEvent1"));
    }

    /// <summary>
    ///     Test that multiple threads can concurrently read type names after registration.
    /// </summary>
    [Fact]
    public void ConcurrentGetTypeName_AfterRegistration_ShouldSucceed()
    {
        DomainEventTypeMapper.Register<TestEvent1>("TestEvent1");
        DomainEventTypeMapper.Register<TestEvent2>("TestEvent2");
        DomainEventTypeMapper.Register<TestEvent3>("TestEvent3");

        const int THREAD_COUNT = 50;
        ConcurrentBag<string> results = [];
        ConcurrentBag<Exception> exceptions = [];

        // Act - Launch multiple threads reading type names
        Parallel.For(
            0,
            THREAD_COUNT,
            i =>
            {
                try
                {
                    Type type = (i % 3) switch
                    {
                        0 => typeof(TestEvent1),
                        1 => typeof(TestEvent2),
                        _ => typeof(TestEvent3),
                    };

                    string typeName = DomainEventTypeMapper.GetTypeName(type);
                    results.Add(typeName);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        );

        Assert.Empty(exceptions);
        Assert.Equal(THREAD_COUNT, results.Count);
        Assert.Contains("TestEvent1", results);
        Assert.Contains("TestEvent2", results);
        Assert.Contains("TestEvent3", results);
    }

    /// <summary>
    ///     Test that multiple threads can concurrently read event types after registration.
    /// </summary>
    [Fact]
    public void ConcurrentGetType_AfterRegistration_ShouldSucceed()
    {
        DomainEventTypeMapper.Register<TestEvent1>("TestEvent1");
        DomainEventTypeMapper.Register<TestEvent2>("TestEvent2");
        DomainEventTypeMapper.Register<TestEvent3>("TestEvent3");
        const int THREAD_COUNT = 50;
        ConcurrentBag<Type> results = [];
        ConcurrentBag<Exception> exceptions = [];

        // Act - Launch multiple threads reading types
        Parallel.For(
            0,
            THREAD_COUNT,
            i =>
            {
                try
                {
                    string typeName = (i % 3) switch
                    {
                        0 => "TestEvent1",
                        1 => "TestEvent2",
                        _ => "TestEvent3",
                    };

                    Type type = DomainEventTypeMapper.GetType(typeName);
                    results.Add(type);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        );

        Assert.Empty(exceptions);
        Assert.Equal(THREAD_COUNT, results.Count);
        Assert.Contains(typeof(TestEvent1), results);
        Assert.Contains(typeof(TestEvent2), results);
        Assert.Contains(typeof(TestEvent3), results);
    }

    /// <summary>
    ///     Test mixed concurrent operations: registration and reads happening simultaneously.
    /// </summary>
    [Fact]
    public void ConcurrentMixedOperations_ShouldSucceed()
    {
        const int THREAD_COUNT = 30;
        ConcurrentBag<Exception> exceptions = [];
        ConcurrentBag<string> readResults = [];

        // Act - Launch threads doing different operations
        Parallel.For(
            0,
            THREAD_COUNT,
            i =>
            {
                try
                {
                    switch (i)
                    {
                        case < 10:
                            // First 10 threads: Register
                            _RegisterEventForThread(i);

                            break;
                        case < 20:
                        {
                            // Next 10 threads: Read type names (might fail if not registered yet)
                            Thread.Sleep(10); // Small delay to increase chance of successful reads
                            if (DomainEventTypeMapper.Contains("Event1"))
                            {
                                string typeName = DomainEventTypeMapper.GetTypeName(typeof(TestEvent1));
                                readResults.Add(typeName);
                            }

                            break;
                        }
                        default:
                        {
                            // Last 10 threads: Check contains
                            Thread.Sleep(20); // Larger delay
                            bool contains = DomainEventTypeMapper.Contains("Event1");
                            if (contains)
                            {
                                readResults.Add("Found");
                            }

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        );

        Assert.Empty(exceptions);
        // At least Event1-Event5 should be registered
        Assert.True(DomainEventTypeMapper.GetAllMappings().Count >= 5);
    }

    /// <summary>
    ///     Test that Contains() works correctly under concurrent access.
    /// </summary>
    [Fact]
    public void ConcurrentContains_AfterRegistration_ShouldSucceed()
    {
        DomainEventTypeMapper.Register<TestEvent1>("TestEvent1");
        DomainEventTypeMapper.Register<TestEvent2>("TestEvent2");
        const int THREAD_COUNT = 40;
        ConcurrentBag<bool> results = [];
        ConcurrentBag<Exception> exceptions = [];

        Parallel.For(
            0,
            THREAD_COUNT,
            i =>
            {
                try
                {
                    bool contains =
                        i % 2 == 0
                            ? DomainEventTypeMapper.Contains("TestEvent1")
                            : DomainEventTypeMapper.Contains("TestEvent2");
                    results.Add(contains);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        );

        Assert.Empty(exceptions);
        Assert.Equal(THREAD_COUNT, results.Count);
        Assert.All(results, result => Assert.True(result));
    }

    /// <summary>
    ///     Stress test with high thread count to verify no race conditions occur.
    /// </summary>
    [Fact]
    public void StressTest_HighConcurrency_ShouldSucceed()
    {
        const int THREAD_COUNT = 100;
        ConcurrentBag<Exception> exceptions = [];
        var registrationCount = 0;
        var readCount = 0;

        // Act - High concurrency with mixed operations
        Parallel.For(
            0,
            THREAD_COUNT,
            i =>
            {
                try
                {
                    if (i % 10 == 0)
                    {
                        // Every 10th thread registers
                        _RegisterStressEvent(i / 10);
                        Interlocked.Increment(ref registrationCount);
                    }
                    else
                    {
                        // Other threads read
                        Thread.Sleep(5); // Small delay
                        _ = DomainEventTypeMapper.GetAllMappings();
                        Interlocked.Increment(ref readCount);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        );

        Assert.Empty(exceptions);
        Assert.True(registrationCount > 0);
        Assert.True(readCount > 0);
        Assert.True(DomainEventTypeMapper.GetAllMappings().Count >= 10);
    }

    /// <summary>
    ///     Registers the event type assigned to the given registering thread (idempotent re-registration
    ///     for thread indexes above the distinct event count).
    /// </summary>
    private static void _RegisterEventForThread(int threadIndex)
    {
        switch (threadIndex)
        {
            case 0:
                DomainEventTypeMapper.Register<TestEvent1>("Event1");
                break;
            case 1:
                DomainEventTypeMapper.Register<TestEvent2>("Event2");
                break;
            case 2:
                DomainEventTypeMapper.Register<TestEvent3>("Event3");
                break;
            case 3:
                DomainEventTypeMapper.Register<TestEvent4>("Event4");
                break;
            case 4:
                DomainEventTypeMapper.Register<TestEvent5>("Event5");
                break;
            default:
                // Re-register existing types (idempotent)
                DomainEventTypeMapper.Register<TestEvent1>("Event1");
                break;
        }
    }

    /// <summary>
    ///     Registers one of the ten stress-test event types based on the event number.
    /// </summary>
    private static void _RegisterStressEvent(int eventNumber)
    {
        switch (eventNumber % 10)
        {
            case 0:
                DomainEventTypeMapper.Register<TestEvent1>($"StressEvent{eventNumber}");
                break;
            case 1:
                DomainEventTypeMapper.Register<TestEvent2>($"StressEvent{eventNumber}");
                break;
            case 2:
                DomainEventTypeMapper.Register<TestEvent3>($"StressEvent{eventNumber}");
                break;
            case 3:
                DomainEventTypeMapper.Register<TestEvent4>($"StressEvent{eventNumber}");
                break;
            case 4:
                DomainEventTypeMapper.Register<TestEvent5>($"StressEvent{eventNumber}");
                break;
            case 5:
                DomainEventTypeMapper.Register<TestEvent6>($"StressEvent{eventNumber}");
                break;
            case 6:
                DomainEventTypeMapper.Register<TestEvent7>($"StressEvent{eventNumber}");
                break;
            case 7:
                DomainEventTypeMapper.Register<TestEvent8>($"StressEvent{eventNumber}");
                break;
            case 8:
                DomainEventTypeMapper.Register<TestEvent9>($"StressEvent{eventNumber}");
                break;
            case 9:
                DomainEventTypeMapper.Register<TestEvent10>($"StressEvent{eventNumber}");
                break;
        }
    }

    // Test event types (need 10 different types for concurrency tests)
    private sealed record TestEvent1(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private sealed record TestEvent2(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private sealed record TestEvent3(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private sealed record TestEvent4(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private sealed record TestEvent5(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private sealed record TestEvent6(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private sealed record TestEvent7(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private sealed record TestEvent8(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private sealed record TestEvent9(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private sealed record TestEvent10(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;
}
