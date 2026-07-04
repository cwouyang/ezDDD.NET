using EzDdd.Entity;

namespace EzDdd.UseCase.Tests.Integration.TestDomain;

/// <summary>
///     Order identifier (value object).
/// </summary>
public sealed record OrderId(string Value) : IValueObject
{
    public override string ToString()
    {
        return Value;
    }
}
