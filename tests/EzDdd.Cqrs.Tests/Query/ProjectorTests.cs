using EzDdd.Cqrs.Query;

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
    public void Projector_MarkerPattern_ProvidesSemanticClarity()
    {
        TestProjector projector = new();
        // ReSharper disable once ConvertTypeCheckToNullCheck
        bool isProjector = projector is IProjector;

        Assert.True(isProjector);
    }

    private class TestProjector : IProjector;
}