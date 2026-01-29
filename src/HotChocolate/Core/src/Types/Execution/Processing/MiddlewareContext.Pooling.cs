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

    public void Initialize(
        object? parent,
        Selection selection,
        ResultElement resultValue,
        OperationContext operationContext,
        IImmutableDictionary<string, object?> scopedContextData,
        Path? path)
    {
        _operationContext = operationContext;
        _operationResultBuilder.Context = _operationContext;
        _services = operationContext.Services;
        _selection = selection;
        _path = path;
        ResultValue = resultValue;
        _parent = parent;
        _parser = operationContext.InputParser;
        ScopedContextData = scopedContextData;
        LocalContextData = s_emptyLocalContextData;
        Arguments = _selection.Arguments;
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
        RequestAborted = CancellationToken.None;
    }
}
