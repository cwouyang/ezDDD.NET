using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.InOut.Messaging;

namespace EzDdd.UseCase.Tests.Port.InOut.Messaging;

public class MessageBusTests
{
#region Interface Characteristics Tests

    [Fact]
    public void MessageBus_IsGenericInterface()
    {
        TestMessageBus<string> bus = new();

        Assert.NotNull(bus);
    }

#endregion

#region Registration and Unregistration Tests

    [Fact]
    public void MessageBus_CanRegisterReactor()
    {
        TestMessageBus<string> bus = new();
        TestReactor<string> reactor = new();

        bus.Register(reactor);

        Assert.Contains(reactor, bus.GetReactors());
    }

    [Fact]
    public void MessageBus_CanUnregisterReactor()
    {
        TestMessageBus<string> bus = new();
        TestReactor<string> reactor = new();
        bus.Register(reactor);

        bus.Unregister(reactor);

        Assert.DoesNotContain(reactor, bus.GetReactors());
    }

    [Fact]
    public void MessageBus_UnregisterNonExistentReactor_DoesNotThrow()
    {
        TestMessageBus<string> bus = new();
        TestReactor<string> reactor = new();

        Exception? exception = Record.Exception(() => bus.Unregister(reactor));

        Assert.Null(exception);
    }

#endregion

#region Message Posting Tests

    [Fact]
    public async Task MessageBus_PostAsync_InvokesRegisteredReactor()
    {
        TestMessageBus<string> bus = new();
        TestReactor<string> reactor = new();
        bus.Register(reactor);

        await bus.PostAsync("test message");

        Assert.True(reactor.WasExecuted);
        Assert.Equal("test message", reactor.ReceivedMessage);
    }

    [Fact]
    public async Task MessageBus_PostAsync_InvokesMultipleReactors()
    {
        TestMessageBus<string> bus = new();
        TestReactor<string> reactor1 = new();
        TestReactor<string> reactor2 = new();
        bus.Register(reactor1);
        bus.Register(reactor2);

        await bus.PostAsync("broadcast");

        Assert.True(reactor1.WasExecuted);
        Assert.True(reactor2.WasExecuted);
        Assert.Equal("broadcast", reactor1.ReceivedMessage);
        Assert.Equal("broadcast", reactor2.ReceivedMessage);
    }

    [Fact]
    public async Task MessageBus_PostAsync_WithNoReactors_DoesNotThrow()
    {
        TestMessageBus<string> bus = new();

        Exception? exception = await Record.ExceptionAsync
        (async () =>
             await bus.PostAsync("no listeners")
        );

        Assert.Null(exception);
    }

    [Fact]
    public async Task MessageBus_PostAsync_AfterUnregister_DoesNotInvokeReactor()
    {
        TestMessageBus<string> bus = new();
        TestReactor<string> reactor = new();
        bus.Register(reactor);
        bus.Unregister(reactor);

        await bus.PostAsync("should not receive");

        Assert.False(reactor.WasExecuted);
    }

#endregion

#region Execution Behavior Tests

    [Fact]
    public async Task MessageBus_PostAsync_IsAsync()
    {
        TestMessageBus<string> bus = new();
        AsyncTestReactor<string> reactor = new();
        bus.Register(reactor);

        Task task = bus.PostAsync("async test");

        Assert.False(task.IsCompleted); // Should not complete immediately
        await task;
        Assert.True(reactor.WasExecuted);
    }

    [Fact]
    public async Task MessageBus_PostAsync_ExecutesReactorsSequentially()
    {
        TestMessageBus<int> bus = new();
        List<int> executionOrder = [];
        OrderTrackingReactor reactor1 = new(1, executionOrder);
        OrderTrackingReactor reactor2 = new(2, executionOrder);
        bus.Register(reactor1);
        bus.Register(reactor2);

        await bus.PostAsync(0);

        Assert.Equal(new[] { 1, 2 }, executionOrder);
    }

#endregion

#region Test Helper Classes

    /// <summary>
    ///     Simple test implementation of IMessageBus for interface contract testing.
    /// </summary>
    private class TestMessageBus<TMessage> : IMessageBus<TMessage>
    {
        private readonly List<IReactor<TMessage>> _reactors = new();

        public void Register(IReactor<TMessage> reactor)
        {
            _reactors.Add(reactor);
        }

        public void Unregister(IReactor<TMessage> reactor)
        {
            _reactors.Remove(reactor);
        }

        public async Task PostAsync(TMessage message)
        {
            foreach (IReactor<TMessage> reactor in _reactors.ToArray())
            {
                await reactor.ExecuteAsync(message);
            }
        }

        public IEnumerable<IReactor<TMessage>> GetReactors()
        {
            return _reactors;
        }
    }

    private class TestReactor<TMessage> : IReactor<TMessage>
    {
        public bool WasExecuted { get; private set; }
        public TMessage? ReceivedMessage { get; private set; }

        public Task ExecuteAsync(TMessage input)
        {
            WasExecuted = true;
            ReceivedMessage = input;
            return Task.CompletedTask;
        }
    }

    private class AsyncTestReactor<TMessage> : IReactor<TMessage>
    {
        public bool WasExecuted { get; private set; }

        public async Task ExecuteAsync(TMessage input)
        {
            await Task.Delay(10); // Simulate async work
            WasExecuted = true;
        }
    }

    private class OrderTrackingReactor : IReactor<int>
    {
        private readonly List<int> _executionOrder;
        private readonly int _id;

        public OrderTrackingReactor(int id, List<int> executionOrder)
        {
            _id = id;
            _executionOrder = executionOrder;
        }

        public Task ExecuteAsync(int input)
        {
            _executionOrder.Add(_id);
            return Task.CompletedTask;
        }
    }

#endregion
}