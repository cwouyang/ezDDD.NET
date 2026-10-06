using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.InOut;

namespace EzDdd.UseCase.Tests.Port.In;

public class ReactorTests
{
    private const string EventType = "AccountCreated";
    private const string JsonContentType = "application/json";
    private static readonly byte[] _EmptyJson = "{}"u8.ToArray();

    private static DomainEventData _NewEventData()
    {
        return new DomainEventData(Guid.NewGuid(), EventType, JsonContentType, _EmptyJson, _EmptyJson);
    }

    [Fact]
    public async Task ExecuteAsync_WhenReactorHandlesEvent_YieldsDefaultOutput()
    {
        RecordingReactor reactor = new();
        DomainEventData data = _NewEventData();

        DefaultOutput output = await reactor.ExecuteAsync(new DomainEventDataInput { Event = data });

        Assert.Same(data, reactor.LastEvent);
        Assert.Equal(ExitCode.Success, output.ExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_WhenReactorReturnsDefaultOutputSubclass_YieldsThatInstance()
    {
        IReactor<DomainEventDataInput> reactor = new SubclassOutputReactor();

        DefaultOutput output = await reactor.ExecuteAsync(new DomainEventDataInput { Event = _NewEventData() });

        Assert.IsType<SubclassOutput>(output);
    }

    [Fact]
    public async Task ExecuteAsync_WhenReactorOfBaseInputUsedAsDerivedInput_ProcessesDerivedInput()
    {
        RecordingReactor baseReactor = new();
        IReactor<DerivedInput> derivedReactor = baseReactor;
        DomainEventData data = _NewEventData();

        DefaultOutput output = await derivedReactor.ExecuteAsync(new DerivedInput { Event = data, Extra = "extra" });

        Assert.Same(data, baseReactor.LastEvent);
        Assert.Equal(ExitCode.Success, output.ExitCode);
    }

    [Fact]
    public void DomainEventDataInput_WhenDerived_CarriesEventAndOwnData()
    {
        DomainEventData data = _NewEventData();

        DerivedInput input = new() { Event = data, Extra = "extra" };

        Assert.Same(data, input.Event);
        Assert.Equal("extra", input.Extra);
    }

    [Fact]
    public void DefaultOutput_WhenSubclassed_FluentSettersReturnDefaultOutput()
    {
        SubclassOutput output = new();

        DefaultOutput result = output.SetMessage("done");

        Assert.Same(output, result);
    }

    private sealed class SubclassOutput : DefaultOutput;

    private sealed record DerivedInput : DomainEventDataInput
    {
        public required string Extra { get; init; }
    }

    private sealed class RecordingReactor : IReactor<DomainEventDataInput>
    {
        public DomainEventData? LastEvent { get; private set; }

        public Task<DefaultOutput> ExecuteAsync(DomainEventDataInput input)
        {
            LastEvent = input.Event;
            return Task.FromResult(new DefaultOutput());
        }
    }

    private sealed class SubclassOutputReactor : IReactor<DomainEventDataInput>
    {
        public Task<DefaultOutput> ExecuteAsync(DomainEventDataInput input)
        {
            return Task.FromResult<DefaultOutput>(new SubclassOutput());
        }
    }
}
