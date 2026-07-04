using EzDdd.UseCase.Exceptions;

namespace EzDdd.UseCase.Tests.Exceptions;

public class PostEventFailureExceptionTests
{
    [Fact]
    public void Constructor_DefaultMessage()
    {
        PostEventFailureException exception = new();

        Assert.Equal("Failed to post message to message producer.", exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_CustomMessage()
    {
        const string customMessage = "Kafka broker unavailable";

        PostEventFailureException exception = new(customMessage);

        Assert.Equal(customMessage, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_WithInnerException()
    {
        const string message = "Failed to post to Kafka";
        IOException innerException = new("Network connection lost");

        PostEventFailureException exception = new(message, innerException);

        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
        Assert.NotNull(exception.InnerException);
        Assert.Equal("Network connection lost", exception.InnerException.Message);
    }
}
