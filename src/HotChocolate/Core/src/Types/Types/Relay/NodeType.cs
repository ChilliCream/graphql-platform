using HotChocolate.Properties;

namespace HotChocolate.Types.Relay;

public sealed class NodeType : InterfaceType<INode>
{
    protected override void Configure(
        IInterfaceTypeDescriptor<INode> descriptor)
    {
        descriptor
            .Name(Names.Node)
            .Description(TypeResources.NodeType_TypeDescription);

        descriptor
            .Field(Names.Id)
            .Type<NonNullType<IdType>>();
    }

    public static class Names
    {
        public static string Node => "Node";

        public static string Id => "id";
    }
}
