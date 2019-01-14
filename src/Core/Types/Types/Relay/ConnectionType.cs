namespace HotChocolate.Types.Relay
{
    public class ConnectionType<T>
        : ObjectType<IConnection>
        , IConnectionType
        where T : INamedOutputType, new()
    {
        public IEdgeType EdgeType { get; private set; }

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

        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            EdgeType = context.GetType<EdgeType<T>>(
                new TypeReference(typeof(EdgeType<T>)));

            base.OnCompleteType(context);
        }
    }
}
