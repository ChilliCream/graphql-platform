using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Resolvers.Expressions;

namespace HotChocolate.Types.Relay.Descriptors
{
    public class NodeDescriptor
        : INodeDescriptor
    {
        private readonly IObjectTypeDescriptor _typeDescriptor;

        public NodeDescriptor(IObjectTypeDescriptor typeDescriptor)
        {
            if (typeDescriptor is null)
            {
                throw new ArgumentNullException(nameof(typeDescriptor));
            }

            _typeDescriptor = typeDescriptor;
        }

        public IObjectFieldDescriptor NodeResolver(
            NodeResolverDelegate<object, object> nodeResolver)
        {
            return NodeResolver<object>(nodeResolver);
        }

        public IObjectFieldDescriptor NodeResolver<TId>(
            NodeResolverDelegate<object, TId> nodeResolver)
        {
            Func<IServiceProvider, INodeResolver> nodeResolverFactory =
                services => new NodeResolver<object, TId>(nodeResolver);

            _typeDescriptor
                .Interface<NodeType>()
                .Extend()
                .OnBeforeCreate(c =>
                {
                    c.ContextData[RelayConstants.NodeResolverFactory] = nodeResolverFactory;
                });

            return _typeDescriptor.Field("id")
                .Type<NonNullType<IdType>>()
                .Use<IdMiddleware>();
        }

        public IObjectFieldDescriptor ResolveNode(
            FieldResolverDelegate fieldResolver)
        {
            throw new NotImplementedException();
        }

        public IObjectFieldDescriptor ResolveNode(
            FieldResolverDelegate fieldResolver, 
            Type resultType)
        {
            throw new NotImplementedException();
        }

        public IObjectFieldDescriptor ResolveNodeWith<TResolver>(
            Expression<Func<TResolver, object>> propertyOrMethod)
        {
            ResolverCompiler.Resolve.Compile(
                new ResolverDescriptor())
        }

        public IObjectFieldDescriptor ResolveNodeWith(MemberInfo propertyOrMethod)
        {
            throw new NotImplementedException();
        }
    }
}
