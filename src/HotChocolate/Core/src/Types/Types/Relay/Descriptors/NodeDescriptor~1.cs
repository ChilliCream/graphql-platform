using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.Expressions;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;

#nullable enable

namespace HotChocolate.Types.Relay.Descriptors
{
    public class NodeDescriptor<TNode>
        : NodeDescriptorBase
        , INodeDescriptor<TNode>
    {
        private readonly IObjectTypeDescriptor<TNode> _typeDescriptor;

        public NodeDescriptor(IObjectTypeDescriptor<TNode> typeDescriptor)
            : base(typeDescriptor.Extend().Context)
        {
            _typeDescriptor = typeDescriptor;

            _typeDescriptor
                .Implements<NodeType>()
                .Extend()
                .OnBeforeCreate(OnCompleteDefinition);
        }

        private void OnCompleteDefinition(ObjectTypeDefinition definition)
        {
            if (Definition.Resolver is null)
            {
                ResolveNodeWith<TNode>();
            }

            if (Definition.Resolver is not null)
            {
                definition.ContextData[WellKnownContextData.NodeResolver] =
                    Definition.Resolver;
            }
        }

        protected override IObjectFieldDescriptor ConfigureNodeField()
        {
            Definition.NodeType = typeof(TNode);

            if (Definition.IdMember is null)
            {
                Definition.IdMember = Context.TypeInspector.GetNodeIdMember(typeof(TNode));
            }

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

        public INodeDescriptor<TNode, TId> IdField<TId>(
            Expression<Func<TNode, TId>> propertyOrMethod)
        {
            if (propertyOrMethod is null)
            {
                throw new ArgumentNullException(nameof(propertyOrMethod));
            }

            MemberInfo member = propertyOrMethod.TryExtractMember();

            if (member is MethodInfo or PropertyInfo)
            {
                Definition.IdMember = member;
                return new NodeDescriptor<TNode, TId>(Context, Definition, ConfigureNodeField);
            }

            throw new ArgumentException(NodeDescriptor_IdMember, nameof(member));
        }

        public INodeDescriptor<TNode> IdField(MemberInfo propertyOrMethod)
        {
            if (propertyOrMethod is null)
            {
                throw new ArgumentNullException(nameof(propertyOrMethod));
            }

            if (propertyOrMethod is MethodInfo or PropertyInfo)
            {
                Definition.IdMember = propertyOrMethod;
                return this;
            }

            throw new ArgumentException(NodeDescriptor_IdField_MustBePropertyOrMethod);
        }

        public IObjectFieldDescriptor NodeResolver(
            NodeResolverDelegate<TNode, object> nodeResolver) =>
            ResolveNode<object>(
                async (ctx, id) => (await nodeResolver(ctx, id).ConfigureAwait(false))!);

        public IObjectFieldDescriptor NodeResolver<TId>(
            NodeResolverDelegate<TNode, TId> nodeResolver) =>
            ResolveNode<TId>(
                async (ctx, id) => (await nodeResolver(ctx, id).ConfigureAwait(false))!);

        public IObjectFieldDescriptor ResolveNodeWith<TResolver>() =>
            ResolveNodeWith(Context.TypeInspector.GetNodeResolverMethod(
                typeof(TNode),
                typeof(TResolver))!);

        public IObjectFieldDescriptor ResolveNodeWith(Type type) =>
            ResolveNodeWith(Context.TypeInspector.GetNodeResolverMethod(
                typeof(TNode),
                type)!);
    }
}
