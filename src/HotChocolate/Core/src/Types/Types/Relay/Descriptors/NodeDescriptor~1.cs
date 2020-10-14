using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.Expressions;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

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

        protected void OnCompleteDefinition(ObjectTypeDefinition definition)
        {
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
                Definition.IdMember = Context.TypeInspector
                    .GetMembers(Definition.NodeType)
                    .OfType<MethodInfo>()
                    .FirstOrDefault(t => string.Equals(
                        t.Name,
                        NodeType.Names.Id,
                        StringComparison.OrdinalIgnoreCase));
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

            if (member is MethodInfo || member is PropertyInfo)
            {
                Definition.IdMember = member;
                return new NodeDescriptor<TNode, TId>(Definition, ConfigureNodeField);
            }

            throw new ArgumentException(
                TypeResources.NodeDescriptor_IdMember,
                nameof(member));
        }

        public IObjectFieldDescriptor NodeResolver(
            NodeResolverDelegate<TNode, object> nodeResolver) =>
            ResolveNode<object>(
                async (ctx, id) => (await nodeResolver(ctx, id).ConfigureAwait(false))!);

        public IObjectFieldDescriptor NodeResolver<TId>(
            NodeResolverDelegate<TNode, TId> nodeResolver) =>
            ResolveNode<TId>(
                async (ctx, id) => (await nodeResolver(ctx, id).ConfigureAwait(false))!);

        public IObjectFieldDescriptor ResolveNodeWith<TResolver>(
            Expression<Func<TResolver, TNode>> method)
        {
            if (method is null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            MemberInfo member = method.TryExtractMember();

            if (member is MethodInfo m)
            {
                FieldResolver resolver =
                    ResolverCompiler.Resolve.Compile(
                        new ResolverDescriptor(
                            typeof(TResolver),
                            typeof(object),
                            new FieldMember("_", "_", m)));
                return ResolveNode(resolver.Resolver);
            }

            throw new ArgumentException(
                TypeResources.NodeDescriptor_MustBeMethod,
                nameof(member));
        }
    }
}
