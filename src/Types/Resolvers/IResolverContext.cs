using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    /// <summary>
    /// The resolver context represent the execution context for a specific
    /// field that is being resolved.
    /// </summary>
    public interface IResolverContext
    {
        /// <summary>
        /// Gets the GraphQL schema on which the query is executed.
        /// </summary>
        ISchema Schema { get; }

        /// <summary>
        /// Gets the object type on which the field resolver is being executed.
        /// </summary>
        ObjectType ObjectType { get; }

        /// <summary>
        /// Gets the field on which the field resolver is being executed.
        /// </summary>
        ObjectField Field { get; }

        /// <summary>
        /// Gets the query that is being executed.
        /// </summary>
        DocumentNode QueryDocument { get; }

        /// <summary>
        /// Gets the operation from the query that is being executed.
        /// </summary>
        OperationDefinitionNode Operation { get; }

        /// <summary>
        /// Gets the field selection for which a field resolver is
        /// being executed.
        /// </summary>
        FieldNode FieldSelection { get; }

        /// <summary>
        /// Gets the source stack containing all previous resolver results
        /// of the current execution path.
        /// </summary>
        ImmutableStack<object> Source { get; }

        /// <summary>
        /// Gets the current execution path.
        /// </summary>
        Path Path { get; }

        /// <summary>
        /// Notifies when the connection underlying this request is aborted
        /// and thus request operations should be cancelled.
        /// </summary>
        CancellationToken RequestAborted { get; }

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
        /// Gets a specific field argument.
        /// </summary>
        /// <param name="name">
        /// The argument name.
        /// </param>
        /// <typeparam name="T">
        /// The type to which the argument shall be casted to.
        /// </typeparam>
        /// <returns>
        /// Returns a specific field argument.
        /// </returns>
        T Argument<T>(NameString name);

        /// <summary>
        /// Gets as specific service from the dependency injection container.
        /// </summary>
        /// <typeparam name="T">
        /// The service type.
        /// </typeparam>
        /// <returns>
        /// Returns the specified service.
        /// </returns>
        T Service<T>();

        /// <summary>
        /// Gets a specific custom context object that can be used
        /// to build up a state.
        /// </summary>
        /// <typeparam name="T"><
        /// The context object type.
        /// </typeparam>
        /// <returns>
        /// Returns the specific custom context object that can be used
        /// to build up a state.
        /// </returns>
        T CustomContext<T>();

        /// <summary>
        /// Gets a custom request property that was provided with the request.
        /// </summary>
        /// <typeparam name="T">
        /// The property value type.
        /// </typeparam>
        /// <returns>
        /// Returns the value of the custom request property.
        /// </returns>
        T CustomProperty<T>(string key);

        /// <summary>
        /// Gets a specific DataLoader.
        /// </summary>
        /// <param name="key">The DataLoader key.</param>
        /// <typeparam name="T">The DataLoader type.</typeparam>
        /// <returns>
        /// Returns the specific DataLoader.
        /// </returns>
        T DataLoader<T>(string key);

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
        /// Report a non-terminating resolver error to the execution enhgine.
        /// The error will be displayed in the errorsection with a reference to
        /// the field selection that is associated with the current
        /// resolver context.
        /// </summary>
        /// <param name="errorMessage">
        /// The error message.
        /// </param>
        void ReportError(string errorMessage);

        [Obsolete("Use RequestAborted.")]
        CancellationToken CancellationToken { get; }
    }
}
