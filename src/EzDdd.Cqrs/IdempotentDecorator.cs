using EzDdd.Cqrs.Command;
using EzDdd.Entity;
using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.InOut;

namespace EzDdd.Cqrs;

/// <summary>
///     <c>IdempotentDecorator</c> makes a message-driven use case skip a domain event that was already
///     applied. Before the decorated use case runs, it parses the id of the data the event targets and asks
///     the inquiry whether the event was applied to that data. When the event targets no data this use case
///     handles, or was already applied, it returns an <c>Ignore</c> output instead of executing.
///     Otherwise it delegates with the same input and returns the output unchanged.
/// </summary>
/// <remarks>
///     <para>
///         The inquiry must answer <c>true</c> only when the event has already been applied to the data,
///         and <c>false</c> when the data does not exist yet. The <c>outputFactory</c> must return a new
///         instance on every call, because the decorator changes it through <c>Ignore()</c>. This decorator
///         is not an <c>IReactor</c>; hold a wrapped reactor as <c>IUseCase&lt;TInput, DefaultOutput&gt;</c>.
///     </para>
///     <para>
///         Limitations: it is a best-effort filter, not mutual exclusion. Concurrent deliveries of the same
///         event for the same data id can both pass, so deliver the events of one data id serially (for
///         example by partitioning by data id), or, when the only effect of the decorated use case is a
///         save, use an archive with optimistic concurrency; side effects before the save can still happen
///         twice. The answer of the inquiry is advisory, because the decorated use case loads the data
///         again. Only <c>ReadModel</c> carries a record that can be used for deduplication, so a reactor
///         that changes an aggregate needs an application-supplied inquiry backed by its own record of
///         handled events.
///     </para>
/// </remarks>
/// <typeparam name="TInput">The type of the use case input, which carries the domain event.</typeparam>
/// <typeparam name="TOutput">The type of the use case output.</typeparam>
public class IdempotentDecorator<TInput, TOutput> : UseCaseDecorator<TInput, TOutput>
    where TInput : DomainEventDataInput
    where TOutput : IOutput
{
    private readonly IInquiry<IdempotentInquiryInput, bool> _inquiry;
    private readonly Func<TOutput> _outputFactory;
    private readonly Func<IInternalDomainEvent, string?> _idParser;

    /// <summary>
    ///     Initializes a new instance of the <see cref="IdempotentDecorator{TInput, TOutput}"/> class.
    /// </summary>
    /// <param name="useCase">The use case to decorate.</param>
    /// <param name="inquiry">Tells whether an event was already applied to the data.</param>
    /// <param name="outputFactory">Creates a new output on every call.</param>
    /// <param name="idParser">Returns the data id of an event, or <c>null</c> when it is not handled.</param>
    public IdempotentDecorator(
        IUseCase<TInput, TOutput> useCase,
        IInquiry<IdempotentInquiryInput, bool> inquiry,
        Func<TOutput> outputFactory,
        Func<IInternalDomainEvent, string?> idParser
    )
        : base(useCase)
    {
        ArgumentNullException.ThrowIfNull(inquiry);
        ArgumentNullException.ThrowIfNull(outputFactory);
        ArgumentNullException.ThrowIfNull(idParser);
        _inquiry = inquiry;
        _outputFactory = outputFactory;
        _idParser = idParser;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="IdempotentDecorator{TInput, TOutput}"/> class.
    /// </summary>
    /// <param name="useCase">The use case to decorate.</param>
    /// <param name="inquiry">Tells whether an event was already applied to the data.</param>
    /// <param name="outputFactory">Creates a new output on every call.</param>
    /// <param name="idParser">Returns the data id of an event, or <c>null</c> when it is not handled.</param>
    public IdempotentDecorator(
        IUseCase<TInput, TOutput> useCase,
        IInquiry<IdempotentInquiryInput, bool> inquiry,
        Func<TOutput> outputFactory,
        IIdempotentIdParser<IInternalDomainEvent> idParser
    )
        : this(useCase, inquiry, outputFactory, _ParseDelegateOf(idParser)) { }

    /// <inheritdoc />
    public override async Task<TOutput> ExecuteAsync(TInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Event, nameof(input));
        IInternalDomainEvent domainEvent = DomainEventMapper.ToDomain<IInternalDomainEvent>(input.Event);
        string? dataId = _idParser(domainEvent);
        if (dataId is null)
        {
            return _IgnoredOutput();
        }

        bool applied = await _inquiry
            .QueryAsync(new IdempotentInquiryInput { DataId = dataId, EventId = domainEvent.Id })
            .ConfigureAwait(false);
        if (applied)
        {
            return _IgnoredOutput();
        }

        return await DecoratedUseCase.ExecuteAsync(input).ConfigureAwait(false);
    }

    private TOutput _IgnoredOutput()
    {
        TOutput output = _outputFactory();
        output.Ignore();
        return output;
    }

    private static Func<IInternalDomainEvent, string?> _ParseDelegateOf(
        IIdempotentIdParser<IInternalDomainEvent> idParser
    )
    {
        ArgumentNullException.ThrowIfNull(idParser);
        return idParser.Parse;
    }
}
