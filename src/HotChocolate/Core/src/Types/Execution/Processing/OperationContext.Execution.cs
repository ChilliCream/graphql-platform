using System.Collections.Immutable;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Text.Json;

namespace HotChocolate.Execution.Processing;

internal sealed partial class OperationContext
{
    /// <summary>
    /// The work scheduler organizes the processing of request tasks.
    /// </summary>
    public WorkScheduler Scheduler
    {
        get
        {
            AssertInitialized();
            return _currentWorkScheduler;
        }
        internal set
        {
            _currentWorkScheduler = value;
        }
    }

    /// <summary>
    /// The result helper which provides utilities to build up the result.
    /// </summary>
    public ResultBuilder Result
    {
        get
        {
            AssertInitialized();
            return _resultBuilder;
        }
    }

    public ResultDocument ResultDocument => throw new NotImplementedException();

    public RequestContext RequestContext
    {
        get
        {
            AssertInitialized();
            return _requestContext;
        }
    }

    public ResolverTask CreateResolverTask(
        object? parent,
        Selection selection,
        ResultElement resultValue,
        IImmutableDictionary<string, object?> scopedContextData,
        Path? path = null)
    {
        AssertInitialized();

        var resolverTask = _resolverTaskFactory.Create();

        resolverTask.Initialize(
            parent,
            selection,
            resultValue,
            this,
            scopedContextData,
            path);

        return resolverTask;
    }
}
