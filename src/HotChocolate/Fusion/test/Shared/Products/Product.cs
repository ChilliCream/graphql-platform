using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Products;

[Node(IdField = nameof(Id))]
public sealed record Product(int Id, string Name, int Price, int Weight, ProductDimension Dimension)
{
    public int Repeat(int num) => num;

    public SomeData RepeatData(SomeData data) => data;
}
