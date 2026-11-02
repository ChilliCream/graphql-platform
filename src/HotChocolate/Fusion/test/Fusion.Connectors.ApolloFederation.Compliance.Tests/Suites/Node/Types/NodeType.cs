using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Node.Types;

/// <summary>
/// Descriptor for the <c>Node</c> interface in the <c>types</c> subgraph.
/// </summary>
public sealed class NodeType : InterfaceType<INode>
{
    protected override void Configure(IInterfaceTypeDescriptor<INode> descriptor)
    {
        descriptor.Name("Node");
        descriptor.Field(n => n.Id).Type<NonNullType<IdType>>();
    }
}
