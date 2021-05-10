using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing.Plan
{
    internal class QueryPlanBuilder
    {
        public static QueryPlan Build(IPreparedOperation operation)
        {
            var context = new QueryPlanBuilderContext(operation);
            Visit(operation.GetRootSelectionSet(), true, context);

            QueryPlanStep? root = null;

            if (context.Batches.Count == 1)
            {
                var steps = context.Batches[0].Steps;

                if (steps.Count == 1)
                {
                    root = steps[0];
                }
                else
                {
                    root = new SequenceQueryPlanStep(context.Batches[0].Steps.ToArray());
                }
            }
            else
            {
                var list = new List<QueryPlanStep>();

                foreach (var batch in context.Batches)
                {
                    if (batch.Steps.Count == 1)
                    {
                        list.Add(batch.Steps[1]);
                    }
                    else if (batch.Steps.Count > 1)
                    {
                        foreach (var step in batch.Steps)
                        {
                            list.Add(step);
                        }
                    }
                }

                root = new SequenceQueryPlanStep(list.ToArray());
            }

            return new QueryPlan(root);
        }

        private static void Visit(
            ISelectionSet selectionSet,
            bool rootSelection,
            QueryPlanBuilderContext context)
        {
            foreach (ISelection selection in selectionSet.Selections)
            {
                Visit(selection, rootSelection, context);
            }
        }

        private static void Visit(
            ISelection selection,
            bool rootSelection,
            QueryPlanBuilderContext context)
        {
            QueryStepBatch current = context.Batches.Peek();
            var pushed = false;

            switch (rootSelection ? SelectionExecutionStrategy.Default : selection.Strategy)
            {
                case SelectionExecutionStrategy.Default:
                case SelectionExecutionStrategy.Pure:
                    {
                        if (!current.Selections.TryPeek(out SelectionBatch selections) ||
                            selections.Strategy == ExecutionStrategy.Serial)
                        {
                            selections = CreateSelectionBatch(current, ExecutionStrategy.Parallel);
                            pushed = true;
                        }
                        selections.Selections.Add(selection.Id);
                        break;
                    }

                case SelectionExecutionStrategy.Serial:
                    {
                        if (!current.Selections.TryPeek(out SelectionBatch selections) ||
                            selections.Strategy == ExecutionStrategy.Parallel)
                        {
                            if (current.Selections.Count > 1)
                            {
                                SelectionBatch prev = current.Selections[current.Selections.Count - 2];
                                if (prev.Selections.Contains(context.Path.Peek().Id))
                                {
                                    prev.Selections.Add(selection.Id);
                                }
                            }
                            else
                            {
                                selections = CreateSelectionBatch(current, ExecutionStrategy.Serial);
                                selections.Selections.Add(selection.Id);
                                pushed = true;
                            }
                        }
                        else
                        {
                            selections.Selections.Add(selection.Id);
                        }
                        break;
                    }
            }

            context.Path.Push(selection);

            if (selection.SelectionSet is not null)
            {
                IPreparedOperation operation = context.Operation;
                foreach (var objectType in operation.GetPossibleTypes(selection.SelectionSet))
                {
                    Visit(
                        operation.GetSelectionSet(selection.SelectionSet, objectType),
                        false,
                        context);
                }
            }

            context.Path.Pop();

            if (pushed)
            {
                current.Selections.Pop();
            }
        }

        private static SelectionBatch CreateSelectionBatch(
            QueryStepBatch queryStep,
            ExecutionStrategy strategy)
        {
            var selections = new SelectionBatch(strategy);
            var step = new ResolverQueryPlanStep(selections.Strategy, selections.Selections);

            queryStep.Steps.Add(step);
            queryStep.Selections.Push(selections);

            return selections;
        }
    }
}
