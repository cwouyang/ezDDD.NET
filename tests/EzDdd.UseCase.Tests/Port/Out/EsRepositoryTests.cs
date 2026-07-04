using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.Out;
using Xunit;

namespace EzDdd.UseCase.Tests.Port.Out;

public class EsRepositoryTests
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
            Id = id;
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
                    _name = te.EventType;
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

    private class MockRepositoryPeer : IRepositoryPeer<EventStoreData<TestId>, TestId>
    {
        private readonly Dictionary<TestId, EventStoreData<TestId>> _store = new();
        public bool ThrowOnSave { get; set; }
        public bool ThrowOnDelete { get; set; }

        public Task<EventStoreData<TestId>?> FindByIdAsync(TestId id)
        {
            _store.TryGetValue(id, out var data);
            return Task.FromResult(data);
        }

        public Task SaveAsync(EventStoreData<TestId> data)
        {
            if (ThrowOnSave)
            {
                throw new RepositoryPeerSaveException("Simulated peer save failure");
            }

            _store[data.Id] = data;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(EventStoreData<TestId> data)
        {
            if (ThrowOnDelete)
            {
                throw new InvalidOperationException("Simulated delete failure");
            }

            _store.Remove(data.Id);
            return Task.CompletedTask;
        }
    }

    #endregion

    #region FindByIdAsync Tests

    [Fact]
    public async Task FindByIdAsync_WithExistingAggregate_ReturnsReconstructedAggregate()
    {
        var peer = new MockRepositoryPeer();
        var repository = new EsRepository<TestAggregate, TestId>(peer);
        var id = new TestId("test-123");
        var aggregate = new TestAggregate(id, "TestName");
        await repository.SaveAsync(aggregate);

        var loaded = await repository.FindByIdAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal(id, loaded.Id);
        Assert.Equal("TestCreated", loaded.Name); // Reconstructed from event
    }

    [Fact]
    public async Task FindByIdAsync_WithNonExistingAggregate_ReturnsNull()
    {
        var peer = new MockRepositoryPeer();
        var repository = new EsRepository<TestAggregate, TestId>(peer);
        var id = new TestId("non-existing");

        var loaded = await repository.FindByIdAsync(id);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task FindByIdAsync_ReconstructsAggregateFromEvents()
    {
        var peer = new MockRepositoryPeer();
        var repository = new EsRepository<TestAggregate, TestId>(peer);
        var id = new TestId("test-456");
        var aggregate = new TestAggregate(id, "Original");
        aggregate.DoSomething("Action1");
        aggregate.DoSomething("Action2");
        await repository.SaveAsync(aggregate);

        var loaded = await repository.FindByIdAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal("Action2", loaded.Name); // Last event sets name
        Assert.Empty(loaded.GetDomainEvents()); // Events cleared after replay
    }

    #endregion

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_WithValidAggregate_DelegatesToPeer()
    {
        var peer = new MockRepositoryPeer();
        var repository = new EsRepository<TestAggregate, TestId>(peer);
        var id = new TestId("test-789");
        var aggregate = new TestAggregate(id, "TestName");

        await repository.SaveAsync(aggregate);

        var stored = await peer.FindByIdAsync(id);
        Assert.NotNull(stored);
        Assert.Equal(id, stored.Id);
        Assert.Single(stored.Events);
    }

    [Fact]
    public async Task SaveAsync_ClearsDomainEventsAfterSuccess()
    {
        var peer = new MockRepositoryPeer();
        var repository = new EsRepository<TestAggregate, TestId>(peer);
        var id = new TestId("test-clear");
        var aggregate = new TestAggregate(id, "TestName");
        Assert.Single(aggregate.GetDomainEvents()); // Has creation event

        await repository.SaveAsync(aggregate);

        Assert.Empty(aggregate.GetDomainEvents()); // Events cleared
    }

    [Fact]
    public async Task SaveAsync_WhenPeerThrows_WrapsExceptionInRepositorySaveException()
    {
        var peer = new MockRepositoryPeer { ThrowOnSave = true };
        var repository = new EsRepository<TestAggregate, TestId>(peer);
        var id = new TestId("test-error");
        var aggregate = new TestAggregate(id, "TestName");

        var exception = await Assert.ThrowsAsync<RepositorySaveException>(() => repository.SaveAsync(aggregate));

        Assert.Contains("Failed to save aggregate", exception.Message);
        Assert.IsType<RepositoryPeerSaveException>(exception.InnerException);
    }

    [Fact]
    public async Task SaveAsync_WhenPeerThrows_DoesNotClearDomainEvents()
    {
        var peer = new MockRepositoryPeer { ThrowOnSave = true };
        var repository = new EsRepository<TestAggregate, TestId>(peer);
        var id = new TestId("test-error-noclear");
        var aggregate = new TestAggregate(id, "TestName");
        var originalEventCount = aggregate.GetDomainEvents().Count;

        try
        {
            await repository.SaveAsync(aggregate);
        }
        catch (RepositorySaveException)
        {
            // Expected
        }

        Assert.Equal(originalEventCount, aggregate.GetDomainEvents().Count); // Events NOT cleared
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_DelegatesToPeer()
    {
        var peer = new MockRepositoryPeer();
        var repository = new EsRepository<TestAggregate, TestId>(peer);
        var id = new TestId("test-delete");
        var aggregate = new TestAggregate(id, "TestName");
        await repository.SaveAsync(aggregate);

        await repository.DeleteAsync(aggregate);

        var deleted = await peer.FindByIdAsync(id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_WhenPeerThrows_PropagatesException()
    {
        var peer = new MockRepositoryPeer { ThrowOnDelete = true };
        var repository = new EsRepository<TestAggregate, TestId>(peer);
        var id = new TestId("test-delete-error");
        var aggregate = new TestAggregate(id, "TestName");

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeleteAsync(aggregate));
    }

    #endregion

    #region Reflection and Constructor Caching Tests

    [Fact]
    public async Task FindByIdAsync_UsesReflectionToInstantiateAggregate()
    {
        var peer = new MockRepositoryPeer();
        var repository = new EsRepository<TestAggregate, TestId>(peer);
        var id = new TestId("test-reflection");
        var aggregate = new TestAggregate(id, "Original");
        await repository.SaveAsync(aggregate);

        var loaded = await repository.FindByIdAsync(id);

        Assert.NotNull(loaded);
        Assert.IsType<TestAggregate>(loaded);
        Assert.Equal(id, loaded.Id);
    }

    [Fact]
    public async Task FindByIdAsync_CachesConstructorInfo_ForPerformance()
    {
        var peer = new MockRepositoryPeer();
        var repository = new EsRepository<TestAggregate, TestId>(peer);
        var id1 = new TestId("test-cache-1");
        var id2 = new TestId("test-cache-2");
        var aggregate1 = new TestAggregate(id1, "First");
        var aggregate2 = new TestAggregate(id2, "Second");
        await repository.SaveAsync(aggregate1);
        await repository.SaveAsync(aggregate2);

        var loaded1 = await repository.FindByIdAsync(id1);
        var loaded2 = await repository.FindByIdAsync(id2);

        Assert.NotNull(loaded1);
        Assert.NotNull(loaded2);
        Assert.Equal(id1, loaded1.Id);
        Assert.Equal(id2, loaded2.Id);
    }

    #endregion

    #region IRepository Interface Implementation Tests

    [Fact]
    public void EsRepository_ImplementsIRepositoryInterface()
    {
        var peer = new MockRepositoryPeer();
        var repository = new EsRepository<TestAggregate, TestId>(peer);

        Assert.IsAssignableFrom<IRepository<TestAggregate, TestId, IInternalDomainEvent>>(repository);
    }

    #endregion
}
