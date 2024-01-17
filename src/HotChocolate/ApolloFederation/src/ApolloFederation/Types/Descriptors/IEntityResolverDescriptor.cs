using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// The entity descriptor allows to specify a reference resolver.
/// </summary>
public interface IEntityResolverDescriptor
{
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
}

/// <summary>
/// The entity descriptor allows to specify a reference resolver.
/// </summary>
public interface IEntityResolverDescriptor<TEntity>
{
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
    /// The reference resolver.
    /// </param>
    /// <returns>
    /// Returns the descriptor for configuration chaining.
    /// </returns>
    IObjectTypeDescriptor ResolveReferenceWith(MethodInfo method);
}
