using EzDdd.Entity;

namespace EzDdd.UseCase.Tests.Integration.TestDomain;

/// <summary>
///     Bank account identifier (value object).
/// </summary>
public sealed record AccountId(string Value) : IValueObject
{
    public override string ToString()
    {
        return Value;
    }
}
