using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.In;

namespace EzDdd.UseCase.Tests.Port.In;

public class UseCaseTests
{
    [Fact]
    public async Task UseCase_ExecuteAsync_ReturnsOutput()
    {
        TestUseCase useCase = new();
        TestInput input = new() { Value = "test" };

        TestOutput output = await useCase.ExecuteAsync(input);

        Assert.NotNull(output);
        Assert.Equal("Processed: test", output.Result);
    }

    [Fact]
    public async Task UseCase_ExecuteAsync_CanThrowUseCaseFailureException()
    {
        FailingUseCase useCase = new();
        TestInput input = new() { Value = "fail" };

        await Assert.ThrowsAsync<UseCaseFailureException>(async () => await useCase.ExecuteAsync(input));
    }

    [Fact]
    public async Task UseCase_WithGenericConstraints_WorksCorrectly()
    {
        GenericUseCase useCase = new();
        TestInput input = new() { Value = "generic" };

        TestOutput output = await useCase.ExecuteAsync(input);

        Assert.NotNull(output);
        Assert.IsAssignableFrom<IOutput>(output);
        Assert.IsAssignableFrom<IInput>(input);
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

    private sealed class TestUseCase : IUseCase<TestInput, TestOutput>
    {
        public Task<TestOutput> ExecuteAsync(TestInput input)
        {
            TestOutput output = new() { Result = $"Processed: {input.Value}" };
            return Task.FromResult(output);
        }
    }

    private sealed class FailingUseCase : IUseCase<TestInput, TestOutput>
    {
        public Task<TestOutput> ExecuteAsync(TestInput input)
        {
            throw new UseCaseFailureException("Use case failed");
        }
    }

    private sealed class GenericUseCase : IUseCase<TestInput, TestOutput>
    {
        public Task<TestOutput> ExecuteAsync(TestInput input)
        {
            return Task.FromResult(new TestOutput());
        }
    }
}
