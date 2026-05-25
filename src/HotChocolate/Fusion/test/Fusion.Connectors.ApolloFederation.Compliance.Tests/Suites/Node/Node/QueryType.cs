using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Node.Node;

/// <summary>
/// Root <c>Query</c> for the <c>node</c> subgraph. Exposes
/// <c>productNode: Node</c> and <c>categoryNode: Node</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("productNode")
            .Type<NodeType>()
            .Resolve(_ => (INode)NodeData.Products[0]);

        descriptor
            .Field("categoryNode")
            .Type<NodeType>()
            .Resolve(_ => (INode)NodeData.Categories[0]);
    }
}
