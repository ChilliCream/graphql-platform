using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation;

public interface IEntityResolverDescriptor
{
    IObjectTypeDescriptor ResolveReference(
        FieldResolverDelegate fieldResolver);

    IObjectTypeDescriptor ResolveReferenceWith<TResolver>(
        Expression<Func<TResolver, object>> method);

    IObjectTypeDescriptor ResolveReferenceWith<TResolver>();

    IObjectTypeDescriptor ResolveReferenceWith(MethodInfo method);

    IObjectTypeDescriptor ResolveReferenceWith(Type type);
}

public interface IEntityResolverDescriptor<TEntity>
{
    IObjectTypeDescriptor ResolveReference(
        FieldResolverDelegate fieldResolver);

    IObjectTypeDescriptor ResolveReference(
        Expression<Func<TEntity, object>> method);

    IObjectTypeDescriptor ResolveReferenceWith<TResolver>(
        Expression<Func<TResolver, object>> method);

    IObjectTypeDescriptor ResolveReferenceWith<TResolver>();

    IObjectTypeDescriptor ResolveReferenceWith(MethodInfo method);

    IObjectTypeDescriptor ResolveReferenceWith(Type type);
}
