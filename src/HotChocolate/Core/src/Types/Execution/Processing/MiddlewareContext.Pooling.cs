using System.Collections.Immutable;
using HotChocolate.Text.Json;

namespace HotChocolate.Execution.Processing;

internal partial class MiddlewareContext
{
    private static readonly ImmutableDictionary<string, object?> s_emptyLocalContextData =
#if NET10_0_OR_GREATER
        [];
#else
        ImmutableDictionary<string, object?>.Empty;
#endif

    public MiddlewareContext()
    {
        _childContext = new PureResolverContext(this);
    }

    /// <summary>
    /// Gets the execution branch identifier this context belongs to.
    /// Set by the owning task (ResolverTask or BatchResolverTask) so that
    /// value completion can read it without requiring it to be passed down.
    /// </summary>
    public int BranchId { get; set; }

    public void Initialize(
        object? parent,
        Selection selection,
        ResultElement resultValue,
        OperationContext operationContext,
        DeferUsage? deferUsage,
        IImmutableDictionary<string, object?> scopedContextData)
    {
        _operationContext = operationContext;
        _operationResultBuilder.Context = _operationContext;
        _services = operationContext.Services;
        _selection = selection;
        ResultValue = resultValue;
        _parent = parent;
        _parser = operationContext.InputParser;
        ScopedContextData = scopedContextData;
        LocalContextData = s_emptyLocalContextData;
        Arguments = _selection.Arguments;
        DeferUsage = deferUsage;
        RequestAborted = _operationContext.RequestAborted;
    }

    public void Clean()
    {
        _childContext.Clear();
        _cleanupTasks.Clear();
        _operationContext = null!;
        _services = null!;
        _selection = null!;
        _parent = null;
        _resolverResult = null;
        _hasResolverResult = false;
        _result = null;
        _parser = null!;
        _path = null;
        _operationResultBuilder.Context = null!;

        ScopedContextData = null!;
        LocalContextData = null!;
        IsResultModified = false;
        ValueType = null;
        ResultValue = default;
        HasErrors = false;
        Arguments = null!;
        BranchId = 0;
        RequestAborted = CancellationToken.None;
    }
}
