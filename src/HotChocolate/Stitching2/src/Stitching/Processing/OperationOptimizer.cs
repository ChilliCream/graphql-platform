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
            FetchTask? parent)
        {
            foreach (ISelection selection in selectionSet.Selections)
            {
                Visit(context, selectionSet, parent);
            }
        }

        private void Visit(
            OptimizerContext context,
            ISelection selection,
            FetchTask? parent)
        {
            IReadOnlyList<IFetchConfiguration> configurations =
                GetFetchConfigurations(selection);

            if (configurations.Count == 0)
            {
                // todo: throw helper.
                throw new Exception();
            }

            IReadOnlyList<ISelection>? handledSelections = null;
            FetchTask? task = null;

            if (configurations.Count == 1)
            {
                IFetchConfiguration? configuration = configurations[0];
                if (!configuration.CanHandleSelections(
                    context.Operation,
                    selection,
                    out handledSelections))
                {
                    task = new FetchTask(
                        new HashSet<ISelection>(handledSelections),
                        configuration,
                        parent?.Path.Push(parent) ?? ImmutableStack<FetchTask>.Empty,
                        Array.Empty<FetchTask>());
                }
            }
            else
            {
                var tasks = new List<FetchTask>();

                foreach (IFetchConfiguration configuration in configurations)
                {
                    if (!configuration.CanHandleSelections(
                        context.Operation,
                        selection,
                        out handledSelections))
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

                    if (task is not null)
                    {
                        tasks.Remove(task);
                    }
                }
            }

            if (task is not null)
            {
                if (parent is null)
                {
                    context.QueryPlan.Add(task);
                }
                else
                {
                    parent.Children.Add(task);
                }

                if (selection.SelectionSet is not null)
                {
                    foreach (IObjectType selectionType in
                        context.Operation.GetPossibleTypes(selection.SelectionSet))
                    {
                        ISelectionSet selectionSet = context.Operation.GetSelectionSet(
                            selection.SelectionSet, selectionType);
                        Visit(context, selectionSet, task);
                    }
                }
            }
        }

        private IReadOnlyList<IFetchConfiguration> GetFetchConfigurations(
            ISelection selection) =>
            (IReadOnlyList<IFetchConfiguration>)selection.Field.ContextData["fetch"]!;

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