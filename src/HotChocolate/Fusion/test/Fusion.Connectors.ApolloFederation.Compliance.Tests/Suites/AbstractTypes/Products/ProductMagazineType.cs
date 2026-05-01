using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Products;

public sealed class ProductMagazineType : ObjectType<MagazineEntity>
{
    protected override void Configure(IObjectTypeDescriptor<MagazineEntity> descriptor)
    {
        descriptor.Name("Magazine");

        descriptor
            .Implements<ProductInterfaceType>()
            .Implements<SimilarInterfaceType>();

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(m => m.Id).Type<NonNullType<IdType>>();
        descriptor.Field(m => m.Sku).Type<StringType>();
        descriptor.Field(m => m.Dimensions).Shareable().Type<ProductDimensionType>();
        descriptor.Field(m => m.Hidden).Type<BooleanType>();

        descriptor
            .Field("createdBy")
            .Type<ProductUserType>()
            .Resolve(ctx =>
            {
                var magazine = ctx.Parent<MagazineEntity>();
                return ProductData.ResolveCreatedBy(magazine.CreatedByInternalId);
            });

        descriptor
            .Field("similar")
            .Type<ListType<ProductMagazineType>>()
            .Resolve(ctx =>
            {
                var magazine = ctx.Parent<MagazineEntity>();
                return ProductData.Magazines.Where(m => m.Id != magazine.Id).ToList();
            });

        descriptor
            .Field("publisherType")
            .Type<PublisherTypeUnion>()
            .Resolve(ctx =>
            {
                var magazine = ctx.Parent<MagazineEntity>();
                return magazine.Publisher;
            });

        descriptor.Field(m => m.CreatedByInternalId).Ignore();
        descriptor.Field(m => m.Publisher).Ignore();
        descriptor.Field(m => m.TypeName).Ignore();
    }

    private static MagazineEntity? ResolveById(string id)
        => ProductData.MagazinesById.TryGetValue(id, out var magazine) ? magazine : null;
}
