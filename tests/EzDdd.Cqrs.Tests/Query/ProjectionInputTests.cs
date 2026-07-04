using EzDdd.Cqrs.Query;

namespace EzDdd.Cqrs.Tests.Query;

public class ProjectionInputTests
{
    [Fact]
    public void Interface_CanBeImplemented()
    {
        TestProjectionInput input = new("test-id");

        Assert.NotNull(input);
        Assert.IsAssignableFrom<IProjectionInput>(input);
    }

    [Fact]
    public void Interface_ProvidesTypeConstraint()
    {
        TestProjectionInput input = new("test-id");

        bool result = AcceptProjectionInput(input);

        Assert.True(result);
        return;

        static bool AcceptProjectionInput(IProjectionInput? input)
        {
            return input != null;
        }
    }

    [Fact]
    public void ConcreteImplementation_CanStoreData()
    {
        const string expectedId = "proj-001";
        const bool expectedFlag = true;

        ComplexProjectionInput input = new(expectedId, expectedFlag);

        Assert.Equal(expectedId, input.Id);
        Assert.Equal(expectedFlag, input.IncludeDetails);
    }

    [Fact]
    public void MultipleImplementations_AreDistinct()
    {
        TestProjectionInput input1 = new("id1");
        ComplexProjectionInput input2 = new("id2", false);

        Assert.IsAssignableFrom<IProjectionInput>(input1);
        Assert.IsAssignableFrom<IProjectionInput>(input2);
        Assert.NotEqual(input1.GetType(), input2.GetType());
    }

    private record TestProjectionInput(string Id) : IProjectionInput;

    private record ComplexProjectionInput(string Id, bool IncludeDetails) : IProjectionInput;
}
