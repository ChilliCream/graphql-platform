using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Products;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("products")
            .Type<ListType<ProductInterfaceType>>()
            .Resolve(_ => ProductData.AllProducts.Select<IProductEntity, object>(p =>
            {
                if (p is BookEntity book)
                {
                    return book;
                }

                return (MagazineEntity)p;
            }).ToList());

        descriptor
            .Field("similar")
            .Argument("id", a => a.Type<NonNullType<IdType>>())
            .Type<ListType<ProductInterfaceType>>()
            .Resolve(ctx =>
            {
                var id = ctx.ArgumentValue<string>("id");

                if (!ProductData.AllProductsById.TryGetValue(id, out var product))
                {
                    return new List<object>();
                }

                var results = new List<object>();

                foreach (var p in ProductData.AllProducts)
                {
                    if (p.Id != product.Id && p.TypeName == product.TypeName)
                    {
                        if (p is BookEntity book)
                        {
                            results.Add(book);
                        }
                        else
                        {
                            results.Add((MagazineEntity)p);
                        }
                    }
                }

                return results;
            });
    }
}
