namespace HotChocolate.Types.Relay
{
    public class EdgeType<T>
        : ObjectType<IEdge>
        , IEdgeType
        where T : INamedOutputType, new()
    {
        public INamedOutputType EntityType { get; private set; }

        protected override void Configure(IObjectTypeDescriptor<IEdge> descriptor)
        {
            // TODO : Fix this with the new schema builder
            descriptor.Name($"{new T().Name}Edge");
            descriptor.Description("An edge in a connection.");

            descriptor.Field(t => t.Cursor)
                .Name("cursor")
                .Description("A cursor for use in pagination.")
                .Type<NonNullType<StringType>>();

            descriptor.Field(t => t.Node)
                .Name("node")
                .Description("The item at the end of the edge.")
                .Type<T>();
        }

        protected override void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
            base.OnRegisterDependencies(context);

            context.RegisterType(new TypeReference(typeof(T)));
        }

        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            EntityType = context.GetType<T>(
                new TypeReference(typeof(T)));

            base.OnCompleteType(context);
        }
    }
}
