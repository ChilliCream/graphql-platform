using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Resolvers
{
    /// <summary>
    /// The context that is available to pure resolvers.
    /// </summary>
    public interface IPureResolverContext : IHasContextData
    {
        /// <summary>
        /// Gets the GraphQL schema on which the query is executed.
        /// </summary>
        ISchema Schema { get; }

        /// <summary>
        /// Gets the root object type of the currently execution operation.
        /// </summary>
        IObjectType RootType { get; }

        /// <summary>
        /// Gets as required service from the dependency injection container.
        /// </summary>
        /// <typeparam name="T">
        /// The service type.
        /// </typeparam>
        /// <returns>
        /// Returns the specified service.
        /// </returns>
        T Service<T>();

        /// <summary>
        /// Gets a resolver object containing one or more resolvers.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the resolver object.
        /// </typeparam>
        /// <returns>
        /// Returns a resolver object containing one or more resolvers.
        /// </returns>
        T Resolver<T>();

        /// <summary>
        /// The scoped context data dictionary can be used by middlewares and
        /// resolvers to store and retrieve data during execution scoped to the
        /// hierarchy
        /// </summary>
        IImmutableDictionary<string, object?> ScopedContextData { get; set; }
    }
}
