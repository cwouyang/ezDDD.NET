namespace EzDdd.Cqrs.Query;

/// <summary>
///     <c>IProjection</c> is an interface for building read models from the query database.
///     Projections aggregate and transform data from <see cref="IArchive{TData, TId}" />
///     into optimized view models for queries.
/// </summary>
/// <typeparam name="TInput">The projection input type, should extend <see cref="IProjectionInput" />.</typeparam>
/// <typeparam name="TOutput">The projection output type (flexible, typically a view model or DTO).</typeparam>
/// <remarks>
///     <para>
///         <b>Key Characteristics</b>:
///     </para>
///     <list type="bullet">
///         <item>
///             <b>Does NOT extend IUseCase</b>: Projections are specialized query builders,
///             simpler than full use cases.
///         </item>
///         <item>
///             <b>Flexible Output</b>: Output can be any type - view models, DTOs, records.
///             Does NOT require <see cref="CqrsOutput{T}" />.
///         </item>
///         <item>
///             <b>Used Within Queries</b>: Queries may use projections to build complex
///             views from multiple read models.
///         </item>
///         <item>
///             <b>Input Constraint</b>: Input should extend <see cref="IProjectionInput" />
///             for type safety.
///         </item>
///     </list>
///     <para>
///         <b>Difference from IQuery</b>:
///     </para>
///     <list type="bullet">
///         <item><c>IQuery</c>: Full use case for read operations, extends IUseCase, requires CqrsOutput</item>
///         <item><c>IProjection</c>: View builder for queries, standalone, flexible output</item>
///     </list>
///     <para>
///         <b>Not to be confused with IProjector</b>: IProjector is a background service
///         that maintains read models, while IProjection builds views from read models.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         // Read model (stored in Archive)
///         public record CustomerReadModel(Guid CustomerId, string Name, string Email);
///         public record OrderReadModel(Guid OrderId, Guid CustomerId, decimal Total);
/// 
///         // View model (projection output)
///         public record CustomerSummaryView(
///             Guid CustomerId,
///             string Name,
///             string Email,
///             int OrderCount,
///             decimal TotalPurchases
///         );
/// 
///         // Projection input
///         public record CustomerSummaryInput(Guid CustomerId) : IProjectionInput;
/// 
///         // Projection implementation
///         public class CustomerSummaryProjection
///             : IProjection&lt;CustomerSummaryInput, CustomerSummaryView&gt;
///         {
///             private readonly IArchive&lt;CustomerReadModel, Guid&gt; _customerArchive;
///             private readonly IArchive&lt;OrderReadModel, Guid&gt; _orderArchive;
/// 
///             public async Task&lt;CustomerSummaryView&gt; QueryAsync(CustomerSummaryInput input)
///             {
///                 var customer = await _customerArchive.FindByIdAsync(input.CustomerId);
///                 // Query orders for this customer...
///                 int orderCount = ...; // Count orders
///                 decimal totalPurchases = ...; // Sum order totals
/// 
///                 return new CustomerSummaryView(
///                     customer.CustomerId,
///                     customer.Name,
///                     customer.Email,
///                     orderCount,
///                     totalPurchases
///                 );
///             }
///         }
/// 
///         // Use within query
///         public class GetCustomerSummaryQuery
///             : IQuery&lt;GetCustomerSummaryInput, GetCustomerSummaryOutput&gt;
///         {
///             private readonly IProjection&lt;CustomerSummaryInput, CustomerSummaryView&gt; _projection;
/// 
///             public async Task&lt;GetCustomerSummaryOutput&gt; ExecuteAsync(
///                 GetCustomerSummaryInput input)
///             {
///                 var view = await _projection.QueryAsync(
///                     new CustomerSummaryInput(input.CustomerId));
/// 
///                 return GetCustomerSummaryOutput.Create()
///                     .SetCustomerSummary(view)
///                     .Succeed();
///             }
///         }
///     </code>
/// </example>
public interface IProjection<in TInput, TOutput>
    where TInput : IProjectionInput
{
    /// <summary>
    ///     Executes the projection query to build a read model view.
    /// </summary>
    /// <param name="input">The projection input.</param>
    /// <returns>A task containing the projected view model.</returns>
    Task<TOutput> QueryAsync(TInput input);
}