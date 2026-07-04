using EzDdd.UseCase.Port.In;
using Xunit;

namespace EzDdd.UseCase.Tests.Port.In;

public class InputOutputTests
{
    #region IInput Tests

    [Fact]
    public void Input_CanBeImplemented_AsMarkerInterface()
    {
        var input = new TestInput();

        Assert.IsAssignableFrom<IInput>(input);
    }

    [Fact]
    public void Input_OfNull_ReturnsNullInputInstance()
    {
        var nullInput = IInput.OfNull();

        Assert.NotNull(nullInput);
        Assert.IsAssignableFrom<IInput>(nullInput);
        Assert.IsType<IInput.NullInput>(nullInput);
    }

    [Fact]
    public void NullInput_CanBeCreatedDirectly()
    {
        var nullInput = new IInput.NullInput();

        Assert.NotNull(nullInput);
        Assert.IsAssignableFrom<IInput>(nullInput);
    }

    #endregion

    #region ExitCode Tests

    [Fact]
    public void ExitCode_Success_HasCodeZero()
    {
        const ExitCode exitCode = ExitCode.Success;

        Assert.Equal(0, exitCode.Code());
        Assert.Equal("Success", exitCode.ToString());
    }

    [Fact]
    public void ExitCode_Failure_HasCodeOne()
    {
        const ExitCode exitCode = ExitCode.Failure;

        Assert.Equal(1, exitCode.Code());
        Assert.Equal("Failure", exitCode.ToString());
    }

    #endregion

    #region IOutput Tests

    [Fact]
    public void Output_SetMessage_ReturnsOutputWithMessage()
    {
        var output = new TestOutput();

        var result = output.SetMessage("Test message");

        Assert.NotNull(result);
        Assert.Equal("Test message", result.Message);
        Assert.Same(output, result);
    }

    [Fact]
    public void Output_SetExitCode_ReturnsOutputWithExitCode()
    {
        var output = new TestOutput();

        var result = output.SetExitCode(ExitCode.Success);

        Assert.NotNull(result);
        Assert.Equal(ExitCode.Success, result.ExitCode);
        Assert.Same(output, result);
    }

    [Fact]
    public void Output_SetId_ReturnsOutputWithId()
    {
        var output = new TestOutput();

        var result = output.SetId("test-id-123");

        Assert.NotNull(result);
        Assert.Equal("test-id-123", result.Id);
        Assert.Same(output, result);
    }

    [Fact]
    public void Output_Succeed_SetsExitCodeToSuccess()
    {
        var output = new TestOutput();

        var result = output.Succeed();

        Assert.Equal(ExitCode.Success, result.ExitCode);
        Assert.Same(output, result);
    }

    [Fact]
    public void Output_Fail_SetsExitCodeToFailure()
    {
        var output = new TestOutput();

        var result = output.Fail();

        Assert.Equal(ExitCode.Failure, result.ExitCode);
        Assert.Same(output, result);
    }

    [Fact]
    public void Output_FluentAPI_CanChainMultipleCalls()
    {
        var output = new TestOutput();

        var result = output.SetId("chain-test").SetMessage("Chained message").Succeed();

        Assert.Equal("chain-test", result.Id);
        Assert.Equal("Chained message", result.Message);
        Assert.Equal(ExitCode.Success, result.ExitCode);
        Assert.Same(output, result);
    }

    #endregion

    #region IVersionedInput Tests

    [Fact]
    public void VersionedInput_CanGetAndSetVersion()
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var input = new TestVersionedInput();

        input.Version = 42;

        Assert.Equal(42, input.Version);
    }

    [Fact]
    public void VersionedInput_IsAlsoAnInput()
    {
        var input = new TestVersionedInput();

        Assert.IsAssignableFrom<IInput>(input);
        Assert.IsAssignableFrom<IVersionedInput>(input);
    }

    #endregion

    #region Test Helper Classes

    private sealed class TestInput : IInput { }

    private sealed class TestOutput : IOutput
    {
        public string Message { get; private set; } = string.Empty;
        public ExitCode ExitCode { get; private set; }
        public string Id { get; private set; } = string.Empty;

        public IOutput SetMessage(string message)
        {
            Message = message;
            return this;
        }

        public IOutput SetExitCode(ExitCode exitCode)
        {
            ExitCode = exitCode;
            return this;
        }

        public IOutput SetId(string id)
        {
            Id = id;
            return this;
        }

        public IOutput Fail()
        {
            ExitCode = ExitCode.Failure;
            return this;
        }

        public IOutput Succeed()
        {
            ExitCode = ExitCode.Success;
            return this;
        }
    }

    private sealed class TestVersionedInput : IVersionedInput
    {
        public long Version { get; set; }
    }

    #endregion
}
