using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionInterfaceDistributed.B;

/// <summary>
/// Descriptor for the <c>WithWarranty</c> interface in subgraph <c>b</c>.
/// </summary>
public sealed class WithWarrantyType : InterfaceType<IWithWarranty>
{
    protected override void Configure(IInterfaceTypeDescriptor<IWithWarranty> descriptor)
    {
        descriptor.Name("WithWarranty");
        descriptor.Field(w => w.Warranty).Type<IntType>();
    }
}
