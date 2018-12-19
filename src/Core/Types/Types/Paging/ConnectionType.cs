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
            descriptor.Description("A connection to a list of items.");

            descriptor.Field(t => t.PageInfo)
                .Name("pageInfo")
                .Description("Information to aid in pagination.")
                .Type<NonNullType<PageInfoType>>();

            descriptor.Field(t => t.Edges)
                .Name("edges")
                .Description("A list of edges.")
                .Type<ListType<NonNullType<EdgeType<T>>>>();
        }

        protected override void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
            base.OnRegisterDependencies(context);

            context.RegisterType(new TypeReference(typeof(T)));
            context.RegisterType(new TypeReference(typeof(EdgeType<T>)));
        }
    }
}
