using EzDdd.Cqrs.Command;
using EzDdd.UseCase.Port.In;

namespace EzDdd.Cqrs.Tests.Command;

public class CommandTests
{
    public CommandTests()
    {
        StatefulTestCommand.Reset();
    }

    [Fact]
    public void Interface_ExtendsIUseCase()
    {
        TestCommand command = new();

        Assert.IsAssignableFrom<IUseCase<TestInput, TestOutput>>(command);
        Assert.IsAssignableFrom<ICommand<TestInput, TestOutput>>(command);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCalled_ShouldReturnCqrsOutput()
    {
        TestCommand command = new();
        TestInput input = new("test-data");

        TestOutput output = await command.ExecuteAsync(input);

        Assert.NotNull(output);
        Assert.IsType<TestOutput>(output);
        Assert.Equal(ExitCode.Success, output.ExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSupportFluentOutputBuilder()
    {
        TestCommand command = new();
        TestInput input = new("test-data");

        TestOutput output = await command.ExecuteAsync(input);

        Assert.Equal("test-id", output.Id);
        Assert.Equal("Command executed successfully", output.Message);
        Assert.Equal("test-data", output.ProcessedData);
        Assert.Equal(ExitCode.Success, output.ExitCode);
    }

    [Fact]
    public async Task Command_CanModifySystemState()
    {
        StatefulTestCommand command = new();
        TestInput input = new("initial");

        TestOutput output = await command.ExecuteAsync(input);

        Assert.Equal(ExitCode.Success, output.ExitCode);
        Assert.Equal(1, StatefulTestCommand.ExecutionCount);
    }

    private record TestInput(string Data) : IInput;

    private class TestOutput : CqrsOutput<TestOutput>
    {
        public string ProcessedData { get; set; } = string.Empty;

        public TestOutput SetProcessedData(string data)
        {
            ProcessedData = data;
            return this;
        }
    }

    private class TestCommand : ICommand<TestInput, TestOutput>
    {
        public Task<TestOutput> ExecuteAsync(TestInput input)
        {
            TestOutput output = TestOutput
                .Create()
                .SetId("test-id")
                .SetMessage("Command executed successfully")
                .SetProcessedData(input.Data)
                .Succeed();

            return Task.FromResult(output);
        }
    }

    private class StatefulTestCommand : ICommand<TestInput, TestOutput>
    {
        public static int ExecutionCount { get; private set; }

        public Task<TestOutput> ExecuteAsync(TestInput input)
        {
            ExecutionCount++;

            TestOutput output = TestOutput.Create().SetProcessedData($"Executed {ExecutionCount} times").Succeed();

            return Task.FromResult(output);
        }

        public static void Reset()
        {
            ExecutionCount = 0;
        }
    }
}
