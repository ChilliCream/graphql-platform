using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.ApolloFederation.Descriptors;

/// <summary>
/// The entity descriptor allows to specify a reference resolver.
/// </summary>
public interface IEntityResolverDescriptor
{
    /// <summary>
    /// Resolve an entity from its representation.
    /// </summary>
    /// <param name="fieldResolver">
    /// The resolver.
    /// </param>
    /// <returns>
    /// Returns the descriptor for configuration chaining.
    /// </returns>
    IObjectTypeDescriptor ResolveReference(
        FieldResolverDelegate fieldResolver);

    /// <summary>
    /// Resolve an entity from its representation.
    /// </summary>
    /// <param name="method">
    /// The reference resolver selector.
    /// </param>
    /// <typeparam name="TResolver">
    /// The type on which the reference resolver is located.
    /// </typeparam>
    /// <returns>
    /// Returns the descriptor for configuration chaining.
    /// </returns>
    IObjectTypeDescriptor ResolveReferenceWith<TResolver>(
        Expression<Func<TResolver, object?>> method);

    /// <summary>
    /// Resolve an entity from its representation.
    /// </summary>
    /// <typeparam name="TResolver">
    /// The type on which the reference resolver is located.
    /// </typeparam>
    /// <returns>
    /// Returns the descriptor for configuration chaining.
    /// </returns>
    IObjectTypeDescriptor ResolveReferenceWith<TResolver>();

    /// <summary>
    /// Resolve an entity from its representation.
    /// </summary>
    /// <param name="method">
    /// The reference resolver.
    /// </param>
    /// <returns>
    /// Returns the descriptor for configuration chaining.
    /// </returns>
    IObjectTypeDescriptor ResolveReferenceWith(MethodInfo method);

    /// <summary>
    /// Resolve an entity from its representation.
    /// </summary>
    /// <param name="type">
    /// The type on which the reference resolver is located.
    /// </param>
    /// <returns>
    /// Returns the descriptor for configuration chaining.
    /// </returns>
    IObjectTypeDescriptor ResolveReferenceWith(Type type);
}

/// <summary>
/// The entity descriptor allows to specify a reference resolver.
/// </summary>
public interface IEntityResolverDescriptor<TEntity>
{
    /// <summary>
    /// Resolve an entity from its representation.
    /// </summary>
    /// <param name="fieldResolver">
    /// The resolver.
    /// </param>
    /// <returns>
    /// Returns the descriptor for configuration chaining.
    /// </returns>
    IObjectTypeDescriptor ResolveReference(
        FieldResolverDelegate fieldResolver);

    /// <summary>
    /// Resolve an entity from its representation.
    /// </summary>
    /// <param name="method">
    /// The reference resolver selector.
    /// </param>
    /// <returns>
    /// Returns the descriptor for configuration chaining.
    /// </returns>
    IObjectTypeDescriptor ResolveReferenceWith(
        Expression<Func<TEntity, object?>> method);

    /// <summary>
    /// Resolve an entity from its representation.
    /// </summary>
    /// <param name="method">
    /// The reference resolver selector.
    /// </param>
    /// <typeparam name="TResolver">
    /// The type on which the reference resolver is located.
    /// </typeparam>
    /// <returns>
    /// Returns the descriptor for configuration chaining.
    /// </returns>
    IObjectTypeDescriptor ResolveReferenceWith<TResolver>(
        Expression<Func<TResolver, object?>> method);

    /// <summary>
    /// Resolve an entity from its representation.
    /// </summary>
    /// <typeparam name="TResolver">
    /// The type on which the reference resolver is located.
    /// </typeparam>
    /// <returns>
    /// Returns the descriptor for configuration chaining.
    /// </returns>
    IObjectTypeDescriptor ResolveReferenceWith<TResolver>();

    /// <summary>
    /// Resolve an entity from its representation.
    /// </summary>
    /// <param name="method">
    /// The reference resolver.
    /// </param>
    /// <returns>
    /// Returns the descriptor for configuration chaining.
    /// </returns>
    IObjectTypeDescriptor ResolveReferenceWith(MethodInfo method);

    /// <summary>
    /// Resolve an entity from its representation.
    /// </summary>
    /// <param name="type">
    /// The type on which the reference resolver is located.
    /// </param>
    /// <returns>
    /// Returns the descriptor for configuration chaining.
    /// </returns>
    IObjectTypeDescriptor ResolveReferenceWith(Type type);
}
