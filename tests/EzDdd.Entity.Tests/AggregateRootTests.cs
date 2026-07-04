namespace EzDdd.Entity.Tests;

public class AggregateRootTests
{
    #region Construction Tests

    [Fact]
    public void AggregateRoot_Constructor_InitializesProperties()
    {
        Guid id = Guid.NewGuid();

        TestAggregate aggregate = new(id, "Test");

        Assert.Equal(id, aggregate.Id);
        Assert.Equal("Test", aggregate.Name);
        Assert.Equal(0L, aggregate.Version); // Version = 0 after first event
        Assert.False(aggregate.IsDeleted);
    }

    [Fact]
    public void AggregateRoot_InitialVersion_IsMinusOne()
    {
        TestAggregate aggregate = new(Guid.NewGuid(), "Test");

        // After constructor adds one event, version should be 0
        // (starts at -1, increments to 0)
        Assert.Equal(0L, aggregate.Version);
    }

    #endregion

    #region Event Collection Tests

    [Fact]
    public void AggregateRoot_Apply_AddsEventToCollection()
    {
        TestAggregate aggregate = new(Guid.NewGuid(), "Test");
        aggregate.ClearDomainEvents(); // Clear constructor event

        aggregate.UpdateValue(42);

        IReadOnlyList<IInternalDomainEvent> events = aggregate.GetDomainEvents();
        Assert.Single(events);
        Assert.IsType<TestUpdatedEvent>(events[0]);
    }

    [Fact]
    public void AggregateRoot_GetDomainEvents_ReturnsReadOnlyList()
    {
        TestAggregate aggregate = new(Guid.NewGuid(), "Test");
        aggregate.UpdateValue(42);

        IReadOnlyList<IInternalDomainEvent> events = aggregate.GetDomainEvents();

        Assert.IsAssignableFrom<IReadOnlyList<IInternalDomainEvent>>(events);
    }

    [Fact]
    public void AggregateRoot_GetDomainEvents_ReturnsSnapshot()
    {
        TestAggregate aggregate = new(Guid.NewGuid(), "Test");
        IReadOnlyList<IInternalDomainEvent> firstSnapshot = aggregate.GetDomainEvents();

        // Add more events after getting snapshot
        aggregate.UpdateValue(42);
        IReadOnlyList<IInternalDomainEvent> secondSnapshot = aggregate.GetDomainEvents();

        // First snapshot unchanged
        Assert.Single(firstSnapshot); // Only constructor event
        Assert.Equal(2, secondSnapshot.Count); // Constructor + update
    }

    [Fact]
    public void AggregateRoot_GetLastDomainEvent_ReturnsLastEvent()
    {
        TestAggregate aggregate = new(Guid.NewGuid(), "Test");
        aggregate.UpdateValue(1);
        aggregate.UpdateValue(2);
        aggregate.UpdateValue(3);

        IInternalDomainEvent? lastEvent = aggregate.GetLastDomainEvent();

        Assert.NotNull(lastEvent);
        Assert.IsType<TestUpdatedEvent>(lastEvent);
        Assert.Equal(3, ((TestUpdatedEvent)lastEvent).Value);
    }

    [Fact]
    public void AggregateRoot_GetLastDomainEvent_WhenNoEvents_ReturnsNull()
    {
        TestAggregate aggregate = new(Guid.NewGuid(), "Test");
        aggregate.ClearDomainEvents();

        IInternalDomainEvent? lastEvent = aggregate.GetLastDomainEvent();

        Assert.Null(lastEvent);
    }

    [Fact]
    public void AggregateRoot_GetDomainEventSize_ReturnsCorrectCount()
    {
        TestAggregate aggregate = new(Guid.NewGuid(), "Test");
        // Constructor adds 1 event

        Assert.Equal(1, aggregate.GetDomainEventSize());

        aggregate.UpdateValue(42);
        Assert.Equal(2, aggregate.GetDomainEventSize());

        aggregate.ClearDomainEvents();
        Assert.Equal(0, aggregate.GetDomainEventSize());
    }

    #endregion

    #region Versioning Tests

    [Fact]
    public void AggregateRoot_Apply_IncrementsVersion()
    {
        TestAggregate aggregate = new(Guid.NewGuid(), "Test");
        long initialVersion = aggregate.Version; // 0 after constructor

        aggregate.UpdateValue(42);

        Assert.Equal(initialVersion + 1, aggregate.Version);
    }

    [Fact]
    public void AggregateRoot_MultipleApplies_IncrementsVersionSequentially()
    {
        TestAggregate aggregate = new(Guid.NewGuid(), "Test");
        // Constructor adds 1 event, version = 0

        aggregate.UpdateValue(1); // version = 1
        aggregate.UpdateValue(2); // version = 2
        aggregate.UpdateValue(3); // version = 3

        Assert.Equal(3L, aggregate.Version);
        Assert.Equal(4, aggregate.GetDomainEventSize()); // 1 constructor + 3 updates
    }

