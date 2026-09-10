using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionInterfaceDistributed.A;

/// <summary>
/// Descriptor for the <c>Product</c> union: <c>Oven | Toaster</c>.
/// </summary>
public sealed class ProductUnionType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("Product");
        descriptor.Type<OvenType>();
        descriptor.Type<ToasterType>();
    }
}
