using EzDdd.Entity;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.UseCase.Tests.Integration.TestDomain;

/// <summary>
///     Order persistence data structure (state sourcing with Transactional Outbox).
/// </summary>
public sealed class OrderData : IOutboxData<OrderId>
{
    // State fields (current snapshot)
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<OrderItemData> Items { get; set; } = [];
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public OrderId Id { get; set; } = default!;
    public long Version { get; set; }
    public IReadOnlyList<IDomainEvent> Events { get; set; } = Array.Empty<IDomainEvent>();
    public string StreamName { get; set; } = string.Empty;
}

/// <summary>
///     Order item data structure.
/// </summary>
public sealed class OrderItemData
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
