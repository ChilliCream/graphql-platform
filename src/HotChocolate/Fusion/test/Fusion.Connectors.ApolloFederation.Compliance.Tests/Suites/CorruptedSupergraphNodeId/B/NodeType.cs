using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.B;

/// <summary>
/// Descriptor for the <c>Node</c> interface in subgraph <c>b</c>.
/// </summary>
public sealed class NodeType : InterfaceType<INode>
{
    protected override void Configure(IInterfaceTypeDescriptor<INode> descriptor)
    {
        descriptor.Name("Node");
        descriptor.Field(n => n.Id).Type<NonNullType<IdType>>();
    }
}