    #endregion

    #region PublishAndClear Tests

    [Fact]
    public void AggregateRoot_ClearDomainEvents_RemovesAllEvents()
    {
        TestAggregate aggregate = new(Guid.NewGuid(), "Test");
        aggregate.UpdateValue(42);
        Assert.True(aggregate.GetDomainEventSize() > 0);

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.GetDomainEvents());
        Assert.Equal(0, aggregate.GetDomainEventSize());
    }

    [Fact]
    public void AggregateRoot_ClearDomainEvents_DoesNotResetVersion()
    {
        TestAggregate aggregate = new(Guid.NewGuid(), "Test");
        aggregate.UpdateValue(42);
        long versionBeforeClear = aggregate.Version;

        aggregate.ClearDomainEvents();

        // Version preserved (repository uses it for optimistic locking)
        Assert.Equal(versionBeforeClear, aggregate.Version);
    }

    #endregion

    #region IsDeleted Tests

    [Fact]
    public void AggregateRoot_IsDeleted_CanBeSetBySubclass()
    {
        TestAggregate aggregate = new(Guid.NewGuid(), "Test");
        Assert.False(aggregate.IsDeleted);

        aggregate.Delete("Test deletion");

        Assert.True(aggregate.IsDeleted);
    }

    #endregion

    #region Contract Tests

    [Fact]
    public void AggregateRoot_ImplementsIEntity()
    {
        TestAggregate aggregate = new(Guid.NewGuid(), "Test");

        Assert.IsAssignableFrom<IEntity<Guid>>(aggregate);
    }

    [Fact]
    public void AggregateRoot_GenericConstraint_EnforcesIInternalDomainEvent()
    {
        // This test verifies compile-time constraint
        // TEvent is constrained to IInternalDomainEvent

        TestAggregate aggregate = new(Guid.NewGuid(), "Test");

        // All events in collection must be IInternalDomainEvent
        IReadOnlyList<IInternalDomainEvent> events = aggregate.GetDomainEvents();
        Assert.All(events, e => Assert.IsAssignableFrom<IInternalDomainEvent>(e));
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task AggregateRoot_ThreadSafety_ConcurrentEventAddition()
    {
        TestAggregate aggregate = new(Guid.NewGuid(), "Test");
        List<Task> tasks = [];
        const int taskCount = 100;

        // Add events concurrently from multiple threads
        for (int i = 0; i < taskCount; i++)
        {
            tasks.Add(Task.Run(() => aggregate.UpdateValue(i)));
        }

        await Task.WhenAll(tasks);

        // All events should be collected
        // 1 constructor event + 100 update events = 101
        Assert.Equal(taskCount + 1, aggregate.GetDomainEventSize());
        Assert.Equal(taskCount, aggregate.Version);
    }

    [Fact]
    public async Task AggregateRoot_ThreadSafety_ConcurrentReadAndWrite()
    {
        TestAggregate aggregate = new(Guid.NewGuid(), "Test");
        List<Task> tasks = [];
        const int operationCount = 50;

        // Concurrent reads and writes
        for (int i = 0; i < operationCount; i++)
        {
            // Write task
            tasks.Add(Task.Run(() => aggregate.UpdateValue(i)));

            // Read task
            tasks.Add(
                Task.Run(() =>
                {
                    IReadOnlyList<IInternalDomainEvent> _ = aggregate.GetDomainEvents();
                    int __ = aggregate.GetDomainEventSize();
                    IInternalDomainEvent? ___ = aggregate.GetLastDomainEvent();
                })
            );
        }

        await Task.WhenAll(tasks);

        // Should not throw
        Assert.True(aggregate.GetDomainEventSize() > 0);
    }

    #endregion

    // Test events
    private sealed record TestCreatedEvent(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string Name,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent;

    private sealed record TestUpdatedEvent(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        int Value,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    private sealed record TestDeletedEvent(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string Reason,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IDestructionEvent;

    // Test aggregate implementation (state sourcing)
    private sealed class TestAggregate : AggregateRoot<Guid, IInternalDomainEvent>
    {
        public TestAggregate(Guid id, string name)
        {
            Id = id;
            Name = name;

            TestCreatedEvent created = new(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                id.ToString(),
                name,
                new Dictionary<string, string>()
            );

            Apply(created);
        }

        public string Name { get; }

        public int Value { get; private set; }

        public void UpdateValue(int newValue)
        {
            Value = newValue;

            TestUpdatedEvent updated = new(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                Id.ToString(),
                newValue,
                new Dictionary<string, string>()
            );

            Apply(updated);
        }

        public void Delete(string reason)
        {
            IsDeleted = true;

            TestDeletedEvent deleted = new(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                Id.ToString(),
                reason,
                new Dictionary<string, string>()
            );

            Apply(deleted);
        }
    }
}
