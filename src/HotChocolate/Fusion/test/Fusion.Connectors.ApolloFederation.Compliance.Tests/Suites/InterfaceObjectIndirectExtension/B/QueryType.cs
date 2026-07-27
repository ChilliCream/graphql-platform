using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.B;

/// <summary>
/// Root <c>Query</c> for the <c>b</c> subgraph. Exposes <c>author: Author</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("author")
            .Type<AuthorType>()
            .Resolve(_ => BData.DefaultAuthor());
    }
}
