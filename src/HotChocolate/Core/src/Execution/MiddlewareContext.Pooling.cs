using System.Collections.Immutable;
using HotChocolate.Execution.Utilities;

namespace HotChocolate.Execution
{
    internal partial class MiddlewareContext
    {
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
            _selection = selection;
            ResultMap = resultMap;
            ResponseIndex = responseIndex;
            _parent = parent;
            Path = path;
            ScopedContextData = scopedContextData;
            LocalContextData = ImmutableDictionary<string, object?>.Empty;
            Arguments = _selection.Arguments;
        }

        public void Reset()
        {
            _operationContext = default!;
            _selection = default!;
            _parent = default;
            _resolverResult = default;
            _hasResolverResult = false;
            _result = default;

            Path = default!;
            ScopedContextData = default!;
            LocalContextData = default!;
            IsResultModified = false;
            ValueType = null;
            ResponseIndex = default;
            ResultMap = default!;
            HasErrors = false;
            Arguments = default!;
        }
    }
}
