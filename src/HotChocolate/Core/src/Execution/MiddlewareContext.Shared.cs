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
            object? parent, 
            IImmutableStack<object?> source,
            Path path,
            IImmutableDictionary<string, object?> scopedContextData)
        {
            _operationContext = operationContext;
            _selection = selection;
            _parent = parent;
            Source = source;
            Path = path;
            ScopedContextData = scopedContextData;
        }

        public void Clear()
        {
            _operationContext = default!;
            _selection = default!;
            _parent = default;
            _resolverResult = default;
            _result = default;

            Source = default!;
            Path = default!;
            ScopedContextData = default!;
            LocalContextData = _emptyContext;
            IsResultModified = false;
        }
    }
}
