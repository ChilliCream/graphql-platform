using System;
using System.Threading.Tasks;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Resolvers
{
    /// <summary>
    /// The middleware context represent the execution context for a field middleware.
    /// </summary>
    public interface IMiddlewareContext : IResolverContext
    {
        IType? ValueType { get; set;}

        /// <summary>
        /// Gets or sets the result of the middleware.
        /// </summary>
        object? Result { get; set; }

        /// <summary>
        /// Defines if at least one middleware has modified the result.
        /// </summary>
        /// <value></value>
        bool IsResultModified { get; }

        /// <summary>
        /// Executes the field resolver and returns its result.
        /// </summary>
        /// <typeparam name="T">
        /// The type to which the result shall be casted.
        /// </typeparam>
        /// <returns>
        /// Returns the resolved field value.
        /// </returns>
        ValueTask<T> ResolveAsync<T>();

        /// <summary>
        /// Register cleanup tasks that will be executed after resolver execution is finished.
        /// </summary>
        /// <param name="action">
        /// Cleanup action.
        /// </param>
        void RegisterForCleanup(Action action);
    }
}
