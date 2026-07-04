using EzDdd.Entity;

namespace EzDdd.UseCase.Tests.Integration.TestDomain;

/// <summary>
///     Money value object with currency.
/// </summary>
public sealed record Money(decimal Amount, string Currency = "USD") : IValueObject
{
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException($"Cannot add different currencies: {Currency} and {other.Currency}");
        }

        return this with
        {
            Amount = Amount + other.Amount,
        };
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot subtract different currencies: {Currency} and {other.Currency}"
            );
        }

        return this with
        {
            Amount = Amount - other.Amount,
        };
    }

    public bool IsPositive()
    {
        return Amount > 0;
    }

    public bool IsNegative()
    {
        return Amount < 0;
    }

    public bool IsZero()
    {
        return Amount == 0;
    }

    public override string ToString()
    {
        return $"{Amount:F2} {Currency}";
    }
}
