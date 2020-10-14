namespace HotChocolate.Types.Relay
{
    public class NodeType
        : InterfaceType<INode>
    {
        protected override void Configure(
            IInterfaceTypeDescriptor<INode> descriptor)
        {
            descriptor
                .Name(Names.Node)
                .Description(
                    "The node interface is implemented by entities that have " +
                    "a global unique identifier.");
            
            descriptor
                .Field(Names.Id)
                .Type<NonNullType<IdType>>();
        }

        public static class Names
        {
            public static NameString Node { get; } = "Node";

            public static NameString Id { get; } = "id";
        }
    }
}
