using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Products;

public sealed class SimilarInterfaceType : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("Similar");
        descriptor.Field("similar").Type<ListType<ProductInterfaceType>>();
    }
}
