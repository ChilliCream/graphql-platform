using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Books;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("books")
            .Type<ListType<BookType>>()
            .Resolve(_ => BookData.Books.Select(
                b => new Book { Id = b.Id, Title = b.Title }).ToList());
    }
}
