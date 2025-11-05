namespace EzDdd.UseCase.Tests.Port.Out;

using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.Out;

public class RepositoryTests
{
#region Test Fixtures

    // Test aggregate for repository testing
    private record TestAggregateId(string Value);

    private record TestEvent
    (
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent;

    private class TestAggregate : AggregateRoot<TestAggregateId, IInternalDomainEvent>
    {
        public string Name { get; } = string.Empty;

        public TestAggregate(TestAggregateId id, string name)
        {
            var @event = new TestEvent
            (
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                id.Value,
                new Dictionary<string, string>()
            );
            Apply(@event);
            Id = id;
            Name = name;
        }

        // Required for framework use
        public TestAggregate() { }
    }

#endregion

#region Mock Implementations

    private class MockRepository : IRepository<TestAggregate, TestAggregateId, IInternalDomainEvent>
    {
        private readonly Dictionary<TestAggregateId, TestAggregate> _store = new();
        public bool ThrowOnSave { get; set; }

        public Task<TestAggregate?> FindByIdAsync(TestAggregateId id)
        {
            _store.TryGetValue(id, out var aggregate);
            return Task.FromResult(aggregate);
        }

        public Task SaveAsync(TestAggregate aggregate)
        {
            if (ThrowOnSave)
            {
                throw new RepositorySaveException(RepositorySaveException.OptimisticLockingFailure);
            }

            _store[aggregate.Id] = aggregate;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(TestAggregate aggregate)
        {
            _store.Remove(aggregate.Id);
            return Task.CompletedTask;
        }
    }

#endregion

#region FindByIdAsync Tests

    [Fact]
    public async Task FindByIdAsync_ExistingAggregate_ReturnsAggregate()
    {
        var repository = new MockRepository();
        var aggregateId = new TestAggregateId("test-1");
        var aggregate = new TestAggregate(aggregateId, "Test Aggregate");
        await repository.SaveAsync(aggregate);

        var result = await repository.FindByIdAsync(aggregateId);

        Assert.NotNull(result);
        Assert.Equal(aggregateId, result.Id);
        Assert.Equal("Test Aggregate", result.Name);
    }

    [Fact]
    public async Task FindByIdAsync_NonExistingAggregate_ReturnsNull()
    {
        var repository = new MockRepository();
        var nonExistingId = new TestAggregateId("non-existing");

        var result = await repository.FindByIdAsync(nonExistingId);

        Assert.Null(result);
    }

#endregion

#region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_NewAggregate_PersistsAggregate()
    {
        var repository = new MockRepository();
        var aggregateId = new TestAggregateId("test-2");
        var aggregate = new TestAggregate(aggregateId, "New Aggregate");

        await repository.SaveAsync(aggregate);

        var retrieved = await repository.FindByIdAsync(aggregateId);
        Assert.NotNull(retrieved);
        Assert.Equal(aggregateId, retrieved.Id);
    }

    [Fact]
    public async Task SaveAsync_ExistingAggregate_UpdatesAggregate()
    {
        var repository = new MockRepository();
        var aggregateId = new TestAggregateId("test-3");
        var aggregate = new TestAggregate(aggregateId, "Original Name");
        await repository.SaveAsync(aggregate);

        await repository.SaveAsync(aggregate);

        var retrieved = await repository.FindByIdAsync(aggregateId);
        Assert.NotNull(retrieved);
    }

    [Fact]
    public async Task SaveAsync_OptimisticLockingFailure_ThrowsRepositorySaveException()
    {
        var repository = new MockRepository { ThrowOnSave = true };
        var aggregateId = new TestAggregateId("test-4");
        var aggregate = new TestAggregate(aggregateId, "Test");

        var exception = await Assert.ThrowsAsync<RepositorySaveException>
        (async () => await repository.SaveAsync(aggregate)
        );

        Assert.Equal(RepositorySaveException.OptimisticLockingFailure, exception.Message);
    }

#endregion

#region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingAggregate_RemovesAggregate()
    {
        var repository = new MockRepository();
        var aggregateId = new TestAggregateId("test-5");
        var aggregate = new TestAggregate(aggregateId, "To Be Deleted");
        await repository.SaveAsync(aggregate);

        await repository.DeleteAsync(aggregate);

        var retrieved = await repository.FindByIdAsync(aggregateId);
        Assert.Null(retrieved);
    }

#endregion

#region Type Constraint Tests

    [Fact]
    public async Task Repository_WorksWithAggregateRootConstraint()
    {
        var repository = new MockRepository();
        var aggregateId = new TestAggregateId("test-6");
        var aggregate = new TestAggregate(aggregateId, "Test");

        IRepository<TestAggregate, TestAggregateId, IInternalDomainEvent> typedRepository = repository;
        await typedRepository.SaveAsync(aggregate);
        var result = await typedRepository.FindByIdAsync(aggregateId);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<AggregateRoot<TestAggregateId, IInternalDomainEvent>>(result);
    }

#endregion
}