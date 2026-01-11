namespace EzDdd.Integration.Tests.TestDomain;

/// <summary>
///     Identity value object for MetadataTestAggregate.
/// </summary>
public sealed record MetadataTestId(string Value)
{
    public override string ToString()
    {
        return Value;
    }
}