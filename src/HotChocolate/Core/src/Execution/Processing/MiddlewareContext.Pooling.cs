using System.Collections.Immutable;

namespace HotChocolate.Execution.Processing;

internal partial class MiddlewareContext
{
    private static readonly ImmutableDictionary<string, object?> s_emptyLocalContextData =
        ImmutableDictionary<string, object?>.Empty;

    public MiddlewareContext()
    {
        _childContext = new PureResolverContext(this);
    }

    public void Initialize(
        OperationContext operationContext,
        ISelection selection,
        ObjectResult parentResult,
        int responseIndex,
        object? parent,
        IImmutableDictionary<string, object?> scopedContextData,
        Path? path)
    {
        _operationContext = operationContext;
        _operationResultBuilder.Context = _operationContext;
        _services = operationContext.Services;
        _selection = selection;
        _path = path;
        ParentResult = parentResult;
        ResponseIndex = responseIndex;
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
        ResponseIndex = 0;
        ParentResult = null!;
        HasErrors = false;
        Arguments = null!;
        RequestAborted = CancellationToken.None;
    }
}
