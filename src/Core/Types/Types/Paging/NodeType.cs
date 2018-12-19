namespace HotChocolate.Types.Paging
{
    // TODO : Add description
    // TODO : Consider moving this type to a different namespace
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
