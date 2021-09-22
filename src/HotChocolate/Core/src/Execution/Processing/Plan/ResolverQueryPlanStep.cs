using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HotChocolate.Execution.Processing.Tasks;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class ResolverQueryPlanStep : ExecutionStep
    {
        private readonly HashSet<int> _ids;
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
            _ids = new HashSet<int>(selections.Select(t => t.Id));
            _selections = selections.ToArray();
        }

        public override string Name =>
            Strategy is ExecutionStrategy.Serial
                ? "SerialResolver"
                : "Resolver";

        public ExecutionStrategy Strategy { get; }

        public override bool TryInitialize(IQueryPlanState state)
        {
            IVariableValueCollection variables = state.Context.Variables;

            foreach (var selection in _selections)
            {
                if (state.Selections.Contains(selection.Id) && selection.IsIncluded(variables))
                {
                    return true;
                }
            }

            return false;
        }

        public override void CompleteTask(IQueryPlanState state, IExecutionTask task)
        {
            Debug.Assert(ReferenceEquals(task.State, this), "The task must be part of this step.");

            ResolverTask resolverTask = (ResolverTask)task;

            foreach (var childTask in resolverTask.ChildTasks)
            {
                state.Selections.Add(childTask.Selection.Id);
            }

            state.Context.Scheduler.Register(resolverTask.ChildTasks);
        }

        public override string ToString()
        {
            return $"{Name}[{string.Join(", ", _selections.Select(t => t.Id))}]";
        }
    }
}
