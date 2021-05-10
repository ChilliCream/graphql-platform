using System.Collections.Generic;
using HotChocolate.Execution.Processing.Tasks;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class ResolverQueryPlanStep : QueryPlanStep
    {
        private readonly HashSet<uint> _selections;

        public ResolverQueryPlanStep(ExecutionStrategy strategy, HashSet<uint> selections)
        {
            Strategy = strategy;
            _selections = selections;
        }

        public override ExecutionStrategy Strategy { get; }

#if NETSTANDARD2_0 || NETCOREAPP3_1
        public IReadOnlyCollection<uint> Selections => _selections;
#else
        public IReadOnlySet<uint> Selections => _selections;
#endif

        public override bool IsPartOf(IExecutionTask task) =>
            task is ResolverTaskBase resolverTask &&
            _selections.Contains(resolverTask.Selection.Id);
    }
}
