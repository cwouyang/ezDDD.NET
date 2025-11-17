using EzDdd.UseCase.Port.In;

namespace EzDdd.Cqrs.Tests;

public class CqrsOutputTests
{
#region Factory and Properties Tests

    [Fact]
    public void Create_WhenCalled_ShouldReturnNewInstance()
    {
        TestOutput output = TestOutput.Create();

        Assert.NotNull(output);
        Assert.IsType<TestOutput>(output);
    }

    [Fact]
    public void Properties_DefaultValues_ShouldBeInitialized()
    {
        TestOutput output = TestOutput.Create();

        Assert.Equal(string.Empty, output.Id);
        Assert.Equal(string.Empty, output.Message);
        Assert.Equal(ExitCode.Success, output.ExitCode);
    }

#endregion

#region Fluent API Tests

    [Fact]
    public void SetId_WhenCalled_ShouldSetIdAndReturnSelf()
    {
        TestOutput output = TestOutput.Create();
        const string expectedId = "test-id-123";

        TestOutput result = output.SetId(expectedId);

        Assert.Equal(expectedId, output.Id);
        Assert.Same(output, result);
        Assert.IsType<TestOutput>(result);
    }

    [Fact]
    public void SetMessage_WhenCalled_ShouldSetMessageAndReturnSelf()
    {
        TestOutput output = TestOutput.Create();
        const string expectedMessage = "Test message";

        TestOutput result = output.SetMessage(expectedMessage);

        Assert.Equal(expectedMessage, output.Message);
        Assert.Same(output, result);
        Assert.IsType<TestOutput>(result);
    }

    [Fact]
    public void SetExitCode_WhenCalled_ShouldSetExitCodeAndReturnSelf()
    {
        TestOutput output = TestOutput.Create();
        const ExitCode expectedExitCode = ExitCode.Failure;

        TestOutput result = output.SetExitCode(expectedExitCode);

        Assert.Equal(expectedExitCode, output.ExitCode);
        Assert.Same(output, result);
        Assert.IsType<TestOutput>(result);
    }

    [Fact]
    public void Fail_WhenCalled_ShouldSetExitCodeToFailureAndReturnSelf()
    {
        TestOutput output = TestOutput.Create();

        TestOutput result = output.Fail();

        Assert.Equal(ExitCode.Failure, output.ExitCode);
        Assert.Same(output, result);
        Assert.IsType<TestOutput>(result);
    }

    [Fact]
    public void Succeed_WhenCalled_ShouldSetExitCodeToSuccessAndReturnSelf()
    {
        TestOutput output = TestOutput.Create()
                                      .Fail();

        TestOutput result = output.Succeed();

        Assert.Equal(ExitCode.Success, output.ExitCode);
        Assert.Same(output, result);
        Assert.IsType<TestOutput>(result);
    }

    [Fact]
    public void FluentAPI_MethodChaining_ShouldWork()
    {
        const string expectedId = "id-001";
        const string expectedMessage = "Operation completed";

        TestOutput output = TestOutput.Create()
                                      .SetId(expectedId)
                                      .SetMessage(expectedMessage)
                                      .Succeed();

        Assert.Equal(expectedId, output.Id);
        Assert.Equal(expectedMessage, output.Message);
        Assert.Equal(ExitCode.Success, output.ExitCode);
        Assert.IsType<TestOutput>(output);
    }

#endregion

#region Subclass Scenarios Tests

    [Fact]
    public void Subclass_WithCustomProperties_ShouldSupportFluentAPI()
    {
        const string expectedData = "custom-data";
        const string expectedId = "sub-001";

        TestOutputWithCustomProperty output = TestOutputWithCustomProperty.Create()
                                                                          .SetId(expectedId)
                                                                          .SetCustomData(expectedData)
                                                                          .Succeed();

        Assert.Equal(expectedId, output.Id);
        Assert.Equal(expectedData, output.CustomData);
        Assert.Equal(ExitCode.Success, output.ExitCode);
        Assert.IsType<TestOutputWithCustomProperty>(output);
    }

    [Fact]
    public void Subclass_MethodChaining_AcrossBaseAndDerived_ShouldPreserveType()
    {
        const string expectedId = "id-002";
        const string expectedMessage = "Test message";
        const string expectedData = "custom-data";
        const int expectedValue = 42;

        TestOutputWithCustomProperty output = TestOutputWithCustomProperty.Create()
                                                                          .SetId(expectedId)
                                                                          .SetCustomData(expectedData)
                                                                          .SetMessage(expectedMessage)
                                                                          .SetCustomValue(expectedValue)
                                                                          .Succeed();

        Assert.IsType<TestOutputWithCustomProperty>(output);
        Assert.Equal(expectedId, output.Id);
        Assert.Equal(expectedMessage, output.Message);
        Assert.Equal(expectedData, output.CustomData);
        Assert.Equal(expectedValue, output.CustomValue);
        Assert.Equal(ExitCode.Success, output.ExitCode);
    }

#endregion

#region Explicit IOutput Implementation Tests

    [Fact]
    public void ExplicitIOutput_SetMessage_ShouldWorkThroughInterface()
    {
        IOutput output = TestOutput.Create();
        const string expectedMessage = "Interface message";

        IOutput result = output.SetMessage(expectedMessage);

        Assert.Equal(expectedMessage, output.Message);
        Assert.Same(output, result);
    }

    [Fact]
    public void ExplicitIOutput_SetExitCode_ShouldWorkThroughInterface()
    {
        IOutput output = TestOutput.Create();
        const ExitCode expectedExitCode = ExitCode.Failure;

        IOutput result = output.SetExitCode(expectedExitCode);

        Assert.Equal(expectedExitCode, output.ExitCode);
        Assert.Same(output, result);
    }

    [Fact]
    public void ExplicitIOutput_Fail_ShouldWorkThroughInterface()
    {
        IOutput output = TestOutput.Create();

        IOutput result = output.Fail();

        Assert.Equal(ExitCode.Failure, output.ExitCode);
        Assert.Same(output, result);
    }

    [Fact]
    public void ExplicitIOutput_Succeed_ShouldWorkThroughInterface()
    {
        IOutput output = TestOutput.Create();

        IOutput result = output.Succeed();

        Assert.Equal(ExitCode.Success, output.ExitCode);
        Assert.Same(output, result);
    }

    [Fact]
    public void ExplicitIOutput_SetId_ShouldWorkThroughInterface()
    {
        IOutput output = TestOutput.Create();
        const string expectedId = "interface-id";

        IOutput result = output.SetId(expectedId);

        Assert.Equal(expectedId, output.Id);
        Assert.Same(output, result);
    }

#endregion

#region Test Helper Classes

    private class TestOutput : CqrsOutput<TestOutput>
    {
    }

    private class TestOutputWithCustomProperty : CqrsOutput<TestOutputWithCustomProperty>
    {
        public string CustomData { get; set; } = string.Empty;
        public int CustomValue { get; set; }

        public TestOutputWithCustomProperty SetCustomData(string data)
        {
            CustomData = data;
            return this;
        }

        public TestOutputWithCustomProperty SetCustomValue(int value)
        {
            CustomValue = value;
            return this;
        }
    }

#endregion
}