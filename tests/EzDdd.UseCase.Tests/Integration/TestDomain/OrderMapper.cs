using System.Reflection;
using EzDdd.UseCase.Port.Out;

namespace EzDdd.UseCase.Tests.Integration.TestDomain;

/// <summary>
///     Mapper for converting between Order aggregate and OrderData.
/// </summary>
public sealed class OrderMapper : OutboxMapper<Order, OrderData, OrderId>
{
    public override OrderData ToData(Order aggregate)
    {
        return new OrderData
        {
            Id = aggregate.Id,
            Version = aggregate.Version,
            Events = aggregate.GetDomainEvents().ToList(),
            StreamName = $"order-{aggregate.Id.Value}",
            CustomerName = aggregate.CustomerName,
            TotalAmount = aggregate.TotalAmount,
            Items = aggregate
                .Items.Select(item => new OrderItemData
                {
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    Price = item.Price,
                })
                .ToList(),
            Status = aggregate.Status,
        };
    }

    public override Order ToDomain(OrderData data)
    {
        // Create aggregate using parameterless constructor
        Order order = new();

        // Restore state using reflection (accessing private fields)
        FieldInfo? idField = typeof(Order).BaseType!.GetField(
            "<Id>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        idField!.SetValue(order, data.Id);

        FieldInfo? versionField = typeof(Order).BaseType!.GetField(
            "<Version>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        versionField!.SetValue(order, data.Version);

        FieldInfo? customerNameField = typeof(Order).GetField(
            "<CustomerName>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        customerNameField!.SetValue(order, data.CustomerName);

        FieldInfo? totalAmountField = typeof(Order).GetField(
            "<TotalAmount>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        totalAmountField!.SetValue(order, data.TotalAmount);

        FieldInfo? itemsField = typeof(Order).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);
        List<OrderItem> itemsList = (List<OrderItem>)itemsField!.GetValue(order)!;
        itemsList.Clear();
        foreach (OrderItemData itemData in data.Items)
        {
            itemsList.Add(new OrderItem(itemData.ProductName, itemData.Quantity, itemData.Price));
        }

        FieldInfo? statusField = typeof(Order).GetField(
            "<Status>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        statusField!.SetValue(order, data.Status);

        return order;
    }
}
