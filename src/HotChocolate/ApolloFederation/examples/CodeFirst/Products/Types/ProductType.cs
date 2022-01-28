using HotChocolate.Types;

namespace Products;

public class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("upc")
            .ResolveReferenceWith(t => GetProduct(default!, default!));
    }

    private static Product GetProduct(
        string upc,
        ProductRepository productRepository)
        => productRepository.GetById(upc);
}
