using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    /// <summary>
    /// The directive context represent the execution context for a specific
    /// directive middleware that is being executed.
    /// </summary>
    public interface IDirectiveContext
        : IMiddlewareContext
    {
        /// <summary>
        /// Gets the directive that is being executed.
        /// </summary>
        IDirective Directive { get; }
    }
}
