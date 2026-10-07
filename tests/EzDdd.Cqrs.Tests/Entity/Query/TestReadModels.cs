using EzDdd.Cqrs.Entity.Query;

namespace EzDdd.Cqrs.Tests.Entity.Query;

public sealed record CounterReadModel(string Id, int Count) : ReadModel;

public record BaseCounterReadModel(string Id, int Count) : ReadModel;

public sealed record VipCounterReadModel(string Id, int Count, string Tier) : BaseCounterReadModel(Id, Count);

public sealed record SmallMemoryReadModel(string Id) : ReadModel(SmallMemoryReadModel.Capacity)
{
    public const int Capacity = 2;
}

public sealed record InvalidCapacityReadModel(int Capacity) : ReadModel(Capacity);

public sealed record ThreeIdMemoryReadModel(string Id) : ReadModel(ThreeIdMemoryReadModel.Capacity)
{
    public const int Capacity = 3;
}
