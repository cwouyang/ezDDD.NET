using EzDdd.Cqrs.Command;

namespace EzDdd.Cqrs;

/// <summary>
///     <c>IdempotentInquiryInput</c> is the input of the inquiry that an
///     <see cref="IdempotentDecorator{TInput, TOutput}"/> uses to find out whether an event has
///     already been applied to a piece of data.
/// </summary>
public class IdempotentInquiryInput : IInquiryInput
{
    /// <summary>
    ///     Gets or sets the id of the data the event targets, as returned by the id parser.
    /// </summary>
    public string DataId { get; set; } = "";

    /// <summary>
    ///     Gets or sets the id of the domain event under test.
    /// </summary>
    public Guid EventId { get; set; }
}
