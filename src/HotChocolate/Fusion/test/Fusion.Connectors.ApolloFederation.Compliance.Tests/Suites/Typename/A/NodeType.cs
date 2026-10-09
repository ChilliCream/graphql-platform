using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Typename.A;

/// <summary>
/// Descriptor for the <c>Node</c> interface in the <c>a</c> subgraph. A
/// single <c>id</c> field shared by <see cref="Oven"/> and
/// <see cref="Toaster"/>.
/// </summary>
public sealed class NodeType : InterfaceType<INode>
{
    protected override void Configure(IInterfaceTypeDescriptor<INode> descriptor)
    {
        descriptor.Name("Node");
        descriptor.Field(n => n.Id).Type<NonNullType<IdType>>();
    }
}
