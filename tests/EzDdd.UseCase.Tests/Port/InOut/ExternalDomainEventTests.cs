using EzDdd.Entity;
using EzDdd.UseCase.Port.InOut;

namespace EzDdd.UseCase.Tests.Port.InOut;

public class ExternalDomainEventTests
{
    [Fact]
    public void ExternalDomainEvent_ShouldExtendIDomainEvent()
    {
        TestExternalEvent externalEvent = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "customer-123",
            new Dictionary<string, string>(),
            "customer-123"
        );

        Assert.IsAssignableFrom<IDomainEvent>(externalEvent);
    }

    [Fact]
    public void ExternalDomainEvent_ShouldBeDistinguishableFromInternalDomainEvent()
    {
        TestExternalEvent externalEvent = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "customer-123",
            new Dictionary<string, string>(),
            "customer-123"
        );

        TestInternalEvent internalEvent = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "account-456",
            new Dictionary<string, string>(),
            "account-456"
        );

        Assert.IsAssignableFrom<IExternalDomainEvent>(externalEvent);
        Assert.IsNotAssignableFrom<IInternalDomainEvent>(externalEvent);
        Assert.IsNotAssignableFrom<IExternalDomainEvent>(internalEvent);
    }

    [Fact]
    public void ExternalDomainEvent_ShouldSupportTypeChecking()
    {
        TestExternalEvent externalEvent = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "customer-123",
            new Dictionary<string, string>(),
            "customer-123"
        );

        IDomainEvent domainEvent = externalEvent;

        Assert.True(domainEvent is IExternalDomainEvent);
        Assert.False(domainEvent is IInternalDomainEvent);

        bool isExternal = domainEvent.GetType().GetInterfaces().Any(i => i == typeof(IExternalDomainEvent));
        Assert.True(isExternal);
    }

    // Test event for external domain event
    private sealed record TestExternalEvent(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata,
        string CustomerId
    ) : IExternalDomainEvent;

    // Test event for internal domain event (for comparison)
    private sealed record TestInternalEvent(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        IReadOnlyDictionary<string, string> Metadata,
        string AccountId
    ) : IInternalDomainEvent;
}
