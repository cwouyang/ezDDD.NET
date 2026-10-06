using EzDdd.Cqrs.Query;
using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.InOut;

namespace EzDdd.Cqrs.Tests.Query;

public class NotifierTests
{
    private static DomainEventDataInput _NewInput()
    {
        byte[] json = "{}"u8.ToArray();
        return new DomainEventDataInput
        {
            Event = new DomainEventData(Guid.NewGuid(), "AccountCreated", "application/json", json, json),
        };
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotifierPublishesEvent_YieldsDefaultOutput()
    {
        RecordingNotifier notifier = new();
        DomainEventDataInput input = _NewInput();

        DefaultOutput output = await notifier.ExecuteAsync(input);

        Assert.Same(input.Event, notifier.LastEvent);
        Assert.Equal(ExitCode.Success, output.ExitCode);
    }

    [Fact]
    public void Interface_IsNotAReactor()
    {
        RecordingNotifier notifier = new();

        bool implementsOpenReactor = notifier
            .GetType()
            .GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReactor<>));

        Assert.False(implementsOpenReactor);
        object boxed = notifier;

        Assert.False(boxed is IReactor<DomainEventDataInput>);
    }

    [Fact]
    public void Interface_IsAUseCase()
    {
        RecordingNotifier notifier = new();

        Assert.IsAssignableFrom<IUseCase<DomainEventDataInput, DefaultOutput>>(notifier);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotifierOfBaseInputUsedAsDerivedInput_ProcessesDerivedInput()
    {
        RecordingNotifier baseNotifier = new();
        INotifier<DerivedInput> derivedNotifier = baseNotifier;
        DomainEventDataInput template = _NewInput();

        DefaultOutput output = await derivedNotifier.ExecuteAsync(
            new DerivedInput { Event = template.Event, Extra = "extra" }
        );

        Assert.Same(template.Event, baseNotifier.LastEvent);
        Assert.Equal(ExitCode.Success, output.ExitCode);
    }

    private sealed record DerivedInput : DomainEventDataInput
    {
        public required string Extra { get; init; }
    }

    private sealed class RecordingNotifier : INotifier<DomainEventDataInput>
    {
        public DomainEventData? LastEvent { get; private set; }

        public Task<DefaultOutput> ExecuteAsync(DomainEventDataInput input)
        {
            LastEvent = input.Event;
            return Task.FromResult(new DefaultOutput());
        }
    }
}
