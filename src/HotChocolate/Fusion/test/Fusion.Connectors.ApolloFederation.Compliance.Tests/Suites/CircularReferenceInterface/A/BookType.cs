using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.CircularReferenceInterface.A;

public sealed class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor
            .Implements<ProductInterfaceType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();
        descriptor.Field(b => b.Price).External().Type<FloatType>();

        descriptor
            .Field("samePriceProduct")
            .Type<BookType>()
            .Provides("price")
            .Resolve(ctx =>
            {
                var book = ctx.Parent<Book>();
                return AData.Books.FirstOrDefault(
                    b => b.Price is not null
                        && b.Price == book.Price
                        && !string.Equals(b.Id, book.Id, StringComparison.Ordinal));
            });
    }

    private static Book? ResolveById(string id)
        => AData.BooksById.TryGetValue(id, out var book) ? book : null;
}
