using System.Collections.Immutable;
using HotChocolate.Execution;
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
        /// Gets the field selection for which a field resolver is
        /// being executed.
        /// </summary>
        IFieldSelection Selection { get; }

        /// <summary>
        /// Gets access to the coerced variable values of the request.
        /// </summary>
        IVariableValueCollection Variables { get; }

        /// <summary>
        /// Gets the previous (parent) resolver result.
        /// </summary>
        /// <typeparam name="T">
        /// The type to which the result shall be casted.
        /// </typeparam>
        /// <returns>
        /// Returns the previous (parent) resolver result.
        /// </returns>
        T Parent<T>();

        /// <summary>
        /// Gets a specific field argument value.
        /// </summary>
        /// <param name="name">
        /// The argument name.
        /// </param>
        /// <typeparam name="T">
        /// The type to which the argument shall be casted to.
        /// </typeparam>
        /// <returns>
        /// Returns the value of the specified field argument as literal.
        /// </returns>
        T ArgumentValue<T>(NameString name);

        /// <summary>
        /// Gets a specific field argument as literal.
        /// </summary>
        /// <param name="name">
        /// The argument name.
        /// </param>
        /// <typeparam name="TValueNode">
        /// The type to which the argument shall be casted to.
        /// </typeparam>
        /// <returns>
        /// Returns the value of the specified field argument as literal.
        /// </returns>
        TValueNode ArgumentLiteral<TValueNode>(NameString name) where TValueNode : IValueNode;

        /// <summary>
        /// Gets a specific field argument as optional.
        /// </summary>
        /// <param name="name">
        /// The argument name.
        /// </param>
        /// <typeparam name="T">
        /// The type to which the argument shall be casted to.
        /// </typeparam>
        /// <returns>
        /// Returns the value of the specified field argument as optional.
        /// </returns>
        Optional<T> ArgumentOptional<T>(NameString name);

        /// <summary>
        /// Gets the value kind of a specific field argument.
        /// </summary>
        /// <param name="name">
        /// The argument name.
        /// </param>
        /// <returns>
        /// Returns the value kind of the specified field argument kind.
        /// </returns>
        ValueKind ArgumentKind(NameString name);

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
    }
}
