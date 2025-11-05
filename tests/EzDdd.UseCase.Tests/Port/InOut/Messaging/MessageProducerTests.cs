using EzDdd.UseCase.Port.InOut.Messaging;

namespace EzDdd.UseCase.Tests.Port.InOut.Messaging;

public class MessageProducerTests
{
#region Interface Characteristics Tests

    [Fact]
    public void MessageProducer_IsGenericInterface()
    {
        TestMessageProducer<string> producer = new();

        Assert.NotNull(producer);
    }

#endregion

#region Message Posting Tests

    [Fact]
    public async Task MessageProducer_PostAsync_CanPostMessage()
    {
        TestMessageProducer<string> producer = new();

        await producer.PostAsync("test message");

        Assert.True(producer.WasPosted);
        Assert.Equal("test message", producer.LastPostedMessage);
    }

    [Fact]
    public async Task MessageProducer_PostAsync_IsAsync()
    {
        AsyncTestProducer<string> producer = new();

        Task task = producer.PostAsync("async test");

        Assert.False(task.IsCompleted); // Should not complete immediately
        await task;
        Assert.True(producer.WasPosted);
    }

    [Fact]
    public async Task MessageProducer_PostAsync_CanPostMultipleTimes()
    {
        TestMessageProducer<int> producer = new();

        await producer.PostAsync(1);
        await producer.PostAsync(2);
        await producer.PostAsync(3);

        Assert.Equal(3, producer.PostCount);
        Assert.Equal(3, producer.LastPostedMessage);
    }

    [Fact]
    public async Task MessageProducer_PostAsync_WithDifferentTypes()
    {
        TestMessageProducer<string> stringProducer = new();
        TestMessageProducer<int> intProducer = new();

        await stringProducer.PostAsync("text");
        await intProducer.PostAsync(42);

        Assert.Equal("text", stringProducer.LastPostedMessage);
        Assert.Equal(42, intProducer.LastPostedMessage);
    }

    [Fact]
    public async Task MessageProducer_PostAsync_WithComplexType()
    {
        TestMessageProducer<TestMessage> producer = new();
        TestMessage message = new() { Id = 1, Content = "complex" };

        await producer.PostAsync(message);

        Assert.Same(message, producer.LastPostedMessage);
    }

#endregion

#region Test Helper Classes

    /// <summary>
    ///     Simple test implementation of IMessageProducer for interface contract testing.
    /// </summary>
    private class TestMessageProducer<TMessage> : IMessageProducer<TMessage>
    {
        public bool WasPosted { get; private set; }
        public TMessage? LastPostedMessage { get; private set; }
        public int PostCount { get; private set; }

        public Task PostAsync(TMessage message)
        {
            WasPosted = true;
            LastPostedMessage = message;
            PostCount++;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            // No-op for test helper
        }
    }

    private class AsyncTestProducer<TMessage> : IMessageProducer<TMessage>
    {
        public bool WasPosted { get; private set; }

        public async Task PostAsync(TMessage message)
        {
            await Task.Delay(10); // Simulate async work
            WasPosted = true;
        }

        public void Dispose()
        {
            // No-op for test helper
        }
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }

#endregion
}