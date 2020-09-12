using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Pagination
{
    public class EdgeType<T>
        : ObjectType<IEdge>
        , IEdgeType
        where T : class, IOutputType
    {
        public IOutputType EntityType { get; private set; } = default!;

        protected override void Configure(
            IObjectTypeDescriptor<IEdge> descriptor)
        {
            descriptor
                .Name(dependency => dependency.Name + "Edge")
                .DependsOn<T>()
                .Description("An edge in a connection.")
                .BindFields(BindingBehavior.Explicit);

            descriptor
                .Field(t => t.Cursor)
                .Name("cursor")
                .Description("A cursor for use in pagination.")
                .Type<NonNullType<StringType>>();

            descriptor
                .Field(t => t.Node)
                .Name("node")
                .Description("The item at the end of the edge.")
                .Type<T>();
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            ObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            EntityType = context.GetType<IOutputType>(
                context.TypeInspector.GetTypeRef(typeof(T)));
        }
    }
}
