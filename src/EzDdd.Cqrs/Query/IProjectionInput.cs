namespace EzDdd.Cqrs.Query;

/// <summary>
///     <c>IProjectionInput</c> is a marker interface for projection input data.
///     <para>
///         Projection is a lightweight query interface used to build read models
///         from the query database. This marker interface provides type safety and
///         semantic clarity for projection input data.
///     </para>
/// </summary>
/// <remarks>
///     <para>
///         <b>Purpose</b>: Type safety for projection inputs
///     </para>
///     <para>
///         <b>Usage</b>: Implement this interface on DTOs or records that serve as
///         projection input data. Projections are typically used to transform or
///         aggregate data from the read model (Archive) into view models optimized
///         for specific query scenarios.
///     </para>
///     <para>
///         <b>Example</b>:
///         <code>
///         public record GetAccountSummaryProjectionInput(
///             AccountId AccountId,
///             bool IncludeTransactionHistory
///         ) : IProjectionInput;
///
///         public class AccountSummaryProjection
///             : IProjection&lt;GetAccountSummaryProjectionInput, AccountSummaryViewModel&gt;
///         {
///             public async Task&lt;AccountSummaryViewModel&gt; QueryAsync(
///                 GetAccountSummaryProjectionInput input)
///             {
///                 // Build view model from archive data
///                 var account = await _archive.FindByIdAsync(input.AccountId);
///                 return new AccountSummaryViewModel(account);
///             }
///         }
///         </code>
///     </para>
/// </remarks>
public interface IProjectionInput
{
    // Pure marker interface - no methods
}
