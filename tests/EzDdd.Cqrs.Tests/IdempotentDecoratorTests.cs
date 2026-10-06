using System.Collections.ObjectModel;
using EzDdd.Cqrs.Command;
using EzDdd.Entity;
using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.InOut;

namespace EzDdd.Cqrs.Tests;

[Collection("DomainEventTypeMapper")]
public class IdempotentDecoratorTests
{
    private const string DataId = "data-1";

    public IdempotentDecoratorTests()
    {
        DomainEventTypeMapper.Register<IdempotentTestEvent>("IdempotentTestEvent");
    }

    public enum ParserKind
    {
        Delegate,
        Interface,
    }

    [Theory]
    [InlineData(ParserKind.Delegate)]
    [InlineData(ParserKind.Interface)]
    public async Task ExecuteAsync_WhenParserReturnsNull_ReturnsIgnoreWithoutInquiringOrExecuting(ParserKind kind)
    {
        Spies spies = new() { ParsedId = null };
        IdempotentDecorator<DomainEventDataInput, DefaultOutput> sut = spies.CreateSut(kind);

        DefaultOutput output = await sut.ExecuteAsync(_NewInput());

        Assert.Equal(ExitCode.Ignore, output.ExitCode);
        Assert.Equal(0, spies.InquiryCalls);
        Assert.Equal(0, spies.UseCaseCalls);
    }

    [Theory]
    [InlineData(ParserKind.Delegate)]
    [InlineData(ParserKind.Interface)]
    public async Task ExecuteAsync_WhenEventAlreadyApplied_ReturnsIgnoreWithoutExecuting(ParserKind kind)
    {
        Spies spies = new() { ParsedId = DataId, AlreadyApplied = true };
        IdempotentDecorator<DomainEventDataInput, DefaultOutput> sut = spies.CreateSut(kind);

        DefaultOutput output = await sut.ExecuteAsync(_NewInput());

        Assert.Equal(ExitCode.Ignore, output.ExitCode);
        Assert.Equal(0, spies.UseCaseCalls);
        Assert.Equal(["parse", "inquire"], spies.CallOrder);
    }

    [Theory]
    [InlineData(ParserKind.Delegate)]
    [InlineData(ParserKind.Interface)]
    public async Task ExecuteAsync_WhenParserReturnsEmptyString_InquiresWithEmptyDataId(ParserKind kind)
    {
        Spies spies = new() { ParsedId = "" };
        IdempotentDecorator<DomainEventDataInput, DefaultOutput> sut = spies.CreateSut(kind);

        await sut.ExecuteAsync(_NewInput());

        IdempotentInquiryInput inquiryInput = Assert.Single(spies.InquiryInputs);
        Assert.Equal("", inquiryInput.DataId);
    }

    [Theory]
    [InlineData(ParserKind.Delegate)]
    [InlineData(ParserKind.Interface)]
    public async Task ExecuteAsync_WhenEventNotYetApplied_DelegatesSameInputAndReturnsSameOutput(ParserKind kind)
    {
        Spies spies = new() { ParsedId = DataId };
        IdempotentDecorator<DomainEventDataInput, DefaultOutput> sut = spies.CreateSut(kind);
        DomainEventDataInput input = _NewInput();

        DefaultOutput output = await sut.ExecuteAsync(input);

        Assert.Same(input, Assert.Single(spies.UseCaseInputs));
        Assert.Same(Assert.Single(spies.UseCaseOutputs), output);
    }

    [Fact]
    public async Task ExecuteAsync_InquiresWithParsedDataIdAndEventIdOfConvertedEvent()
    {
        Spies spies = new() { ParsedId = DataId };
        IdempotentDecorator<DomainEventDataInput, DefaultOutput> sut = spies.CreateSut(ParserKind.Delegate);
        Guid firstEventId = Guid.NewGuid();
        Guid secondEventId = Guid.NewGuid();

        await sut.ExecuteAsync(_NewInput(firstEventId));
        await sut.ExecuteAsync(_NewInput(secondEventId));

        Assert.Equal(2, spies.InquiryInputs.Count);
        Assert.NotSame(spies.InquiryInputs[0], spies.InquiryInputs[1]);
        Assert.Equal((DataId, firstEventId), (spies.InquiryInputs[0].DataId, spies.InquiryInputs[0].EventId));
        Assert.Equal((DataId, secondEventId), (spies.InquiryInputs[1].DataId, spies.InquiryInputs[1].EventId));
    }

