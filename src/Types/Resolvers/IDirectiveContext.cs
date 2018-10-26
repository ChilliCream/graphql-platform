using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    /// <summary>
    /// The directive context represent the execution context for a specific
    /// directive middleware that is being executed.
    /// </summary>
    public interface IDirectiveContext
        : IResolverContext
    {
        /// <summary>
        /// Gets the directive that is being executed.
        /// </summary>
        IDirective Directive { get; }

        /// <summary>
        /// Gets or sets the result of the middleware.
        /// </summary>
        object Result { get; set; }

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
        Task<T> ResolveAsync<T>();
    }
}
