using EzDdd.UseCase.Port.In;
using EzDdd.UseCase.Tests.Integration.TestDomain;

namespace EzDdd.Cqrs.Tests.Integration.TestDomain;

/// <summary>
///     Input for getting account summary query.
/// </summary>
/// <param name="AccountId">The account identifier to query.</param>
public sealed record GetAccountSummaryInput(AccountId AccountId) : IInput;