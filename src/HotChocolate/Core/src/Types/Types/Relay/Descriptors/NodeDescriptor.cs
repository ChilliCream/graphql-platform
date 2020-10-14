using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Relay.Descriptors
{
    public class NodeDescriptor
        : NodeDescriptorBase
        , INodeDescriptor
    {
        private readonly IObjectTypeDescriptor _typeDescriptor;

        public NodeDescriptor(IObjectTypeDescriptor descriptor)
            : base(descriptor.Extend().Context)
        {
            _typeDescriptor = descriptor;

            _typeDescriptor
                .Implements<NodeType>()
                .Extend()
                .OnBeforeCreate(OnCompleteDefinition);
        }

        private void OnCompleteDefinition(ObjectTypeDefinition definition)
        {
            if (Definition.Resolver is not null)
            {
                definition.ContextData[WellKnownContextData.NodeResolver] =
                    Definition.Resolver;
            }
        }

        protected override IObjectFieldDescriptor ConfigureNodeField()
        {
            return Definition.IdMember is null
                ? _typeDescriptor
                    .Field(NodeType.Names.Id)
                    .Type<NonNullType<IdType>>()
                    .Use<IdMiddleware>()
                : _typeDescriptor
                    .Field(Definition.IdMember)
                    .Name(NodeType.Names.Id)
                    .Type<NonNullType<IdType>>()
                    .Use<IdMiddleware>();
        }

        public IObjectFieldDescriptor NodeResolver(
            NodeResolverDelegate<object, object> nodeResolver) =>
            ResolveNode(nodeResolver);

        public IObjectFieldDescriptor NodeResolver<TId>(
            NodeResolverDelegate<object, TId> nodeResolver) =>
            ResolveNode(nodeResolver);
    }
}
