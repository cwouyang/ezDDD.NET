namespace EzDdd.Cqrs.Command;

/// <summary>
///     <c>IInquiryInput</c> is a marker interface for inquiry input data.
///     <para>
///         Inquiry is a lightweight query interface used primarily by the command side
///         for validation purposes. This marker interface provides type safety and
///         semantic clarity for inquiry input data.
///     </para>
/// </summary>
/// <remarks>
///     <para>
///         <b>Purpose</b>: Type safety for inquiry inputs
///     </para>
///     <para>
///         <b>Usage</b>: Implement this interface on DTOs or records that serve as
///         inquiry input data. Inquiries are typically used within commands to validate
///         preconditions or check system state before executing business logic.
///     </para>
///     <para>
///         <b>Example</b>:
///         <code>
///         public record CheckAccountExistsInput(AccountId AccountId) : IInquiryInput;
///
///         public class CheckAccountExistsInquiry
///             : IInquiry&lt;CheckAccountExistsInput, bool&gt;
///         {
///             public async Task&lt;bool&gt; QueryAsync(CheckAccountExistsInput input)
///             {
///                 // Validation logic here
///                 return await _archive.FindByIdAsync(input.AccountId) != null;
///             }
///         }
///         </code>
///     </para>
/// </remarks>
public interface IInquiryInput
{
    // Pure marker interface - no methods
}
