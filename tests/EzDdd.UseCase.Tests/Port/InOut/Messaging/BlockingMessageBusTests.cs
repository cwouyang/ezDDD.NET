using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.InOut.Messaging;

namespace EzDdd.UseCase.Tests.Port.InOut.Messaging;

public class BlockingMessageBusTests
{
#region Instantiation and Interface Tests

    [Fact]
    public void BlockingMessageBus_CanBeInstantiated()
    {
        BlockingMessageBus<string> bus = new();

        Assert.NotNull(bus);
    }

    [Fact]
    public void BlockingMessageBus_ImplementsIMessageBus()
    {
        BlockingMessageBus<string> bus = new();

        Assert.IsAssignableFrom<IMessageBus<string>>(bus);
    }

#endregion

#region Registration Tests

    [Fact]
    public async Task Register_AddsReactorToBus()
    {
        BlockingMessageBus<string> bus = new();
        TestReactor<string> reactor = new();

        bus.Register(reactor);
        await bus.PostAsync("test");

        Assert.True(reactor.WasExecuted);
    }

    [Fact]
    public async Task Unregister_RemovesReactorFromBus()
    {
        BlockingMessageBus<string> bus = new();
        TestReactor<string> reactor = new();

        bus.Register(reactor);
        bus.Unregister(reactor);
        await bus.PostAsync("test");

        Assert.False(reactor.WasExecuted);
    }

#endregion

#region Message Posting Tests

    [Fact]
    public async Task PostAsync_InvokesAllRegisteredReactors()
    {
        BlockingMessageBus<string> bus = new();
        TestReactor<string> reactor1 = new();
        TestReactor<string> reactor2 = new();
        TestReactor<string> reactor3 = new();

        bus.Register(reactor1);
        bus.Register(reactor2);
        bus.Register(reactor3);
        await bus.PostAsync("broadcast");

        Assert.True(reactor1.WasExecuted);
        Assert.True(reactor2.WasExecuted);
        Assert.True(reactor3.WasExecuted);
        Assert.Equal("broadcast", reactor1.ReceivedMessage);
        Assert.Equal("broadcast", reactor2.ReceivedMessage);
        Assert.Equal("broadcast", reactor3.ReceivedMessage);
    }

    [Fact]
    public async Task PostAsync_WithNoReactors_CompletesWithoutError()
    {
        BlockingMessageBus<string> bus = new();

        Exception? exception = await Record.ExceptionAsync
        (async () =>
             await bus.PostAsync("no listeners")
        );

        Assert.Null(exception);
    }

    [Fact]
    public async Task PostAsync_ExecutesReactorsSequentially()
    {
        BlockingMessageBus<int> bus = new();
        List<int> executionOrder = [];
        OrderTrackingReactor reactor1 = new(1, executionOrder);
        OrderTrackingReactor reactor2 = new(2, executionOrder);
        OrderTrackingReactor reactor3 = new(3, executionOrder);

        bus.Register(reactor1);
        bus.Register(reactor2);
        bus.Register(reactor3);
        await bus.PostAsync(0);

        Assert.Equal(new[] { 1, 2, 3 }, executionOrder);
    }

#endregion

#region Snapshot Pattern Tests

    [Fact]
    public async Task PostAsync_UsesSnapshotPattern_RegisterDuringPostDoesNotAffectCurrentPost()
    {
        BlockingMessageBus<string> bus = new();
        TestReactor<string> reactor1 = new();
        TestReactor<string> reactor2 = new();
        TestReactor<string> lateReactor = new();
        bus.Register(reactor1);
        bus.Register(reactor2);

        // Start posting
        Task postTask = bus.PostAsync("message");

        // Register new reactor during post (should not receive current message)
        bus.Register(lateReactor);

        await postTask;

        Assert.True(reactor1.WasExecuted);
        Assert.True(reactor2.WasExecuted);
        Assert.False(lateReactor.WasExecuted); // Should not receive message from ongoing post
    }

