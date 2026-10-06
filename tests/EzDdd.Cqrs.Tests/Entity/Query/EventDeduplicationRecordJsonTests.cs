using System.Text.Json;
using System.Text.Json.Serialization;
using EzDdd.Common;
using EzDdd.Cqrs.Entity.Query;

namespace EzDdd.Cqrs.Tests.Entity.Query;

public class EventDeduplicationRecordJsonTests
{
    private static readonly Guid FirstId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SecondId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid ThirdId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static TheoryData<string> OptionSets => ["JsonUtil", "Default"];

    private static JsonSerializerOptions OptionsFor(string name) =>
        string.Equals(name, "JsonUtil", StringComparison.Ordinal) ? JsonUtil.Options : JsonSerializerOptions.Default;

    [Theory]
    [MemberData(nameof(OptionSets))]
    public void Serialize_WritesProcessedEventIdsThenCapacityInJavaShape(string optionSet)
    {
        EventDeduplicationRecord record = new(7);
        record.SetEventId(FirstId);
        record.SetEventId(SecondId);

        string json = JsonSerializer.Serialize(record, OptionsFor(optionSet));

        Assert.Equal(
            """{"processedEventIds":["11111111-1111-1111-1111-111111111111","22222222-2222-2222-2222-222222222222"],"MAX_EVENT_CAPACITY":7}""",
            json
        );
    }

    [Theory]
    [MemberData(nameof(OptionSets))]
    public void Deserialize_AfterSerialize_KeepsIdsTheirOrderAndCapacity(string optionSet)
    {
        JsonSerializerOptions options = OptionsFor(optionSet);
        EventDeduplicationRecord original = new(2);
        original.SetEventId(FirstId);
        original.SetEventId(SecondId);

        EventDeduplicationRecord? restored = JsonSerializer.Deserialize<EventDeduplicationRecord>(
            JsonSerializer.Serialize(original, options),
            options
        );

        Assert.NotNull(restored);
        Assert.Equal(JsonSerializer.Serialize(original, options), JsonSerializer.Serialize(restored, options));
        restored.SetEventId(ThirdId);
        Assert.False(restored.IsEventHandled(FirstId));
        Assert.True(restored.IsEventHandled(SecondId));
        Assert.True(restored.IsEventHandled(ThirdId));
    }

    [Theory]
    [MemberData(nameof(OptionSets))]
    public void Deserialize_WithMissingNullZeroOrNegativeCapacity_UsesDefaultCapacity(string optionSet)
    {
        string[] documents =
        [
            """{"processedEventIds":[]}""",
            """{"processedEventIds":[],"MAX_EVENT_CAPACITY":null}""",
            """{"processedEventIds":[],"MAX_EVENT_CAPACITY":0}""",
            """{"processedEventIds":[],"MAX_EVENT_CAPACITY":-3}""",
        ];

        foreach (string document in documents)
        {
            EventDeduplicationRecord record = Read(document, optionSet);

            Assert.Equal(EventDeduplicationRecord.DefaultMaxEventCapacity, CapacityOf(record));
        }
    }

    [Theory]
    [InlineData("JsonUtil", "11111111-AAAA-1111-1111-111111111111")]
    [InlineData("JsonUtil", "{11111111-aaaa-1111-1111-111111111111}")]
    [InlineData("JsonUtil", "11111111aaaa11111111111111111111")]
    [InlineData("Default", "11111111-AAAA-1111-1111-111111111111")]
    [InlineData("Default", "{11111111-aaaa-1111-1111-111111111111}")]
    [InlineData("Default", "11111111aaaa11111111111111111111")]
    public void Deserialize_WithGuidInAnotherFormat_MatchesTheGuid(string optionSet, string stored)
    {
        Guid guid = Guid.Parse("11111111-aaaa-1111-1111-111111111111");

        EventDeduplicationRecord record = Read($$"""{"processedEventIds":["{{stored}}"]}""", optionSet);

        Assert.True(record.IsEventHandled(guid));
    }

    [Theory]
    [MemberData(nameof(OptionSets))]
    public void Deserialize_WithNonGuidString_LoadsAndMatchesNothing(string optionSet)
    {
        EventDeduplicationRecord record = Read("""{"processedEventIds":["not-a-guid"]}""", optionSet);

        Assert.False(record.IsEventHandled(Guid.Empty));
        Assert.False(record.IsEventHandled(FirstId));
    }

