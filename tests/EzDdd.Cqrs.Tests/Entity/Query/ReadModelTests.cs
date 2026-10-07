using EzDdd.Cqrs.Entity.Query;

namespace EzDdd.Cqrs.Tests.Entity.Query;

public class ReadModelTests
{
    [Fact]
    public void UpdateEventDeduplicationRecord_MarksTheEventAsHandled()
    {
        CounterReadModel model = new("counter-1", 0);
        Guid eventId = Guid.NewGuid();

        model.UpdateEventDeduplicationRecord(eventId);

        Assert.True(model.EventDeduplicationRecord.IsEventHandled(eventId));
    }

    [Fact]
    public void With_CreatesACopyWhoseRecordIsIndependentOfTheOriginalInBothDirections()
    {
        CounterReadModel original = new("counter-1", 0);
        Guid shared = Guid.NewGuid();
        Guid onlyInCopy = Guid.NewGuid();
        Guid onlyInOriginal = Guid.NewGuid();
        original.UpdateEventDeduplicationRecord(shared);
        CounterReadModel copy = original with { Count = 1 };

        copy.UpdateEventDeduplicationRecord(onlyInCopy);
        original.UpdateEventDeduplicationRecord(onlyInOriginal);

        Assert.True(copy.EventDeduplicationRecord.IsEventHandled(shared));
        Assert.False(original.EventDeduplicationRecord.IsEventHandled(onlyInCopy));
        Assert.False(copy.EventDeduplicationRecord.IsEventHandled(onlyInOriginal));
    }

    [Fact]
    public void With_OnADerivedOfDerivedModel_CreatesAnIndependentRecord()
    {
        VipCounterReadModel original = new("counter-1", 0, "gold");
        Guid onlyInCopy = Guid.NewGuid();
        Guid onlyInOriginal = Guid.NewGuid();
        VipCounterReadModel copy = original with { Tier = "platinum" };

        copy.UpdateEventDeduplicationRecord(onlyInCopy);
        original.UpdateEventDeduplicationRecord(onlyInOriginal);

        Assert.False(original.EventDeduplicationRecord.IsEventHandled(onlyInCopy));
        Assert.False(copy.EventDeduplicationRecord.IsEventHandled(onlyInOriginal));
    }

    [Fact]
    public void With_CarriesTheCapacityOfTheOriginal()
    {
        SmallMemoryReadModel original = new("small-1");
        Guid[] ids = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        SmallMemoryReadModel copy = original with { Id = "small-2" };

        foreach (Guid id in ids)
        {
            copy.UpdateEventDeduplicationRecord(id);
        }

        Assert.False(copy.EventDeduplicationRecord.IsEventHandled(ids[0]));
        Assert.True(copy.EventDeduplicationRecord.IsEventHandled(ids[1]));
        Assert.True(copy.EventDeduplicationRecord.IsEventHandled(ids[2]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithCapacityBelowOne_ThrowsArgumentOutOfRange(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InvalidCapacityReadModel(capacity));
    }

    [Fact]
    public void Equals_IgnoresTheDeduplicationRecord()
    {
        CounterReadModel first = new("counter-1", 3);
        CounterReadModel second = new("counter-1", 3);
        first.UpdateEventDeduplicationRecord(Guid.NewGuid());
        second.UpdateEventDeduplicationRecord(Guid.NewGuid());

        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_StillComparesOwnMembers()
    {
        CounterReadModel first = new("counter-1", 3);
        CounterReadModel second = new("counter-1", 4);

        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Equals_BetweenABaseAndADerivedOfDerivedModelWithTheSameBaseMembers_IsFalseInBothDirections()
    {
        BaseCounterReadModel baseModel = new("counter-1", 3);
        BaseCounterReadModel derived = new VipCounterReadModel("counter-1", 3, "gold");

        Assert.False(baseModel.Equals(derived));
        Assert.False(derived.Equals(baseModel));
    }

    [Fact]
    public void Equals_BetweenTwoDifferentDerivedTypes_IsFalse()
    {
        ReadModel counter = new CounterReadModel("id-1", 3);
        ReadModel small = new SmallMemoryReadModel("id-1");

        Assert.False(counter.Equals(small));
        Assert.False(small.Equals(counter));
    }

    [Fact]
    public void With_OnAPopulatedModelAtCapacity_KeepsTheIdsAndCapacityInTheCopy()
    {
        ThreeIdMemoryReadModel original = new("three-1");
        Guid a = Guid.NewGuid();
        Guid b = Guid.NewGuid();
        Guid c = Guid.NewGuid();
        Guid d = Guid.NewGuid();
        original.UpdateEventDeduplicationRecord(a);
        original.UpdateEventDeduplicationRecord(b);
        original.UpdateEventDeduplicationRecord(c);
        ThreeIdMemoryReadModel copy = original with { Id = "three-2" };

        copy.UpdateEventDeduplicationRecord(d);

        Assert.False(copy.EventDeduplicationRecord.IsEventHandled(a));
        Assert.True(copy.EventDeduplicationRecord.IsEventHandled(b));
        Assert.True(copy.EventDeduplicationRecord.IsEventHandled(c));
        Assert.True(copy.EventDeduplicationRecord.IsEventHandled(d));
        Assert.True(original.EventDeduplicationRecord.IsEventHandled(a));
        Assert.True(original.EventDeduplicationRecord.IsEventHandled(b));
        Assert.True(original.EventDeduplicationRecord.IsEventHandled(c));
        Assert.False(original.EventDeduplicationRecord.IsEventHandled(d));
    }
}