    [Theory]
    [InlineData(ParserKind.Delegate, null, false)]
    [InlineData(ParserKind.Delegate, DataId, true)]
    [InlineData(ParserKind.Interface, null, false)]
    [InlineData(ParserKind.Interface, DataId, true)]
    public async Task ExecuteAsync_WhenIgnoring_ReturnsAFreshOutputEachTime(
        ParserKind kind,
        string? parsedId,
        bool applied
    )
    {
        Spies spies = new() { ParsedId = parsedId, AlreadyApplied = applied };
        IdempotentDecorator<DomainEventDataInput, DefaultOutput> sut = spies.CreateSut(kind);

        DefaultOutput first = await sut.ExecuteAsync(_NewInput());
        DefaultOutput second = await sut.ExecuteAsync(_NewInput());

        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInquiryThrowsSynchronously_PropagatesSameException()
    {
        InvalidOperationException failure = new("inquiry failed");
        Spies spies = new()
        {
            ParsedId = DataId,
            InquiryFailure = failure,
            InquiryThrowsSynchronously = true,
        };
        IdempotentDecorator<DomainEventDataInput, DefaultOutput> sut = spies.CreateSut(ParserKind.Delegate);

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(_NewInput())
        );

        Assert.Same(failure, thrown);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInquiryTaskFaults_PropagatesSameException()
    {
        InvalidOperationException failure = new("inquiry failed");
        Spies spies = new() { ParsedId = DataId, InquiryFailure = failure };
        IdempotentDecorator<DomainEventDataInput, DefaultOutput> sut = spies.CreateSut(ParserKind.Delegate);

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(_NewInput())
        );

        Assert.Same(failure, thrown);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDecoratedUseCaseThrows_PropagatesSameException()
    {
        InvalidOperationException failure = new("use case failed");
        Spies spies = new() { ParsedId = DataId, UseCaseFailure = failure };
        IdempotentDecorator<DomainEventDataInput, DefaultOutput> sut = spies.CreateSut(ParserKind.Delegate);

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(_NewInput())
        );

        Assert.Same(failure, thrown);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEventTypeIsNotRegistered_PropagatesConversionFailureWithoutParsingOrInquiring()
    {
        Spies spies = new() { ParsedId = DataId };
        IdempotentDecorator<DomainEventDataInput, DefaultOutput> sut = spies.CreateSut(ParserKind.Delegate);
        DomainEventDataInput input = new()
        {
            Event = DomainEventDataBuilder.Json("UnregisteredEventType", (object)new { Id = Guid.NewGuid() }).Build(),
        };

        await Assert.ThrowsAnyAsync<Exception>(() => sut.ExecuteAsync(input));

        Assert.Empty(spies.CallOrder);
        Assert.Equal(0, spies.UseCaseCalls);
    }

    [Theory]
    [InlineData(ParserKind.Delegate)]
    [InlineData(ParserKind.Interface)]
    public async Task ExecuteAsync_WithNullInput_ThrowsArgumentNullException(ParserKind kind)
    {
        IdempotentDecorator<DomainEventDataInput, DefaultOutput> sut = new Spies().CreateSut(kind);

        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.ExecuteAsync(null!));
    }

