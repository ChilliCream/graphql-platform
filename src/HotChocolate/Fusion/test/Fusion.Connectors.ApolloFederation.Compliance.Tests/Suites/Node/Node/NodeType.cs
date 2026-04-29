using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Node.Node;

/// <summary>
/// Descriptor for the <c>Node</c> interface: the single <c>id</c> field
/// shared by all implementers.
/// </summary>
public sealed class NodeType : InterfaceType<INode>
{
    protected override void Configure(IInterfaceTypeDescriptor<INode> descriptor)
    {
        descriptor.Name("Node");
        descriptor.Field(n => n.Id).Type<NonNullType<IdType>>();
    }
}
