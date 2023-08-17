using System.Collections.Immutable;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Processing;

internal partial class MiddlewareContext
{
    private static readonly ImmutableDictionary<string, object?> _emptyLocalContextData =
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
        LocalContextData = _emptyLocalContextData;
        Arguments = _selection.Arguments;
        RequestAborted = _operationContext.RequestAborted;
    }

    public void Clean()
    {
        _childContext.Clear();
        _cleanupTasks.Clear();
        _operationContext = default!;
        _services = default!;
        _selection = default!;
        _parent = default;
        _resolverResult = default;
        _hasResolverResult = false;
        _result = default;
        _parser = default!;
        _path = default;
        _operationResultBuilder.Context = default!;

        ScopedContextData = default!;
        LocalContextData = default!;
        IsResultModified = false;
        ValueType = null;
        ResponseIndex = default;
        ParentResult = default!;
        HasErrors = false;
        Arguments = default!;
        RequestAborted = default!;
    }
}
