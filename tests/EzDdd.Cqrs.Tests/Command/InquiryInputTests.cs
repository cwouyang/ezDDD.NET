using EzDdd.Cqrs.Command;

namespace EzDdd.Cqrs.Tests.Command;

public class InquiryInputTests
{
    [Fact]
    public void Interface_CanBeImplemented()
    {
        TestInquiryInput input = new("test-value");

        Assert.NotNull(input);
        Assert.IsAssignableFrom<IInquiryInput>(input);
    }

    [Fact]
    public void Interface_ProvidesTypeConstraint()
    {
        TestInquiryInput input = new("test-value");

        bool result = AcceptInquiryInput(input);

        Assert.True(result);
        return;

        static bool AcceptInquiryInput(IInquiryInput? input)
        {
            return input != null;
        }
    }

    [Fact]
    public void ConcreteImplementation_CanStoreData()
    {
        const string expectedValue = "test-data";

        TestInquiryInput input = new(expectedValue);

        Assert.Equal(expectedValue, input.Data);
    }

    [Fact]
    public void MultipleImplementations_AreDistinct()
    {
        TestInquiryInput input1 = new("value1");
        AnotherTestInquiryInput input2 = new(42);

        Assert.IsAssignableFrom<IInquiryInput>(input1);
        Assert.IsAssignableFrom<IInquiryInput>(input2);
        Assert.NotEqual(input1.GetType(), input2.GetType());
    }

    private sealed record TestInquiryInput(string Data) : IInquiryInput;

    private sealed record AnotherTestInquiryInput(int Value) : IInquiryInput;
}
