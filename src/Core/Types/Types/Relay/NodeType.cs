namespace HotChocolate.Types.Relay
{
    public class NodeType
        : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name("Node");
            descriptor.Description(
                "The node interface is implemented by entities that have " +
                "a gloabl unique identifier.");
            descriptor.Field("id").Type<NonNullType<IdType>>();
        }
    }
}
