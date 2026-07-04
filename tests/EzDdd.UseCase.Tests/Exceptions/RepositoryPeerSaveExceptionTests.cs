namespace EzDdd.UseCase.Tests.Exceptions;

using EzDdd.UseCase.Exceptions;

public class RepositoryPeerSaveExceptionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NoArgs_CreatesException()
    {
        var exception = new RepositoryPeerSaveException();

        Assert.NotNull(exception);
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void Constructor_WithMessage_StoresMessage()
    {
        const string expectedMessage = "Database save failed";

        var exception = new RepositoryPeerSaveException(expectedMessage);

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void Constructor_WithInnerException_WrapsException()
    {
        var innerException = new InvalidOperationException("SQL error");
        const string message = "Peer save failed";

        var exception = new RepositoryPeerSaveException(message, innerException);

        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void Constructor_WithInnerExceptionOnly_UsesInnerMessage()
    {
        var innerException = new InvalidOperationException("Connection timeout");

        var exception = new RepositoryPeerSaveException(innerException);

        Assert.Equal(innerException.Message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    #endregion

    #region Type Hierarchy Tests

    [Fact]
    public void ExceptionCanBeCaughtAsBaseException()
    {
        var exception = new RepositoryPeerSaveException("Test exception");

        Assert.IsAssignableFrom<Exception>(exception);
        Assert.IsType<RepositoryPeerSaveException>(exception);
    }

    #endregion

    #region Exception Translation Tests

    [Fact]
    public void ExceptionTranslation_PeerToRepository_WorksCorrectly()
    {
        var peerException = new RepositoryPeerSaveException("Database error");

        var repositoryException = new RepositorySaveException(
            RepositorySaveException.OptimisticLockingFailure,
            peerException
        );

        Assert.Equal(RepositorySaveException.OptimisticLockingFailure, repositoryException.Message);
        Assert.Same(peerException, repositoryException.InnerException);
        Assert.IsType<RepositoryPeerSaveException>(repositoryException.InnerException);
    }

    #endregion
}
