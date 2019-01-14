namespace HotChocolate.Types.Relay
{
    // TODO : Add description
    public class NodeType
        : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name("Node");
            descriptor.Field("id").Type<NonNullType<IdType>>();
        }
    }
}
