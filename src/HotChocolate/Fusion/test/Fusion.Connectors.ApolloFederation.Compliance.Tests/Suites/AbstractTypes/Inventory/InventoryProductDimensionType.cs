using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Inventory;

public sealed class InventoryProductDimensionType : ObjectType<ProductDimension>
{
    protected override void Configure(IObjectTypeDescriptor<ProductDimension> descriptor)
    {
        descriptor.Name("ProductDimension").Shareable();
        descriptor.Field(d => d.Size).Type<StringType>();
        descriptor.Field(d => d.Weight).Type<FloatType>();
    }
}
