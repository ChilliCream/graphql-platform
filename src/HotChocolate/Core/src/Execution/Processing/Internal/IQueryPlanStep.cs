using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing.Internal
{
    internal class QueryPlan
    {
        QueryPlanStep Root { get; }

        /// <summary>
        /// Gets the first step that needs to be executed.
        /// </summary>
        QueryPlanStep First { get; }

        // IQueryPlanStep? GetExecutionStep(ISelection selection);


    }

    internal class QueryPlanBuilder
    {
        public QueryPlan Build(IPreparedOperation operation)
        {

        }

        private void Visit(ISelectionVariants selectionVariants)
        {
            foreach (var objectType in selectionVariants.GetPossibleTypes())
            {
                Visit(selectionVariants.GetSelectionSet(objectType));
            }
        }

        private void Visit(ISelectionSet selectionSet)
        {
            foreach (ISelection selection in selectionSet.Selections)
            {
                Visit(selection);
            }
        }

        private void Visit(ISelection selection, QueryPlanBuilderContext context)
        {
            QueryBatch current = context.Batches.Peek();

            switch (selection.Strategy)
            {
                case SelectionExecutionStrategy.Default:
                case SelectionExecutionStrategy.Pure:
                    if (current.SelectionStrategy == ExecutionStrategy.Serial)
                    {
                        var selections = new HashSet<uint> { selection.Id };
                        var step = new ResolverQueryPlanStep(
                            ExecutionStrategy.Serial,
                            selections);

                        current.Batch.Add(step);
                        current.Selections.Push(selections);
                    }
                    else
                    {
                        current.Selections.Peek().Add(selection.Id);
                    }
                    break;

                case SelectionExecutionStrategy.Serial:
                    if (current.SelectionStrategy == ExecutionStrategy.Parallel)
                    {
                        if (context.Batches.Count > 1)
                        {
                            QueryBatch parent = context.Batches[^2];

                        }

                    }
                    else
                    {
                        current.Selections.Peek().Add(selection.Id);
                    }
                    break;

                default:
                    break;
            }
        }
    }

    internal class QueryPlanBuilderContext
    {
        public List<QueryBatch> Batches { get; } = new();
    }

    internal class QueryBatch
    {
        public ExecutionStrategy BatchStrategy { get; set; } = ExecutionStrategy.Parallel;

        public List<QueryPlanStep> Batch { get; } = new();

        public ExecutionStrategy SelectionStrategy { get; set; } = ExecutionStrategy.Parallel;

        public List<HashSet<uint>> Selections { get; } = new();
    }

    internal abstract class QueryPlanStep
    {
        public abstract ExecutionStrategy Strategy { get; }

        public QueryPlanStep? Next { get; internal set; }

        public virtual IReadOnlyList<QueryPlanStep> Steps => Array.Empty<QueryPlanStep>();


        public virtual void Initialize(IOperationContext context)
        {


        }

        public virtual bool IsAllowed(IExecutionTask task) => true;
    }

    internal sealed class ResolverQueryPlanStep : QueryPlanStep
    {
        private readonly HashSet<uint> _selections;

        public ResolverQueryPlanStep(
            ExecutionStrategy strategy,
            HashSet<uint> selections)
        {
            Strategy = strategy;
            _selections = selections;
        }

        public override ExecutionStrategy Strategy { get; }

        public override bool IsAllowed(IExecutionTask task) =>
            task is ResolverTaskBase resolverTask &&
            _selections.Contains(resolverTask.Selection.Id);
    }

    public enum ExecutionStrategy
    {
        Serial,
        Parallel
    }
}
