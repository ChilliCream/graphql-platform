using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SkipEntityFetch.B;

public sealed class ProductInventoryType : ObjectType<ProductInventory>
{
    protected override void Configure(IObjectTypeDescriptor<ProductInventory> descriptor)
    {
        descriptor.Field(i => i.InStock).Type<BooleanType>();
        descriptor.Field(i => i.Warehouse).Type<StringType>();
    }
}
