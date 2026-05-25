using HotChocolate.Features;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// The internal context of the execution engine.
/// </summary>
internal sealed partial class OperationContext : IFeatureProvider
{
    public IDictionary<string, object?> ContextData
    {
        get
        {
            AssertInitialized();
            return _contextData;
        }
    }

    /// <summary>
    /// Gets a cancellation token is used to signal
    /// if the request has be aborted.
    /// </summary>
    public CancellationToken RequestAborted
    {
        get
        {
            AssertInitialized();
            return _requestAborted;
        }
    }

    /// <summary>
    /// Gets whether null values should be propagated to nullable parents
    /// (standard GraphQL behavior). When false, null stays at the field that caused it.
    /// </summary>
    public bool PropagateNullValues => _propagateNullValues;

    /// <inheritdoc />
    public IFeatureCollection Features
    {
        get
        {
            AssertInitialized();
            return _requestContext.Features;
        }
    }
}
