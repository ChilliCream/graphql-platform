using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithArgument.B;

/// <summary>
/// Root <c>Query</c> for the <c>b</c> subgraph. Exposes the
/// <c>products: [Product]</c> list field.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("products")
            .Type<ListType<ProductType>>()
            .Resolve(_ => BData.Products);
    }
}
