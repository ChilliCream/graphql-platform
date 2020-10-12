using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    public interface IEntityResolverDescriptor
    {
        IObjectTypeDescriptor ResolveEntity(
            FieldResolverDelegate fieldResolver);

        IObjectTypeDescriptor ResolveEntityWith<TResolver>(
            Expression<Func<TResolver, object>> method);

        IObjectTypeDescriptor ResolveEntityWith<TResolver>();

        IObjectTypeDescriptor ResolveEntityWith(MethodInfo method);

        IObjectTypeDescriptor ResolveEntityWith(Type type);
    }
}