    [Theory]
    [InlineData(ParserKind.Delegate)]
    [InlineData(ParserKind.Interface)]
    public async Task ExecuteAsync_WithNullEvent_ThrowsArgumentNullException(ParserKind kind)
    {
        IdempotentDecorator<DomainEventDataInput, DefaultOutput> sut = new Spies().CreateSut(kind);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.ExecuteAsync(new DomainEventDataInput { Event = null! })
        );
    }

    [Fact]
    public void Constructor_WithNullArgument_ThrowsArgumentNullException()
    {
        Spies spies = new();
        Func<DefaultOutput> factory = () => new DefaultOutput();
        Func<IInternalDomainEvent, string?> parseFunc = spies.Parse;

        Assert.Throws<ArgumentNullException>(() => _Create(null!, spies, factory, parseFunc));
        Assert.Throws<ArgumentNullException>(() => _Create(spies, null!, factory, parseFunc));
        Assert.Throws<ArgumentNullException>(() => _Create(spies, spies, null!, parseFunc));
        Assert.Throws<ArgumentNullException>(() =>
            _Create(spies, spies, factory, (Func<IInternalDomainEvent, string?>)null!)
        );
    }

    [Fact]
    public void Constructor_WithNullArgumentAndParserInterface_ThrowsArgumentNullException()
    {
        Spies spies = new();
        Func<DefaultOutput> factory = () => new DefaultOutput();
        IIdempotentIdParser<IInternalDomainEvent> parser = spies;

        Assert.Throws<ArgumentNullException>(() => _Create(null!, spies, factory, parser));
        Assert.Throws<ArgumentNullException>(() => _Create(spies, null!, factory, parser));
        Assert.Throws<ArgumentNullException>(() => _Create(spies, spies, null!, parser));
        Assert.Throws<ArgumentNullException>(() =>
            _Create(spies, spies, factory, (IIdempotentIdParser<IInternalDomainEvent>)null!)
        );
    }

    [Fact]
    public async Task ExecuteAsync_WithConcurrentCallsForDifferentEvents_KeepsEachCallsInquiryInputAndOutput()
    {
        Spies spies = new() { ParsedId = DataId, AlreadyApplied = true };
        IdempotentDecorator<DomainEventDataInput, DefaultOutput> sut = spies.CreateSut(ParserKind.Delegate);
        Guid firstEventId = Guid.NewGuid();
        Guid secondEventId = Guid.NewGuid();

        DefaultOutput[] outputs = await Task.WhenAll(
            sut.ExecuteAsync(_NewInput(firstEventId)),
            sut.ExecuteAsync(_NewInput(secondEventId))
        );

        Assert.NotSame(outputs[0], outputs[1]);
        Assert.Equivalent(new[] { firstEventId, secondEventId }, spies.InquiryInputs.Select(i => i.EventId));
        Assert.NotSame(spies.InquiryInputs[0], spies.InquiryInputs[1]);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(DataId, true)]
    public async Task ExecuteAsync_WhenIgnoring_ReturnsTheFactoryOutputInstanceItIgnored(string? parsedId, bool applied)
    {
        Spies spies = new() { ParsedId = parsedId, AlreadyApplied = applied };
        IdempotentDecorator<DomainEventDataInput, SelfMutatingOutput> sut = new(
            new NeverCalledUseCase(),
            spies,
            () => new SelfMutatingOutput(),
            _ => spies.ParsedId
        );

        SelfMutatingOutput output = await sut.ExecuteAsync(_NewInput());

        Assert.Equal(ExitCode.Ignore, output.ExitCode);
    }

    private static IdempotentDecorator<DomainEventDataInput, DefaultOutput> _Create(
        IUseCase<DomainEventDataInput, DefaultOutput> useCase,
        IInquiry<IdempotentInquiryInput, bool> inquiry,
        Func<DefaultOutput> outputFactory,
        Func<IInternalDomainEvent, string?> idParser
    ) => new(useCase, inquiry, outputFactory, idParser);

    private static IdempotentDecorator<DomainEventDataInput, DefaultOutput> _Create(
        IUseCase<DomainEventDataInput, DefaultOutput> useCase,
        IInquiry<IdempotentInquiryInput, bool> inquiry,
        Func<DefaultOutput> outputFactory,
        IIdempotentIdParser<IInternalDomainEvent> idParser
    ) => new(useCase, inquiry, outputFactory, idParser);

    private static DomainEventDataInput _NewInput(Guid? eventId = null) =>
        new()
        {
            Event = DomainEventMapper.ToData(
                new IdempotentTestEvent(eventId ?? Guid.NewGuid(), DateTimeOffset.UtcNow, DataId)
            ),
        };

    private sealed record IdempotentTestEvent(Guid Id, DateTimeOffset OccurredOn, string Source) : IInternalDomainEvent
    {
        public IReadOnlyDictionary<string, string> Metadata { get; init; } =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
    }

    private sealed class NeverCalledUseCase : IUseCase<DomainEventDataInput, SelfMutatingOutput>
    {
        public Task<SelfMutatingOutput> ExecuteAsync(DomainEventDataInput input) =>
            throw new InvalidOperationException("The decorated use case must not run.");
    }

    // Mutates itself on Ignore() but returns a different instance, like a hand-written IOutput may.
    private sealed class SelfMutatingOutput : IOutput
    {
        public string Message { get; private set; } = "";

        public ExitCode ExitCode { get; private set; }

        public string Id { get; private set; } = "";

        public IOutput SetMessage(string message)
        {
            Message = message;
            return this;
        }

        public IOutput SetExitCode(ExitCode exitCode)
        {
            ExitCode = exitCode;
            return this;
        }

        public IOutput Fail() => SetExitCode(ExitCode.Failure);

        public IOutput Succeed() => SetExitCode(ExitCode.Success);

        public IOutput Ignore()
        {
            ExitCode = ExitCode.Ignore;
            return new SelfMutatingOutput();
        }

        public IOutput Reject() => SetExitCode(ExitCode.Reject);

        public IOutput SetId(string id)
        {
            Id = id;
            return this;
        }
    }

    private sealed class Spies
        : IUseCase<DomainEventDataInput, DefaultOutput>,
            IInquiry<IdempotentInquiryInput, bool>,
            IIdempotentIdParser<IInternalDomainEvent>
    {
        public string? ParsedId { get; init; }

        public bool AlreadyApplied { get; init; }

        public Exception? InquiryFailure { get; init; }

        public bool InquiryThrowsSynchronously { get; init; }

        public Exception? UseCaseFailure { get; init; }

        public List<string> CallOrder { get; } = [];

        public List<IdempotentInquiryInput> InquiryInputs { get; } = [];

        public List<DomainEventDataInput> UseCaseInputs { get; } = [];

        public List<DefaultOutput> UseCaseOutputs { get; } = [];

        public int InquiryCalls { get; private set; }

        public int UseCaseCalls { get; private set; }

        public IdempotentDecorator<DomainEventDataInput, DefaultOutput> CreateSut(ParserKind kind) =>
            kind == ParserKind.Interface
                ? _Create(this, this, () => new DefaultOutput(), (IIdempotentIdParser<IInternalDomainEvent>)this)
                : _Create(this, this, () => new DefaultOutput(), (Func<IInternalDomainEvent, string?>)Parse);

        public string? Parse(IInternalDomainEvent input)
        {
            CallOrder.Add("parse");
            return ParsedId;
        }

        public Task<bool> QueryAsync(IdempotentInquiryInput input)
        {
            if (InquiryFailure is not null && InquiryThrowsSynchronously)
            {
                throw InquiryFailure;
            }

            return _QueryCoreAsync(input);
        }

        public async Task<DefaultOutput> ExecuteAsync(DomainEventDataInput input)
        {
            await Task.Yield();
            if (UseCaseFailure is not null)
            {
                throw UseCaseFailure;
            }

            UseCaseCalls++;
            UseCaseInputs.Add(input);
            DefaultOutput output = new();
            UseCaseOutputs.Add(output);
            return output;
        }

        private async Task<bool> _QueryCoreAsync(IdempotentInquiryInput input)
        {
            await Task.Yield();
            if (InquiryFailure is not null)
            {
                throw InquiryFailure;
            }

            lock (InquiryInputs)
            {
                InquiryInputs.Add(input);
                InquiryCalls++;
                CallOrder.Add("inquire");
            }

            return AlreadyApplied;
        }
    }
}
