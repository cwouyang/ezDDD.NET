using System.Reflection;
using EzDdd.Cqrs.Entity.Query;

namespace EzDdd.Cqrs.Tests.Entity.Query;

public class EventDeduplicationRecordTests
{
    [Fact]
    public void IsEventHandled_OnEmptyRecord_ReturnsFalse()
    {
        EventDeduplicationRecord record = new();

        bool handled = record.IsEventHandled(Guid.NewGuid());

        Assert.False(handled);
    }

    [Fact]
    public void IsEventHandled_AfterSetEventId_ReturnsTrue()
    {
        EventDeduplicationRecord record = new();
        Guid eventId = Guid.NewGuid();

        record.SetEventId(eventId);

        Assert.True(record.IsEventHandled(eventId));
    }

    [Fact]
    public void SetEventId_AtCapacity_KeepsAllIds()
    {
        const int capacity = 3;
        EventDeduplicationRecord record = new(capacity);
        Guid[] ids = NewIds(capacity);

        RecordAll(record, ids);

        Assert.All(ids, id => Assert.True(record.IsEventHandled(id)));
    }

    [Fact]
    public void SetEventId_BeyondCapacity_ForgetsOnlyTheEldestId()
    {
        const int capacity = 3;
        EventDeduplicationRecord record = new(capacity);
        Guid[] ids = NewIds(capacity + 1);

        RecordAll(record, ids);

        Assert.False(record.IsEventHandled(ids[0]));
        Assert.All(ids.Skip(1), id => Assert.True(record.IsEventHandled(id)));
    }

    [Fact]
    public void SetEventId_WithCapacityOne_ReplacesThePreviousId()
    {
        EventDeduplicationRecord record = new(1);
        Guid[] ids = NewIds(2);

        RecordAll(record, ids);

        Assert.False(record.IsEventHandled(ids[0]));
        Assert.True(record.IsEventHandled(ids[1]));
    }

    [Fact]
    public void SetEventId_ForRememberedId_DoesNotRefreshItsPosition()
    {
        EventDeduplicationRecord record = new(2);
        Guid[] ids = NewIds(3);
        record.SetEventId(ids[0]);
        record.SetEventId(ids[1]);

        record.SetEventId(ids[0]);
        record.SetEventId(ids[2]);

        Assert.False(record.IsEventHandled(ids[0]));
        Assert.True(record.IsEventHandled(ids[1]));
        Assert.True(record.IsEventHandled(ids[2]));
    }

    [Fact]
    public void SetEventId_ForEmptyGuid_BehavesLikeAnyOtherId()
    {
        EventDeduplicationRecord record = new(1);
        Guid other = Guid.NewGuid();

        record.SetEventId(Guid.Empty);
        bool knownAfterRecording = record.IsEventHandled(Guid.Empty);
        record.SetEventId(other);

        Assert.True(knownAfterRecording);
        Assert.False(record.IsEventHandled(Guid.Empty));
        Assert.True(record.IsEventHandled(other));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithCapacityBelowOne_ThrowsArgumentOutOfRange(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EventDeduplicationRecord(capacity));
    }

    [Fact]
    public void Constructor_Parameterless_RemembersFiftyIds()
    {
        EventDeduplicationRecord record = new();
        Guid[] ids = NewIds(EventDeduplicationRecord.DefaultMaxEventCapacity + 1);

        RecordAll(record, ids);

        Assert.Equal(50, EventDeduplicationRecord.DefaultMaxEventCapacity);
        Assert.False(record.IsEventHandled(ids[0]));
        Assert.All(ids.Skip(1), id => Assert.True(record.IsEventHandled(id)));
    }

    // The copy member is internal and this test assembly has no InternalsVisibleTo grant, so it is reached by reflection.
    [Fact]
    public void Copy_HasSameIdsAndCapacity()
    {
        EventDeduplicationRecord original = new(2);
        Guid[] ids = NewIds(3);
        original.SetEventId(ids[0]);
        original.SetEventId(ids[1]);

        EventDeduplicationRecord copy = CopyOf(original);
        copy.SetEventId(ids[2]);

        Assert.True(original.IsEventHandled(ids[0]));
        Assert.True(original.IsEventHandled(ids[1]));
        Assert.False(copy.IsEventHandled(ids[0]));
        Assert.True(copy.IsEventHandled(ids[1]));
        Assert.True(copy.IsEventHandled(ids[2]));
    }

    [Fact]
    public void Copy_IsIndependentOfTheOriginalInBothDirections()
    {
        EventDeduplicationRecord original = new(5);
        Guid shared = Guid.NewGuid();
        Guid onlyInCopy = Guid.NewGuid();
        Guid onlyInOriginal = Guid.NewGuid();
        original.SetEventId(shared);
        EventDeduplicationRecord copy = CopyOf(original);

        copy.SetEventId(onlyInCopy);
        original.SetEventId(onlyInOriginal);

        Assert.False(original.IsEventHandled(onlyInCopy));
        Assert.False(copy.IsEventHandled(onlyInOriginal));
        Assert.True(copy.IsEventHandled(shared));
    }

    private static EventDeduplicationRecord CopyOf(EventDeduplicationRecord record)
    {
        MethodInfo method =
            typeof(EventDeduplicationRecord).GetMethod("Copy", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("EventDeduplicationRecord.Copy was not found.");
        return (EventDeduplicationRecord)method.Invoke(record, null)!;
    }

    private static Guid[] NewIds(int count) => [.. Enumerable.Range(0, count).Select(_ => Guid.NewGuid())];

    private static void RecordAll(EventDeduplicationRecord record, IEnumerable<Guid> ids)
    {
        foreach (Guid id in ids)
        {
            record.SetEventId(id);
        }
    }
}
