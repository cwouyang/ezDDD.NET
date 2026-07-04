using EzDdd.Cqrs.Command;
using EzDdd.UseCase.Port.In;

namespace EzDdd.Cqrs.Tests.Command;

public class InquiryTests
{
    [Fact]
    public async Task QueryAsync_WhenCalled_ShouldReturnResult()
    {
        TestInquiry inquiry = new();
        TestInquiryInput input = new("test-value");

        bool result = await inquiry.QueryAsync(input);

        Assert.True(result);
    }

    [Fact]
    public async Task Inquiry_CanReturnBool()
    {
        BooleanInquiry inquiry = new();
        TestInquiryInput input = new("valid");

        bool result = await inquiry.QueryAsync(input);

        Assert.True(result);
    }

    [Fact]
    public async Task Inquiry_CanReturnDto()
    {
        DtoInquiry inquiry = new();
        TestInquiryInput input = new("test-id");

        TestInquiryDto result = await inquiry.QueryAsync(input);

        Assert.NotNull(result);
        Assert.Equal("test-id", result.Id);
        Assert.Equal("Found", result.Status);
    }

    [Fact]
    public async Task Inquiry_UsedWithinCommand_ShouldValidatePreconditions()
    {
        BooleanInquiry inquiry = new();
        CommandWithInquiry command = new(inquiry);
        TestInquiryInput input = new("valid");

        TestCommandOutput output = await command.ExecuteAsync(input);

        Assert.Equal(ExitCode.Success, output.ExitCode);
        Assert.Contains("Validation passed", output.Message);
    }

    [Fact]
    public async Task Inquiry_WhenValidationFails_CommandShouldFail()
    {
        BooleanInquiry inquiry = new();
        CommandWithInquiry command = new(inquiry);
        TestInquiryInput input = new("invalid");

        TestCommandOutput output = await command.ExecuteAsync(input);

        Assert.Equal(ExitCode.Failure, output.ExitCode);
        Assert.Contains("Validation failed", output.Message);
    }

    private record TestInquiryInput(string Value) : IInput, IInquiryInput;

    private record TestInquiryDto(string Id, string Status);

    private class TestInquiry : IInquiry<TestInquiryInput, bool>
    {
        public Task<bool> QueryAsync(TestInquiryInput input)
        {
            return Task.FromResult(true);
        }
    }

    private class BooleanInquiry : IInquiry<TestInquiryInput, bool>
    {
        public Task<bool> QueryAsync(TestInquiryInput input)
        {
            bool isValid = input.Value == "valid";
            return Task.FromResult(isValid);
        }
    }

    private class DtoInquiry : IInquiry<TestInquiryInput, TestInquiryDto>
    {
        public Task<TestInquiryDto> QueryAsync(TestInquiryInput input)
        {
            TestInquiryDto dto = new(input.Value, "Found");
            return Task.FromResult(dto);
        }
    }

    private class CommandWithInquiry(IInquiry<TestInquiryInput, bool> inquiry)
        : ICommand<TestInquiryInput, TestCommandOutput>
    {
        public async Task<TestCommandOutput> ExecuteAsync(TestInquiryInput input)
        {
            bool isValid = await inquiry.QueryAsync(input);

            if (!isValid)
            {
                return TestCommandOutput.Create().SetMessage("Validation failed").Fail();
            }

            return TestCommandOutput.Create().SetMessage("Validation passed - Command executed").Succeed();
        }
    }

    private class TestCommandOutput : CqrsOutput<TestCommandOutput> { }
}
