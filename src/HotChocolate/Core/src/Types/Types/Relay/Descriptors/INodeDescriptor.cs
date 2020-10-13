using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Relay.Descriptors
{
    public interface INodeDescriptor
    {
        [Obsolete]
        IObjectFieldDescriptor NodeResolver(
            NodeResolverDelegate<object, object> nodeResolver);

        [Obsolete]
        IObjectFieldDescriptor NodeResolver<TId>(
            NodeResolverDelegate<object, TId> nodeResolver);

        IObjectFieldDescriptor ResolveNode(
            FieldResolverDelegate fieldResolver);

        IObjectFieldDescriptor ResolveNode(
            FieldResolverDelegate fieldResolver,
            Type resultType);

        IObjectFieldDescriptor ResolveNodeWith<TResolver>(
            Expression<Func<TResolver, object>> propertyOrMethod);

        IObjectFieldDescriptor ResolveNodeWith(MemberInfo propertyOrMethod);
    }
}
