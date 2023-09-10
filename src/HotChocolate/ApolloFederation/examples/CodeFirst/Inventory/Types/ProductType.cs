using HotChocolate.Types;

namespace Inventory;

public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .ExtendServiceType()
            .Key("upc")
            .ResolveReferenceWith(t => GetProduct(default!));

        descriptor
            .Field(t => t.Upc)
            .External();

        descriptor
            .Field(t => t.Weight)
            .External();

        descriptor
            .Field(t => t.Price)
            .External();

        // free for expensive items, else the estimate is based on weight
        descriptor
            .Field("shippingEstimate")
            .Requires("price weight")
            .Resolve(
                ctx => ctx.Parent<Product>().Price <= 1000
                    ? (int)(ctx.Parent<Product>().Weight * 0.5)
                    : 0);
    }

    private static Product GetProduct(string upc) => new(upc);
}
