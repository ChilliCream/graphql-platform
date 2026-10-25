using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Typename.A;

/// <summary>
/// Descriptor for the <c>Product</c> union in the <c>a</c> subgraph
/// (<c>union Product = Oven | Toaster</c>).
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
