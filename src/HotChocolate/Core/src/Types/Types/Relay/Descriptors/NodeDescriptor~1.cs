using System;
using System.Linq.Expressions;

namespace HotChocolate.Types.Relay.Descriptors
{
    public class NodeDescriptor<TNode>
        : INodeDescriptor<TNode>
    {
        private readonly IObjectTypeDescriptor<TNode> _typeDescriptor;

        public NodeDescriptor(IObjectTypeDescriptor<TNode> typeDescriptor)
        {
            if (typeDescriptor is null)
            {
                throw new ArgumentNullException(nameof(typeDescriptor));
            }

            _typeDescriptor = typeDescriptor;
        }

        public INodeResolverDescriptor<TNode, TId> IdField<TId>(
            Expression<Func<TNode, TId>> propertyOrMethod)
        {
            return new NodeResolverDescriptor<TNode, TId>(
                _typeDescriptor, propertyOrMethod);
        }

        public IObjectFieldDescriptor NodeResolver(
            NodeResolverDelegate<TNode, object> nodeResolver)
        {
            return NodeResolver<object>(nodeResolver);
        }

        public IObjectFieldDescriptor NodeResolver<TId>(
            NodeResolverDelegate<TNode, TId> nodeResolver)
        {
            Func<IServiceProvider, INodeResolver> nodeResolverFactory =
                services => new NodeResolver<TNode, TId>(nodeResolver);

            _typeDescriptor
                .Interface<NodeType>()
                .Extend()
                .OnBeforeCreate(c =>
                {
                    c.ContextData[RelayConstants.NodeResolverFactory] =
                        nodeResolverFactory;
                });

            return _typeDescriptor.Field("id")
                .Type<NonNullType<IdType>>()
                .Use<IdMiddleware>();;
        }
    }
}
