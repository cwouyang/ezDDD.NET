using EzDdd.UseCase.Port.InOut.Messaging;

namespace EzDdd.UseCase.Tests.Port.InOut.Messaging;

public class InMemoryMessageProducerTests
{
#region Initialization Tests

    [Fact]
    public void Constructor_InitializesEmptyQueue()
    {
        using InMemoryMessageProducer<string> producer = new();

        Assert.Empty(producer.PostedMessages);
    }

#endregion

#region Type-Specific Tests

    [Fact]
    public async Task PostAsync_ComplexType_StoresCorrectly()
    {
        var complexObject = new { Id = Guid.NewGuid(), Name = "Test", Value = 42 };
        using InMemoryMessageProducer<object> producer = new();

        await producer.PostAsync(complexObject);

        Assert.Single(producer.PostedMessages);
        object postedObject = producer.PostedMessages.First();
        Assert.Same(complexObject, postedObject);
    }

#endregion

#region PostAsync Tests

    [Fact]
    public async Task PostAsync_SingleMessage_StoresMessage()
    {
        using InMemoryMessageProducer<string> producer = new();
        const string message = "test-message";

        await producer.PostAsync(message);

        Assert.Single(producer.PostedMessages);
        Assert.Contains(message, producer.PostedMessages);
    }

    [Fact]
    public async Task PostAsync_MultipleMessages_StoresAllMessages()
    {
        using InMemoryMessageProducer<string> producer = new();
        string[] messages = ["message1", "message2", "message3"];

        foreach (string message in messages)
        {
            await producer.PostAsync(message);
        }

        Assert.Equal(3, producer.PostedMessages.Count);
        foreach (string message in messages)
        {
            Assert.Contains(message, producer.PostedMessages);
        }
    }

    [Fact]
    public async Task PostAsync_MultipleMessages_MaintainsOrder()
    {
        using InMemoryMessageProducer<string> producer = new();
        string[] messages = ["first", "second", "third"];

        foreach (string message in messages)
        {
            await producer.PostAsync(message);
        }

        List<string> postedMessages = producer.PostedMessages.ToList();
        Assert.Equal("first", postedMessages[0]);
        Assert.Equal("second", postedMessages[1]);
        Assert.Equal("third", postedMessages[2]);
    }

    [Fact]
    public async Task PostAsync_NullMessage_ThrowsArgumentNullException()
    {
        using InMemoryMessageProducer<string> producer = new();

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await producer.PostAsync(null!));
    }

    [Fact]
    public async Task PostAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        InMemoryMessageProducer<string> producer = new();
        producer.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await producer.PostAsync("message"));
    }

    [Fact]
    public async Task PostAsync_CompletesImmediately()
    {
        using InMemoryMessageProducer<string> producer = new();

        Task task = producer.PostAsync("message");

        Assert.True(task.IsCompleted);
        await task; // Ensure no exceptions
    }

#endregion

#region Thread Safety Tests

    [Fact]
    public async Task PostAsync_ConcurrentCalls_AllMessagesStored()
    {
        using InMemoryMessageProducer<string> producer = new();
        const int taskCount = 100;
        List<Task> tasks = [];

        for (int i = 0; i < taskCount; i++)
        {
            int index = i; // Capture for closure
            // ReSharper disable once AccessToDisposedClosure
            tasks.Add(Task.Run(async () => await producer.PostAsync($"message-{index}")));
        }

        await Task.WhenAll(tasks);

        Assert.Equal(taskCount, producer.PostedMessages.Count);
        for (int i = 0; i < taskCount; i++)
        {
            Assert.Contains($"message-{i}", producer.PostedMessages);
        }
    }

    [Fact]
    public async Task PostAsync_ConcurrentCallsWithDispose_NoDataCorruption()
    {
        InMemoryMessageProducer<string> producer = new();
        const int taskCount = 50;
        List<Task> tasks = [];

        for (int i = 0; i < taskCount; i++)
        {
            int index = i; // Capture for closure
            tasks.Add
            (
                Task.Run
                (async () =>
                    {
                        try
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            await producer.PostAsync($"message-{index}");
                        }
                        catch (ObjectDisposedException)
                        {
                            // Expected if disposed during execution
                        }
                    }
                )
            );
        }

        // Dispose midway through posting
        await Task.Delay(10);
        producer.Dispose();

        await Task.WhenAll(tasks);

        // Assert - No exceptions thrown, and PostedMessages returns empty array after disposal
        Assert.Empty(producer.PostedMessages);
    }

#endregion

#region Disposal Tests

    [Fact]
    public async Task Dispose_ClearsAllMessages()
    {
        InMemoryMessageProducer<string> producer = new();
        await producer.PostAsync("message1");
        await producer.PostAsync("message2");

        producer.Dispose();

        Assert.Empty(producer.PostedMessages);
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        InMemoryMessageProducer<string> producer = new();

        producer.Dispose();
        producer.Dispose(); // Second call should not throw
        producer.Dispose(); // Third call should not throw
    }

    [Fact]
    public async Task Dispose_PostedMessages_ReturnsEmptyAfterDisposal()
    {
        InMemoryMessageProducer<string> producer = new();
        await producer.PostAsync("message");

        producer.Dispose();

        Assert.Empty(producer.PostedMessages);
        Assert.IsAssignableFrom<IReadOnlyCollection<string>>(producer.PostedMessages);
    }

#endregion

#region PostedMessages Property Tests

    [Fact]
    public async Task PostedMessages_ReturnsAllPostedMessages()
    {
        using InMemoryMessageProducer<string> producer = new();
        await producer.PostAsync("message1");
        await producer.PostAsync("message2");
        await producer.PostAsync("message3");

        IReadOnlyCollection<string> postedMessages = producer.PostedMessages;

        Assert.Equal(3, postedMessages.Count);
        Assert.Contains("message1", postedMessages);
        Assert.Contains("message2", postedMessages);
        Assert.Contains("message3", postedMessages);
    }

    [Fact]
    public async Task PostedMessages_IsReadOnly_CannotModifyOriginal()
    {
        using InMemoryMessageProducer<string> producer = new();
        await producer.PostAsync("message1");
        await producer.PostAsync("message2");

        // ReSharper disable once CollectionNeverQueried.Local
        List<string> postedMessages = producer.PostedMessages.ToList();
        postedMessages.Add("message3"); // Modify the copy

        // Assert - Original is unchanged
        Assert.Equal(2, producer.PostedMessages.Count);
        Assert.DoesNotContain("message3", producer.PostedMessages);
    }

    [Fact]
    public async Task PostedMessages_MultipleCalls_ReturnsIndependentSnapshots()
    {
        using InMemoryMessageProducer<string> producer = new();
        await producer.PostAsync("message1");

        IReadOnlyCollection<string> snapshot1 = producer.PostedMessages;
        await producer.PostAsync("message2");
        IReadOnlyCollection<string> snapshot2 = producer.PostedMessages;

        Assert.Single(snapshot1);
        Assert.Equal(2, snapshot2.Count);
    }

#endregion
}