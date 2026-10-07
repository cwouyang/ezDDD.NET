using System.Text.Json;
using EzDdd.Common;
using EzDdd.Cqrs.Entity.Query;

namespace EzDdd.Cqrs.Tests.Entity.Query;

public class ReadModelJsonTests
{
    private static readonly Guid FirstId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SecondId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ThirdId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static TheoryData<string> OptionSets => ["JsonUtil", "Default"];

    private static JsonSerializerOptions OptionsFor(string name) =>
        string.Equals(name, "JsonUtil", StringComparison.Ordinal) ? JsonUtil.Options : JsonSerializerOptions.Default;

    [Theory]
    [MemberData(nameof(OptionSets))]
    public void Roundtrip_OfAPositionalDerivedModel_KeepsTheHandledIds(string optionSet)
    {
        JsonSerializerOptions options = OptionsFor(optionSet);
        VipCounterReadModel model = new("counter-1", 3, "gold");
        model.UpdateEventDeduplicationRecord(FirstId);
        model.UpdateEventDeduplicationRecord(SecondId);

        VipCounterReadModel? loaded = JsonSerializer.Deserialize<VipCounterReadModel>(
            JsonSerializer.Serialize(model, options),
            options
        );

        Assert.NotNull(loaded);
        Assert.Equal("gold", loaded.Tier);
        Assert.True(loaded.EventDeduplicationRecord.IsEventHandled(FirstId));
        Assert.True(loaded.EventDeduplicationRecord.IsEventHandled(SecondId));
        Assert.False(loaded.EventDeduplicationRecord.IsEventHandled(ThirdId));
    }

    [Theory]
    [MemberData(nameof(OptionSets))]
    public void Serialize_WritesTheRecordUnderTheCamelCaseName(string optionSet)
    {
        CounterReadModel model = new("counter-1", 3);
        model.UpdateEventDeduplicationRecord(FirstId);

        string json = JsonSerializer.Serialize(model, OptionsFor(optionSet));

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement record = document.RootElement.GetProperty("eventDeduplicationRecord");
        Assert.Equal("11111111-1111-1111-1111-111111111111", record.GetProperty("processedEventIds")[0].GetString());
    }

    [Theory]
    [MemberData(nameof(OptionSets))]
    public void Deserialize_WithoutTheRecord_KeepsTheRecordTheConstructorCreated(string optionSet)
    {
        const string json = """{"Id":"small-1"}""";

        SmallMemoryReadModel? loaded = JsonSerializer.Deserialize<SmallMemoryReadModel>(json, OptionsFor(optionSet));

        Assert.NotNull(loaded);
        Assert.False(loaded.EventDeduplicationRecord.IsEventHandled(FirstId));
        Assert.Equal(SmallMemoryReadModel.Capacity, CapacityOf(loaded, optionSet));
    }

    [Theory]
    [MemberData(nameof(OptionSets))]
    public void Deserialize_WithANullRecord_KeepsTheRecordTheConstructorCreated(string optionSet)
    {
        const string json = """{"Id":"small-1","eventDeduplicationRecord":null}""";

        SmallMemoryReadModel? loaded = JsonSerializer.Deserialize<SmallMemoryReadModel>(json, OptionsFor(optionSet));

        Assert.NotNull(loaded);
        Assert.Equal(SmallMemoryReadModel.Capacity, CapacityOf(loaded, optionSet));
    }

    [Theory]
    [MemberData(nameof(OptionSets))]
    public void Deserialize_WithAStoredCapacity_PrefersItOverTheConstructorCapacity(string optionSet)
    {
        const string json =
            """{"Id":"small-1","eventDeduplicationRecord":{"processedEventIds":[],"MAX_EVENT_CAPACITY":9}}""";

        SmallMemoryReadModel? loaded = JsonSerializer.Deserialize<SmallMemoryReadModel>(json, OptionsFor(optionSet));

        Assert.NotNull(loaded);
        Assert.Equal(9, CapacityOf(loaded, optionSet));
    }

    [Theory]
    [MemberData(nameof(OptionSets))]
    public void Deserialize_WithAStoredRecordLackingCapacity_ReadsTheDefaultCapacity(string optionSet)
    {
        const string json = """{"Id":"small-1","eventDeduplicationRecord":{"processedEventIds":[]}}""";

        SmallMemoryReadModel? loaded = JsonSerializer.Deserialize<SmallMemoryReadModel>(json, OptionsFor(optionSet));

        Assert.NotNull(loaded);
        Assert.Equal(EventDeduplicationRecord.DefaultMaxEventCapacity, CapacityOf(loaded, optionSet));
    }

    [Theory]
    [MemberData(nameof(OptionSets))]
    public void Roundtrip_OfAPopulatedSmallCapacityModel_KeepsTheCapacityAndIdOrder(string optionSet)
    {
        JsonSerializerOptions options = OptionsFor(optionSet);
        ThreeIdMemoryReadModel model = new("three-1");
        model.UpdateEventDeduplicationRecord(FirstId);
        model.UpdateEventDeduplicationRecord(SecondId);
        model.UpdateEventDeduplicationRecord(ThirdId);
        Guid fourth = Guid.NewGuid();

        ThreeIdMemoryReadModel? loaded = JsonSerializer.Deserialize<ThreeIdMemoryReadModel>(
            JsonSerializer.Serialize(model, options),
            options
        );
        Assert.NotNull(loaded);
        loaded.UpdateEventDeduplicationRecord(fourth);

        Assert.Equal(ThreeIdMemoryReadModel.Capacity, CapacityOf(loaded, optionSet));
        Assert.False(loaded.EventDeduplicationRecord.IsEventHandled(FirstId));
        Assert.True(loaded.EventDeduplicationRecord.IsEventHandled(SecondId));
        Assert.True(loaded.EventDeduplicationRecord.IsEventHandled(ThirdId));
        Assert.True(loaded.EventDeduplicationRecord.IsEventHandled(fourth));
    }

    // The record exposes its capacity only through its JSON form.
    private static int CapacityOf(ReadModel model, string optionSet)
    {
        string json = JsonSerializer.Serialize(model.EventDeduplicationRecord, OptionsFor(optionSet));
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("MAX_EVENT_CAPACITY").GetInt32();
    }
}
