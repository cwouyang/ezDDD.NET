using EzDdd.Cqrs.Command;
using EzDdd.Cqrs.Entity.Query;
using EzDdd.Cqrs.Query;
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
[Collection("DomainEventTypeMapper")]
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
        JsonCopyArchive<AccountSummaryReadModel, AccountId> archive = new(m => m.AccountId);
        return _CreateInfrastructure(repository, archive, archive);
    }

    private static CqrsTestInfrastructure _CreateInfrastructure(
        EsRepository<BankAccount, AccountId> repository,
        JsonCopyArchive<AccountSummaryReadModel, AccountId> storeArchive,
        IArchive<AccountSummaryReadModel, AccountId> projectorArchive
    )
    {
        AccountProjector projector = new(projectorArchive);
        IUseCase<DomainEventDataInput, DefaultOutput> reactor = new IdempotentDecorator<
            DomainEventDataInput,
            DefaultOutput
        >(
            projector,
            new AccountEventHandledInquiry(projectorArchive),
            () => new DefaultOutput(),
            (IInternalDomainEvent e) => e.Source
        );

        return new CqrsTestInfrastructure
        {
            Repository = repository,
            Archive = storeArchive,
            Reactor = reactor,
            Query = new GetAccountSummaryQuery(storeArchive),
        };
    }

    private sealed class AccountEventHandledInquiry : IInquiry<IdempotentInquiryInput, bool>
    {
        private readonly IArchive<AccountSummaryReadModel, AccountId> _archive;

        public AccountEventHandledInquiry(IArchive<AccountSummaryReadModel, AccountId> archive)
        {
            _archive = archive;
        }

        public async Task<bool> QueryAsync(IdempotentInquiryInput input)
        {
            AccountSummaryReadModel? readModel = await _archive.FindByIdAsync(new AccountId(input.DataId));
            return readModel?.EventDeduplicationRecord.IsEventHandled(input.EventId) ?? false;
        }
    }

    /// <summary>
    ///     Archive spy that makes the first save fail, like a store outage, and delegates every other call.
    /// </summary>
    private sealed class SaveFailsOnceArchive : IArchive<AccountSummaryReadModel, AccountId>
    {
        private readonly IArchive<AccountSummaryReadModel, AccountId> _inner;
        private bool _failed;

        public SaveFailsOnceArchive(IArchive<AccountSummaryReadModel, AccountId> inner)
        {
            _inner = inner;
        }

        public Task<AccountSummaryReadModel?> FindByIdAsync(AccountId id)
        {
            return _inner.FindByIdAsync(id);
        }

        public Task SaveAsync(AccountSummaryReadModel data)
        {
            if (_failed)
            {
                return _inner.SaveAsync(data);
            }

            _failed = true;
            throw new InvalidOperationException("Simulated save failure.");
        }

        public Task DeleteAsync(AccountSummaryReadModel data)
        {
            return _inner.DeleteAsync(data);
        }
    }

    /// <summary>
    ///     Test-only read model that remembers just three event ids.
    /// </summary>
    private sealed record CappedCounterReadModel : ReadModel
    {
        public CappedCounterReadModel(AccountId accountId, int applied)
            : base(3)
        {
            AccountId = accountId;
            Applied = applied;
        }

        public AccountId AccountId { get; init; }
        public int Applied { get; init; }
    }

    private sealed class CappedCounterReactor : IReactor<DomainEventDataInput>
    {
        private readonly IArchive<CappedCounterReadModel, AccountId> _archive;

        public CappedCounterReactor(IArchive<CappedCounterReadModel, AccountId> archive)
        {
            _archive = archive;
        }

        public async Task<DefaultOutput> ExecuteAsync(DomainEventDataInput input)
        {
            IInternalDomainEvent domainEvent = DomainEventMapper.ToDomain<IInternalDomainEvent>(input.Event);
            AccountId id = new(domainEvent.Source);
            CappedCounterReadModel current = await _archive.FindByIdAsync(id) ?? new CappedCounterReadModel(id, 0);
            CappedCounterReadModel updated = current with { Applied = current.Applied + 1 };
            updated.UpdateEventDeduplicationRecord(domainEvent.Id);
            await _archive.SaveAsync(updated);
            return DefaultOutput.Create().Succeed();
        }
    }

    private sealed class CappedCounterInquiry : IInquiry<IdempotentInquiryInput, bool>
    {
        private readonly IArchive<CappedCounterReadModel, AccountId> _archive;

        public CappedCounterInquiry(IArchive<CappedCounterReadModel, AccountId> archive)
        {
            _archive = archive;
        }

        public async Task<bool> QueryAsync(IdempotentInquiryInput input)
        {
            CappedCounterReadModel? readModel = await _archive.FindByIdAsync(new AccountId(input.DataId));
            return readModel?.EventDeduplicationRecord.IsEventHandled(input.EventId) ?? false;
        }
    }

    private sealed class CqrsTestInfrastructure
    {
        public required EsRepository<BankAccount, AccountId> Repository { get; init; }
        public required JsonCopyArchive<AccountSummaryReadModel, AccountId> Archive { get; init; }
        public required IUseCase<DomainEventDataInput, DefaultOutput> Reactor { get; init; }
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

                // Process event through the decorated reactor (the relay's downstream consumer)
                await Reactor.ExecuteAsync(new DomainEventDataInput { Event = eventData });
            }
        }
    }

    #endregion

    #region Deduplication Tests

    private static DomainEventDataInput _InputOf(IInternalDomainEvent domainEvent)
    {
        return new DomainEventDataInput { Event = DomainEventMapper.ToData(domainEvent) };
    }

    [Fact]
    public async Task FirstDelivery_ShouldSucceedAndProjectTheEvent()
    {
        CqrsTestInfrastructure infra = _CreateInfrastructure();
        AccountId accountId = new("ACC-DEDUP-001");
        BankAccount account = new(accountId, "Heidi Klein", new Money(100m));
        DomainEventDataInput created = _InputOf(account.GetDomainEvents().Single());

        DefaultOutput output = await infra.Reactor.ExecuteAsync(created);

        AccountSummaryReadModel? stored = await infra.Archive.FindByIdAsync(accountId);
        Assert.Equal(ExitCode.Success, output.ExitCode);
        Assert.NotNull(stored);
        Assert.Equal(100m, stored.Balance);
        Assert.True(stored.EventDeduplicationRecord.IsEventHandled(created.Event.Id));
    }

    [Fact]
    public async Task Redelivery_ShouldBeIgnoredAndLeaveTheStoredReadModelUnchanged()
    {
        CqrsTestInfrastructure infra = _CreateInfrastructure();
        AccountId accountId = new("ACC-DEDUP-002");
        BankAccount account = new(accountId, "Judy Lane", new Money(100m));
        account.Deposit(new Money(25m));
        List<DomainEventDataInput> inputs = account.GetDomainEvents().Select(_InputOf).ToList();
        foreach (DomainEventDataInput input in inputs)
        {
            await infra.Reactor.ExecuteAsync(input);
        }

        string? jsonBefore = infra.Archive.GetStoredJson(accountId);

        DefaultOutput output = await infra.Reactor.ExecuteAsync(inputs[1]);

        Assert.Equal(ExitCode.Ignore, output.ExitCode);
        Assert.Equal(jsonBefore, infra.Archive.GetStoredJson(accountId));
    }

    [Fact]
    public async Task RedeliveryAfterFailedSave_ShouldProjectTheEvent()
    {
        EsRepository<BankAccount, AccountId> repository = new(new InMemoryEventStorePeer());
        JsonCopyArchive<AccountSummaryReadModel, AccountId> store = new(m => m.AccountId);
        CqrsTestInfrastructure infra = _CreateInfrastructure(repository, store, new SaveFailsOnceArchive(store));
        AccountId accountId = new("ACC-DEDUP-003");
        BankAccount account = new(accountId, "Karl Moore", new Money(100m));
        DomainEventDataInput created = _InputOf(account.GetDomainEvents().Single());
        await Assert.ThrowsAsync<InvalidOperationException>(() => infra.Reactor.ExecuteAsync(created));

        DefaultOutput output = await infra.Reactor.ExecuteAsync(created);

        AccountSummaryReadModel? stored = await store.FindByIdAsync(accountId);
        Assert.Equal(ExitCode.Success, output.ExitCode);
        Assert.NotNull(stored);
        Assert.True(stored.EventDeduplicationRecord.IsEventHandled(created.Event.Id));
    }

    [Fact]
    public async Task RedeliveryOlderThanTheCapacity_ShouldBeProjectedAgain()
    {
        _CreateInfrastructure();
        const int capacity = 3;
        const int furtherEvents = capacity + 1;
        JsonCopyArchive<CappedCounterReadModel, AccountId> archive = new(m => m.AccountId);
        IdempotentDecorator<DomainEventDataInput, DefaultOutput> reactor = new(
            new CappedCounterReactor(archive),
            new CappedCounterInquiry(archive),
            () => new DefaultOutput(),
            (IInternalDomainEvent e) => e.Source
        );
        AccountId accountId = new("ACC-DEDUP-004");
        List<DomainEventDataInput> inputs = Enumerable
            .Range(0, 1 + furtherEvents)
            .Select(_ => _InputOf(new MoneyDeposited(Guid.NewGuid(), DateTimeOffset.UtcNow, accountId, new Money(1m))))
            .ToList();
        foreach (DomainEventDataInput input in inputs)
        {
            await reactor.ExecuteAsync(input);
        }

        DefaultOutput output = await reactor.ExecuteAsync(inputs[0]);

        CappedCounterReadModel? stored = await archive.FindByIdAsync(accountId);
        Assert.Equal(ExitCode.Success, output.ExitCode);
        Assert.NotNull(stored);
        Assert.Equal(inputs.Count + 1, stored.Applied);
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
