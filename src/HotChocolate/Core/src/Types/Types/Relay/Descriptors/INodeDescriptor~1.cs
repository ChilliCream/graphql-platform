using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types.Relay.Descriptors
{
    public interface INodeDescriptor<TNode> : IDescriptor
    {
        INodeDescriptor<TNode, TId> IdField<TId>(
            Expression<Func<TNode, TId>> propertyOrMethod);

        INodeDescriptor<TNode> IdField(MemberInfo propertyOrMethod);

        [Obsolete("Use ResolveNode.")]
        IObjectFieldDescriptor NodeResolver(
            NodeResolverDelegate<TNode, object> nodeResolver);

        [Obsolete("Use ResolveNode.")]
        IObjectFieldDescriptor NodeResolver<TId>(
            NodeResolverDelegate<TNode, TId> nodeResolver);

        IObjectFieldDescriptor ResolveNode(
            FieldResolverDelegate fieldResolver);

        IObjectFieldDescriptor ResolveNode<TId>(
            NodeResolverDelegate<object, TId> fieldResolver);

        IObjectFieldDescriptor ResolveNodeWith<TResolver>(
            Expression<Func<TResolver, TNode>> method);

        IObjectFieldDescriptor ResolveNodeWith<TResolver>();

        IObjectFieldDescriptor ResolveNodeWith(MethodInfo method);

        IObjectFieldDescriptor ResolveNodeWith(Type type);
    }
}
