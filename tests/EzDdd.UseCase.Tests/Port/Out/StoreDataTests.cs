namespace EzDdd.UseCase.Tests.Port.Out;

using EzDdd.Entity;
using EzDdd.UseCase.Port.Out;

public class StoreDataTests
{
#region Test Helpers

    private class TestStoreData : IStoreData<string>
    {
        public long Version { get; set; } = -1;
        public string Id { get; set; } = string.Empty;
        public IReadOnlyList<IDomainEvent> Events { get; set; } = new List<IDomainEvent>();
        public string StreamName { get; set; } = string.Empty;
    }

    private record TestEvent
    (
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent;

#endregion

#region Property Tests

    [Fact]
    public void Version_DefaultValue_IsMinusOne()
    {
        var storeData = new TestStoreData();

        Assert.Equal(-1, storeData.Version);
    }

    [Fact]
    public void Id_SetAndGet_ReturnsCorrectValue()
    {
        var storeData = new TestStoreData();
        const string expectedId = "test-id-123";

        storeData.Id = expectedId;

        Assert.Equal(expectedId, storeData.Id);
    }

    [Fact]
    public void Events_InitiallyEmpty()
    {
        var storeData = new TestStoreData();

        Assert.NotNull(storeData.Events);
        Assert.Empty(storeData.Events);
    }

#endregion

#region GetOptimisticLockVersion Tests

    [Fact]
    public void GetOptimisticLockVersion_NewAggregate_ReturnsZero()
    {
        IStoreData<string> storeData = new TestStoreData
        {
            Version = -1,
            Events = new List<IDomainEvent>
            {
                new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", new Dictionary<string, string>())
            }
        };

        var optimisticLockVersion = storeData.GetOptimisticLockVersion();

        Assert.Equal(0, optimisticLockVersion); // -1 + 1 = 0
    }

    [Fact]
    public void GetOptimisticLockVersion_ExistingAggregateWithOneEvent_ReturnsOne()
    {
        IStoreData<string> storeData = new TestStoreData
        {
            Version = 0,
            Events = new List<IDomainEvent>
            {
                new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", new Dictionary<string, string>())
            }
        };

        var optimisticLockVersion = storeData.GetOptimisticLockVersion();

        Assert.Equal(1, optimisticLockVersion); // 0 + 1 = 1
    }

    [Fact]
    public void GetOptimisticLockVersion_ExistingAggregateWithMultipleEvents_ReturnsCorrectValue()
    {
        IStoreData<string> storeData = new TestStoreData
        {
            Version = 5,
            Events = new List<IDomainEvent>
            {
                new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", new Dictionary<string, string>()),
                new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", new Dictionary<string, string>()),
                new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", new Dictionary<string, string>())
            }
        };

        var optimisticLockVersion = storeData.GetOptimisticLockVersion();

        Assert.Equal(8, optimisticLockVersion); // 5 + 3 = 8
    }

#endregion

#region StreamName Tests

    [Fact]
    public void StreamName_SetAndGet_ReturnsCorrectValue()
    {
        var storeData = new TestStoreData();
        const string expectedStreamName = "account-123";

        storeData.StreamName = expectedStreamName;

        Assert.Equal(expectedStreamName, storeData.StreamName);
    }

    [Fact]
    public void StreamName_FollowsConvention_CategoryDashId()
    {
        var storeData = new TestStoreData { StreamName = "bankaccount-acc-456" };

        Assert.Contains("-", storeData.StreamName);
        Assert.StartsWith("bankaccount-", storeData.StreamName);
    }

#endregion

#region Lifecycle Tests

    [Fact]
    public void CompleteLifecycle_NewToExisting_VersionProgression()
    {
        IStoreData<string> storeData = new TestStoreData { Id = "account-789", Version = -1, StreamName = "account-789" };

        storeData.Events = new List<IDomainEvent>
        {
            new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", new Dictionary<string, string>())
        };

        Assert.Equal(0, storeData.GetOptimisticLockVersion());

        storeData.Version = 0;
        storeData.Events = new List<IDomainEvent>();

        Assert.Equal(0, storeData.GetOptimisticLockVersion());

        storeData.Events = new List<IDomainEvent>
        {
            new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, "test", new Dictionary<string, string>())
        };

        Assert.Equal(1, storeData.GetOptimisticLockVersion());
    }

#endregion
}