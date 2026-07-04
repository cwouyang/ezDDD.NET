using EzDdd.UseCase.Port.InOut;
using EzDdd.UseCase.Port.InOut.Messaging;

namespace EzDdd.UseCase.Tests.Port.InOut.Messaging;

public class ExternalDomainEventPublisherTests
{
    [Fact]
    public void Interface_CanBeImplemented()
    {
        TestPublisher publisher = new();

        Assert.IsAssignableFrom<IExternalDomainEventPublisher<BaseExternalEvent>>(publisher);
    }

    [Fact]
    public async Task PublishAsync_WhenCalled_PublishesEvent()
    {
        TestPublisher publisher = new();
        BaseExternalEvent externalEvent = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "customer-123",
            new Dictionary<string, string>()
        );

        await publisher.PublishAsync(externalEvent);

        Assert.Same(externalEvent, publisher.LastPublishedEvent);
    }

    [Fact]
    public void Interface_InputIsContravariant()
    {
        TestPublisher publisher = new();

        // Compile-time verification: IExternalDomainEventPublisher<in TEvent> allows assigning
        // a publisher of a base event type to a more derived event type.
        IExternalDomainEventPublisher<DerivedExternalEvent> derivedPublisher = publisher;

        Assert.Same(publisher, derivedPublisher);
    }

    private class TestPublisher : IExternalDomainEventPublisher<BaseExternalEvent>
    {
        public BaseExternalEvent? LastPublishedEvent { get; private set; }

        public Task PublishAsync(BaseExternalEvent @event)
        {
            LastPublishedEvent = @event;
            return Task.CompletedTask;
        }
    }

    // Base external event for publisher tests
    private record BaseExternalEvent(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata
    ) : IExternalDomainEvent;

    // Derived external event for the contravariance test
    private record DerivedExternalEvent(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata,
        string CustomerId
    ) : BaseExternalEvent(Id, OccurredOn, Source, Metadata);
}
