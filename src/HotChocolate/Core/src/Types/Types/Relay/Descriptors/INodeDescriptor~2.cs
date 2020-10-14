using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types.Relay.Descriptors
{
    public interface INodeDescriptor<TNode, out TId> : IDescriptor
    {
        [Obsolete("Use ResolveNode.")]
        IObjectFieldDescriptor NodeResolver(
            NodeResolverDelegate<TNode, TId> nodeResolver);

        IObjectFieldDescriptor ResolveNode(
            FieldResolverDelegate fieldResolver);

        IObjectFieldDescriptor ResolveNode(
            NodeResolverDelegate<TNode, TId> fieldResolver);

        IObjectFieldDescriptor ResolveNodeWith<TResolver>(
            Expression<Func<TResolver, TNode>> method);

        IObjectFieldDescriptor ResolveNodeWith(MethodInfo method);

        IObjectFieldDescriptor ResolveNodeWith<TResolver>();
    }
}
