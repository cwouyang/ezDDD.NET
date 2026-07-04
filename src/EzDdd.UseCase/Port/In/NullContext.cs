namespace EzDdd.UseCase.Port.In;

/// <summary>
///     <c>NullContext</c> is a null object pattern implementation for reconcilers that
///     do not require any input context. Use this instead of <c>null</c> or <c>object</c>
///     to maintain type safety.
/// </summary>
/// <remarks>
///     This class uses the singleton pattern to avoid unnecessary object allocations.
///     Access the single instance via <see cref="Instance" />.
/// </remarks>
public sealed class NullContext
{
    /// <summary>
    ///     Gets the singleton instance of <c>NullContext</c>.
    /// </summary>
    public static readonly NullContext Instance = new();

    /// <summary>
    ///     Prevents external instantiation. Use <see cref="Instance" /> instead.
    /// </summary>
    private NullContext() { }
}
