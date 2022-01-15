using System.Collections.Immutable;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Processing;

internal partial class MiddlewareContext
{
    public MiddlewareContext()
    {
        _childContext = new PureResolverContext(this);
    }

    public void Initialize(
        IOperationContext operationContext,
        ISelection selection,
        ResultMap resultMap,
        int responseIndex,
        object? parent,
        Path path,
        IImmutableDictionary<string, object?> scopedContextData)
    {
        _operationContext = operationContext;
        _services = operationContext.Services;
        _selection = selection;
        ResultMap = resultMap;
        ResponseIndex = responseIndex;
        _parent = parent;
        _parser = _operationContext.Services.GetRequiredService<InputParser>();
        Path = path;
        ScopedContextData = scopedContextData;
        LocalContextData = ImmutableDictionary<string, object?>.Empty;
        Arguments = _selection.Arguments;
        RequestAborted = _operationContext.RequestAborted;
    }

    public void Clean()
    {
        _childContext.Clear();
        _operationContext = default!;
        _services = default!;
        _selection = default!;
        _parent = default;
        _resolverResult = default;
        _hasResolverResult = false;
        _result = default;
        _parser = default!;

        Path = default!;
        ScopedContextData = default!;
        LocalContextData = default!;
        IsResultModified = false;
        ValueType = null;
        ResponseIndex = default;
        ResultMap = default!;
        HasErrors = false;
        Arguments = default!;
        RequestAborted = default!;
    }
}
