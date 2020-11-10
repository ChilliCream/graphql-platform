using System;
using System.Reflection;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;

#nullable enable

namespace HotChocolate.Types.Relay.Descriptors
{
    public class NodeDescriptor
        : NodeDescriptorBase
        , INodeDescriptor
    {
        private readonly IObjectTypeDescriptor _typeDescriptor;

        public NodeDescriptor(IObjectTypeDescriptor descriptor, Type? nodeType = null)
            : base(descriptor.Extend().Context)
        {
            _typeDescriptor = descriptor;

            _typeDescriptor
                .Implements<NodeType>()
                .Extend()
                .OnBeforeCreate(OnCompleteDefinition);

            Definition.NodeType = nodeType;
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

        public INodeDescriptor IdField(MemberInfo propertyOrMethod)
        {
            if (propertyOrMethod is null)
            {
                throw new ArgumentNullException(nameof(propertyOrMethod));
            }

            if (propertyOrMethod is PropertyInfo or MethodInfo)
            {
                Definition.IdMember = propertyOrMethod;
                return this;
            }

            throw new ArgumentException(NodeDescriptor_IdField_MustBePropertyOrMethod);
        }

        public IObjectFieldDescriptor NodeResolver(
            NodeResolverDelegate<object, object> nodeResolver) =>
            ResolveNode(nodeResolver);

        public IObjectFieldDescriptor NodeResolver<TId>(
            NodeResolverDelegate<object, TId> nodeResolver) =>
            ResolveNode(nodeResolver);

        public IObjectFieldDescriptor ResolveNodeWith<TResolver>() =>
            ResolveNodeWith(Context.TypeInspector.GetNodeResolverMethod(
                Definition.NodeType ?? typeof(TResolver),
                typeof(TResolver))!);

        public IObjectFieldDescriptor ResolveNodeWith(Type type) =>
            ResolveNodeWith(Context.TypeInspector.GetNodeResolverMethod(
                Definition.NodeType ?? type,
                type)!);
    }
}
