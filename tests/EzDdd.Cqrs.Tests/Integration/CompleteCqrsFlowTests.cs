using EzDdd.Cqrs.Tests.Integration.TestDomain;
using EzDdd.Cqrs.Tests.Query.TestHelpers;
using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.InOut;
using EzDdd.UseCase.Port.Out;
using EzDdd.UseCase.Tests.Integration.TestDomain;

namespace EzDdd.Cqrs.Tests.Integration;

/// <summary>
///     Integration tests for complete CQRS flow.
///     Tests the entire flow: Command → Aggregate → Events → Repository → Relay → Projector → Archive → Query.
/// </summary>
public sealed class CompleteCqrsFlowTests
{
    #region Setup Infrastructure

    /// <summary>
    ///     Creates the complete CQRS infrastructure for testing.
    /// </summary>
    private static CqrsTestInfrastructure _CreateInfrastructure()
    {
        DomainEventTypeMapper.Register<AccountCreated>("AccountCreated");
        DomainEventTypeMapper.Register<MoneyDeposited>("MoneyDeposited");
        DomainEventTypeMapper.Register<MoneyWithdrawn>("MoneyWithdrawn");
        DomainEventTypeMapper.Register<AccountClosed>("AccountClosed");

        InMemoryEventStorePeer eventStorePeer = new();
        EsRepository<BankAccount, AccountId> repository = new(eventStorePeer);
        InMemoryArchive<AccountSummaryReadModel, AccountId> archive = new(m => m.AccountId);
        AccountProjector projector = new(archive);
        GetAccountSummaryQuery query = new(archive);

        return new CqrsTestInfrastructure
        {
            Repository = repository,
            Archive = archive,
            Projector = projector,
            Query = query,
        };
    }

    private sealed class CqrsTestInfrastructure
    {
        public required EsRepository<BankAccount, AccountId> Repository { get; init; }
        public required InMemoryArchive<AccountSummaryReadModel, AccountId> Archive { get; init; }
        public required AccountProjector Projector { get; init; }
        public required GetAccountSummaryQuery Query { get; init; }

        /// <summary>
        ///     Helper method to save aggregate and manually publish events (simulating Relay pattern).
        /// </summary>
        public async Task SaveAndPublishAsync(BankAccount aggregate)
        {
            // Capture events before save
            List<IInternalDomainEvent> events = aggregate.GetDomainEvents().ToList();

            // Save aggregate (Repository does NOT publish events)
            await Repository.SaveAsync(aggregate);

            // Manually publish events (simulating EventStoreRelay)
            foreach (IInternalDomainEvent domainEvent in events)
            {
                DomainEventData eventData = DomainEventMapper.ToData(domainEvent);

                // Process event through projector (the relay's downstream consumer)
                await Projector.ExecuteAsync(eventData);
            }
        }
    }

    #endregion

    #region Command to Query Flow Tests

    [Fact]
    public async Task CreateAccount_Command_ShouldBeQueryable()
    {
        CqrsTestInfrastructure infra = _CreateInfrastructure();
        AccountId accountId = new("ACC-001");
        const string owner = "John Doe";
        Money initialBalance = new(1000m);

        BankAccount account = new(accountId, owner, initialBalance);
        await infra.SaveAndPublishAsync(account);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        GetAccountSummaryInput input = new(accountId);
        GetAccountSummaryOutput output = await infra.Query.ExecuteAsync(input);

        Assert.Equal(ExitCode.Success, output.ExitCode);
        Assert.Equal("ACC-001", output.AccountId);
        Assert.Equal("John Doe", output.Owner);
        Assert.Equal(1000m, output.Balance);
        Assert.Equal(0, output.TransactionCount);
    }

    [Fact]
    public async Task NonExistentAccount_Query_ShouldThrowException()
    {
        CqrsTestInfrastructure infra = _CreateInfrastructure();
        AccountId accountId = new("NON-EXISTENT");

        GetAccountSummaryInput input = new(accountId);

        await Assert.ThrowsAsync<UseCaseFailureException>(() => infra.Query.ExecuteAsync(input));
    }

