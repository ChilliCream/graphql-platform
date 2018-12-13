namespace HotChocolate.Types.Paging
{
    public class ConnectionType<T>
        : ObjectType<IConnection>
        where T : INamedOutputType, new()
    {
        protected override void Configure(
            IObjectTypeDescriptor<IConnection> descriptor)
        {
            // TODO : Fix this with the new schema builder
            descriptor.Name($"{new T().Name}Connection");

            descriptor.Field(t => t.PageInfo)
                .Type<NonNullType<PageInfoType>>();

            descriptor.Field(t => t.GetEdgesAsync(default))
                .Type<ListType<NonNullType<EdgeType<T>>>>();
        }
    }
}
