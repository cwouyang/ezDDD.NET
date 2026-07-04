using EzDdd.UseCase.Port.In;

namespace EzDdd.Cqrs.Query;

/// <summary>
///     <c>IQuery</c> is a marker interface for query operations (read side).
///     Queries are responsible for reading system state without modifying it,
///     typically from optimized read models in an <see cref="IArchive{TData, TId}" />.
/// </summary>
/// <typeparam name="TInput">The query input type.</typeparam>
/// <typeparam name="TOutput">The query output type, must extend <see cref="CqrsOutput{T}" />.</typeparam>
/// <remarks>
///     <para>
///         In CQRS, queries represent read operations that retrieve system state.
///         They are separated from commands (write operations) to enable:
///     </para>
///     <list type="bullet">
///         <item>Independent scaling of read and write workloads</item>
///         <item>Optimized data models for each responsibility (denormalized read models)</item>
///         <item>Clear separation of concerns</item>
///     </list>
///     <para>
///         Queries typically:
///     </para>
///     <list type="number">
///         <item>Access read models from <see cref="IArchive{TData, TId}" /> (query database)</item>
///         <item>May use <see cref="IProjection{TInput, TOutput}" /> to build complex views</item>
///         <item>Return data without side effects (no state modification)</item>
///     </list>
///     <para>
///         Read models in the query database are kept eventually consistent with the write model
///         via <see cref="IProjector{TInput}" /> background services that listen to domain events.
///     </para>
///     <para>
///         <b>Generic Variance</b>: This interface uses contravariant input (<c>in TInput</c>)
///         to enable flexible query composition, but does NOT use covariant output (<c>out TOutput</c>)
///         because it would conflict with the <c>new()</c> constraint on the base
///         <see cref="IUseCase{TInput,TOutput}" /> interface and the <c>CqrsOutput&lt;TOutput&gt;</c> constraint.
///         Covariance requires output-only usage, but <c>new()</c> requires instantiation (input operation).
///         See ADR-0021 (Generic Variance Annotations) for detailed explanation of this design decision.
///     </para>
///     <para>
///         <b>Extensibility</b>:
///     </para>
///     <list type="bullet">
///         <item>Implement this interface to create custom query use cases for read operations</item>
///         <item>Can be used with dependency injection frameworks (e.g., Microsoft.Extensions.DependencyInjection)</item>
///         <item>Compatible with middleware patterns for cross-cutting concerns (caching, logging, performance monitoring)</item>
///         <item>Works with <see cref="IArchive{TData,TId}" /> for optimized read model access</item>
///         <item>Can compose with <see cref="IProjection{TInput,TOutput}" /> for complex view building</item>
///         <item>Can apply decorator pattern for query result transformation or caching</item>
///     </list>
/// </remarks>
/// <example>
///     <code>
///         public class GetAccountSummaryQuery
///             : IQuery&lt;GetAccountSummaryInput, GetAccountSummaryOutput&gt;
///         {
///             private readonly IArchive&lt;AccountSummaryReadModel, AccountId&gt; _archive;
///
///             public async Task&lt;GetAccountSummaryOutput&gt; ExecuteAsync(
///                 GetAccountSummaryInput input)
///             {
///                 var readModel = await _archive.FindByIdAsync(input.AccountId);
///
///                 if (readModel == null)
///                 {
///                     throw new UseCaseFailureException("Account not found");
///                 }
///
///                 return GetAccountSummaryOutput.Create()
///                     .SetAccountNumber(readModel.AccountNumber)
///                     .SetBalance(readModel.Balance)
///                     .Succeed();
///             }
///         }
///     </code>
/// </example>
public interface IQuery<in TInput, TOutput> : IUseCase<TInput, TOutput>
    where TInput : IInput
    where TOutput : CqrsOutput<TOutput>, new()
{
    // Marker interface - no additional methods beyond IUseCase.ExecuteAsync()
}
