using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NullKeys.A;

/// <summary>
/// Root <c>Query</c> for the <c>a</c> subgraph. Exposes
/// <c>bookContainers: [BookContainer]</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);
        descriptor
            .Field("bookContainers")
            .Type<ListType<BookContainerType>>()
            .Resolve(_ => AData.Books.Select(b => new BookContainer { Book = b }).ToArray());
    }
}
