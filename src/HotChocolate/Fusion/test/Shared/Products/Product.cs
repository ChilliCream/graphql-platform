using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Products;

[Node(IdField = nameof(Reviews.Product.Upc))]
public sealed record Product(int Upc, string Name, int Price, int Weight);
