using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

// note: we are not asserting the properties here for performance reasons.
// These properties are used by the middleware context which means that potentially
// all fields will access them.
internal sealed partial class OperationContext
{
    /// <summary>
    /// Gets the request scoped services
    /// </summary>
    public IServiceProvider Services => _services;

    /// <summary>
    /// Gets the activator helper class.
    /// </summary>
    public ResolverProvider Resolvers => _resolvers;

    /// <summary>
    /// Gets access to the input parser.
    /// </summary>
    public InputParser InputParser => _inputParser;

    /// <summary>
    /// Gets the service scope initializer.
    /// </summary>
    public AggregateServiceScopeInitializer ServiceScopeInitializer => _serviceScopeInitializer;
}
