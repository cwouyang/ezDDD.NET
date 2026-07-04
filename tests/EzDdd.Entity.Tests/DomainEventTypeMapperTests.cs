namespace EzDdd.Entity.Tests;

[Collection("DomainEventTypeMapper")]
public class DomainEventTypeMapperTests
{
    #region Register Tests

    [Fact]
    public void Register_WithTypeAndName_StoresMapping()
    {
        DomainEventTypeMapper.Clear(); // Reset for test isolation

        DomainEventTypeMapper.Register<TestEvent1>("TestEvent1");

        Assert.True(DomainEventTypeMapper.Contains("TestEvent1"));
    }

    [Fact]
    public void Register_WithSameNameTwice_ThrowsException()
    {
        DomainEventTypeMapper.Clear();
        DomainEventTypeMapper.Register<TestEvent1>("TestEvent1");

        Assert.Throws<ArgumentException>(() => DomainEventTypeMapper.Register<TestEvent2>("TestEvent1"));
    }

    [Fact]
    public void Register_WithSameTypeTwice_ThrowsException()
    {
        DomainEventTypeMapper.Clear();
        DomainEventTypeMapper.Register<TestEvent1>("TestEvent1");

        Assert.Throws<ArgumentException>(() => DomainEventTypeMapper.Register<TestEvent1>("TestEvent1_Duplicate"));
    }

    #endregion

    #region GetTypeName Tests

    [Fact]
    public void GetTypeName_FromType_ReturnsRegisteredName()
    {
        DomainEventTypeMapper.Clear();
        DomainEventTypeMapper.Register<TestEvent1>("TestEvent1");

        string typeName = DomainEventTypeMapper.GetTypeName(typeof(TestEvent1));

        Assert.Equal("TestEvent1", typeName);
    }

    [Fact]
    public void GetTypeName_FromEvent_ReturnsRegisteredName()
    {
        DomainEventTypeMapper.Clear();
        DomainEventTypeMapper.Register<TestEvent1>("TestEvent1");
        TestEvent1 @event = new(Guid.NewGuid(), DateTimeOffset.UtcNow, "source", new Dictionary<string, string>());

        string typeName = DomainEventTypeMapper.GetTypeName(@event);

        Assert.Equal("TestEvent1", typeName);
    }

    [Fact]
    public void GetTypeName_UnregisteredType_ThrowsException()
    {
        DomainEventTypeMapper.Clear();

        Assert.Throws<InvalidOperationException>(() => DomainEventTypeMapper.GetTypeName(typeof(TestEvent1)));
    }

    #endregion

    #region GetType Tests

    [Fact]
    public void GetType_WithRegisteredName_ReturnsType()
    {
        DomainEventTypeMapper.Clear();
        DomainEventTypeMapper.Register<TestEvent1>("TestEvent1");

        Type type = DomainEventTypeMapper.GetType("TestEvent1");

        Assert.Equal(typeof(TestEvent1), type);
    }

    [Fact]
    public void GetType_WithUnregisteredName_ThrowsException()
    {
        DomainEventTypeMapper.Clear();

        Assert.Throws<InvalidOperationException>(() => DomainEventTypeMapper.GetType("UnknownEvent"));
    }

    #endregion

    #region Contains Tests

    [Fact]
    public void Contains_WithRegisteredName_ReturnsTrue()
    {
        DomainEventTypeMapper.Clear();
        DomainEventTypeMapper.Register<TestEvent1>("TestEvent1");

        bool contains = DomainEventTypeMapper.Contains("TestEvent1");

        Assert.True(contains);
    }

    [Fact]
    public void Contains_WithUnregisteredName_ReturnsFalse()
    {
        DomainEventTypeMapper.Clear();

        bool contains = DomainEventTypeMapper.Contains("UnknownEvent");

        Assert.False(contains);
    }

    #endregion

    #region GetAllMappings Tests

