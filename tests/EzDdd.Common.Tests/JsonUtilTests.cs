using System.Text;
using System.Text.Json;

namespace EzDdd.Common.Tests;

public class JsonUtilTests
{
    #region AsString Tests

    [Fact]
    public void AsString_WhenGivenSimpleObject_SerializesSuccessfully()
    {
        User user = new() { Id = 1, Name = "Alice" };

        string json = JsonUtil.AsString(user);

        Assert.Contains("\"Id\":1", json);
        Assert.Contains("\"Name\":\"Alice\"", json);
    }

    [Fact]
    public void AsString_WhenGivenRecordType_SerializesSuccessfully()
    {
        UserDto dto = new(42, "Bob");

        string json = JsonUtil.AsString(dto);

        Assert.Contains("\"Id\":42", json);
        Assert.Contains("\"Name\":\"Bob\"", json);
    }

    [Fact]
    public void AsString_WhenGivenDateTime_UsesIso8601Format()
    {
        DateTime timestamp = new(2025, 10, 31, 12, 30, 45, DateTimeKind.Utc);
        OrderCreated @event = new() { OrderId = 100, CreatedAt = timestamp };

        string json = JsonUtil.AsString(@event);

        Assert.Contains("2025-10-31", json);
        Assert.Contains("12:30:45", json);
        Assert.DoesNotContain("1730379045", json); // Should not be Unix timestamp
    }

    [Fact]
    public void AsString_WhenGivenPublicFields_SerializesFields()
    {
        EntityWithPublicFields entity = new() { Id = 10, Data = "Field Value" };

        string json = JsonUtil.AsString(entity);

        Assert.Contains("\"Id\":10", json);
        Assert.Contains("\"Data\":\"Field Value\"", json);
    }

    #endregion

    #region ReadValue Tests

    [Theory]
    [InlineData("{\"Id\":1,\"Name\":\"Alice\"}")]
    [InlineData("{\"id\":1,\"name\":\"Alice\"}")]
    [InlineData("{\"ID\":1,\"NAME\":\"Alice\"}")]
    public void ReadValue_WhenGivenValidJson_DeserializesSuccessfully(string json)
    {
        User? user = JsonUtil.ReadValue<User>(json);

        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public void ReadValue_WhenGivenRecordType_DeserializesSuccessfully()
    {
        const string json = "{\"Id\":42,\"Name\":\"Bob\"}";

        UserDto? dto = JsonUtil.ReadValue<UserDto>(json);

        Assert.NotNull(dto);
        Assert.Equal(42, dto.Id);
        Assert.Equal("Bob", dto.Name);
    }

    [Theory]
    [InlineData("{invalid json}")]
    [InlineData("{")]
    [InlineData("")]
    public void ReadValue_WhenGivenInvalidJson_ThrowsException(string invalidJson)
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            JsonUtil.ReadValue<User>(invalidJson)
        );

