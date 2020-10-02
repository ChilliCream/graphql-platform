using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Execution.Utilities
{
    public interface ITaskDefinition
    {
        int Id { get; }

        ITaskDefinition Parent { get; }

        IReadOnlyList<ITaskDefinition> DependsOn { get; }

        IReadOnlyList<IExecutionTask> Create(/*????*/);
    }

    internal class TaskContext
    {
        public IOperationContext OperationContext { get; }

        public ISelection Selection { get; }

        public int ResponseIndex { get; }

        public object? Parent { get; }

        public Path Path { get; }

        public IImmutableDictionary<string, object?> ScopedContext { get; }

        // ?? Dependencies
    }
}
