using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Products;

public sealed class PublisherTypeUnion : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("PublisherType");
        descriptor.Type<ProductAgencyType>();
        descriptor.Type<SelfType>();
    }
}
