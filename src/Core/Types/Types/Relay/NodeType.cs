namespace HotChocolate.Types.Relay
{
    public class NodeType
        : InterfaceType<INode>
    {
        protected override void Configure(
            IInterfaceTypeDescriptor<INode> descriptor)
        {
            descriptor.Name("Node");
            descriptor.Description(
                "The node interface is implemented by entities that have " +
                "a global unique identifier.");
            descriptor.Field("id").Type<NonNullType<IdType>>();
        }
    }
}
