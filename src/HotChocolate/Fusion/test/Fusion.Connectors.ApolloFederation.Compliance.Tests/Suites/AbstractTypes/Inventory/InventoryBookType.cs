using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Inventory;

public sealed class InventoryBookType : ObjectType<InventoryBook>
{
    protected override void Configure(IObjectTypeDescriptor<InventoryBook> descriptor)
    {
        descriptor.Name("Book");

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();
        descriptor.Field(b => b.Dimensions).External().Type<InventoryProductDimensionType>();

        descriptor
            .Field("delivery")
            .Argument("zip", a => a.Type<StringType>())
            .Type<DeliveryEstimatesType>()
            .Requires("dimensions { size weight }")
            .Resolve(ctx =>
            {
                var book = ctx.Parent<InventoryBook>();
                var zip = ctx.ArgumentValue<string?>("zip");
                return InventoryData.ComputeDelivery(zip, book.Dimensions);
            });
    }

    private static InventoryBook? ResolveById(string id)
        => InventoryData.KnownBookIds.Contains(id)
            ? new InventoryBook { Id = id }
            : null;
}
