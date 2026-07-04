using EzDdd.Entity;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.UseCase.Tests.Port.Out;

/// <summary>
///     Verifies that <see cref="OutboxRepository{TAggregate, TData, TId}.FindByIdAsync" />
///     does not return an aggregate whose <see cref="AggregateRoot{TId, TEvent}.IsDeleted" />
///     flag is <c>true</c>.
/// </summary>
/// <remarks>
///     Ported from Java ezddd <c>OutboxRepositoryFindByIdTest</c> (commit <c>3aac0f5</c>).
/// </remarks>
public class OutboxRepositoryFindByIdTests
{
    [Fact]
    public async Task FindByIdAsync_WhenAggregateIsDeleted_ReturnsNull()
    {
        OutboxRepository<TestAggregate, TestOutboxData, string> repository = NewRepository();
        await repository.SaveAsync(new TestAggregate("id-1", isDeleted: true));

        TestAggregate? result = await repository.FindByIdAsync("id-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindByIdAsync_WhenAggregateIsNotDeleted_ReturnsAggregate()
    {
        OutboxRepository<TestAggregate, TestOutboxData, string> repository = NewRepository();
        await repository.SaveAsync(new TestAggregate("id-1", isDeleted: false));

        TestAggregate? result = await repository.FindByIdAsync("id-1");

        Assert.NotNull(result);
        Assert.False(result.IsDeleted);
    }

    private static OutboxRepository<TestAggregate, TestOutboxData, string> NewRepository()
    {
        InMemoryPeer peer = new();
        return new OutboxRepository<TestAggregate, TestOutboxData, string>(peer, new TestOutboxMapper());
    }

    #region Test Infrastructure

    private class TestAggregate : AggregateRoot<string, IInternalDomainEvent>
    {
        public TestAggregate(string id, bool isDeleted)
        {
            Id = id;
            IsDeleted = isDeleted;
        }
    }

    private class TestOutboxData : IOutboxData<string>
    {
        public TestOutboxData(string id, bool isDeleted)
        {
            Id = id;
            IsDeleted = isDeleted;
        }

        public bool IsDeleted { get; }
        public string Id { get; set; }
        public long Version { get; set; }
        public IReadOnlyList<IDomainEvent> Events { get; set; } = [];
        public string StreamName { get; set; } = string.Empty;

        public long GetOptimisticLockVersion()
        {
            return Version;
        }
    }

    private class TestOutboxMapper : OutboxMapper<TestAggregate, TestOutboxData, string>
    {
        public override TestOutboxData ToData(TestAggregate aggregate)
        {
            return new TestOutboxData(aggregate.Id, aggregate.IsDeleted);
        }

        public override TestAggregate ToDomain(TestOutboxData data)
        {
            return new TestAggregate(data.Id, data.IsDeleted);
        }
    }

    private class InMemoryPeer : IRepositoryPeer<TestOutboxData, string>
    {
        private readonly Dictionary<string, TestOutboxData> _storage = new();

        public Task<TestOutboxData?> FindByIdAsync(string id)
        {
            _storage.TryGetValue(id, out TestOutboxData? data);
            return Task.FromResult(data);
        }

        public Task SaveAsync(TestOutboxData data)
        {
            _storage[data.Id] = data;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(TestOutboxData data)
        {
            _storage.Remove(data.Id);
            return Task.CompletedTask;
        }
    }

    #endregion
}
