using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Products;

public sealed class ProductDimensionType : ObjectType<ProductDimensionValue>
{
    protected override void Configure(IObjectTypeDescriptor<ProductDimensionValue> descriptor)
    {
        descriptor.Name("ProductDimension").Shareable();
        descriptor.Field(d => d.Size).Type<StringType>();
        descriptor.Field(d => d.Weight).Type<FloatType>();
    }
}
