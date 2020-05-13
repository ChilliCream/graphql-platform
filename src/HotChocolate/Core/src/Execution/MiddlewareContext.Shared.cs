using System.Collections.Immutable;
using HotChocolate.Execution.Utilities;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution
{
    internal partial class MiddlewareContext : IMiddlewareContext
    {
        private readonly static IImmutableDictionary<string, object?> _emptyContext = 
            ImmutableDictionary<string, object?>.Empty;

        public void Initialize(
            IOperationContext operationContext, 
            IPreparedSelection selection,
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
            Arguments = _selection.Arguments;
        }

        public void Clear()
        {
            _operationContext = default!;
            _selection = default!;
            _parent = default;
            _resolverResult = default;
            _result = default;

            Path = default!;
            ScopedContextData = default!;
            LocalContextData = _emptyContext;
            IsResultModified = false;
            ValueType = null;
            ResponseIndex = default;
            ResultMap = default!;
            HasErrors = false;
            Arguments = null;
        }
    }
}
