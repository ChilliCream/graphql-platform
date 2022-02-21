using HotChocolate.ApolloFederation;

namespace Inventory;

[ExtendServiceType]
public class Product
{
    public Product(string upc)
    {
        Upc = upc;
    }

    [Key]
    [External]
    public string Upc { get; }

    [External]
    public int Weight { get; private set; }

    [External]
    public int Price { get; private set; }

    public bool InStock { get; } = true;

    // free for expensive items, else the estimate is based on weight
    [Requires("price weight")]
    public int GetShippingEstimate()
        => Price > 1000 ? 0 : (int)(Weight * 0.5);

    [ReferenceResolver]
    public static Product GetProduct(string upc)
        => new Product(upc);
}
