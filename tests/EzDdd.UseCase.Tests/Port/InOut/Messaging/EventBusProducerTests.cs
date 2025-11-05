using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.InOut;
using EzDdd.UseCase.Port.InOut.Messaging;

namespace EzDdd.UseCase.Tests.Port.InOut.Messaging;

public class EventBusProducerTests
{
#region Instantiation and Interface Tests

    [Fact]
    public void EventBusProducer_CanBeInstantiated()
    {
        BlockingMessageBus<DomainEventData> bus = new();
        EventBusProducer producer = new(bus);

        Assert.NotNull(producer);
    }

    [Fact]
    public void EventBusProducer_ImplementsIMessageProducer()
    {
        BlockingMessageBus<DomainEventData> bus = new();
        EventBusProducer producer = new(bus);

        Assert.IsAssignableFrom<IMessageProducer<DomainEventData>>(producer);
    }

#endregion

#region PostAsync Behavior Tests

    [Fact]
    public async Task PostAsync_DelegatesToUnderlyingBus()
    {
        TestMessageBus bus = new();
        EventBusProducer producer = new(bus);
        DomainEventData eventData = _CreateTestEventData();

        await producer.PostAsync(eventData);

        Assert.True(bus.WasPosted);
        Assert.Same(eventData, bus.LastPostedEvent);
    }

    [Fact]
    public async Task PostAsync_WithMultipleEvents_DelegatesToBusEachTime()
    {
        TestMessageBus bus = new();
        EventBusProducer producer = new(bus);
        DomainEventData event1 = _CreateTestEventData("event1");
        DomainEventData event2 = _CreateTestEventData("event2");
        DomainEventData event3 = _CreateTestEventData("event3");

        await producer.PostAsync(event1);
        await producer.PostAsync(event2);
        await producer.PostAsync(event3);

        Assert.Equal(3, bus.PostCount);
        Assert.Same(event3, bus.LastPostedEvent);
    }

    [Fact]
    public async Task PostAsync_WithRealBlockingMessageBus_InvokesReactors()
    {
        BlockingMessageBus<DomainEventData> bus = new();
        TestEventReactor reactor = new();
        EventBusProducer producer = new(bus);
        DomainEventData eventData = _CreateTestEventData();

        bus.Register(reactor);
        await producer.PostAsync(eventData);

        Assert.True(reactor.WasExecuted);
        Assert.Same(eventData, reactor.ReceivedEvent);
    }

    [Fact]
    public async Task PostAsync_IsAsync()
    {
        AsyncTestMessageBus bus = new();
        EventBusProducer producer = new(bus);
        DomainEventData eventData = _CreateTestEventData();

        Task task = producer.PostAsync(eventData);

        Assert.False(task.IsCompleted); // Should not complete immediately
        await task;
        Assert.True(bus.WasPosted);
    }

    [Fact]
    public async Task EventBusProducer_WorksWithDomainEventData()
    {
        BlockingMessageBus<DomainEventData> bus = new();
        EventBusProducer producer = new(bus);
        DomainEventData eventData = new
        (
            Guid.NewGuid(),
            "TestEvent",
            "application/json",
            [1, 2, 3],
            [4, 5, 6]
        );

        Exception? exception = await Record.ExceptionAsync
        (async () =>
             await producer.PostAsync(eventData)
        );

        Assert.Null(exception);
    }

#endregion

#region Disposal Tests

    [Fact]
    public void EventBusProducer_ImplementsIDisposable()
    {
        BlockingMessageBus<DomainEventData> bus = new();
        EventBusProducer producer = new(bus);

        Assert.IsAssignableFrom<IDisposable>(producer);
    }

    [Fact]
    public void Dispose_CanBeCalledSuccessfully()
    {
        BlockingMessageBus<DomainEventData> bus = new();
        EventBusProducer producer = new(bus);

        Exception? exception = Record.Exception(() => producer.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_IsIdempotent()
    {
        BlockingMessageBus<DomainEventData> bus = new();
        EventBusProducer producer = new(bus);

        producer.Dispose();
        Exception? exception = Record.Exception(() => producer.Dispose());

        Assert.Null(exception); // Should not throw on second dispose
    }

    [Fact]
    public void Dispose_WithUsingStatement_DisposesSuccessfully()
    {
        BlockingMessageBus<DomainEventData> bus = new();

        Exception? exception = Record.Exception
        (() =>
            {
                using EventBusProducer producer = new(bus);
                // Producer will be disposed at end of scope
            }
        );

        Assert.Null(exception);
    }

    [Fact]
    public async Task PostAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        BlockingMessageBus<DomainEventData> bus = new();
        EventBusProducer producer = new(bus);
        DomainEventData eventData = _CreateTestEventData();

        producer.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>
        (async () =>
             await producer.PostAsync(eventData)
        );
    }

    [Fact]
    public async Task PostAsync_BeforeDispose_WorksNormally()
    {
        TestMessageBus bus = new();
        EventBusProducer producer = new(bus);
        DomainEventData eventData = _CreateTestEventData();

        await producer.PostAsync(eventData);
        producer.Dispose();

        Assert.True(bus.WasPosted);
        Assert.Same(eventData, bus.LastPostedEvent);
    }

#endregion

#region Test Helper Methods

    private static DomainEventData _CreateTestEventData(string eventType = "TestEvent")
    {
        return new DomainEventData
        (
            Guid.NewGuid(),
            eventType,
            "application/json",
            [1, 2, 3],
            []
        );
    }

#endregion

#region Test Helper Classes

    private class TestMessageBus : IMessageBus<DomainEventData>
    {
        public bool WasPosted { get; private set; }
        public DomainEventData? LastPostedEvent { get; private set; }
        public int PostCount { get; private set; }

        public void Register(IReactor<DomainEventData> reactor)
        {
            // Not used in producer tests
        }

        public void Unregister(IReactor<DomainEventData> reactor)
        {
            // Not used in producer tests
        }

        public Task PostAsync(DomainEventData message)
        {
            WasPosted = true;
            LastPostedEvent = message;
            PostCount++;
            return Task.CompletedTask;
        }
    }

    private class AsyncTestMessageBus : IMessageBus<DomainEventData>
    {
        public bool WasPosted { get; private set; }

        public void Register(IReactor<DomainEventData> reactor)
        {
        }

        public void Unregister(IReactor<DomainEventData> reactor)
        {
        }

        public async Task PostAsync(DomainEventData message)
        {
            await Task.Delay(10); // Simulate async work
            WasPosted = true;
        }
    }

    private class TestEventReactor : IReactor<DomainEventData>
    {
        public bool WasExecuted { get; private set; }
        public DomainEventData? ReceivedEvent { get; private set; }

        public Task ExecuteAsync(DomainEventData input)
        {
            WasExecuted = true;
            ReceivedEvent = input;
            return Task.CompletedTask;
        }
    }

#endregion
}