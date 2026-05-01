using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Books;

public sealed class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();
        descriptor.Field(b => b.Title).Type<StringType>();
    }

    private static Book? ResolveById(string id)
        => BookData.BooksById.TryGetValue(id, out var book) ? book : null;
}
