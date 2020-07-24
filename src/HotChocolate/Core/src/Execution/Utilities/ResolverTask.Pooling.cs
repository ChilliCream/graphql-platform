using System.Collections.Immutable;

namespace HotChocolate.Execution.Utilities
{
    internal sealed partial class ResolverTask
    {
        public void Initialize(
            IOperationContext operationContext,
            IPreparedSelection selection,
            ResultMap resultMap,
            int responseIndex,
            object? parent,
            Path path,
            IImmutableDictionary<string, object?> scopedContextData)
        {
            _task = default;
            _operationContext = operationContext;
            _selection = selection;
            _context.Initialize(
                operationContext,
                selection,
                resultMap,
                responseIndex,
                parent,
                path,
                scopedContextData);
        }

        public bool Reset()
        {
            _task = default!;
            _operationContext = default!;
            _selection = default!;
            _context.Reset();
            return true;
        }
    }
}
