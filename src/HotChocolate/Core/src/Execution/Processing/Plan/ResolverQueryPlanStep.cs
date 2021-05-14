using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class ResolverQueryPlanStep : QueryPlanStep
    {
        private readonly HashSet<uint> _ids;
        private readonly ISelection[] _selections;

        public ResolverQueryPlanStep(
            ExecutionStrategy strategy,
            IReadOnlyList<ISelection> selections)
        {
            if (selections is null)
            {
                throw new ArgumentNullException(nameof(selections));
            }

            Strategy = strategy;
            _ids = new HashSet<uint>(selections.Select(t => t.Id));
            _selections = selections.ToArray();
        }

        public override ExecutionStrategy Strategy { get; }

        public override bool Initialize(IOperationContext context)
        {
            IVariableValueCollection variables = context.Variables;

            for (var i = 0; i < _selections.Length; i++)
            {
                if (_selections[i].IsIncluded(variables))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool IsPartOf(IExecutionTask task) =>
            task is ResolverTaskBase resolverTask &&
            _ids.Contains(resolverTask.Selection.Id);
    }
}
