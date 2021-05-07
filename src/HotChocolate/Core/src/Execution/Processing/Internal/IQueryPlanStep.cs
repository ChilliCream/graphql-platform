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
            var context = new QueryPlanBuilderContext(operation);
            Visit(operation.GetRootSelectionSet(), context);


        }

        private void Visit(ISelectionSet selectionSet, QueryPlanBuilderContext context)
        {
            foreach (ISelection selection in selectionSet.Selections)
            {
                Visit(selection, context);
            }
        }

        private void Visit(ISelection selection, QueryPlanBuilderContext context)
        {
            QueryStepBatch current = context.Steps.Peek();
            SelectionBatch selections = current.Selections.Peek();
            var pushed = false;

            switch (selection.Strategy)
            {
                case SelectionExecutionStrategy.Default:
                case SelectionExecutionStrategy.Pure:
                    if (selections.Strategy == ExecutionStrategy.Serial)
                    {
                        selections = CreateSelectionBatch(current, ExecutionStrategy.Serial);
                        pushed = true;
                    }
                    selections.Selections.Add(selection.Id);
                    break;

                case SelectionExecutionStrategy.Serial:
                    if (selections.Strategy == ExecutionStrategy.Parallel)
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

            context.Path.Push(selection);

            if (selection.SelectionSet is not null)
            {
                IPreparedOperation operation = context.Operation;
                foreach (var objectType in operation.GetPossibleTypes(selection.SelectionSet))
                {
                    Visit(operation.GetSelectionSet(selection.SelectionSet, objectType), context);
                }
            }

            context.Path.Pop();

            if (pushed)
            {
                current.Selections.Pop();
            }
        }

        private SelectionBatch CreateSelectionBatch(
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

    internal sealed class QueryPlanBuilderContext
    {
        public QueryPlanBuilderContext(IPreparedOperation operation)
        {
            Operation = operation;
            Steps.Add(new QueryStepBatch(ExecutionStrategy.Serial));
        }

        public IPreparedOperation Operation { get; }

        public List<QueryStepBatch> Steps { get; } = new();

        public List<ISelection> Path { get; } = new();
    }

    internal sealed class QueryStepBatch
    {
        public QueryStepBatch(ExecutionStrategy strategy)
        {
            Strategy = strategy;
        }

        public ExecutionStrategy Strategy { get; }

        public List<QueryPlanStep> Steps { get; } = new();

        public List<SelectionBatch> Selections { get; } = new();
    }

    internal readonly struct SelectionBatch
    {
        public SelectionBatch(ExecutionStrategy strategy)
        {
            Strategy = strategy;
            Selections = new HashSet<uint>();
        }

        public ExecutionStrategy Strategy { get; }

        public HashSet<uint> Selections { get; }
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

    internal sealed class SequenceQueryPlanStep : QueryPlanStep
    {
        private readonly QueryPlanStep[] _steps;

        public SequenceQueryPlanStep(QueryPlanStep[] steps)
        {
            _steps = steps;
        }

        public override ExecutionStrategy Strategy => ExecutionStrategy.Serial;

        public override IReadOnlyList<QueryPlanStep> Steps => _steps;
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
