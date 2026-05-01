using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Products;

public sealed class ProductBookType : ObjectType<BookEntity>
{
    protected override void Configure(IObjectTypeDescriptor<BookEntity> descriptor)
    {
        descriptor.Name("Book");

        descriptor
            .Implements<ProductInterfaceType>()
            .Implements<SimilarInterfaceType>();

        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();
        descriptor.Field(b => b.Sku).Type<StringType>();
        descriptor.Field(b => b.Dimensions).Shareable().Type<ProductDimensionType>();
        descriptor.Field(b => b.Hidden).Type<BooleanType>();

        descriptor
            .Field("createdBy")
            .Type<ProductUserType>()
            .Resolve(ctx =>
            {
                var book = ctx.Parent<BookEntity>();
                return ProductData.ResolveCreatedBy(book.CreatedByInternalId);
            });

        descriptor
            .Field("similar")
            .Type<ListType<ProductBookType>>()
            .Resolve(ctx =>
            {
                var book = ctx.Parent<BookEntity>();
                return ProductData.Books.Where(b => b.Id != book.Id).ToList();
            });

        descriptor
            .Field("publisherType")
            .Type<PublisherTypeUnion>()
            .Resolve(ctx =>
            {
                var book = ctx.Parent<BookEntity>();
                return book.Publisher;
            });

        descriptor.Field(b => b.CreatedByInternalId).Ignore();
        descriptor.Field(b => b.Publisher).Ignore();
        descriptor.Field(b => b.TypeName).Ignore();
    }

    private static BookEntity? ResolveById(string id)
        => ProductData.BooksById.TryGetValue(id, out var book) ? book : null;
}
