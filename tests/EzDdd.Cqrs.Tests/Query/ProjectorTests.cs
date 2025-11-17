using EzDdd.Cqrs.Query;
using EzDdd.UseCase.Port.In;

namespace EzDdd.Cqrs.Tests.Query;

public class ProjectorTests
{
    [Fact]
    public void Interface_CanBeImplemented()
    {
        TestProjector projector = new();

        Assert.IsAssignableFrom<IProjector>(projector);
    }

    [Fact]
    public void Projector_CanAlsoImplementReactor()
    {
        ReactorProjector projector = new();

        Assert.IsAssignableFrom<IProjector>(projector);
        Assert.IsAssignableFrom<IReactor<object>>(projector);
    }

    [Fact]
    public void Projector_MarkerPattern_ProvidesSemanticClarity()
    {
        TestProjector projector = new();
        // ReSharper disable once ConvertTypeCheckToNullCheck
        bool isProjector = projector is IProjector;

        Assert.True(isProjector);
    }

    private class TestProjector : IProjector
    {
    }

    private class ReactorProjector : IProjector, IReactor<object>
    {
        public Task ExecuteAsync(object input)
        {
            return Task.CompletedTask;
        }
    }
}