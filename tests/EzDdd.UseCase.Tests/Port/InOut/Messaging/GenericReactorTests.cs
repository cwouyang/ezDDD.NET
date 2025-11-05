using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.InOut.Messaging;

namespace EzDdd.UseCase.Tests.Port.InOut.Messaging;

public class GenericReactorTests
{
#region Instantiation and Interface Tests

    [Fact]
    public void GenericReactor_CanBeInstantiated()
    {
        GenericReactor<string> reactor = new(_ => Task.CompletedTask);

        Assert.NotNull(reactor);
    }

    [Fact]
    public void GenericReactor_ImplementsIReactor()
    {
        GenericReactor<string> reactor = new(_ => Task.CompletedTask);

        Assert.IsAssignableFrom<IReactor<string>>(reactor);
    }

#endregion

#region ExecuteAsync Behavior Tests

    [Fact]
    public async Task ExecuteAsync_InvokesProvidedHandler()
    {
        bool wasInvoked = false;
        GenericReactor<string> reactor = new
        (_ =>
            {
                wasInvoked = true;
                return Task.CompletedTask;
            }
        );

        await reactor.ExecuteAsync("test");

        Assert.True(wasInvoked);
    }

    [Fact]
    public async Task ExecuteAsync_PassesMessageToHandler()
    {
        string? receivedMessage = null;
        GenericReactor<string> reactor = new
        (msg =>
            {
                receivedMessage = msg;
                return Task.CompletedTask;
            }
        );

        await reactor.ExecuteAsync("test message");

        Assert.Equal("test message", receivedMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithAsyncHandler_AwaitsCompletion()
    {
        bool completed = false;
        GenericReactor<string> reactor = new
        (async _ =>
            {
                await Task.Delay(10);
                completed = true;
            }
        );

        await reactor.ExecuteAsync("test");

        Assert.True(completed);
    }

    [Fact]
    public async Task ExecuteAsync_CanBeCalledMultipleTimes()
    {
        int count = 0;
        GenericReactor<int> reactor = new
        (msg =>
            {
                count += msg;
                return Task.CompletedTask;
            }
        );

        await reactor.ExecuteAsync(1);
        await reactor.ExecuteAsync(2);
        await reactor.ExecuteAsync(3);

        Assert.Equal(6, count);
    }

#endregion

#region Message Type Tests

    [Fact]
    public async Task ExecuteAsync_WithDifferentMessageTypes()
    {
        // String reactor
        string? receivedString = null;
        GenericReactor<string> stringReactor = new
        (msg =>
            {
                receivedString = msg;
                return Task.CompletedTask;
            }
        );

        // Int reactor
        int receivedInt = 0;
        GenericReactor<int> intReactor = new
        (msg =>
            {
                receivedInt = msg;
                return Task.CompletedTask;
            }
        );

        await stringReactor.ExecuteAsync("text");
        await intReactor.ExecuteAsync(42);

        Assert.Equal("text", receivedString);
        Assert.Equal(42, receivedInt);
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexType()
    {
        TestMessage? receivedMessage = null;
        GenericReactor<TestMessage> reactor = new
        (msg =>
            {
                receivedMessage = msg;
                return Task.CompletedTask;
            }
        );

        TestMessage message = new() { Id = 1, Content = "complex" };
        await reactor.ExecuteAsync(message);

        Assert.Same(message, receivedMessage);
        Assert.Equal(1, receivedMessage?.Id);
        Assert.Equal("complex", receivedMessage?.Content);
    }

#endregion

#region Advanced Behavior Tests

    [Fact]
    public async Task ExecuteAsync_WithLambdaExpression_WorksCorrectly()
    {
        List<string> messages = [];
        GenericReactor<string> reactor = new
        (async msg =>
            {
                await Task.Delay(1); // Simulate async work
                messages.Add(msg);
            }
        );

        await reactor.ExecuteAsync("first");
        await reactor.ExecuteAsync("second");

        Assert.Equal(2, messages.Count);
        Assert.Equal("first", messages[0]);
        Assert.Equal("second", messages[1]);
    }

    [Fact]
    public async Task ExecuteAsync_HandlerException_PropagatesCorrectly()
    {
        GenericReactor<string> reactor = new
        (_ =>
             throw new InvalidOperationException("Handler error")
        );

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>
            (async () => await reactor.ExecuteAsync("test"));

        Assert.Equal("Handler error", exception.Message);
    }

#endregion

#region Message Bus Integration Tests

    [Fact]
    public async Task GenericReactor_WorksWithMessageBus()
    {
        BlockingMessageBus<string> bus = new();
        List<string> receivedMessages = [];

        // Register reactor with lambda
        bus.Register
        (
            new GenericReactor<string>
            (msg =>
                {
                    receivedMessages.Add(msg);
                    return Task.CompletedTask;
                }
            )
        );

        await bus.PostAsync("message1");
        await bus.PostAsync("message2");

        Assert.Equal(2, receivedMessages.Count);
        Assert.Equal("message1", receivedMessages[0]);
        Assert.Equal("message2", receivedMessages[1]);
    }

    [Fact]
    public async Task GenericReactor_MultipleReactors_OnSameBus()
    {
        BlockingMessageBus<int> bus = new();
        int sum1 = 0;
        int sum2 = 0;

        bus.Register
        (
            new GenericReactor<int>
            (msg =>
                {
                    sum1 += msg;
                    return Task.CompletedTask;
                }
            )
        );

        bus.Register
        (
            new GenericReactor<int>
            (msg =>
                {
                    sum2 += msg * 2;
                    return Task.CompletedTask;
                }
            )
        );

        await bus.PostAsync(5);

        Assert.Equal(5, sum1);
        Assert.Equal(10, sum2);
    }

#endregion

#region Test Helper Classes

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }

#endregion
}