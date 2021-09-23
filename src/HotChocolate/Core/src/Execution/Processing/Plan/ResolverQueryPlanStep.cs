using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HotChocolate.Execution.Processing.Tasks;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class ResolverQueryPlanStep : ExecutionStep
    {
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
            _selections = selections.ToArray();
        }

        public override string Name =>
            Strategy is ExecutionStrategy.Serial
                ? "SerialResolver"
                : "Resolver";

        public ExecutionStrategy Strategy { get; }

        public IReadOnlyList<ISelection> Selections => _selections;

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

            if (task is ResolverTask resolverTask)
            {
                foreach (var childTask in resolverTask.ChildTasks)
                {
                    state.Selections.Add(childTask.Selection.Id);
                }

                state.RegisterUnsafe(resolverTask.ChildTasks);
            }
        }

        public override string ToString()
        {
            return $"{Name}[{string.Join(", ", _selections.Select(t => t.Id))}]";
        }
    }
}
