using EzDdd.Cqrs.Query;
using EzDdd.UseCase.Port.In;

namespace EzDdd.Cqrs.Tests.Query;

public class ProjectionTests
{
    [Fact]
    public async Task QueryAsync_WhenCalled_ShouldReturnResult()
    {
        TestProjection projection = new();
        TestProjectionInput input = new("test-id");

        TestProjectionDto result = await projection.QueryAsync(input);

        Assert.NotNull(result);
        Assert.Equal("test-id", result.Id);
    }

    [Fact]
    public async Task Projection_CanReturnViewModel()
    {
        ViewModelProjection projection = new();
        TestProjectionInput input = new("customer-123");

        CustomerViewModel result = await projection.QueryAsync(input);

        Assert.NotNull(result);
        Assert.Equal("customer-123", result.CustomerId);
        Assert.Equal("John Doe", result.CustomerName);
    }

    [Fact]
    public async Task Projection_CanReturnDto()
    {
        DtoProjection projection = new();
        TestProjectionInput input = new("account-456");

        AccountDto result = await projection.QueryAsync(input);

        Assert.NotNull(result);
        Assert.Equal("account-456", result.AccountId);
        Assert.Equal(1000.00m, result.Balance);
    }

    [Fact]
    public async Task Projection_UsedWithinQuery_ShouldBuildComplexView()
    {
        ViewModelProjection projection = new();
        QueryWithProjection query = new(projection);
        TestInput input = new("customer-123");

        TestQueryOutput output = await query.ExecuteAsync(input);

        Assert.Equal(ExitCode.Success, output.ExitCode);
        Assert.Contains("customer-123", output.CustomerInfo);
        Assert.Contains("John Doe", output.CustomerInfo);
    }

    [Fact]
    public async Task Projection_InputMustImplementIProjectionInput()
    {
        TestProjection projection = new();
        TestProjectionInput input = new("test-id");

        Assert.IsAssignableFrom<IProjectionInput>(input);

        TestProjectionDto result = await projection.QueryAsync(input);

        Assert.NotNull(result);
    }

    private record TestProjectionInput(string Id) : IProjectionInput;

    private record TestInput(string CustomerId) : IInput;

    private record TestProjectionDto(string Id, string Name);

    private record CustomerViewModel(string CustomerId, string CustomerName);

    private record AccountDto(string AccountId, decimal Balance);

    private class TestProjection : IProjection<TestProjectionInput, TestProjectionDto>
    {
        public Task<TestProjectionDto> QueryAsync(TestProjectionInput input)
        {
            TestProjectionDto dto = new(input.Id, "Test Name");
            return Task.FromResult(dto);
        }
    }

    private class ViewModelProjection : IProjection<TestProjectionInput, CustomerViewModel>
    {
        public Task<CustomerViewModel> QueryAsync(TestProjectionInput input)
        {
            CustomerViewModel viewModel = new(input.Id, "John Doe");
            return Task.FromResult(viewModel);
        }
    }

    private class DtoProjection : IProjection<TestProjectionInput, AccountDto>
    {
        public Task<AccountDto> QueryAsync(TestProjectionInput input)
        {
            AccountDto dto = new(input.Id, 1000.00m);
            return Task.FromResult(dto);
        }
    }

    private class QueryWithProjection(IProjection<TestProjectionInput, CustomerViewModel> projection)
        : IQuery<TestInput, TestQueryOutput>
    {
        public async Task<TestQueryOutput> ExecuteAsync(TestInput input)
        {
            TestProjectionInput projectionInput = new(input.CustomerId);
            CustomerViewModel viewModel = await projection.QueryAsync(projectionInput);

            return TestQueryOutput
                .Create()
                .SetCustomerInfo($"{viewModel.CustomerId}: {viewModel.CustomerName}")
                .Succeed();
        }
    }

    private class TestQueryOutput : CqrsOutput<TestQueryOutput>
    {
        public string CustomerInfo { get; set; } = string.Empty;

        public TestQueryOutput SetCustomerInfo(string info)
        {
            CustomerInfo = info;
            return this;
        }
    }
}
