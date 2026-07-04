using EzDdd.Entity;
using EzDdd.UseCase.Exceptions;
using EzDdd.UseCase.Port.Out;
using EzDdd.UseCase.Tests.Integration.TestDomain;

namespace EzDdd.UseCase.Tests.Integration;

public sealed class StateSourcingWorkflowTests : IDisposable
{
    private readonly InMemoryOutboxPeer _peer;
    private readonly OutboxRepository<Order, OrderData, OrderId> _repository;

    public StateSourcingWorkflowTests()
    {
        // Register domain events for serialization
        DomainEventTypeMapper.Register<OrderCreated>("OrderCreated");
        DomainEventTypeMapper.Register<OrderItemAdded>("OrderItemAdded");
        DomainEventTypeMapper.Register<OrderConfirmed>("OrderConfirmed");
        DomainEventTypeMapper.Register<OrderCancelled>("OrderCancelled");

        _peer = new InMemoryOutboxPeer();
        _repository = new OutboxRepository<Order, OrderData, OrderId>(_peer, new OrderMapper());
    }

    public void Dispose()
    {
        _peer.Clear();
    }

    [Fact]
    public async Task CreateOrder_AndSave_StoresStateAndEvents()
    {
        OrderId orderId = new("order-001");
        Order order = new(orderId, "Alice Johnson");

        await _repository.SaveAsync(order);

        Order? loaded = await _repository.FindByIdAsync(orderId);
        Assert.NotNull(loaded);
        Assert.Equal("Alice Johnson", loaded.CustomerName);
        Assert.Equal(0m, loaded.TotalAmount);
        Assert.Empty(loaded.Items);
        Assert.Equal(OrderStatus.Draft, loaded.Status);
        Assert.Equal(0, loaded.Version);
    }

    [Fact]
    public async Task AddItems_AndSave_UpdatesState()
    {
        OrderId orderId = new("order-002");
        Order order = new(orderId, "Bob Smith");
        await _repository.SaveAsync(order);

        order.AddItem("Product A", 2, 10m);
        order.AddItem("Product B", 1, 25m);
        await _repository.SaveAsync(order);

        Order? loaded = await _repository.FindByIdAsync(orderId);
        Assert.NotNull(loaded);
        Assert.Equal(45m, loaded.TotalAmount); // 2*10 + 1*25
        Assert.Equal(2, loaded.Items.Count);
        Assert.Equal("Product A", loaded.Items[0].ProductName);
        Assert.Equal("Product B", loaded.Items[1].ProductName);
        Assert.Equal(2, loaded.Version); // Version 2 after two additional operations
    }

    [Fact]
    public async Task SaveAndLoad_RestoresCompleteState()
    {
        OrderId orderId = new("order-003");
        Order order = new(orderId, "Charlie Davis");
        order.AddItem("Laptop", 1, 1200m);
        order.AddItem("Mouse", 2, 30m);
        order.AddItem("Keyboard", 1, 80m);
        order.Confirm();

        await _repository.SaveAsync(order);
        Order? loaded = await _repository.FindByIdAsync(orderId);

        Assert.NotNull(loaded);
        Assert.Equal("Charlie Davis", loaded.CustomerName);
        Assert.Equal(1340m, loaded.TotalAmount); // 1200 + 2*30 + 80
        Assert.Equal(3, loaded.Items.Count);
        Assert.Equal(OrderStatus.Confirmed, loaded.Status);
        Assert.Equal(4, loaded.Version); // Create + 3 items + confirm
    }

    [Fact]
    public async Task TransactionalOutbox_StoresEventsWithState()
    {
        OrderId orderId = new("order-004");
        Order order = new(orderId, "Diana Lee");
        order.AddItem("Book", 3, 15m);
        await _repository.SaveAsync(order);

        OrderData? data = await _peer.FindByIdAsync(orderId);

        Assert.NotNull(data);
        Assert.Equal("Diana Lee", data.CustomerName);
        Assert.Equal(45m, data.TotalAmount);
        Assert.Equal(2, data.Events.Count); // OrderCreated + OrderItemAdded
        Assert.IsType<OrderCreated>(data.Events[0]);
        Assert.IsType<OrderItemAdded>(data.Events[1]);
    }

