using EzDdd.Cqrs.Query;
using EzDdd.UseCase.Port.In;

namespace EzDdd.Cqrs.Tests.Query;

public class ProjectorTests
{
    [Fact]
    public void Interface_CanBeImplemented()
    {
        TestProjector projector = new();

        Assert.IsAssignableFrom<IProjector<string>>(projector);
    }

    [Fact]
    public void Interface_ExtendsIReactor()
    {
        TestProjector projector = new();

        Assert.IsAssignableFrom<IReactor<string>>(projector);
    }

    [Fact]
    public void Interface_InputIsContravariant()
    {
        ObjectProjector projector = new();

        // Compile-time verification: IProjector<in TInput> allows assigning
        // a projector of a base input type to a more derived input type.
        IProjector<string> stringProjector = projector;

        Assert.Same(projector, stringProjector);
    }

    private class TestProjector : IProjector<string>
    {
        public Task ExecuteAsync(string input)
        {
            return Task.CompletedTask;
        }
    }

    private class ObjectProjector : IProjector<object>
    {
        public Task ExecuteAsync(object input)
        {
            return Task.CompletedTask;
        }
    }
}
