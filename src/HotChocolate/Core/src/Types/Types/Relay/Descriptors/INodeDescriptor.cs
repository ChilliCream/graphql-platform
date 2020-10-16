using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types.Relay.Descriptors
{
    public interface INodeDescriptor : IDescriptor
    {
        INodeDescriptor IdField(MemberInfo propertyOrMethod);

        [Obsolete("Use ResolveNode.")]
        IObjectFieldDescriptor NodeResolver(
            NodeResolverDelegate<object, object> nodeResolver);

        [Obsolete("Use ResolveNode.")]
        IObjectFieldDescriptor NodeResolver<TId>(
            NodeResolverDelegate<object, TId> nodeResolver);

        IObjectFieldDescriptor ResolveNode(
            FieldResolverDelegate fieldResolver);

        IObjectFieldDescriptor ResolveNode<TId>(
            NodeResolverDelegate<object, TId> fieldResolver);

        IObjectFieldDescriptor ResolveNodeWith<TResolver>(
            Expression<Func<TResolver, object>> method);

        IObjectFieldDescriptor ResolveNodeWith<TResolver>();

        IObjectFieldDescriptor ResolveNodeWith(MethodInfo method);

        IObjectFieldDescriptor ResolveNodeWith(Type type);
    }
}
