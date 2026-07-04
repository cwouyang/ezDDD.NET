namespace EzDdd.UseCase.Tests.Exceptions;

using EzDdd.UseCase.Exceptions;

public class RepositorySaveExceptionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NoArgs_CreatesException()
    {
        var exception = new RepositorySaveException();

        Assert.NotNull(exception);
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void Constructor_WithMessage_StoresMessage()
    {
        const string expectedMessage = "Save operation failed";

        var exception = new RepositorySaveException(expectedMessage);

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void Constructor_WithInnerException_WrapsException()
    {
        var innerException = new InvalidOperationException("Database error");
        const string message = "Repository save failed";

        var exception = new RepositorySaveException(message, innerException);

        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void Constructor_WithInnerExceptionOnly_UsesInnerMessage()
    {
        var innerException = new InvalidOperationException("Database connection lost");

        var exception = new RepositorySaveException(innerException);

        Assert.Equal(innerException.Message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    #endregion

    #region Optimistic Locking Tests

    [Fact]
    public void OptimisticLockingFailure_Constant_HasCorrectValue()
    {
        Assert.Equal("Optimistic locking failure", RepositorySaveException.OptimisticLockingFailure);
    }

    [Fact]
    public void ThrowWithOptimisticLockingFailure_ContainsCorrectMessage()
    {
        var exception = new RepositorySaveException(RepositorySaveException.OptimisticLockingFailure);

        Assert.Equal("Optimistic locking failure", exception.Message);
    }

    #endregion

    #region Type Hierarchy Tests

    [Fact]
    public void ExceptionCanBeCaughtAsBaseException()
    {
        var exception = new RepositorySaveException("Test exception");

        Assert.IsAssignableFrom<Exception>(exception);
        Assert.IsType<RepositorySaveException>(exception);
    }

    #endregion
}
