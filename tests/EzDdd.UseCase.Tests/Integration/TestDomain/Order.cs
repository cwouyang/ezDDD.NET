using EzDdd.Entity;

namespace EzDdd.UseCase.Tests.Integration.TestDomain;

/// <summary>
///     Order aggregate root (state-sourced with Transactional Outbox).
/// </summary>
public sealed class Order : AggregateRoot<OrderId, IInternalDomainEvent>
{
    private readonly List<OrderItem> _items = [];

    // Parameterless constructor for OutboxMapper reconstruction
    public Order() { }

    // Constructor for creation
    public Order(OrderId id, string customerName)
    {
        Id = id;
        CustomerName = customerName;
        TotalAmount = 0;
        Status = OrderStatus.Draft;

        OrderCreated @event = new(Guid.NewGuid(), DateTimeOffset.UtcNow, id, customerName, 0);
        Apply(@event);
    }

    // Properties for testing
    public string CustomerName { get; } = string.Empty;

    public decimal TotalAmount { get; private set; }

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;

    public void AddItem(string productName, int quantity, decimal price)
    {
        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot add items to order in {Status} status");
        }

        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be positive");
        }

        if (price < 0)
        {
            throw new InvalidOperationException("Price cannot be negative");
        }

        OrderItem item = new(productName, quantity, price);
        _items.Add(item);
        TotalAmount += item.Subtotal;

        OrderItemAdded @event = new(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, productName, quantity, price);
        Apply(@event);
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot confirm order in {Status} status");
        }

        if (_items.Count == 0)
        {
            throw new InvalidOperationException("Cannot confirm order with no items");
        }

        Status = OrderStatus.Confirmed;

        OrderConfirmed @event = new(Guid.NewGuid(), DateTimeOffset.UtcNow, Id);
        Apply(@event);
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Order is already cancelled");
        }

        Status = OrderStatus.Cancelled;

        OrderCancelled @event = new(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, reason);
        Apply(@event);
    }
}

/// <summary>
///     Order item entity.
/// </summary>
public sealed class OrderItem
{
    public OrderItem(string productName, int quantity, decimal price)
    {
        ProductName = productName;
        Quantity = quantity;
        Price = price;
    }

    public string ProductName { get; }
    public int Quantity { get; }
    public decimal Price { get; }
    public decimal Subtotal => Quantity * Price;
}

/// <summary>
///     Order status enumeration.
/// </summary>
public enum OrderStatus
{
    Draft,
    Confirmed,
    Cancelled,
}
