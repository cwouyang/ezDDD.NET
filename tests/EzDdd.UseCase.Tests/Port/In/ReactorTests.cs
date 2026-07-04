using EzDdd.UseCase.Port.In;

namespace EzDdd.UseCase.Tests.Port.In;

public class ReactorTests
{
    [Fact]
    public void Interface_CanBeImplemented()
    {
        TestReactor reactor = new();

        Assert.IsAssignableFrom<IReactor<string>>(reactor);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCalled_ProcessesInput()
    {
        TestReactor reactor = new();

        await reactor.ExecuteAsync("event-1");

        Assert.Equal("event-1", reactor.LastInput);
    }

    [Fact]
    public void Interface_InputIsContravariant()
    {
        ObjectReactor reactor = new();

        // Compile-time verification: IReactor<in TInput> allows assigning
        // a reactor of a base input type to a more derived input type.
        IReactor<string> stringReactor = reactor;

        Assert.Same(reactor, stringReactor);
    }

    private class TestReactor : IReactor<string>
    {
        public string? LastInput { get; private set; }

        public Task ExecuteAsync(string input)
        {
            LastInput = input;
            return Task.CompletedTask;
        }
    }

    private class ObjectReactor : IReactor<object>
    {
        public Task ExecuteAsync(object input)
        {
            return Task.CompletedTask;
        }
    }
}
