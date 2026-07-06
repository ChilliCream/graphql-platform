using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Inventory;

public sealed class InventoryMagazineType : ObjectType<InventoryMagazine>
{
    protected override void Configure(IObjectTypeDescriptor<InventoryMagazine> descriptor)
    {
        descriptor.Name("Magazine");

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(m => m.Id).Type<NonNullType<IdType>>();
        descriptor.Field(m => m.Dimensions).External().Type<InventoryProductDimensionType>();

        descriptor
            .Field("delivery")
            .Argument("zip", a => a.Type<StringType>())
            .Type<DeliveryEstimatesType>()
            .Requires("dimensions { size weight }")
            .Resolve(ctx =>
            {
                var magazine = ctx.Parent<InventoryMagazine>();
                var zip = ctx.ArgumentValue<string?>("zip");
                return InventoryData.ComputeDelivery(zip, magazine.Dimensions);
            });
    }

    private static InventoryMagazine? ResolveById(string id)
        => InventoryData.KnownMagazineIds.Contains(id)
            ? new InventoryMagazine { Id = id }
            : null;
}
