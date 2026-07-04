using EzDdd.Entity;
using EzDdd.UseCase.Port.Out;
using Xunit;

namespace EzDdd.UseCase.Tests.Port.Out;

public class EventStoreMapperTests
{
    #region Test Helpers

    private record TestId(string Value)
    {
        public override string ToString() => Value;
    }

    private record TestEvent(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string EventType,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent;

    private class TestAggregate : EsAggregateRoot<TestId, IInternalDomainEvent>
    {
        private string _name = string.Empty;

        public string Name => _name;

        public TestAggregate(TestId id, string name)
            : base()
        {
            Id = id; // Set Id directly since we're not calling base(id)
            var @event = new TestEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                id.Value,
                "TestCreated",
                new Dictionary<string, string>()
            );
            Apply(@event);
            _name = name;
        }

        public TestAggregate(IEnumerable<IInternalDomainEvent> events)
            : base(events) { }

        public void DoSomething(string action)
        {
            var @event = new TestEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                Id.Value,
                action,
                new Dictionary<string, string>()
            );
            Apply(@event);
        }

        protected override void _When(IInternalDomainEvent @event)
        {
            switch (@event)
            {
                case TestEvent te:
                    _name = "Reconstructed";
                    // Extract Id from event source if not set
                    if (Id == null || Id.Value == string.Empty)
                    {
                        Id = new TestId(te.Source);
                    }

                    break;
                default:
                    throw new InvalidOperationException($"Unknown event type: {@event.GetType().Name}");
            }
        }

        protected override void _EnsureInvariant()
        {
            // No invariants for test aggregate
        }

        public override string GetCategory() => "test";
    }

    #endregion

    #region ToData Tests

    [Fact]
    public void ToData_WithValidAggregate_ReturnsEventStoreData()
    {
        var id = new TestId("test-123");
        var aggregate = new TestAggregate(id, "TestName");

        var data = EventStoreMapper.ToData(aggregate);

        Assert.NotNull(data);
        Assert.Equal(id, data.Id);
        Assert.Equal(0, data.Version); // New aggregate has version 0
        Assert.Single(data.Events); // One construction event
        Assert.Equal("test-test-123", data.StreamName);
    }

    [Fact]
    public void ToData_CreatesDefensiveCopyOfEvents()
    {
        var id = new TestId("test-456");
        var aggregate = new TestAggregate(id, "TestName");
        var originalEventCount = aggregate.GetDomainEvents().Count;

        var data = EventStoreMapper.ToData(aggregate);
        aggregate.ClearDomainEvents(); // Clear events in aggregate

        Assert.Equal(originalEventCount, data.Events.Count); // Data should still have events
    }

    [Fact]
    public void ToData_PreservesEventOrder()
    {
        var id = new TestId("test-789");
        var aggregate = new TestAggregate(id, "TestName");
        var originalEvents = aggregate.GetDomainEvents().ToList();

        var data = EventStoreMapper.ToData(aggregate);

        Assert.Equal(originalEvents.Count, data.Events.Count);
        for (int i = 0; i < originalEvents.Count; i++)
        {
            Assert.Same(originalEvents[i], data.Events[i]);
        }
    }

    [Fact]
    public void ToData_WithMultipleEvents_MapsAllEvents()
    {
        var id = new TestId("test-multi");
        var aggregate = new TestAggregate(id, "TestName");
        aggregate.DoSomething("Action1");
        aggregate.DoSomething("Action2");
        // Total: 3 events (1 creation + 2 actions)

        var data = EventStoreMapper.ToData(aggregate);

        Assert.Equal(3, data.Events.Count);
    }

    [Fact]
    public void ToData_WithVersionedAggregate_PreservesVersion()
    {
        var id = new TestId("test-versioned");
        var events = new List<IInternalDomainEvent>
        {
            new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, id.Value, "Event1", new Dictionary<string, string>()),
        };
        var aggregate = new TestAggregate(events);

        // Simulate that aggregate was loaded with version 5
        // (In real scenario, this would be set by repository during load)
        // For testing, we'll verify the Version property is correctly copied

        var data = EventStoreMapper.ToData(aggregate);

        Assert.Equal(aggregate.Version, data.Version);
    }

    #endregion

    #region ToDomain Tests

    [Fact]
    public void ToDomain_ThrowsNotSupportedException()
    {
        var data = new EventStoreData<TestId>
        {
            Id = new TestId("test-123"),
            Version = 0,
            Events = new List<IInternalDomainEvent>(),
            StreamName = "test-123",
        };

        var exception = Assert.Throws<NotSupportedException>(() =>
            EventStoreMapper.ToDomain<TestAggregate, TestId>(data)
        );

        Assert.Contains("Event sourcing aggregates are reconstructed from events", exception.Message);
    }

    [Fact]
    public void ToDomain_WithAnyData_AlwaysThrows()
    {
        var data = new EventStoreData<TestId>
        {
            Id = new TestId("test-456"),
            Version = 10,
            Events = new List<IInternalDomainEvent>
            {
                new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", "Event", new Dictionary<string, string>()),
            },
            StreamName = "test-456",
        };

        Assert.Throws<NotSupportedException>(() => EventStoreMapper.ToDomain<TestAggregate, TestId>(data));
    }

    #endregion
}
