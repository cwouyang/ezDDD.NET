namespace EzDdd.UseCase.Tests.Port.Out;

using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.Out;

public class RepositoryPeerTests
{
    #region Test Fixtures

    // Test data structure implementing IStoreData
    private record TestDataId(string Value);

    private class TestStoreDataImpl : IStoreData<TestDataId>
    {
        public long Version { get; set; } = -1;
        public TestDataId Id { get; set; } = new("default");
        public IReadOnlyList<IDomainEvent> Events { get; set; } = new List<IDomainEvent>();
        public string StreamName { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }

    #endregion

    #region Mock Implementations

    private class MockRepositoryPeer : IRepositoryPeer<TestStoreDataImpl, TestDataId>
    {
        private readonly Dictionary<TestDataId, TestStoreDataImpl> _store = new();
        public bool ThrowOnSave { get; set; }

        public Task<TestStoreDataImpl?> FindByIdAsync(TestDataId id)
        {
            _store.TryGetValue(id, out var data);
            return Task.FromResult(data);
        }

        public Task SaveAsync(TestStoreDataImpl data)
        {
            if (ThrowOnSave)
            {
                throw new RepositoryPeerSaveException("Database error");
            }

            // Simulate version increment after save
            _store[data.Id] = data;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(TestStoreDataImpl data)
        {
            _store.Remove(data.Id);
            return Task.CompletedTask;
        }
    }

    #endregion

    #region FindByIdAsync Tests

    [Fact]
    public async Task FindByIdAsync_ExistingData_ReturnsData()
    {
        var peer = new MockRepositoryPeer();
        var dataId = new TestDataId("test-1");
        var storeData = new TestStoreDataImpl
        {
            Id = dataId,
            Version = 0,
            Data = "Test Data",
        };
        await peer.SaveAsync(storeData);

        var result = await peer.FindByIdAsync(dataId);

        Assert.NotNull(result);
        Assert.Equal(dataId, result.Id);
        Assert.Equal("Test Data", result.Data);
    }

    [Fact]
    public async Task FindByIdAsync_NonExistingData_ReturnsNull()
    {
        var peer = new MockRepositoryPeer();
        var nonExistingId = new TestDataId("non-existing");

        var result = await peer.FindByIdAsync(nonExistingId);

        Assert.Null(result);
    }

    #endregion

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_NewData_PersistsData()
    {
        var peer = new MockRepositoryPeer();
        var dataId = new TestDataId("test-2");
        var storeData = new TestStoreDataImpl
        {
            Id = dataId,
            Version = -1,
            Data = "New Data",
        };

        await peer.SaveAsync(storeData);

        var retrieved = await peer.FindByIdAsync(dataId);
        Assert.NotNull(retrieved);
        Assert.Equal(dataId, retrieved.Id);
    }

    [Fact]
    public async Task SaveAsync_ExistingData_UpdatesData()
    {
        var peer = new MockRepositoryPeer();
        var dataId = new TestDataId("test-3");
        var storeData = new TestStoreDataImpl
        {
            Id = dataId,
            Version = 0,
            Data = "Updated Data",
        };
        await peer.SaveAsync(storeData);

        await peer.SaveAsync(storeData);

        var retrieved = await peer.FindByIdAsync(dataId);
        Assert.NotNull(retrieved);
    }

    [Fact]
    public async Task SaveAsync_DatabaseError_ThrowsRepositoryPeerSaveException()
    {
        var peer = new MockRepositoryPeer { ThrowOnSave = true };
        var dataId = new TestDataId("test-4");
        var storeData = new TestStoreDataImpl { Id = dataId, Data = "Test" };

        var exception = await Assert.ThrowsAsync<RepositoryPeerSaveException>(async () =>
            await peer.SaveAsync(storeData)
        );

        Assert.Equal("Database error", exception.Message);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingData_RemovesData()
    {
        var peer = new MockRepositoryPeer();
        var dataId = new TestDataId("test-5");
        var storeData = new TestStoreDataImpl { Id = dataId, Data = "To Be Deleted" };
        await peer.SaveAsync(storeData);

        await peer.DeleteAsync(storeData);

        var retrieved = await peer.FindByIdAsync(dataId);
        Assert.Null(retrieved);
    }

    #endregion

    #region Type Constraint Tests

    [Fact]
    public async Task RepositoryPeer_WorksWithStoreDataConstraint()
    {
        var peer = new MockRepositoryPeer();
        var dataId = new TestDataId("test-6");
        var storeData = new TestStoreDataImpl { Id = dataId, Data = "Test" };

        IRepositoryPeer<TestStoreDataImpl, TestDataId> typedPeer = peer;
        await typedPeer.SaveAsync(storeData);
        var result = await typedPeer.FindByIdAsync(dataId);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IStoreData<TestDataId>>(result);
    }

    [Fact]
    public async Task RepositoryPeer_TransactionBoundaryNote_CompileTimeCheck()
    {
        // This test serves as documentation that transaction boundaries
        // MUST be implemented at the IRepositoryPeer layer
        var peer = new MockRepositoryPeer();
        var dataId = new TestDataId("test-7");
        var storeData = new TestStoreDataImpl
        {
            Id = dataId,
            Data = "Test",
            Events = new List<IDomainEvent>
            {
                // In real implementation, SaveAsync would:
                // 1. Begin transaction
                // 2. Save aggregate state
                // 3. Save events (outbox)
                // 4. Commit transaction atomically
            },
        };

        await peer.SaveAsync(storeData);

        var retrieved = await peer.FindByIdAsync(dataId);
        Assert.NotNull(retrieved);
    }

    #endregion
}
