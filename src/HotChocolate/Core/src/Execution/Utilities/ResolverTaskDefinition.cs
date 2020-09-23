using System.Collections.Immutable;

namespace HotChocolate.Execution.Utilities
{
    internal readonly struct ResolverTaskDefinition
    {
        public ResolverTaskDefinition(
            IOperationContext operationContext, 
            ISelection selection, 
            int responseIndex, 
            ResultMap resultMap, 
            object? parent, 
            Path path, 
            IImmutableDictionary<string, object?> scopedContextData)
        {
            OperationContext = operationContext;
            Selection = selection;
            ResponseIndex = responseIndex;
            ResultMap = resultMap;
            Parent = parent;
            Path = path;
            ScopedContextData = scopedContextData;
        }

        public IOperationContext OperationContext { get; }

        public ISelection Selection { get; }

        public int ResponseIndex { get; }

        public ResultMap ResultMap { get; }

        public object? Parent { get; }

        public Path Path { get; }
        
        public IImmutableDictionary<string, object?> ScopedContextData { get; }
    }
}