    [Fact]
    public async Task PostAsync_UsesSnapshotPattern_UnregisterDuringPostDoesNotAffectCurrentPost()
    {
        BlockingMessageBus<string> bus = new();
        SlowReactor<string> slowReactor = new(100); // 100ms delay
        TestReactor<string> fastReactor = new();
        bus.Register(slowReactor);
        bus.Register(fastReactor);

        // Start posting (slowReactor will take 100ms)
        Task postTask = bus.PostAsync("message");

        // Give it a moment to start
        await Task.Delay(10);

        // Unregister fastReactor during post (snapshot should still include it)
        bus.Unregister(fastReactor);

        await postTask;

        Assert.True(slowReactor.WasExecuted);
        Assert.True(fastReactor.WasExecuted); // Should still execute (was in snapshot)
    }

#endregion

#region Advanced Registration and Async Tests

    [Fact]
    public async Task Register_CanRegisterSameReactorMultipleTimes()
    {
        BlockingMessageBus<string> bus = new();
        CountingReactor<string> reactor = new();

        bus.Register(reactor);
        bus.Register(reactor); // Register same reactor twice
        await bus.PostAsync("test");

        Assert.Equal(2, reactor.ExecutionCount); // Should be invoked twice
    }

    [Fact]
    public async Task Unregister_RemovesOnlyOneRegistration()
    {
        BlockingMessageBus<string> bus = new();
        CountingReactor<string> reactor = new();

        bus.Register(reactor);
        bus.Register(reactor);
        bus.Unregister(reactor); // Remove one registration
        await bus.PostAsync("test");

        Assert.Equal(1, reactor.ExecutionCount); // Should be invoked once (one registration left)
    }

    [Fact]
    public void Unregister_NonExistentReactor_DoesNotThrow()
    {
        BlockingMessageBus<string> bus = new();
        TestReactor<string> reactor = new();

        Exception? exception = Record.Exception(() => bus.Unregister(reactor));

        Assert.Null(exception);
    }

    [Fact]
    public async Task PostAsync_IsAsync()
    {
        BlockingMessageBus<string> bus = new();
        SlowReactor<string> reactor = new(50);

        bus.Register(reactor);
        Task task = bus.PostAsync("async test");

        Assert.False(task.IsCompleted); // Should not complete immediately
        await task;
        Assert.True(reactor.WasExecuted);
    }

#endregion

#region Thread Safety Tests

    [Fact]
    public async Task PostAsync_ThreadSafe_ConcurrentPosts()
    {
        BlockingMessageBus<int> bus = new();
        CountingReactor<int> reactor = new();

        bus.Register(reactor);

        // Post 10 messages concurrently
        Task[] tasks = Enumerable.Range(0, 10)
                                 .Select(i => bus.PostAsync(i))
                                 .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(10, reactor.ExecutionCount);
    }

    [Fact]
    public async Task PostAsync_ThreadSafe_ConcurrentRegistrations()
    {
        BlockingMessageBus<string> bus = new();
        TestReactor<string>[] reactors = Enumerable.Range(0, 10)
                                                   .Select(_ => new TestReactor<string>())
                                                   .ToArray();

        // Register 10 reactors concurrently
        Task[] registerTasks = reactors
                               .Select(r => Task.Run(() => bus.Register(r)))
                               .ToArray();

        await Task.WhenAll(registerTasks);

        // Post message
        await bus.PostAsync("test");

        // All reactors should receive the message
        Assert.All(reactors, r => Assert.True(r.WasExecuted));
    }

#endregion

#region Test Helper Classes

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

    private class SlowReactor<TMessage> : IReactor<TMessage>
    {
        private readonly int _delayMs;

        public SlowReactor(int delayMs)
        {
            _delayMs = delayMs;
        }

        public bool WasExecuted { get; private set; }

        public async Task ExecuteAsync(TMessage input)
        {
            await Task.Delay(_delayMs);
            WasExecuted = true;
        }
    }

    private class CountingReactor<TMessage> : IReactor<TMessage>
    {
        private int _count;
        public int ExecutionCount => _count;

        public Task ExecuteAsync(TMessage input)
        {
            Interlocked.Increment(ref _count);
            return Task.CompletedTask;
        }
    }

#endregion
}