namespace HotChocolate.Types.Paging
{
    public class EdgeType<T>
        : ObjectType<IEdge>
        where T : INamedOutputType, new()
    {
        protected override void Configure(IObjectTypeDescriptor<IEdge> descriptor)
        {
            // TODO : Fix this with the new schema builder
            descriptor.Name($"{new T().Name}Edge");

            descriptor.Field(t => t.Cursor)
                .Type<NonNullType<StringType>>();

            descriptor.Field(t => t.Node)
                .Type<T>();
        }
    }
}