    [Fact]
    public async Task ConfirmOrder_ChangesStatus()
    {
        OrderId orderId = new("order-005");
        Order order = new(orderId, "Eve Martinez");
        order.AddItem("Chair", 4, 50m);
        await _repository.SaveAsync(order);

        order.Confirm();
        await _repository.SaveAsync(order);

        Order? loaded = await _repository.FindByIdAsync(orderId);
        Assert.NotNull(loaded);
        Assert.Equal(OrderStatus.Confirmed, loaded.Status);
        Assert.Equal(2, loaded.Version);
    }

    [Fact]
    public async Task CancelOrder_UpdatesStatus()
    {
        OrderId orderId = new("order-006");
        Order order = new(orderId, "Frank Garcia");
        order.AddItem("Table", 1, 300m);
        await _repository.SaveAsync(order);

        order.Cancel("Customer request");
        await _repository.SaveAsync(order);

        Order? loaded = await _repository.FindByIdAsync(orderId);
        Assert.NotNull(loaded);
        Assert.Equal(OrderStatus.Cancelled, loaded.Status);
    }

    [Fact]
    public void BusinessRuleViolation_ThrowsException()
    {
        OrderId orderId = new("order-007");
        Order order = new(orderId, "Grace Kim");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
        {
            order.Confirm();
        });
        Assert.Contains("no items", exception.Message);
    }

    [Fact]
    public async Task ConcurrentSave_OptimisticLock_ThrowsException()
    {
        OrderId orderId = new("order-008");
        Order order1 = new(orderId, "Henry Wilson");
        await _repository.SaveAsync(order1);

        // Load same order twice (simulating concurrent access)
        Order? order2 = await _repository.FindByIdAsync(orderId);
        Order? order3 = await _repository.FindByIdAsync(orderId);

        Assert.NotNull(order2);
        Assert.NotNull(order3);

        order2.AddItem("Item A", 1, 100m);
        await _repository.SaveAsync(order2);

        order3.AddItem("Item B", 1, 200m);
        await Assert.ThrowsAsync<RepositorySaveException>(async () =>
        {
            await _repository.SaveAsync(order3);
        });
    }

    [Fact]
    public async Task LoadNonExistentOrder_ReturnsNull()
    {
        OrderId orderId = new("non-existent");

        Order? loaded = await _repository.FindByIdAsync(orderId);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task CompleteWorkflow_MultipleOperations_Success()
    {
        OrderId orderId = new("order-010");
        Order order = new(orderId, "Ivy Chen");

        order.AddItem("Monitor", 2, 300m);
        order.AddItem("HDMI Cable", 2, 15m);
        order.AddItem("Stand", 2, 50m);
        await _repository.SaveAsync(order);

        // Load and continue operations
        Order? loaded = await _repository.FindByIdAsync(orderId);
        Assert.NotNull(loaded);
        Assert.Equal(730m, loaded.TotalAmount); // 2*300 + 2*15 + 2*50
        Assert.Equal(3, loaded.Version); // Create + 3 items

        // Confirm the order
        loaded.Confirm();
        await _repository.SaveAsync(loaded);

        // Reload and verify final state
        Order? reloaded = await _repository.FindByIdAsync(orderId);
        Assert.NotNull(reloaded);
        Assert.Equal(OrderStatus.Confirmed, reloaded.Status);
        Assert.Equal(730m, reloaded.TotalAmount);
        Assert.Equal(3, reloaded.Items.Count);
        Assert.Equal(4, reloaded.Version); // + Confirm
    }
}
