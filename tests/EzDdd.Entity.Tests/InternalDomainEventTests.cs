using System.Reflection;

namespace EzDdd.Entity.Tests;

public class InternalDomainEventTests
{
    [Fact]
    public void IInternalDomainEvent_ExtendsIDomainEvent()
    {
        AggregateUpdated @event = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "source",
            "value",
            new Dictionary<string, string>()
        );

        Assert.IsAssignableFrom<IDomainEvent>(@event);
        Assert.IsAssignableFrom<IInternalDomainEvent>(@event);
    }

    [Fact]
    public void IInternalDomainEvent_IsMarkerInterface_HasNoAdditionalMembers()
    {
        IEnumerable<MethodInfo> internalMethods = typeof(IInternalDomainEvent)
            .GetMethods()
            .Where(m => !m.IsSpecialName); // Exclude property getters
        IEnumerable<PropertyInfo> internalProperties = typeof(IInternalDomainEvent)
            .GetProperties()
            .Where(p => p.DeclaringType == typeof(IInternalDomainEvent)); // Only declared in IInternalDomainEvent

        // No additional members beyond IDomainEvent
        Assert.Empty(internalMethods);
        Assert.Empty(internalProperties);
    }

    [Fact]
    public void ConstructionEvent_ImplementsIConstructionEvent()
    {
        AggregateCreated @event = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "aggregate-123",
            "TestAggregate",
            new Dictionary<string, string>()
        );

        // Construction event implements marker
        Assert.IsAssignableFrom<IInternalDomainEvent.IConstructionEvent>(@event);
        Assert.IsAssignableFrom<IInternalDomainEvent>(@event);
        Assert.IsAssignableFrom<IDomainEvent>(@event);
    }

    [Fact]
    public void CommandEvent_DoesNotImplementLifecycleMarkers()
    {
        AggregateUpdated @event = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "aggregate-123",
            "NewValue",
            new Dictionary<string, string>()
        );

        // Command events are just IInternalDomainEvent
        Assert.IsAssignableFrom<IInternalDomainEvent>(@event);
        Assert.IsNotAssignableFrom<IInternalDomainEvent.IConstructionEvent>(@event);
        Assert.IsNotAssignableFrom<IInternalDomainEvent.IDestructionEvent>(@event);
    }

    [Fact]
    public void DestructionEvent_ImplementsIDestructionEvent()
    {
        AggregateDeleted @event = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "aggregate-123",
            "User requested deletion",
            new Dictionary<string, string>()
        );

        // Destruction event implements marker
        Assert.IsAssignableFrom<IInternalDomainEvent.IDestructionEvent>(@event);
        Assert.IsAssignableFrom<IInternalDomainEvent>(@event);
        Assert.IsAssignableFrom<IDomainEvent>(@event);
    }

    [Fact]
    public void LifecycleMarkers_AreMarkerInterfaces_HaveNoMembers()
    {
        MethodInfo[] constructionMethods = typeof(IInternalDomainEvent.IConstructionEvent).GetMethods();
        PropertyInfo[] constructionProperties = typeof(IInternalDomainEvent.IConstructionEvent).GetProperties();
        MethodInfo[] destructionMethods = typeof(IInternalDomainEvent.IDestructionEvent).GetMethods();
        PropertyInfo[] destructionProperties = typeof(IInternalDomainEvent.IDestructionEvent).GetProperties();

        // Pure marker interfaces
        Assert.Empty(constructionMethods);
        Assert.Empty(constructionProperties);
        Assert.Empty(destructionMethods);
        Assert.Empty(destructionProperties);
    }

    [Fact]
    public void IInternalDomainEvent_PatternMatching_WorksWithSwitchExpression()
    {
        AggregateCreated constructionEvent = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "agg-1",
            "Test",
            new Dictionary<string, string>()
        );
        AggregateUpdated commandEvent = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "agg-1",
            "Updated",
            new Dictionary<string, string>()
        );
        AggregateDeleted destructionEvent = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "agg-1",
            "Deleted",
            new Dictionary<string, string>()
        );

        string constructionResult = ClassifyEvent(constructionEvent);
        string commandResult = ClassifyEvent(commandEvent);
        string destructionResult = ClassifyEvent(destructionEvent);

        Assert.Equal("Construction", constructionResult);
        Assert.Equal("Command", commandResult);
        Assert.Equal("Destruction", destructionResult);
        return;

        // Helper methods for pattern matching test
        static string ClassifyEvent(IInternalDomainEvent @event) =>
            @event switch
            {
                IInternalDomainEvent.IConstructionEvent => "Construction",
                IInternalDomainEvent.IDestructionEvent => "Destruction",
                _ => "Command",
            };
    }

    [Fact]
    public void IInternalDomainEvent_TypeConstraint_WorksWithGenericMethods()
    {
        AggregateUpdated @event = new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "source",
            "value",
            new Dictionary<string, string>()
        );

        string source = GetEventSource(@event);

        Assert.Equal(@event.Source, source);
        return;

        static string GetEventSource<TEvent>(TEvent @event)
            where TEvent : IInternalDomainEvent => @event.Source;
    }

    [Fact]
    public void ConstructionEvent_NestedInterface_AccessibleViaParent()
    {
        Type nestedType = typeof(IInternalDomainEvent.IConstructionEvent);

        // Nested interface is accessible
        Assert.NotNull(nestedType);
        Assert.True(nestedType.IsInterface);
        Assert.True(nestedType.IsNestedPublic);
        Assert.Equal("IConstructionEvent", nestedType.Name);
    }

    // Construction event (R1 rule)
    private record AggregateCreated(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string Name,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IConstructionEvent;

    // Command event (R2 rule)
    private record AggregateUpdated(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string NewValue,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent;

    // Destruction event (R3 rule)
    private record AggregateDeleted(
        Guid Id,
        DateTimeOffset OccurredOn,
        string Source,
        string Reason,
        IReadOnlyDictionary<string, string> Metadata
    ) : IInternalDomainEvent, IInternalDomainEvent.IDestructionEvent;
}
