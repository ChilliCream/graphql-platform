using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay.Descriptors
{
    public interface INodeDescriptor<TNode>
    {
        INodeDescriptor<TNode> IdField<TId>(
            Expression<Func<TNode, TId>> propertyOrMethod);

        [Obsolete]
        IObjectFieldDescriptor NodeResolver(
            NodeResolverDelegate<TNode, object> nodeResolver);

        [Obsolete]
        IObjectFieldDescriptor NodeResolver<TId>(
            NodeResolverDelegate<TNode, TId> nodeResolver);

        IObjectFieldDescriptor ResolveNode(
            FieldResolverDelegate fieldResolver);

        IObjectFieldDescriptor ResolveNode(
            FieldResolverDelegate fieldResolver,
            Type resultType);

        IObjectFieldDescriptor ResolveNode(
            Expression<Func<TNode, object>> propertyOrMethod);

        IObjectFieldDescriptor ResolveNodeWith<TResolver>(
            Expression<Func<TResolver, object>> propertyOrMethod);

        IObjectFieldDescriptor ResolveNodeWith(MemberInfo propertyOrMethod);
    }
}
