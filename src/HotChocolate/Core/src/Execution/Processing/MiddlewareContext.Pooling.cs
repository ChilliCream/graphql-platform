using System.Collections.Immutable;

namespace HotChocolate.Execution.Processing
{
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
            Path = path;
            ScopedContextData = scopedContextData;
            LocalContextData = ImmutableDictionary<string, object?>.Empty;
            Arguments = _selection.Arguments;
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
