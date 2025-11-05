using EzDdd.UseCase.Port.InOut;
using EzDdd.UseCase.Port.InOut.Messaging;

namespace EzDdd.UseCase.Tests.Integration;

public sealed class MessageBusIntegrationTests
{
    [Fact]
    public async Task SingleReactor_ReceivesEvent()
    {
        BlockingMessageBus<DomainEventData> bus = new();
        List<DomainEventData> received = [];
        GenericReactor<DomainEventData> reactor = new
        (async evt =>
            {
                received.Add(evt);
                await Task.CompletedTask;
            }
        );

        bus.Register(reactor);

        DomainEventData eventData = new
        (
            Guid.NewGuid(),
            "TestEvent",
            "application/json",
            [1, 2, 3],
            []
        );

        await bus.PostAsync(eventData);

        Assert.Single(received);
        Assert.Equal(eventData.Id, received[0].Id);
        Assert.Equal("TestEvent", received[0].EventType);
    }

    [Fact]
    public async Task MultipleReactors_ReceiveEventsInOrder()
    {
        BlockingMessageBus<DomainEventData> bus = new();
        List<string> order = [];

        GenericReactor<DomainEventData> reactor1 = new
        (async evt =>
            {
                order.Add("Reactor1");
                await Task.CompletedTask;
            }
        );

        GenericReactor<DomainEventData> reactor2 = new
        (async evt =>
            {
                order.Add("Reactor2");
                await Task.CompletedTask;
            }
        );

        GenericReactor<DomainEventData> reactor3 = new
        (async evt =>
            {
                order.Add("Reactor3");
                await Task.CompletedTask;
            }
        );

        bus.Register(reactor1);
        bus.Register(reactor2);
        bus.Register(reactor3);

        DomainEventData eventData = new
        (
            Guid.NewGuid(),
            "OrderEvent",
            "application/json",
            [],
            []
        );

        await bus.PostAsync(eventData);

        Assert.Equal(3, order.Count);
        Assert.Equal("Reactor1", order[0]);
        Assert.Equal("Reactor2", order[1]);
        Assert.Equal("Reactor3", order[2]);
    }

    [Fact]
    public async Task EventBusProducer_PostsEventsToBus()
    {
        BlockingMessageBus<DomainEventData> bus = new();
        EventBusProducer producer = new(bus);
        List<DomainEventData> received = [];

        GenericReactor<DomainEventData> reactor = new
        (async evt =>
            {
                received.Add(evt);
                await Task.CompletedTask;
            }
        );

        bus.Register(reactor);

        DomainEventData eventData = new
        (
            Guid.NewGuid(),
            "AccountCreated",
            "application/json",
            [10, 20, 30],
            []
        );

        await producer.PostAsync(eventData);

        Assert.Single(received);
        Assert.Equal(eventData.Id, received[0].Id);
        Assert.Equal("AccountCreated", received[0].EventType);
    }

    [Fact]
    public async Task ConcurrentPosts_AllReactorsReceiveAllEvents()
    {
        BlockingMessageBus<DomainEventData> bus = new();
        List<Guid> reactor1Received = [];
        List<Guid> reactor2Received = [];
        object lockObject = new();

        GenericReactor<DomainEventData> reactor1 = new
        (async evt =>
            {
                lock (lockObject)
                {
                    reactor1Received.Add(evt.Id);
                }

                await Task.Delay(1); // Simulate processing
            }
        );

        GenericReactor<DomainEventData> reactor2 = new
        (async evt =>
            {
                lock (lockObject)
                {
                    reactor2Received.Add(evt.Id);
                }

                await Task.Delay(1); // Simulate processing
            }
        );

        bus.Register(reactor1);
        bus.Register(reactor2);

        List<Task> tasks = [];
        List<Guid> eventIds = [];

        for (int i = 0; i < 10; i++)
        {
            Guid eventId = Guid.NewGuid();
            eventIds.Add(eventId);

            DomainEventData eventData = new
            (
                eventId,
                $"Event{i}",
                "application/json",
                [],
                []
            );

            tasks.Add(Task.Run(async () => await bus.PostAsync(eventData)));
        }

        await Task.WhenAll(tasks);

        Assert.Equal(10, reactor1Received.Count);
        Assert.Equal(10, reactor2Received.Count);

        // All event IDs should be present
        foreach (Guid eventId in eventIds)
        {
            Assert.Contains(eventId, reactor1Received);
            Assert.Contains(eventId, reactor2Received);
        }
    }

