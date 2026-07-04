using EzDdd.Entity;
using EzDdd.UseCase.Port.Out;
using EzDdd.UseCase.Tests.Integration.TestDomain;

namespace EzDdd.UseCase.Tests.Integration.Services;

/// <summary>
/// Tests for TransferMoneyService (example of Service Layer Pattern).
/// </summary>
public sealed class TransferMoneyServiceTests
{
    #region Setup

    private static ITransferMoneyService CreateService(
        IRepository<BankAccount, AccountId, IInternalDomainEvent> repository
    )
    {
        return new TransferMoneyService(repository);
    }

    private static async Task<BankAccount> CreateAndSaveAccount(
        IRepository<BankAccount, AccountId, IInternalDomainEvent> repository,
        string owner,
        Money initialBalance
    )
    {
        var account = new BankAccount(new AccountId(Guid.NewGuid().ToString()), owner, initialBalance);

        await repository.SaveAsync(account);
        return account;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task TransferAsync_WithSufficientBalance_TransfersSuccessfully()
    {
        // Arrange
        var repository = new InMemoryRepository<BankAccount, AccountId>();
        var service = CreateService(repository);

        var fromAccount = await CreateAndSaveAccount(repository, "Alice", new Money(1000, "USD"));
        var toAccount = await CreateAndSaveAccount(repository, "Bob", new Money(500, "USD"));
        var transferAmount = new Money(300, "USD");

        // Act
        var result = await service.TransferAsync(fromAccount.Id, toAccount.Id, transferAmount);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TransferStatus.Success, result.Status);
        Assert.Equal(fromAccount.Id, result.FromAccountId);
        Assert.Equal(toAccount.Id, result.ToAccountId);
        Assert.Equal(transferAmount, result.Amount);

        // Verify balances
        var updatedFrom = await repository.FindByIdAsync(fromAccount.Id);
        var updatedTo = await repository.FindByIdAsync(toAccount.Id);

        Assert.NotNull(updatedFrom);
        Assert.NotNull(updatedTo);
        Assert.Equal(new Money(700, "USD"), updatedFrom.Balance);
        Assert.Equal(new Money(800, "USD"), updatedTo.Balance);
    }

    [Fact]
    public async Task TransferAsync_WithExactBalance_TransfersSuccessfully()
    {
        // Arrange
        var repository = new InMemoryRepository<BankAccount, AccountId>();
        var service = CreateService(repository);

        var fromAccount = await CreateAndSaveAccount(repository, "Alice", new Money(500, "USD"));
        var toAccount = await CreateAndSaveAccount(repository, "Bob", new Money(0, "USD"));
        var transferAmount = new Money(500, "USD");

        // Act
        var result = await service.TransferAsync(fromAccount.Id, toAccount.Id, transferAmount);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TransferStatus.Success, result.Status);

        var updatedFrom = await repository.FindByIdAsync(fromAccount.Id);
        var updatedTo = await repository.FindByIdAsync(toAccount.Id);

