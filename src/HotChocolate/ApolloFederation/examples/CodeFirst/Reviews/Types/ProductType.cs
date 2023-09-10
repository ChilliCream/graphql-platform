using HotChocolate.Types;

namespace Reviews;

public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .ExtendServiceType()
            .Key("upc")
            .ResolveReferenceWith(t => GetByIdAsync(default!));

        descriptor
            .Field(t => t.Upc)
            .External();

        descriptor
            .Field("reviews")
            .Type<NonNullType<ListType<NonNullType<ReviewType>>>>()
            .Resolve(async ctx =>
            {
                var repository = ctx.Service<ReviewRepository>();
                var upc = ctx.Parent<Product>().Upc;
                return await repository.GetByProductUpcAsync(upc);
            });
    }

    private static Product GetByIdAsync(string upc) => new(upc);
}