    [Fact]
    public void GetAllMappings_ReturnsAllRegisteredMappings()
    {
        DomainEventTypeMapper.Clear();
        DomainEventTypeMapper.Register<TestEvent1>("TestEvent1");
        DomainEventTypeMapper.Register<TestEvent2>("TestEvent2");
        DomainEventTypeMapper.Register<TestEvent3>("TestEvent3");

        IReadOnlyDictionary<string, Type> mappings = DomainEventTypeMapper.GetAllMappings();

        Assert.Equal(3, mappings.Count);
        Assert.True(mappings.ContainsKey("TestEvent1"));
        Assert.True(mappings.ContainsKey("TestEvent2"));
        Assert.True(mappings.ContainsKey("TestEvent3"));
        Assert.Equal(typeof(TestEvent1), mappings["TestEvent1"]);
    }

    [Fact]
    public void GetAllMappings_ReturnsReadOnlyDictionary()
    {
        DomainEventTypeMapper.Clear();
        DomainEventTypeMapper.Register<TestEvent1>("TestEvent1");

        IReadOnlyDictionary<string, Type> mappings = DomainEventTypeMapper.GetAllMappings();

        Assert.IsAssignableFrom<IReadOnlyDictionary<string, Type>>(mappings);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllMappings()
    {
        DomainEventTypeMapper.Register<TestEvent1>("TestEvent1");
        DomainEventTypeMapper.Register<TestEvent2>("TestEvent2");

        DomainEventTypeMapper.Clear();

        Assert.False(DomainEventTypeMapper.Contains("TestEvent1"));
        Assert.False(DomainEventTypeMapper.Contains("TestEvent2"));
        Assert.Empty(DomainEventTypeMapper.GetAllMappings());
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task ThreadSafety_ConcurrentRegister_HandlesCorrectly()
    {
        DomainEventTypeMapper.Clear();
        List<Task> tasks = [];
        const int eventCount = 50;

        // Register events concurrently
        for (int i = 0; i < eventCount; i++)
        {
            int index = i;
            tasks.Add(
                Task.Run(() =>
                {
                    try
                    {
                        // Each task registers a unique event type
                        // Using TestEvent1, TestEvent2, TestEvent3 repeatedly
                        switch (index % 3)
                        {
                            case 0:
                                DomainEventTypeMapper.Register<TestEvent1>($"Event_{index}");
                                break;
                            case 1:
                                DomainEventTypeMapper.Register<TestEvent2>($"Event_{index}");
                                break;
                            default:
                                DomainEventTypeMapper.Register<TestEvent3>($"Event_{index}");
                                break;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Duplicate registration is expected in concurrent scenario
                    }
                })
            );
        }
        await Task.WhenAll(tasks);

        // All unique registrations should be present
        IReadOnlyDictionary<string, Type> mappings = DomainEventTypeMapper.GetAllMappings();
        Assert.True(mappings.Count > 0);
        Assert.True(mappings.Count <= eventCount);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentReadWrite_NoDeadlock()
    {
        DomainEventTypeMapper.Clear();
        DomainEventTypeMapper.Register<TestEvent1>("TestEvent1");
        List<Task> tasks = [];
        const int operationCount = 100;

        // Concurrent reads and writes
        for (int i = 0; i < operationCount; i++)
        {
            int index = i;

            // Write task
            tasks.Add(
                Task.Run(() =>
                {
                    try
                    {
                        DomainEventTypeMapper.Register<TestEvent2>($"Event_{index}");
                    }
                    catch (ArgumentException) { }
                })
            );

            // Read task
            tasks.Add(
                Task.Run(() =>
                {
                    try
                    {
                        string _ = DomainEventTypeMapper.GetTypeName(typeof(TestEvent1));
                        bool __ = DomainEventTypeMapper.Contains("TestEvent1");
                        IReadOnlyDictionary<string, Type> ___ = DomainEventTypeMapper.GetAllMappings();
                    }
                    catch { }
                })
            );
        }
        await Task.WhenAll(tasks);

        Assert.True(DomainEventTypeMapper.Contains("TestEvent1"));
    }

    #endregion

    // Test events
    private record TestEvent1(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private record TestEvent2(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        int Value,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private record TestEvent3(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;
}