    [Fact]
    public async Task DynamicRegistration_DuringPost_DoesNotAffectCurrentPost()
    {
        BlockingMessageBus<DomainEventData> bus = new();
        bool reactor1Executed = false;
        bool reactor2Executed = false;
        bool reactor3Executed = false;

        GenericReactor<DomainEventData> reactor3 = new
        (async e =>
            {
                reactor3Executed = true;
                await Task.CompletedTask;
            }
        );

        GenericReactor<DomainEventData> reactor1 = new
        (async evt =>
            {
                reactor1Executed = true;

                // Register reactor3 during execution (should not receive this event)
                bus.Register(reactor3);

                await Task.CompletedTask;
            }
        );

        GenericReactor<DomainEventData> reactor2 = new
        (async evt =>
            {
                reactor2Executed = true;
                await Task.CompletedTask;
            }
        );

        bus.Register(reactor1);
        bus.Register(reactor2);

        DomainEventData eventData = new
        (
            Guid.NewGuid(),
            "TestEvent",
            "application/json",
            [],
            []
        );

        await bus.PostAsync(eventData);

        Assert.True(reactor1Executed);
        Assert.True(reactor2Executed);
        Assert.False(reactor3Executed); // Not executed because registered during current post
    }

    [Fact]
    public async Task Unregister_RemovesReactorFromBus()
    {
        BlockingMessageBus<DomainEventData> bus = new();
        int reactor1Count = 0;
        int reactor2Count = 0;

        GenericReactor<DomainEventData> reactor1 = new
        (async evt =>
            {
                reactor1Count++;
                await Task.CompletedTask;
            }
        );

        GenericReactor<DomainEventData> reactor2 = new
        (async evt =>
            {
                reactor2Count++;
                await Task.CompletedTask;
            }
        );

        bus.Register(reactor1);
        bus.Register(reactor2);

        DomainEventData eventData = new
        (
            Guid.NewGuid(),
            "Event1",
            "application/json",
            [],
            []
        );

        await bus.PostAsync(eventData);
        Assert.Equal(1, reactor1Count);
        Assert.Equal(1, reactor2Count);

        // Unregister reactor1
        bus.Unregister(reactor1);

        // Second post: only reactor2 receives
        await bus.PostAsync(eventData);
        Assert.Equal(1, reactor1Count); // Still 1 (not incremented)
        Assert.Equal(2, reactor2Count); // Incremented to 2
    }

    [Fact]
    public async Task ReactorException_PropagatesAndStopsSubsequentReactors()
    {
        BlockingMessageBus<DomainEventData> bus = new();
        bool reactor1Executed = false;
        bool reactor2Executed = false;
        bool reactor3Executed = false;

        GenericReactor<DomainEventData> reactor1 = new
        (async evt =>
            {
                reactor1Executed = true;
                await Task.CompletedTask;
            }
        );

        GenericReactor<DomainEventData> reactor2 = new
        (async evt =>
            {
                reactor2Executed = true;
                throw new InvalidOperationException("Reactor2 failed");
#pragma warning disable CS0162 // Unreachable code detected
                await Task.CompletedTask;
#pragma warning restore CS0162
            }
        );

        GenericReactor<DomainEventData> reactor3 = new
        (async evt =>
            {
                reactor3Executed = true;
                await Task.CompletedTask;
            }
        );

        bus.Register(reactor1);
        bus.Register(reactor2);
        bus.Register(reactor3);

        DomainEventData eventData = new
        (
            Guid.NewGuid(),
            "TestEvent",
            "application/json",
            [],
            []
        );

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>
        (async () =>
            {
                await bus.PostAsync(eventData);
            }
        );

        Assert.Equal("Reactor2 failed", exception.Message);
        Assert.True(reactor1Executed); // Executed before exception
        Assert.True(reactor2Executed); // Executed and threw
        Assert.False(reactor3Executed); // NOT executed (stopped by reactor2's exception)
    }
}