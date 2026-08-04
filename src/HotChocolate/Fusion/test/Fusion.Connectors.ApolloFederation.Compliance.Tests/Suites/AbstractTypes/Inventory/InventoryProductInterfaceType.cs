using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Inventory;

public sealed class InventoryProductInterfaceType : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("Product");

        descriptor.Field("id").Type<NonNullType<IdType>>();
        descriptor.Field("dimensions").Type<InventoryProductDimensionType>();
        descriptor.Field("delivery")
            .Argument("zip", a => a.Type<StringType>())
            .Type<DeliveryEstimatesType>();
    }
}
