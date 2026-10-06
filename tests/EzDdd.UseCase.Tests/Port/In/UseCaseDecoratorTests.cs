using EzDdd.UseCase.Port.In;

namespace EzDdd.UseCase.Tests.Port.In;

public class UseCaseDecoratorTests
{
    [Fact]
    public async Task UseCaseDecorator_ExecuteAsync_CanDelegateToDecoratedUseCase()
    {
        DelegatingDecorator decorator = new(new EchoUseCase());
        TestInput input = new() { Value = "order-42" };

        TestOutput output = await decorator.ExecuteAsync(input);

        Assert.Equal("Echo: order-42", output.Result);
    }

    [Fact]
    public void UseCaseDecorator_WithNullUseCase_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DelegatingDecorator(null!));
    }

    [Fact]
    public async Task UseCaseDecorator_ExecuteAsync_CanReturnOwnOutputWithoutDelegating()
    {
        EchoUseCase decorated = new();
        RejectingDecorator decorator = new(decorated);
        TestInput input = new() { Value = "order-42" };

        TestOutput output = await decorator.ExecuteAsync(input);

        Assert.Equal(ExitCode.Reject, output.ExitCode);
        Assert.Equal(0, decorated.CallCount);
    }

    [Fact]
    public void UseCaseDecorator_Instance_IsAssignableToDecoratedInPort()
    {
        DelegatingDecorator decorator = new(new EchoUseCase());

        Assert.IsAssignableFrom<IUseCase<TestInput, TestOutput>>(decorator);
    }

    // ========================================
    // Test Helper Classes
    // ========================================

    private sealed class TestInput : IInput
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class TestOutput : IOutput
    {
        public string Result { get; set; } = string.Empty;
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

        public IOutput Fail() => SetExitCode(ExitCode.Failure);

        public IOutput Ignore() => SetExitCode(ExitCode.Ignore);

        public IOutput Reject() => SetExitCode(ExitCode.Reject);

        public IOutput Succeed() => SetExitCode(ExitCode.Success);
    }

    private sealed class EchoUseCase : IUseCase<TestInput, TestOutput>
    {
        public int CallCount { get; private set; }

        public Task<TestOutput> ExecuteAsync(TestInput input)
        {
            CallCount++;
            return Task.FromResult(new TestOutput { Result = $"Echo: {input.Value}" });
        }
    }

    private sealed class RejectingDecorator(IUseCase<TestInput, TestOutput> useCase)
        : UseCaseDecorator<TestInput, TestOutput>(useCase)
    {
        public override Task<TestOutput> ExecuteAsync(TestInput input)
        {
            TestOutput output = new();
            output.Reject();
            return Task.FromResult(output);
        }
    }

    private sealed class DelegatingDecorator(IUseCase<TestInput, TestOutput> useCase)
        : UseCaseDecorator<TestInput, TestOutput>(useCase)
    {
        public override Task<TestOutput> ExecuteAsync(TestInput input) => DecoratedUseCase.ExecuteAsync(input);
    }
}
