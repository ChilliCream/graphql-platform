using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.CircularReferenceInterface.A;

public sealed class ProductInterfaceType : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("Product");
        descriptor.Field("samePriceProduct").Type<ProductInterfaceType>();
    }
}
