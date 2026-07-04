using EzDdd.Cqrs.Query;
using EzDdd.UseCase.Port.In;

namespace EzDdd.Cqrs.Tests.Query;

public class NotifierTests
{
    [Fact]
    public void Interface_CanBeImplemented()
    {
        TestNotifier notifier = new();

        Assert.IsAssignableFrom<INotifier<string>>(notifier);
    }

    [Fact]
    public void Interface_ExtendsIReactor()
    {
        TestNotifier notifier = new();

        Assert.IsAssignableFrom<IReactor<string>>(notifier);
    }

    [Fact]
    public void Interface_InputIsContravariant()
    {
        ObjectNotifier notifier = new();

        // Compile-time verification: INotifier<in TInput> allows assigning
        // a notifier of a base input type to a more derived input type.
        INotifier<string> stringNotifier = notifier;

        Assert.Same(notifier, stringNotifier);
    }

    private class TestNotifier : INotifier<string>
    {
        public Task ExecuteAsync(string input)
        {
            return Task.CompletedTask;
        }
    }

    private class ObjectNotifier : INotifier<object>
    {
        public Task ExecuteAsync(object input)
        {
            return Task.CompletedTask;
        }
    }
}
