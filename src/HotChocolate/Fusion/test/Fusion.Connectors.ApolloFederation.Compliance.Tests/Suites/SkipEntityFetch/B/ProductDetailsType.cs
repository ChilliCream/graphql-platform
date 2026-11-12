using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SkipEntityFetch.B;

public sealed class ProductDetailsType : ObjectType<ProductDetails>
{
    protected override void Configure(IObjectTypeDescriptor<ProductDetails> descriptor)
    {
        descriptor.Field(d => d.Color).Type<StringType>();
        descriptor.Field(d => d.Size).Type<StringType>();
    }
}