        Assert.Contains("Failed to deserialize JSON", exception.Message);
    }

    #endregion

    #region ReadAs Tests

    [Fact]
    public void ReadAs_WhenGivenByteArray_DeserializesSuccessfully()
    {
        byte[] bytes = "{\"Id\":1,\"Name\":\"Alice\"}"u8.ToArray();

        User? user = JsonUtil.ReadAs<User>(bytes);

        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public void ReadAs_WhenGivenEmptyByteArray_ThrowsException()
    {
        byte[] emptyBytes = [];

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            JsonUtil.ReadAs<User>(emptyBytes)
        );

        Assert.Contains("Failed to deserialize byte array", exception.Message);
    }

    #endregion

    #region ReadTree Tests

    [Fact]
    public void ReadTree_WhenGivenString_ParsesSuccessfully()
    {
        const string json = "{\"name\":\"Alice\",\"age\":30}";

        using JsonDocument doc = JsonUtil.ReadTree(json);

        Assert.Equal("Alice", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal(30, doc.RootElement.GetProperty("age").GetInt32());
    }

    [Fact]
    public void ReadTree_WhenGivenByteArray_ParsesSuccessfully()
    {
        byte[] bytes = "{\"name\":\"Bob\",\"age\":25}"u8.ToArray();

        using JsonDocument doc = JsonUtil.ReadTree(bytes);

        Assert.Equal("Bob", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal(25, doc.RootElement.GetProperty("age").GetInt32());
    }

    [Fact]
    public void ReadTree_WhenGivenInvalidJson_ThrowsException()
    {
        const string invalidJson = "{invalid}";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            JsonUtil.ReadTree(invalidJson)
        );

        Assert.Contains("Failed to parse JSON", exception.Message);
    }

    [Fact]
    public void ReadTree_WhenGivenComplexStructure_NavigatesCorrectly()
    {
        const string json = "{\"user\":{\"id\":1,\"profile\":{\"name\":\"Alice\"}}}";

        using JsonDocument doc = JsonUtil.ReadTree(json);

        JsonElement user = doc.RootElement.GetProperty("user");
        JsonElement profile = user.GetProperty("profile");
        Assert.Equal(1, user.GetProperty("id").GetInt32());
        Assert.Equal("Alice", profile.GetProperty("name").GetString());
    }

    #endregion

    #region DeepCopy Tests

    [Fact]
    public void DeepCopy_WhenGivenSimpleObject_CreatesIndependentCopy()
    {
        User original = new() { Id = 1, Name = "Alice" };

        User? copy = JsonUtil.DeepCopy(original);

        Assert.NotNull(copy);
        Assert.Equal(original.Id, copy.Id);
        Assert.Equal(original.Name, copy.Name);

        copy.Name = "Bob";
        Assert.Equal("Alice", original.Name);
    }

    [Fact]
    public void DeepCopy_WhenGivenRecordType_CreatesIndependentCopy()
    {
        MutableRecord original = new() { Id = 1, Data = "Original" };

        MutableRecord? copy = JsonUtil.DeepCopy(original);

        Assert.NotNull(copy);
        Assert.Equal(original.Id, copy.Id);
        Assert.Equal(original.Data, copy.Data);
        Assert.NotSame(original, copy);
    }

    [Fact]
    public void DeepCopy_WhenGivenNull_ReturnsDefault()
    {
        User? original = null;

        User? copy = JsonUtil.DeepCopy(original);

        Assert.Null(copy);
    }

    [Fact]
    public void DeepCopy_WhenGivenComplexObject_CopiesAllProperties()
    {
        Order original = new()
        {
            Id = 100,
            ProductName = "Widget",
            Quantity = 5,
            UnitPrice = 10.50m,
        };

        Order? copy = JsonUtil.DeepCopy(original);

        Assert.NotNull(copy);
        Assert.Equal(original.Id, copy.Id);
        Assert.Equal(original.ProductName, copy.ProductName);
        Assert.Equal(original.Quantity, copy.Quantity);
        Assert.Equal(original.UnitPrice, copy.UnitPrice);
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void AsString_AndReadValue_RoundTripSuccessfully()
    {
        User original = new() { Id = 42, Name = "Charlie" };

        string json = JsonUtil.AsString(original);
        User? restored = JsonUtil.ReadValue<User>(json);

        Assert.NotNull(restored);
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.Name, restored.Name);
    }

    [Fact]
    public void ReadAs_AndAsString_RoundTripSuccessfully()
    {
        UserDto original = new(99, "Diana");
        string json = JsonUtil.AsString(original);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        UserDto? restored = JsonUtil.ReadAs<UserDto>(bytes);

        Assert.NotNull(restored);
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.Name, restored.Name);
    }

    #endregion

    // Test domain classes
    private class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private record UserDto(int Id, string Name);

    private class OrderCreated
    {
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class EntityWithPublicFields
    {
        public string Data = string.Empty;
        public int Id;
    }

    private class Order
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    private record MutableRecord
    {
        public int Id { get; set; }
        public string Data { get; set; } = string.Empty;
    }
}
