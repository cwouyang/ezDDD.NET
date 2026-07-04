using EzDdd.Entity;
using EzDdd.UseCase.Port.Out;
using Xunit;

namespace EzDdd.UseCase.Tests.Port.Out;

public class EventStoreDataTests
{
    #region Test Helpers

    private sealed record TestId(string Value);

    private sealed record TestEvent(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string EventType,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    #endregion

    #region Constructor and Default Values

    [Fact]
    public void Constructor_WhenCreated_SetsDefaultValues()
    {
        var data = new EventStoreData<TestId>();

        Assert.Equal(0, data.Version);
        Assert.Null(data.Id);
        Assert.NotNull(data.Events);
        Assert.Empty(data.Events);
        Assert.Equal(string.Empty, data.StreamName);
    }

    #endregion

    #region Property Setters and Getters

    [Fact]
    public void Id_WhenSet_CanBeRetrieved()
    {
        var data = new EventStoreData<TestId>();
        var id = new TestId("test-123");

        data.Id = id;

        Assert.Equal(id, data.Id);
    }

    [Fact]
    public void Version_WhenSet_CanBeRetrieved()
    {
        var data = new EventStoreData<TestId>();

        data.Version = 5;

        Assert.Equal(5, data.Version);
    }

    [Fact]
    public void Events_WhenSet_CanBeRetrieved()
    {
        var data = new EventStoreData<TestId>();
        var events = new List<IDomainEvent>
        {
            new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", "Event1", new Dictionary<string, string>()),
            new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", "Event2", new Dictionary<string, string>()),
        };

        data.Events = events;

        Assert.Equal(2, data.Events.Count);
        Assert.Equal(events[0], data.Events[0]);
        Assert.Equal(events[1], data.Events[1]);
    }

    [Fact]
    public void StreamName_WhenSet_CanBeRetrieved()
    {
        var data = new EventStoreData<TestId>();

        data.StreamName = "account-123";

        Assert.Equal("account-123", data.StreamName);
    }

    #endregion

    #region GetOptimisticLockVersion

    [Fact]
    public void GetOptimisticLockVersion_WithNoEvents_ReturnsVersion()
    {
        var data = new EventStoreData<TestId> { Version = 5, Events = [] };

        var lockVersion = data.GetOptimisticLockVersion();

        Assert.Equal(5, lockVersion); // Version + 0 events
    }

    [Fact]
    public void GetOptimisticLockVersion_WithEvents_ReturnsVersionPlusEventCount()
    {
        var events = new List<IDomainEvent>
        {
            new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", "Event1", new Dictionary<string, string>()),
            new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", "Event2", new Dictionary<string, string>()),
            new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", "Event3", new Dictionary<string, string>()),
        };
        var data = new EventStoreData<TestId> { Version = 5, Events = events };

        var lockVersion = data.GetOptimisticLockVersion();

        Assert.Equal(8, lockVersion); // Version (5) + 3 events
    }

    [Fact]
    public void GetOptimisticLockVersion_WithZeroVersion_ReturnsEventCount()
    {
        var events = new List<IDomainEvent>
        {
            new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", "Event1", new Dictionary<string, string>()),
        };
        var data = new EventStoreData<TestId> { Version = 0, Events = events };

        var lockVersion = data.GetOptimisticLockVersion();

        Assert.Equal(1, lockVersion); // Version (0) + 1 event
    }

    #endregion

    #region IStoreData Interface Implementation

    [Fact]
    public void EventStoreData_ImplementsIStoreDataInterface()
    {
        var data = new EventStoreData<TestId>();

        Assert.IsAssignableFrom<IStoreData<TestId>>(data);
    }

    [Fact]
    public void EventStoreData_IStoreDataMembers_AreAccessible()
    {
        IStoreData<TestId> data = new EventStoreData<TestId>
        {
            Id = new TestId("test-id"),
            Version = 3,
            StreamName = "test-stream",
        };

        Assert.Equal(new TestId("test-id"), data.Id);
        Assert.Equal("test-stream", data.StreamName);
        Assert.Equal(3, data.GetOptimisticLockVersion());
    }

    #endregion
}
