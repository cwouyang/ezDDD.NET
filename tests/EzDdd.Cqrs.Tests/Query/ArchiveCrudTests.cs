using EzDdd.Cqrs.Tests.Query.TestHelpers;

namespace EzDdd.Cqrs.Tests.Query;

public class ArchiveCrudTests
{
#region Thread Safety Tests

    [Fact]
    public async Task ConcurrentOperations_ShouldBeThreadSafe()
    {
        InMemoryArchive<TestReadModel, int> archive = new(x => x.IntId);
        List<Task> tasks = [];

        for (int i = 0; i < 100; i++)
        {
            int id = i;
            tasks.Add
            (
                Task.Run
                (async () =>
                    {
                        TestReadModel item = new($"item-{id}", $"Data {id}", id);
                        await archive.SaveAsync(item);
                    }
                )
            );
        }

        await Task.WhenAll(tasks);

        Assert.Equal(100, archive.Count);

        for (int i = 0; i < 100; i++)
        {
            TestReadModel? result = await archive.FindByIdAsync(i);
            Assert.NotNull(result);
        }
    }

#endregion

    private record TestReadModel(string Id, string Data, int IntId = 0);

#region FindByIdAsync Tests

    [Fact]
    public async Task FindByIdAsync_WhenItemExists_ShouldReturnItem()
    {
        InMemoryArchive<TestReadModel, string> archive = new(x => x.Id);
        TestReadModel readModel = new("test-1", "Test Data");
        await archive.SaveAsync(readModel);

        TestReadModel? result = await archive.FindByIdAsync("test-1");

        Assert.NotNull(result);
        Assert.Equal("test-1", result.Id);
        Assert.Equal("Test Data", result.Data);
    }

    [Fact]
    public async Task FindByIdAsync_WhenItemDoesNotExist_ShouldReturnNull()
    {
        InMemoryArchive<TestReadModel, string> archive = new(x => x.Id);

        TestReadModel? result = await archive.FindByIdAsync("non-existent");

        Assert.Null(result);
    }

#endregion

#region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_WhenNewItem_ShouldInsertItem()
    {
        InMemoryArchive<TestReadModel, string> archive = new(x => x.Id);
        TestReadModel readModel = new("test-1", "Test Data");

        await archive.SaveAsync(readModel);

        TestReadModel? result = await archive.FindByIdAsync("test-1");
        Assert.NotNull(result);
        Assert.Equal("Test Data", result.Data);
        Assert.Equal(1, archive.Count);
    }

    [Fact]
    public async Task SaveAsync_WhenExistingItem_ShouldUpdateItem()
    {
        InMemoryArchive<TestReadModel, string> archive = new(x => x.Id);
        TestReadModel original = new("test-1", "Original Data");
        await archive.SaveAsync(original);

        TestReadModel updated = new("test-1", "Updated Data");
        await archive.SaveAsync(updated);

        TestReadModel? result = await archive.FindByIdAsync("test-1");
        Assert.NotNull(result);
        Assert.Equal("Updated Data", result.Data);
        Assert.Equal(1, archive.Count);
    }

    [Fact]
    public async Task SaveAsync_WhenCalledMultipleTimes_ShouldBeIdempotent()
    {
        InMemoryArchive<TestReadModel, string> archive = new(x => x.Id);
        TestReadModel readModel = new("test-1", "Test Data");

        await archive.SaveAsync(readModel);
        await archive.SaveAsync(readModel);
        await archive.SaveAsync(readModel);

        TestReadModel? result = await archive.FindByIdAsync("test-1");
        Assert.NotNull(result);
        Assert.Equal(1, archive.Count);
    }

    [Fact]
    public async Task SaveAsync_WhenDataIsNull_ShouldThrowArgumentNullException()
    {
        InMemoryArchive<TestReadModel, string> archive = new(x => x.Id);

        await Assert.ThrowsAsync<ArgumentNullException>(() => archive.SaveAsync(null!));
    }

#endregion

#region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenItemExists_ShouldRemoveItem()
    {
        InMemoryArchive<TestReadModel, string> archive = new(x => x.Id);
        TestReadModel readModel = new("test-1", "Test Data");
        await archive.SaveAsync(readModel);

        await archive.DeleteAsync(readModel);

        TestReadModel? result = await archive.FindByIdAsync("test-1");
        Assert.Null(result);
        Assert.Equal(0, archive.Count);
    }

    [Fact]
    public async Task DeleteAsync_WhenItemDoesNotExist_ShouldNotThrowException()
    {
        InMemoryArchive<TestReadModel, string> archive = new(x => x.Id);
        TestReadModel readModel = new("non-existent", "Test Data");

        await archive.DeleteAsync(readModel);

        Assert.Equal(0, archive.Count);
    }

    [Fact]
    public async Task DeleteAsync_WhenCalledMultipleTimes_ShouldBeIdempotent()
    {
        InMemoryArchive<TestReadModel, string> archive = new(x => x.Id);
        TestReadModel readModel = new("test-1", "Test Data");
        await archive.SaveAsync(readModel);

        await archive.DeleteAsync(readModel);
        await archive.DeleteAsync(readModel);
        await archive.DeleteAsync(readModel);

        TestReadModel? result = await archive.FindByIdAsync("test-1");
        Assert.Null(result);
        Assert.Equal(0, archive.Count);
    }

    [Fact]
    public async Task DeleteAsync_WhenDataIsNull_ShouldThrowArgumentNullException()
    {
        InMemoryArchive<TestReadModel, string> archive = new(x => x.Id);

        await Assert.ThrowsAsync<ArgumentNullException>(() => archive.DeleteAsync(null!));
    }

#endregion

#region Complete CRUD Flow Tests

    [Fact]
    public async Task CompleteCrudFlow_ShouldWorkCorrectly()
    {
        InMemoryArchive<TestReadModel, string> archive = new(x => x.Id);

        TestReadModel item1 = new("item-1", "Data 1");
        await archive.SaveAsync(item1);

        TestReadModel item2 = new("item-2", "Data 2");
        await archive.SaveAsync(item2);
        Assert.Equal(2, archive.Count);

        TestReadModel? foundItem1 = await archive.FindByIdAsync("item-1");
        Assert.NotNull(foundItem1);
        Assert.Equal("Data 1", foundItem1.Data);

        TestReadModel updatedItem1 = new("item-1", "Updated Data 1");
        await archive.SaveAsync(updatedItem1);
        TestReadModel? refoundItem1 = await archive.FindByIdAsync("item-1");
        Assert.Equal("Updated Data 1", refoundItem1!.Data);
        Assert.Equal(2, archive.Count);

        await archive.DeleteAsync(item1);
        Assert.Equal(1, archive.Count);
        TestReadModel? deletedItem = await archive.FindByIdAsync("item-1");
        Assert.Null(deletedItem);

        TestReadModel? stillExists = await archive.FindByIdAsync("item-2");
        Assert.NotNull(stillExists);
    }

    [Fact]
    public async Task MultipleItems_ShouldBeStoredIndependently()
    {
        InMemoryArchive<TestReadModel, string> archive = new(x => x.Id);

        TestReadModel[] items =
        [
            new("id-1", "Data 1"), new("id-2", "Data 2"), new("id-3", "Data 3")
        ];

        foreach (TestReadModel item in items)
        {
            await archive.SaveAsync(item);
        }

        Assert.Equal(3, archive.Count);

        TestReadModel? retrieved = await archive.FindByIdAsync("id-2");
        Assert.NotNull(retrieved);
        Assert.Equal("Data 2", retrieved.Data);
    }

#endregion
}