    #endregion

    #region Event Projection Tests

    [Fact]
    public async Task AccountCreatedEvent_ShouldCreateReadModel()
    {
        CqrsTestInfrastructure infra = _CreateInfrastructure();
        AccountId accountId = new("ACC-002");
        const string owner = "Jane Smith";
        Money initialBalance = new(2000m);

        BankAccount account = new(accountId, owner, initialBalance);
        await infra.SaveAndPublishAsync(account);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        AccountSummaryReadModel? readModel = await infra.Archive.FindByIdAsync(accountId);

        Assert.NotNull(readModel);
        Assert.Equal(accountId, readModel.AccountId);
        Assert.Equal("Jane Smith", readModel.Owner);
        Assert.Equal(2000m, readModel.Balance);
        Assert.Equal(0, readModel.TransactionCount);
    }

    [Fact]
    public async Task MoneyDepositedEvent_ShouldUpdateReadModel()
    {
        CqrsTestInfrastructure infra = _CreateInfrastructure();
        AccountId accountId = new("ACC-003");
        BankAccount account = new(accountId, "Bob Wilson", new Money(500m));
        await infra.SaveAndPublishAsync(account);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        account.Deposit(new Money(300m));
        await infra.SaveAndPublishAsync(account);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        AccountSummaryReadModel? readModel = await infra.Archive.FindByIdAsync(accountId);

        Assert.NotNull(readModel);
        Assert.Equal(800m, readModel.Balance);
        Assert.Equal(1, readModel.TransactionCount);
    }

    [Fact]
    public async Task MoneyWithdrawnEvent_ShouldUpdateReadModel()
    {
        CqrsTestInfrastructure infra = _CreateInfrastructure();
        AccountId accountId = new("ACC-004");
        BankAccount account = new(accountId, "Alice Brown", new Money(1000m));
        await infra.SaveAndPublishAsync(account);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        account.Withdraw(new Money(250m));
        await infra.SaveAndPublishAsync(account);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        AccountSummaryReadModel? readModel = await infra.Archive.FindByIdAsync(accountId);

        Assert.NotNull(readModel);
        Assert.Equal(750m, readModel.Balance);
        Assert.Equal(1, readModel.TransactionCount);
    }

    #endregion

    #region Complete CQRS Flow Tests

    [Fact]
    public async Task CompleteFlow_CreateDepositWithdrawQuery_ShouldWork()
    {
        CqrsTestInfrastructure infra = _CreateInfrastructure();
        AccountId accountId = new("ACC-005");
        const string owner = "Charlie Davis";
        Money initialBalance = new(5000m);

        BankAccount account = new(accountId, owner, initialBalance);
        await infra.SaveAndPublishAsync(account);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        account.Deposit(new Money(1500m));
        await infra.SaveAndPublishAsync(account);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        account.Withdraw(new Money(2000m));
        await infra.SaveAndPublishAsync(account);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        GetAccountSummaryInput input = new(accountId);
        GetAccountSummaryOutput output = await infra.Query.ExecuteAsync(input);

        Assert.Equal(ExitCode.Success, output.ExitCode);
        Assert.Equal("ACC-005", output.AccountId);
        Assert.Equal("Charlie Davis", output.Owner);
        Assert.Equal(4500m, output.Balance);
        Assert.Equal(2, output.TransactionCount);
    }

    [Fact]
    public async Task MultipleOperations_ShouldMaintainConsistency()
    {
        CqrsTestInfrastructure infra = _CreateInfrastructure();
        AccountId accountId = new("ACC-006");
        BankAccount account = new(accountId, "David Evans", new Money(10000m));
        await infra.SaveAndPublishAsync(account);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        for (int i = 0; i < 5; i++)
        {
            account.Deposit(new Money(100m));
            await infra.SaveAndPublishAsync(account);
            // Allow time for async event propagation to projector
            await Task.Delay(50);
        }

        GetAccountSummaryInput input = new(accountId);
        GetAccountSummaryOutput output = await infra.Query.ExecuteAsync(input);

        Assert.Equal(10500m, output.Balance);
        Assert.Equal(5, output.TransactionCount);
    }

