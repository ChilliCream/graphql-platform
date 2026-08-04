using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InterfaceObjectWithRequires.B;

/// <summary>
/// Root <c>Query</c> for the <c>b</c> subgraph. Exposes
/// <c>anotherUsers: [NodeWithName]</c> returning the seeded
/// <c>@interfaceObject</c> nodes.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);
        descriptor
            .Field("anotherUsers")
            .Type<ListType<NodeWithNameType>>()
            .Resolve(_ => BData.Nodes);
    }
}