        Assert.NotNull(updatedFrom);
        Assert.NotNull(updatedTo);
        Assert.Equal(new Money(0, "USD"), updatedFrom.Balance);
        Assert.Equal(new Money(500, "USD"), updatedTo.Balance);
    }

    [Fact]
    public async Task TransferAsync_WithSmallAmount_TransfersSuccessfully()
    {
        // Arrange
        var repository = new InMemoryRepository<BankAccount, AccountId>();
        var service = CreateService(repository);

        var fromAccount = await CreateAndSaveAccount(repository, "Alice", new Money(100, "USD"));
        var toAccount = await CreateAndSaveAccount(repository, "Bob", new Money(50, "USD"));
        var transferAmount = new Money(0.01m, "USD");

        // Act
        var result = await service.TransferAsync(fromAccount.Id, toAccount.Id, transferAmount);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TransferStatus.Success, result.Status);

        var updatedFrom = await repository.FindByIdAsync(fromAccount.Id);
        var updatedTo = await repository.FindByIdAsync(toAccount.Id);

        Assert.NotNull(updatedFrom);
        Assert.NotNull(updatedTo);
        Assert.Equal(new Money(99.99m, "USD"), updatedFrom.Balance);
        Assert.Equal(new Money(50.01m, "USD"), updatedTo.Balance);
    }

    #endregion

    #region Validation Scenarios

    [Fact]
    public async Task TransferAsync_WithInsufficientBalance_ThrowsException()
    {
        // Arrange
        var repository = new InMemoryRepository<BankAccount, AccountId>();
        var service = CreateService(repository);

        var fromAccount = await CreateAndSaveAccount(repository, "Alice", new Money(100, "USD"));
        var toAccount = await CreateAndSaveAccount(repository, "Bob", new Money(500, "USD"));
        var transferAmount = new Money(200, "USD");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InsufficientBalanceException>(() =>
            service.TransferAsync(fromAccount.Id, toAccount.Id, transferAmount)
        );

        Assert.Equal(fromAccount.Id, exception.AccountId);
        Assert.Equal(new Money(100, "USD"), exception.CurrentBalance);
        Assert.Equal(new Money(200, "USD"), exception.RequestedAmount);
    }

    [Fact]
    public async Task TransferAsync_WithZeroAmount_ThrowsException()
    {
        // Arrange
        var repository = new InMemoryRepository<BankAccount, AccountId>();
        var service = CreateService(repository);

        var fromAccount = await CreateAndSaveAccount(repository, "Alice", new Money(1000, "USD"));
        var toAccount = await CreateAndSaveAccount(repository, "Bob", new Money(500, "USD"));
        var transferAmount = new Money(0, "USD");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidTransferAmountException>(() =>
            service.TransferAsync(fromAccount.Id, toAccount.Id, transferAmount)
        );
    }

    [Fact]
    public async Task TransferAsync_WithNegativeAmount_ThrowsException()
    {
        // Arrange
        var repository = new InMemoryRepository<BankAccount, AccountId>();
        var service = CreateService(repository);

        var fromAccount = await CreateAndSaveAccount(repository, "Alice", new Money(1000, "USD"));
        var toAccount = await CreateAndSaveAccount(repository, "Bob", new Money(500, "USD"));
        var transferAmount = new Money(-100, "USD");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidTransferAmountException>(() =>
            service.TransferAsync(fromAccount.Id, toAccount.Id, transferAmount)
        );
    }

    [Fact]
    public async Task TransferAsync_ExceedingTransferLimit_ThrowsException()
    {
        // Arrange
        var repository = new InMemoryRepository<BankAccount, AccountId>();
        var service = CreateService(repository);

        var fromAccount = await CreateAndSaveAccount(repository, "Alice", new Money(20000, "USD"));
        var toAccount = await CreateAndSaveAccount(repository, "Bob", new Money(500, "USD"));
        var transferAmount = new Money(15000, "USD"); // Exceeds $10,000 limit

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TransferLimitExceededException>(() =>
            service.TransferAsync(fromAccount.Id, toAccount.Id, transferAmount)
        );

        Assert.Equal(new Money(15000, "USD"), exception.RequestedAmount);
        Assert.Equal(new Money(10000, "USD"), exception.MaxLimit);
    }

    [Fact]
    public async Task TransferAsync_FromClosedAccount_ThrowsException()
    {
        // Arrange
        var repository = new InMemoryRepository<BankAccount, AccountId>();
        var service = CreateService(repository);

        var fromAccount = await CreateAndSaveAccount(repository, "Alice", new Money(1000, "USD"));
        var toAccount = await CreateAndSaveAccount(repository, "Bob", new Money(500, "USD"));

        fromAccount.Close("Account closed by user");
        await repository.SaveAsync(fromAccount);

        var transferAmount = new Money(100, "USD");

        // Act & Assert
        await Assert.ThrowsAsync<AccountClosedException>(() =>
            service.TransferAsync(fromAccount.Id, toAccount.Id, transferAmount)
        );
    }

    [Fact]
    public async Task TransferAsync_ToClosedAccount_ThrowsException()
    {
        // Arrange
        var repository = new InMemoryRepository<BankAccount, AccountId>();
        var service = CreateService(repository);

        var fromAccount = await CreateAndSaveAccount(repository, "Alice", new Money(1000, "USD"));
        var toAccount = await CreateAndSaveAccount(repository, "Bob", new Money(500, "USD"));

        toAccount.Close("Account closed by user");
        await repository.SaveAsync(toAccount);

        var transferAmount = new Money(100, "USD");

        // Act & Assert
        await Assert.ThrowsAsync<AccountClosedException>(() =>
            service.TransferAsync(fromAccount.Id, toAccount.Id, transferAmount)
        );
    }

    #endregion

    #region Account Not Found Scenarios

    [Fact]
    public async Task TransferAsync_FromAccountNotFound_ThrowsException()
    {
        // Arrange
        var repository = new InMemoryRepository<BankAccount, AccountId>();
        var service = CreateService(repository);

        var toAccount = await CreateAndSaveAccount(repository, "Bob", new Money(500, "USD"));
        var nonExistentAccountId = new AccountId(Guid.NewGuid().ToString());
        var transferAmount = new Money(100, "USD");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            service.TransferAsync(nonExistentAccountId, toAccount.Id, transferAmount)
        );

        Assert.Equal(nonExistentAccountId, exception.AccountId);
    }

    [Fact]
    public async Task TransferAsync_ToAccountNotFound_ThrowsException()
    {
        // Arrange
        var repository = new InMemoryRepository<BankAccount, AccountId>();
        var service = CreateService(repository);

        var fromAccount = await CreateAndSaveAccount(repository, "Alice", new Money(1000, "USD"));
        var nonExistentAccountId = new AccountId(Guid.NewGuid().ToString());
        var transferAmount = new Money(100, "USD");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            service.TransferAsync(fromAccount.Id, nonExistentAccountId, transferAmount)
        );

        Assert.Equal(nonExistentAccountId, exception.AccountId);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task TransferAsync_SameAccount_ThrowsException()
    {
        // Arrange
        var repository = new InMemoryRepository<BankAccount, AccountId>();
        var service = CreateService(repository);

        var account = await CreateAndSaveAccount(repository, "Alice", new Money(1000, "USD"));
        var transferAmount = new Money(100, "USD");

        // Act & Assert
        await Assert.ThrowsAsync<SameAccountTransferException>(() =>
            service.TransferAsync(account.Id, account.Id, transferAmount)
        );
    }

    [Fact]
    public async Task TransferAsync_WithLargeDifferentBalances_TransfersSuccessfully()
    {
        // Arrange
        var repository = new InMemoryRepository<BankAccount, AccountId>();
        var service = CreateService(repository);

        var fromAccount = await CreateAndSaveAccount(repository, "Alice", new Money(1000000, "USD"));
        var toAccount = await CreateAndSaveAccount(repository, "Bob", new Money(10, "USD"));
        var transferAmount = new Money(9999, "USD");

        // Act
        var result = await service.TransferAsync(fromAccount.Id, toAccount.Id, transferAmount);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TransferStatus.Success, result.Status);

        var updatedFrom = await repository.FindByIdAsync(fromAccount.Id);
        var updatedTo = await repository.FindByIdAsync(toAccount.Id);

        Assert.NotNull(updatedFrom);
        Assert.NotNull(updatedTo);
        Assert.Equal(new Money(990001, "USD"), updatedFrom.Balance);
        Assert.Equal(new Money(10009, "USD"), updatedTo.Balance);
    }

    #endregion
}
