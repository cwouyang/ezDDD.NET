using System.Reflection;

namespace EzDdd.Entity.Tests;

public class ValueObjectTests
{
    [Fact]
    public void IValueObject_RecordType_HasStructuralEquality()
    {
        MoneyRecord money1 = new(100m, "USD");
        MoneyRecord money2 = new(100m, "USD");
        MoneyRecord money3 = new(200m, "USD");

        // Records provide structural equality automatically
        Assert.Equal(money1, money2);
        Assert.NotEqual(money1, money3);
    }

    [Fact]
    public void IValueObject_RecordType_IsImmutable()
    {
        MoneyRecord money = new(100m, "USD");

        // Record properties are init-only (cannot be modified)
        // money.Amount = 200m; // Compilation error - this is the point
        Assert.Equal(100m, money.Amount);
    }

    [Fact]
    public void IValueObject_ClassType_CanImplementManualEquality()
    {
        MoneyClass money1 = new(100m, "USD");
        MoneyClass money2 = new(100m, "USD");
        MoneyClass money3 = new(200m, "USD");

        // Manual Equals implementation works
        Assert.Equal(money1, money2);
        Assert.NotEqual(money1, money3);
    }

    [Fact]
    public void IValueObject_IsMarkerInterface_HasNoMembers()
    {
        MethodInfo[] methods = typeof(IValueObject).GetMethods();
        PropertyInfo[] properties = typeof(IValueObject).GetProperties();

        // Pure marker interface has zero methods and properties
        Assert.Empty(methods);
        Assert.Empty(properties);
    }

    [Fact]
    public void IValueObject_CanBeUsedAsTypeConstraint()
    {
        MoneyRecord recordMoney = new(100m, "USD");
        MoneyClass classMoney = new(100m, "USD");

        bool recordResult = _ProcessValueObject(recordMoney);
        bool classResult = _ProcessValueObject(classMoney);

        // Both implementations work with generic constraint
        Assert.True(recordResult);
        Assert.True(classResult);
    }

    [Fact]
    public void IValueObject_RecordWithMethod_WithExpression_CreatesNewInstance()
    {
        MoneyRecord original = new(100m, "USD");

        MoneyRecord modified = original with { Amount = 200m };

        Assert.Equal(100m, original.Amount); // Original unchanged
        Assert.Equal(200m, modified.Amount); // New instance created
        Assert.NotEqual(original, modified);
    }

    // Helper method demonstrating generic constraint usage
    private static bool _ProcessValueObject<T>(T valueObject)
        where T : IValueObject
    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    {
        return valueObject is not null;
    }

    // Record-based value object (recommended approach)
    private sealed record MoneyRecord(decimal Amount, string Currency) : IValueObject;

    // Class-based value object (manual equality)
    private sealed class MoneyClass(decimal amount, string currency) : IValueObject
    {
        public decimal Amount { get; } = amount;
        public string Currency { get; } = currency;

        public override bool Equals(object? obj)
        {
            return obj is MoneyClass other
                && Amount == other.Amount
                && string.Equals(Currency, other.Currency, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Amount, Currency);
        }
    }
}
