using System;
using System.Linq.Expressions;

namespace HotChocolate.Types.Relay.Descriptors
{
    public class NodeResolverDescriptor<TNode, TId>
        : INodeResolverDescriptor<TNode, TId>
    {
        private readonly IObjectTypeDescriptor<TNode> _typeDescriptor;
        private readonly Expression<Func<TNode, TId>> _propertyOrMethod;

        public NodeResolverDescriptor(
            IObjectTypeDescriptor<TNode> typeDescriptor,
            Expression<Func<TNode, TId>> propertyOrMethod)
        {
            if (typeDescriptor is null)
            {
                throw new ArgumentNullException(nameof(typeDescriptor));
            }

            if (propertyOrMethod is null)
            {
                throw new ArgumentNullException(nameof(propertyOrMethod));
            }

            _typeDescriptor = typeDescriptor;
            _propertyOrMethod = propertyOrMethod;
        }

        public IObjectFieldDescriptor NodeResolver(
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

            return _typeDescriptor.Field(_propertyOrMethod)
                .Type<NonNullType<IdType>>()
                .Use<IdMiddleware>()
                .Name("id");
        }
    }
}
