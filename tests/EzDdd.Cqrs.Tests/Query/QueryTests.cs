using EzDdd.Cqrs.Query;
using EzDdd.UseCase.Port.In;

namespace EzDdd.Cqrs.Tests.Query;

public class QueryTests
{
    [Fact]
    public void Interface_ExtendsIUseCase()
    {
        TestQuery query = new();

        Assert.IsAssignableFrom<IUseCase<TestInput, TestOutput>>(query);
        Assert.IsAssignableFrom<IQuery<TestInput, TestOutput>>(query);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCalled_ShouldReturnCqrsOutput()
    {
        TestQuery query = new();
        TestInput input = new("test-id");

        TestOutput output = await query.ExecuteAsync(input);

        Assert.NotNull(output);
        Assert.IsType<TestOutput>(output);
        Assert.Equal(ExitCode.Success, output.ExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSupportFluentOutputBuilder()
    {
        TestQuery query = new();
        TestInput input = new("test-id");

        TestOutput output = await query.ExecuteAsync(input);

        Assert.Equal("test-id", output.Id);
        Assert.Equal("Query executed successfully", output.Message);
        Assert.Equal("Retrieved data for test-id", output.RetrievedData);
        Assert.Equal(ExitCode.Success, output.ExitCode);
    }

    [Fact]
    public async Task Query_ShouldNotModifySystemState()
    {
        TestQuery query = new();
        TestInput input = new("test-id");
        TestOutput firstResult = await query.ExecuteAsync(input);

        TestOutput secondResult = await query.ExecuteAsync(input);

        Assert.Equal(firstResult.RetrievedData, secondResult.RetrievedData);
    }

    private record TestInput(string Id) : IInput;

    private class TestOutput : CqrsOutput<TestOutput>
    {
        public string RetrievedData { get; set; } = string.Empty;

        public TestOutput SetRetrievedData(string data)
        {
            RetrievedData = data;
            return this;
        }
    }

    private class TestQuery : IQuery<TestInput, TestOutput>
    {
        public Task<TestOutput> ExecuteAsync(TestInput input)
        {
            TestOutput output = TestOutput
                .Create()
                .SetId(input.Id)
                .SetMessage("Query executed successfully")
                .SetRetrievedData($"Retrieved data for {input.Id}")
                .Succeed();

            return Task.FromResult(output);
        }
    }
}
