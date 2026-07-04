// ReSharper disable ConvertToLocalFunction

namespace EzDdd.Common.Tests;

public class ConverterTests
{
    [Fact]
    public void Converter_WhenUsingLambdaExpression_ConvertsSuccessfully()
    {
        // ReSharper disable once ConvertClosureToMethodGroup
        Converter<string, int> converter = s => int.Parse(s);

        int result = converter("42");

        Assert.Equal(42, result);
    }

    [Fact]
    public void Converter_WhenUsingMethodReference_ConvertsSuccessfully()
    {
        Converter<string, int> converter = int.Parse;

        int result = converter("123");

        Assert.Equal(123, result);
    }

    [Fact]
    public void Converter_WhenUsingMultiLineExpression_ConvertsSuccessfully()
    {
        Converter<User, UserDto> converter = user =>
        {
            // ReSharper disable once ConvertToLambdaExpression
            return new UserDto(user.Id, user.Name);
        };
        User user = new(1, "Alice");

        UserDto result = converter(user);

        Assert.Equal(1, result.Id);
        Assert.Equal("Alice", result.Name);
    }

    [Fact]
    public void Converter_WhenUsingMultipleConverters_MaintainsIndependence()
    {
        Converter<int, string> intToString = i => i.ToString();
        // ReSharper disable once ConvertClosureToMethodGroup
        Converter<string, int> stringToInt = s => int.Parse(s);

        string str = intToString(123);
        int num = stringToInt("456");

        Assert.Equal("123", str);
        Assert.Equal(456, num);
    }

    [Fact]
    public void Converter_WhenChainingConversions_WorksCorrectly()
    {
        Converter<int, string> intToString = i => i.ToString();
        // ReSharper disable once ConvertClosureToMethodGroup
        Converter<string, double> stringToDouble = s => double.Parse(s);

        string intermediate = intToString(42);
        double result = stringToDouble(intermediate);

        Assert.Equal(42.0, result);
    }

    [Fact]
    public void Converter_SupportsCovariance_AllowsDerivedTypeReturn()
    {
        Converter<string, string> stringToString = s => s.ToUpper();
        Converter<string, object> stringToObject = stringToString;

        object result = stringToObject("hello");

        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void Converter_SupportsContravariance_AllowsBaseTypeInput()
    {
        Converter<object, string> objectToString = obj => obj.ToString() ?? string.Empty;
        Converter<string, string> stringConverter = objectToString;

        string result = stringConverter("test");

        Assert.Equal("test", result);
    }

    [Fact]
    public void Converter_WhenUsingNullableReferenceTypes_HandlesNullAppropriately()
    {
        Converter<string?, string> nullableConverter = s => s ?? "default";

        string result = nullableConverter(null);

        Assert.Equal("default", result);
    }

    [Fact]
    public void Converter_WhenPerformingComplexTransformation_TransformsAllProperties()
    {
        Converter<Order, OrderSummary> converter = order => new OrderSummary(
            order.Id,
            $"{order.ProductName} (x{order.Quantity})",
            order.Quantity * order.UnitPrice
        );
        Order order = new(100, "Product A", 2, 50.0m);

        OrderSummary result = converter(order);

        Assert.Equal(100, result.OrderId);
        Assert.Equal("Product A (x2)", result.Description);
        Assert.Equal(100.0m, result.TotalAmount);
    }

    [Fact]
    public void Converter_WhenUsingStaticMethod_WorksAsMethodGroup()
    {
        Converter<string, string> converter = _ConvertToUpper;

        string result = converter("hello");

        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void Converter_WhenUsingInstanceMethod_WorksAsMethodGroup()
    {
        ConversionHelper helper = new();
        Converter<int, string> converter = helper.IntToString;

        string result = converter(42);

        Assert.Equal("Number: 42", result);
    }

    // Test helper methods
    private static string _ConvertToUpper(string input)
    {
        return input.ToUpper();
    }

    // Test domain classes
    private record User(int Id, string Name);

    private record UserDto(int Id, string Name);

    private record Order(int Id, string ProductName, int Quantity, decimal UnitPrice);

    private record OrderSummary(int OrderId, string Description, decimal TotalAmount);

    private class ConversionHelper
    {
        public string IntToString(int value)
        {
            return $"Number: {value}";
        }
    }
}
