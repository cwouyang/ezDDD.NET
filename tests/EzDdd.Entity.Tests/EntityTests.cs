namespace EzDdd.Entity.Tests;

public class EntityTests
{
    [Fact]
    public void IEntity_WithGuidId_ReturnsCorrectId()
    {
        Guid expectedId = Guid.NewGuid();

        TestEntity entity = new(expectedId);

        Assert.Equal(expectedId, entity.Id);
    }

    [Fact]
    public void IEntity_WithStringId_ReturnsCorrectId()
    {
        const string expectedId = "test-entity-123";

        TestStringEntity entity = new(expectedId);

        Assert.Equal(expectedId, entity.Id);
    }

    [Fact]
    public void IEntity_Covariance_AllowsAssignmentToBaseType()
    {
        TestStringEntity concreteEntity = new("test-123");

        // Covariance: can assign IEntity<string> to IEntity<object> due to <out TId>
        // Note: Covariance only works with reference types (string is reference type)
        IEntity<object> baseEntity = concreteEntity;

        Assert.NotNull(baseEntity);
        Assert.IsType<string>(baseEntity.Id);
        Assert.Equal("test-123", baseEntity.Id);
    }

    [Fact]
    public void IEntity_DifferentImplementations_CanHaveDifferentIdTypes()
    {
        TestEntity guidEntity = new(Guid.NewGuid());
        TestStringEntity stringEntity = new("test-123");

        Assert.IsAssignableFrom<IEntity<Guid>>(guidEntity);
        Assert.IsAssignableFrom<IEntity<string>>(stringEntity);
    }

    [Fact]
    public void IEntity_GenericConstraint_CanBeUsedInGenericMethod()
    {
        TestEntity entity = new(Guid.NewGuid());

        Guid id = GetEntityId(entity);

        Assert.Equal(entity.Id, id);
        return;

        // Helper method demonstrating generic constraint usage
        static TId GetEntityId<TId>(IEntity<TId> entity) => entity.Id;
    }

    // Test implementation classes
    private class TestEntity(Guid id) : IEntity<Guid>
    {
        public Guid Id { get; } = id;
    }

    private class TestStringEntity(string id) : IEntity<string>
    {
        public string Id { get; } = id;
    }
}
