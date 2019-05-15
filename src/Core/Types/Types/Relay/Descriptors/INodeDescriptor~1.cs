using System;
using System.Linq.Expressions;

namespace HotChocolate.Types.Relay.Descriptors
{
    public interface INodeDescriptor<TNode>
    {
        INodeResolverDescriptor<TNode, TId> IdField<TId>(
            Expression<Func<TNode, TId>> propertyOrMethod);

        IObjectFieldDescriptor NodeResolver(
            NodeResolverDelegate<TNode, object> nodeResolver);

        IObjectFieldDescriptor NodeResolver<TId>(
            NodeResolverDelegate<TNode, TId> nodeResolver);
    }
}