    [Theory]
    [MemberData(nameof(OptionSets))]
    public void Deserialize_WithDuplicateGuidsInAnyCase_KeepsTheFirstOccurrencePosition(string optionSet)
    {
        string document =
            """{"processedEventIds":["11111111-1111-1111-1111-111111111111","22222222-2222-2222-2222-222222222222","11111111111111111111111111111111"],"MAX_EVENT_CAPACITY":2}""";
        EventDeduplicationRecord record = Read(document, optionSet);

        record.SetEventId(ThirdId);

        Assert.False(record.IsEventHandled(FirstId));
        Assert.True(record.IsEventHandled(SecondId));
        Assert.True(record.IsEventHandled(ThirdId));
    }

    [Theory]
    [MemberData(nameof(OptionSets))]
    public void Deserialize_WithNullEntries_DropsThem(string optionSet)
    {
        EventDeduplicationRecord record = Read(
            """{"processedEventIds":[null,"11111111-1111-1111-1111-111111111111",null]}""",
            optionSet
        );

        Assert.Equal(
            """{"processedEventIds":["11111111-1111-1111-1111-111111111111"],"MAX_EVENT_CAPACITY":50}""",
            JsonSerializer.Serialize(record, OptionsFor(optionSet))
        );
    }

    [Theory]
    [InlineData("JsonUtil", """{"MAX_EVENT_CAPACITY":5}""")]
    [InlineData("JsonUtil", """{"processedEventIds":null,"MAX_EVENT_CAPACITY":5}""")]
    [InlineData("Default", """{"MAX_EVENT_CAPACITY":5}""")]
    [InlineData("Default", """{"processedEventIds":null,"MAX_EVENT_CAPACITY":5}""")]
    public void Deserialize_WithMissingOrNullIds_HasNoIds(string optionSet, string document)
    {
        EventDeduplicationRecord record = Read(document, optionSet);

        Assert.Equal(
            """{"processedEventIds":[],"MAX_EVENT_CAPACITY":5}""",
            JsonSerializer.Serialize(record, OptionsFor(optionSet))
        );
    }

    [Theory]
    [MemberData(nameof(OptionSets))]
    public void Deserialize_WithMoreIdsThanCapacity_KeepsThemUntilTheNextSetEventId(string optionSet)
    {
        string document =
            """{"processedEventIds":["11111111-1111-1111-1111-111111111111","22222222-2222-2222-2222-222222222222","33333333-3333-3333-3333-333333333333"],"MAX_EVENT_CAPACITY":2}""";
        EventDeduplicationRecord record = Read(document, optionSet);
        Assert.True(record.IsEventHandled(FirstId));

        record.SetEventId(Guid.NewGuid());

        Assert.False(record.IsEventHandled(FirstId));
        Assert.False(record.IsEventHandled(SecondId));
        Assert.True(record.IsEventHandled(ThirdId));
    }

    [Fact]
    public void Deserialize_WithCapacityOfWrongJsonType_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<EventDeduplicationRecord>("""{"MAX_EVENT_CAPACITY":"50"}""", JsonUtil.Options)
        );
    }

    [Fact]
    public void ReadValue_WithCapacityOfWrongJsonType_ThrowsInvalidOperationExceptionWrappingJsonException()
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            JsonUtil.ReadValue<EventDeduplicationRecord>("""{"MAX_EVENT_CAPACITY":"50"}""")
        );

        Assert.IsType<JsonException>(error.InnerException);
    }

    [Fact]
    public void Deserialize_WithPopulateCreationHandling_StillReadsStoredIds()
    {
        JsonSerializerOptions options = new() { PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate };

        EventDeduplicationRecord? record = JsonSerializer.Deserialize<EventDeduplicationRecord>(
            """{"processedEventIds":["11111111-1111-1111-1111-111111111111"],"MAX_EVENT_CAPACITY":5}""",
            options
        );

        Assert.NotNull(record);
        Assert.True(record.IsEventHandled(FirstId));
        Assert.Equal(
            """{"processedEventIds":["11111111-1111-1111-1111-111111111111"],"MAX_EVENT_CAPACITY":5}""",
            JsonSerializer.Serialize(record, options)
        );
    }

    private static EventDeduplicationRecord Read(string json, string optionSet) =>
        JsonSerializer.Deserialize<EventDeduplicationRecord>(json, OptionsFor(optionSet))!;

    // The capacity has no public accessor, so observe it as the number of ids that survive eviction.
    private static int CapacityOf(EventDeduplicationRecord record)
    {
        List<Guid> ids = Enumerable.Range(0, 1000).Select(_ => Guid.NewGuid()).ToList();
        foreach (Guid id in ids)
        {
            record.SetEventId(id);
        }

        return ids.Count(record.IsEventHandled);
    }
}
