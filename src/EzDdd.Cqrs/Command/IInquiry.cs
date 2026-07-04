using EzDdd.Cqrs.Query;

namespace EzDdd.Cqrs.Command;

/// <summary>
///     <c>IInquiry</c> is a lightweight query interface used primarily by the command side
///     for validation purposes.
///     <para>
///         Inquiries are simple, focused queries that commands use to validate preconditions
///         or check system state before executing business logic. Unlike queries in the
///         query side, inquiries do not extend IUseCase and have minimal overhead.
///     </para>
/// </summary>
/// <typeparam name="TInput">
///     The input type for the inquiry. Typically implements <see cref="IInquiryInput" />
///     for type safety, but not required.
/// </typeparam>
/// <typeparam name="TOutput">
///     The output type for the inquiry. Can be any type (bool, DTO, etc.).
///     Does NOT require <see cref="CqrsOutput{T}" /> constraint for flexibility.
/// </typeparam>
/// <remarks>
///     <para>
///         <b>Purpose</b>: Lightweight validation queries for command side
///     </para>
///     <para>
///         <b>Key Differences from IQuery</b>:
///         <list type="bullet">
///             <item>Does NOT extend IUseCase (simpler, less overhead)</item>
///             <item>Does NOT require CqrsOutput (flexible output types)</item>
///             <item>Used within commands for validation, not exposed to clients</item>
///             <item>Synchronous or asynchronous execution</item>
///         </list>
///     </para>
///     <para>
///         <b>Use Cases</b>:
///         <list type="bullet">
///             <item>Check if entity exists before deletion</item>
///             <item>Validate uniqueness constraints</item>
///             <item>Verify permissions or business rules</item>
///             <item>Query system state for command logic</item>
///         </list>
///     </para>
///     <para>
///         <b>Example</b>:
///         <code>
///         public record CheckAccountExistsInput(AccountId AccountId) : IInquiryInput;
///
///         public class CheckAccountExistsInquiry
///             : IInquiry&lt;CheckAccountExistsInput, bool&gt;
///         {
///             private readonly IArchive&lt;AccountSummary, AccountId&gt; _archive;
///
///             public CheckAccountExistsInquiry(
///                 IArchive&lt;AccountSummary, AccountId&gt; archive)
///             {
///                 _archive = archive;
///             }
///
///             public async Task&lt;bool&gt; QueryAsync(CheckAccountExistsInput input)
///             {
///                 var account = await _archive.FindByIdAsync(input.AccountId);
///                 return account != null;
///             }
///         }
///
///         // Usage within a command:
///         public class TransferMoneyCommand : ICommand&lt;TransferInput, TransferOutput&gt;
///         {
///             private readonly IInquiry&lt;CheckAccountExistsInput, bool&gt; _accountExistsInquiry;
///
///             public async Task&lt;TransferOutput&gt; ExecuteAsync(TransferInput input)
///             {
///                 // Validate source account exists
///                 var sourceExists = await _accountExistsInquiry.QueryAsync(
///                     new CheckAccountExistsInput(input.SourceAccountId)
///                 );
///
///                 if (!sourceExists)
///                 {
///                     return TransferOutput.Create()
///                         .SetMessage("Source account not found")
///                         .Fail();
///                 }
///
///                 // Continue with transfer logic...
///             }
///         }
///         </code>
///     </para>
///     <para>
///         <b>Example 2 - Validate Business Rules</b>:
///         <code>
///         // Input: Account ID and withdrawal amount
///         public record ValidateWithdrawalInput(
///             AccountId AccountId,
///             decimal Amount
///         ) : IInquiryInput;
///
///         // Output: Validation result with sufficient balance flag
///         public record ValidateWithdrawalOutput(
///             bool IsSufficient,
///             decimal CurrentBalance
///         );
///
///         // Inquiry: Check if account has sufficient balance for withdrawal
///         public class ValidateWithdrawalInquiry
///             : IInquiry&lt;ValidateWithdrawalInput, ValidateWithdrawalOutput&gt;
///         {
///             private readonly IArchive&lt;AccountSummary, AccountId&gt; _archive;
///
///             public async Task&lt;ValidateWithdrawalOutput&gt; QueryAsync(
///                 ValidateWithdrawalInput input)
///             {
///                 var account = await _archive.FindByIdAsync(input.AccountId);
///                 if (account == null)
///                     return new ValidateWithdrawalOutput(false, 0m);
///
///                 bool isSufficient = account.Balance >= input.Amount;
///                 return new ValidateWithdrawalOutput(isSufficient, account.Balance);
///             }
///         }
///
///         // Usage in command:
///         public class WithdrawMoneyCommand : ICommand&lt;WithdrawInput, WithdrawOutput&gt;
///         {
///             private readonly IInquiry&lt;ValidateWithdrawalInput, ValidateWithdrawalOutput&gt;
///                 _validateInquiry;
///
///             public async Task&lt;WithdrawOutput&gt; ExecuteAsync(WithdrawInput input)
///             {
///                 // Pre-validation using inquiry
///                 var validation = await _validateInquiry.QueryAsync(
///                     new ValidateWithdrawalInput(input.AccountId, input.Amount)
///                 );
///
///                 if (!validation.IsSufficient)
///                 {
///                     return WithdrawOutput.Create()
///                         .SetMessage($"Insufficient balance. Current: {validation.CurrentBalance}")
///                         .Fail();
///                 }
///
///                 // Proceed with withdrawal logic...
///             }
///         }
///         </code>
///     </para>
///     <para>
///         <b>Design Rationale</b>: Inquiries are intentionally kept simple and independent
///         of IUseCase to avoid the overhead of the use case infrastructure when performing
///         quick validation checks within commands.
///     </para>
/// </remarks>
/// <seealso cref="IInquiryInput" />
/// <seealso cref="ICommand{TInput,TOutput}" />
/// <seealso cref="IQuery{TInput,TOutput}" />
public interface IInquiry<in TInput, TOutput>
{
    /// <summary>
    ///     Executes the inquiry and returns the result.
    /// </summary>
    /// <param name="input">The input parameters for the inquiry.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the inquiry result.
    /// </returns>
    /// <remarks>
    ///     This method is typically used within commands to validate preconditions
    ///     or check system state before executing business logic.
    /// </remarks>
    Task<TOutput> QueryAsync(TInput input);
}
