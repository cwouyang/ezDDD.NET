using EzDdd.Cqrs.Query;
using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.Cqrs.Command;

/// <summary>
///     <c>ICommand</c> is a marker interface for command operations in CQRS.
///     <para>
///         Commands represent the write side of CQRS, responsible for modifying
///         system state. They execute business logic, update aggregates, and
///         produce domain events.
///     </para>
/// </summary>
/// <typeparam name="TInput">
///     The input type for the command, must implement <see cref="IInput" />.
/// </typeparam>
/// <typeparam name="TOutput">
///     The output type for the command, must extend <see cref="CqrsOutput{T}" />.
/// </typeparam>
/// <remarks>
///     <para>
///         <b>Purpose</b>: Semantic marker for write operations
///     </para>
///     <para>
///         <b>Design Pattern</b>: Command pattern for CQRS write side
///     </para>
///     <para>
///         <b>Key Characteristics</b>:
///         <list type="bullet">
///             <item>Inherits <see cref="IUseCase{TInput,TOutput}.ExecuteAsync" /> method</item>
///             <item>Modifies system state (creates, updates, deletes aggregates)</item>
///             <item>Uses <see cref="IRepository{TAggregate,TId,TEvent}" /> for persistence</item>
///             <item>Returns <see cref="CqrsOutput{T}" /> with operation result</item>
///             <item>May use <see cref="IInquiry{TInput,TOutput}" /> for validation</item>
///         </list>
///     </para>
///     <para>
///         <b>Example</b>:
///         <code>
///         public record CreateAccountInput(
///             AccountId AccountId,
///             string AccountNumber,
///             Money InitialBalance
///         ) : IInput;
/// 
///         public class CreateAccountOutput : CqrsOutput&lt;CreateAccountOutput&gt;
///         {
///             public string AccountNumber { get; set; } = string.Empty;
///         }
/// 
///         public class CreateAccountCommand
///             : ICommand&lt;CreateAccountInput, CreateAccountOutput&gt;
///         {
///             private readonly IRepository&lt;BankAccount, AccountId&gt; _repository;
/// 
///             public CreateAccountCommand(IRepository&lt;BankAccount, AccountId&gt; repository)
///             {
///                 _repository = repository;
///             }
/// 
///             public async Task&lt;CreateAccountOutput&gt; ExecuteAsync(CreateAccountInput input)
///             {
///                 var account = BankAccount.Create(
///                     input.AccountId,
///                     input.AccountNumber,
///                     input.InitialBalance
///                 );
/// 
///                 await _repository.SaveAsync(account);
/// 
///                 return CreateAccountOutput.Create()
///                     .SetId(input.AccountId.Value)
///                     .SetAccountNumber(input.AccountNumber)
///                     .SetMessage("Account created successfully")
///                     .Succeed();
///             }
///         }
///         </code>
///     </para>
///     <para>
///         <b>CQRS Pattern</b>: Commands are separated from queries to enable:
///         <list type="bullet">
///             <item>Independent scaling of read and write operations</item>
///             <item>Optimized data models for each side</item>
///             <item>Clear separation of concerns</item>
///             <item>Event-driven architecture support</item>
///         </list>
///     </para>
///     <para>
///         <b>Generic Variance</b>: This interface uses contravariant input (<c>in TInput</c>)
///         to enable flexible command composition, but does NOT use covariant output (<c>out TOutput</c>)
///         because it would conflict with the <c>new()</c> constraint on the base
///         <see cref="IUseCase{TInput,TOutput}" /> interface and the <c>CqrsOutput&lt;TOutput&gt;</c> constraint.
///         Covariance requires output-only usage, but <c>new()</c> requires instantiation (input operation).
///         See ADR-0021 (Generic Variance Annotations) for detailed explanation of this design decision.
///     </para>
///     <para>
///         <b>Extensibility</b>:
///     </para>
///     <list type="bullet">
///         <item>Implement this interface to create custom command use cases for write operations</item>
///         <item>Can be used with dependency injection frameworks (e.g., Microsoft.Extensions.DependencyInjection)</item>
///         <item>Compatible with middleware patterns for cross-cutting concerns (logging, validation, authorization)</item>
///         <item>
///             Domain events persisted by commands are published by an independent application-layer
///             relay service (Transactional Outbox pattern) - commands never publish events directly
///         </item>
///         <item>Can compose multiple commands using decorator or chain-of-responsibility patterns</item>
///     </list>
/// </remarks>
/// <seealso cref="IUseCase{TInput,TOutput}" />
/// <seealso cref="IQuery{TInput,TOutput}" />
/// <seealso cref="IInquiry{TInput,TOutput}" />
public interface ICommand<in TInput, TOutput> : IUseCase<TInput, TOutput>
    where TInput : IInput
    where TOutput : CqrsOutput<TOutput>, new()
{
    // Marker interface - inherits ExecuteAsync(TInput input) from IUseCase
}