using HotChocolate.Types.Relay;
using Microsoft.AspNetCore.Http.Features;

namespace HotChocolate.Fusion.Shared.Products;

[Node(IdField = nameof(Reviews.Product.Upc))]
public sealed record Product(int Upc, string Name, int Price, int Weight)
{
    public int Repeat(int num) => num;
}
