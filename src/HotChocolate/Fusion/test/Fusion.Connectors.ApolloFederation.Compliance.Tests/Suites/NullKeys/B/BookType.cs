using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NullKeys.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Book</c> entity in the
/// <c>b</c> subgraph (two keys: <c>id</c> and <c>upc</c>). The reference
/// resolver mirrors the audit's null-handling: it returns <see langword="null"/>
/// when the looked-up book has id <c>3</c>, simulating a partial entity
/// store where one row is unavailable.
/// </summary>
public sealed class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor
            .Key("upc")
            .ResolveReferenceWith(_ => ResolveByUpc(default!));

        descriptor.Field(b => b.Id).Type<NonNullType<IdType>>();
        descriptor.Field(b => b.Upc).Type<NonNullType<IdType>>();
    }

    private static Book? ResolveById(string id)
    {
        var book = BData.Books.FirstOrDefault(
            b => string.Equals(b.Id, id, StringComparison.Ordinal));
        return ApplyNullPolicy(book);
    }

    private static Book? ResolveByUpc(string upc)
    {
        var book = BData.Books.FirstOrDefault(
            b => string.Equals(b.Upc, upc, StringComparison.Ordinal));
        return ApplyNullPolicy(book);
    }

    private static Book? ApplyNullPolicy(Book? book)
    {
        if (book is null)
        {
            return null;
        }

        // Mirror the audit fixture: subgraph 'b' deliberately returns null
        // for the book with id "3" so the planner has to short-circuit the
        // downstream entity call to subgraph 'c'.
        return string.Equals(book.Id, "3", StringComparison.Ordinal) ? null : book;
    }
}
