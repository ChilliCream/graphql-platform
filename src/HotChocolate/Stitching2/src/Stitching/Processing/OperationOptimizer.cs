using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Processing
{
    public class OperationOptimizer
    {
        public void Optimize(IPreparedOperation operation, ObjectType objectType)
        {
            Visit(
                new OptimizerContext(operation),
                operation.GetRootSelectionSet(),
                null);
        }

        private void Visit(
            OptimizerContext context,
            ISelectionSet selectionSet,
            IObjectType objectType,
            FetchTask? parent)
        {
            if (!context.Processed.IsSupersetOf(selectionSet.Selections))
            {
                IReadOnlyList<IFetchConfiguration> configurations =
                    GetFetchConfigurations(objectType);

                var tasks = new List<FetchTask>();
                FetchTask? task = null;

                foreach (IFetchConfiguration configuration in configurations)
                {
                    if (configuration.CanHandleSelections(
                        context.Operation,
                        selectionSet,
                        objectType,
                        out IReadOnlyList<ISelection> handledSelections))
                    {
                        var current = new FetchTask(
                            new HashSet<ISelection>(handledSelections),
                            configuration,
                            parent?.Path.Push(parent) ?? ImmutableStack<FetchTask>.Empty,
                            tasks);

                        if (task is null || task.Selections.Count < handledSelections.Count)
                        {
                            task = current;
                        }

                        tasks.Add(current);
                    }
                }

                if (task is not null)
                {
                    tasks.Remove(task);

                    foreach (ISelection handledSelection in task.Selections)
                    {
                        context.Processed.Add(handledSelection);
                    }

                    if (parent is null)
                    {
                        context.QueryPlan.Add(task);
                    }
                    else
                    {
                        parent.Children.Add(task);
                    }

                    parent = task;
                }
            }

            foreach (ISelection selection in selectionSet.Selections)
            {
                Visit(context, selection, parent);
            }
        }

        private void Visit(
            OptimizerContext context,
            ISelection selection,
            FetchTask? parent)
        {
            if (selection.SelectionSet is not null)
            {
                foreach (IObjectType objectType in
                    context.Operation.GetPossibleTypes(selection.SelectionSet))
                {
                    ISelectionSet selectionSet = 
                        context.Operation.GetSelectionSet(selection.SelectionSet, objectType);
                    Visit(context, selectionSet, objectType, parent);
                }
            }
        }

        private IReadOnlyList<IFetchConfiguration> GetFetchConfigurations(
            IObjectType objectType) =>
            (IReadOnlyList<IFetchConfiguration>)objectType.ContextData["fetch"]!;

        private class OptimizerContext
        {
            public OptimizerContext(IPreparedOperation operation)
            {
                Operation = operation;
                Processed = new HashSet<ISelection>();
                QueryPlan = new List<FetchTask>();
            }

            public IPreparedOperation Operation { get; }

            public ISet<ISelection> Processed { get; }

            public IList<FetchTask> QueryPlan { get; }
        }

        // TODO : Naming
        private class FetchTask
        {
            public FetchTask(
                ISet<ISelection> selections,
                IFetchConfiguration configuration,
                IImmutableStack<FetchTask> path,
                IReadOnlyList<FetchTask> alternativeTasks)
            {
                Selections = selections;
                Configuration = configuration;
                Path = path;
                AlternativeTasks = alternativeTasks;
                Children = new List<FetchTask>();
            }

            public ISet<ISelection> Selections { get; }

            public IFetchConfiguration Configuration { get; }

            public IReadOnlyList<FetchTask> AlternativeTasks { get; }

            public IImmutableStack<FetchTask> Path { get; }

            // TODO : Naming
            public IList<FetchTask> Children { get; }
        }
    }
}