    #endregion

    #region Deletion Flow Tests

    [Fact]
    public async Task AccountClosed_ShouldRemoveReadModel()
    {
        CqrsTestInfrastructure infra = _CreateInfrastructure();
        AccountId accountId = new("ACC-007");
        BankAccount account = new(accountId, "Eve Foster", new Money(100m));
        await infra.SaveAndPublishAsync(account);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        AccountSummaryReadModel? readModelBeforeClose = await infra.Archive.FindByIdAsync(accountId);
        Assert.NotNull(readModelBeforeClose);

        account.Close("Account closure requested");
        await infra.SaveAndPublishAsync(account);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        AccountSummaryReadModel? readModelAfterClose = await infra.Archive.FindByIdAsync(accountId);

        Assert.Null(readModelAfterClose);
    }

    [Fact]
    public async Task AccountClosedThenQueried_ShouldThrowException()
    {
        CqrsTestInfrastructure infra = _CreateInfrastructure();
        AccountId accountId = new("ACC-008");
        BankAccount account = new(accountId, "Frank Green", new Money(500m));
        await infra.SaveAndPublishAsync(account);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        account.Close("No longer needed");
        await infra.SaveAndPublishAsync(account);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        GetAccountSummaryInput input = new(accountId);

        await Assert.ThrowsAsync<UseCaseFailureException>(() => infra.Query.ExecuteAsync(input));
    }

    #endregion

    #region Event Replay Consistency Tests

    [Fact]
    public async Task EventReplay_ShouldProduceSameReadModel()
    {
        CqrsTestInfrastructure infra = _CreateInfrastructure();
        AccountId accountId = new("ACC-009");
        BankAccount account = new(accountId, "Grace Hill", new Money(3000m));
        await infra.SaveAndPublishAsync(account);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        BankAccount? account1 = await infra.Repository.FindByIdAsync(accountId);
        Assert.NotNull(account1);
        account1.Deposit(new Money(500m));
        await infra.SaveAndPublishAsync(account1);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        BankAccount? account2 = await infra.Repository.FindByIdAsync(accountId);
        Assert.NotNull(account2);
        account2.Deposit(new Money(700m));
        await infra.SaveAndPublishAsync(account2);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        BankAccount? account3 = await infra.Repository.FindByIdAsync(accountId);
        Assert.NotNull(account3);
        account3.Withdraw(new Money(200m));
        await infra.SaveAndPublishAsync(account3);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        AccountSummaryReadModel? readModel = await infra.Archive.FindByIdAsync(accountId);
        Assert.NotNull(readModel);

        BankAccount? aggregate = await infra.Repository.FindByIdAsync(accountId);
        Assert.NotNull(aggregate);

        Assert.Equal(aggregate.Balance.Amount, readModel.Balance);
        Assert.Equal(3, readModel.TransactionCount);
    }

    [Fact]
    public async Task ConcurrentArchiveSaves_IdempotentOperation_ShouldHandleCorrectly()
    {
        CqrsTestInfrastructure infra = _CreateInfrastructure();
        AccountId accountId = new("ACC-CONCURRENT-001");
        BankAccount account = new(accountId, "Ivan Jackson", new Money(1000m));

        account.Deposit(new Money(100m));
        account.Deposit(new Money(50m));

        await infra.SaveAndPublishAsync(account);
        // Allow time for async event propagation to projector
        await Task.Delay(50);

        AccountSummaryReadModel? readModel = await infra.Archive.FindByIdAsync(accountId);
        Assert.NotNull(readModel);

        List<Task> tasks = [];
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(infra.Archive.SaveAsync(readModel));
        }

        await Task.WhenAll(tasks);

        GetAccountSummaryOutput result = await infra.Query.ExecuteAsync(new GetAccountSummaryInput(accountId));

        Assert.Equal(ExitCode.Success, result.ExitCode);
        Assert.Equal(1150m, result.Balance);
        Assert.Equal("ACC-CONCURRENT-001", result.AccountId);
        Assert.Equal(2, result.TransactionCount);
    }

    #endregion
}
