using EzDdd.UseCase.Exceptions;

namespace EzDdd.UseCase.Tests.Exceptions;

public class UseCaseFailureExceptionTests
{
    [Fact]
    public void UseCaseFailureException_DefaultConstructor_CreatesException()
    {
        UseCaseFailureException exception = new();

        Assert.NotNull(exception);
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void UseCaseFailureException_WithMessage_StoresMessage()
    {
        const string expectedMessage = "Use case failed";

        UseCaseFailureException exception = new(expectedMessage);

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void UseCaseFailureException_WithMessageAndCause_StoresBoth()
    {
        const string expectedMessage = "Use case failed";
        InvalidOperationException innerException = new("Inner error");

        UseCaseFailureException exception = new(expectedMessage, innerException);

        Assert.Equal(expectedMessage, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void UseCaseFailureException_CanBeThrown()
    {
        void ThrowException()
        {
            throw new UseCaseFailureException("Test exception");
        }

        UseCaseFailureException exception = Assert.Throws<UseCaseFailureException>(ThrowException);

        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public async Task UseCaseFailureException_CanBeThrownAsync()
    {
        UseCaseFailureException exception = await Assert.ThrowsAsync<UseCaseFailureException>
        (async () =>
            {
                await Task.Yield();
                throw new UseCaseFailureException("Async test exception");
            }
        );

        Assert.Equal("Async test exception", exception.Message);
    }
}