using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Parallel = HotChocolate.Fusion.Execution.Nodes.Parallel;

namespace HotChocolate.Fusion.Planning.Pipeline;

internal sealed class ExecutionTreeBuilderMiddleware(ISchema schema) : IQueryPlanMiddleware
{
    private readonly ISchema _schema = schema
        ?? throw new ArgumentNullException(nameof(schema));

    public void Invoke(QueryPlanContext context, QueryPlanDelegate next)
    {
        var backlog = new Queue<BacklogItem>();

        // Depending on the operation type we will create the root nodes of the query plan.
        var rootNode = context.Operation.Type is OperationType.Subscription
            ? CreateSubscriptionRoot(context, backlog)
            : CreateDefaultRoot(context, backlog);
        context.SetRootNode(rootNode);

        if (backlog.Count > 0)
        {
            // If there are nodes that are dependant on the root node and are
            // not yet integrated into the execution tree we will start
            // processing the backlog to do so.
            ProcessBacklog(context, backlog);
        }

        // We fail if we were unable to include all execution steps into the query plan.
        context.EnsureAllStepsAreCompleted();

        next(context);
    }

    private void ProcessBacklog(
        QueryPlanContext context,
        Queue<BacklogItem> backlog)
    {
        var stack = new Stack<QueryPlanNode>();
        var lookup = BuildSelectionSetToStepLookup(context, stack);

        while (backlog.TryDequeue(out var next))
        {
            // If the batch contains only one node we can add it to the parent node which will be
            // a serial node. Same applies if the current batch steps deal with the
            // mutation selection set as we need to adhere to the GraphQL spec and ensure
            // that mutation fields are executed serially.
            if (next.Batch.Length == 1 ||
                (_schema.MutationType?.Name.EqualsOrdinal(
                    next.Batch[0].Step.SelectionSetTypeMetadata.Name) ?? false))
            {
                var single = next.Batch[0];
                next.Parent.AddNode(single.Node);

                var selectionSet = Unsafe.As<SelectionSet>(ResolveSelectionSet(context, single.Step));

                if (NeedsComposition(single.Step, selectionSet))
                {
                    var compose = new Compose(context.NextNodeId(), selectionSet);
                    next.Parent.AddNode(compose);
                }

                Complete(context, stack, single.Node);
            }

            // If there are multiple side-effect-free nodes we can
            // group them into a parallel execution node.
            else
            {
                var parallel = new Parallel(context.NextNodeId());
                var selectionSets = new List<SelectionSet>();

                foreach (var item in next.Batch)
                {
                    var selectionSet = Unsafe.As<SelectionSet>(ResolveSelectionSet(context, item.Step));

                    if (NeedsComposition(item.Step, selectionSet))
                    {
                        selectionSets.Add(selectionSet);
                    }

                    parallel.AddNode(item.Node);
                    Complete(context, stack, item.Node);
                }

                next.Parent.AddNode(parallel);

                if (selectionSets.Count > 0)
                {
                    // we will chain a single composition step at the end.
                    // The parent node always is serial.
                    var compose = new Compose(context.NextNodeId(), selectionSets);
                    next.Parent.AddNode(compose);
                }
            }

            var batch = context.NextBatch();

            if (batch.Length > 0)
            {
                backlog.Enqueue(next with { Batch = batch, });
            }
        }

        bool NeedsComposition(ExecutionStep step, SelectionSet selectionSet)
        {
            var steps = lookup[selectionSet];
            steps.Remove(step);
            return steps.Count == 0;
        }
    }

    private Dictionary<ISelectionSet, HashSet<ExecutionStep>> BuildSelectionSetToStepLookup(
        QueryPlanContext context,
        Stack<QueryPlanNode> stack)
    {
        var map = new Dictionary<ISelectionSet, HashSet<ExecutionStep>>();
        var childSteps = new HashSet<ExecutionStep>();

        foreach (var item in context.AllNodes())
        {
            CollectChildSteps(context, stack, childSteps, item.Node);
            var selectionSet = ResolveSelectionSet(context, item.Step);

            if (!map.TryGetValue(selectionSet, out var set))
            {
                set = [];
                map.Add(selectionSet, set);
            }

            set.Add(item.Step);
        }

        foreach (var sets in map.Values)
        {
            sets.ExceptWith(childSteps);
        }

        return map;
    }

    private static void CollectChildSteps(
        QueryPlanContext context,
        Stack<QueryPlanNode> stack,
        HashSet<ExecutionStep> childSteps,
        QueryPlanNode node)
    {
        if (node.Nodes.Count == 0)
        {
            return;
        }

        EnqueueChildren(node, stack);

        while (stack.TryPop(out var current))
        {
            EnqueueChildren(current, stack);

            if (context.TryGetExecutionStep(current, out var executionStep))
            {
                childSteps.Add(executionStep);
            }
        }

        static void EnqueueChildren(QueryPlanNode node, Stack<QueryPlanNode> stack)
        {
            foreach (var next in node.Nodes)
            {
                stack.Push(next);
            }
        }
    }

    private static Sequence CreateDefaultRoot(
        QueryPlanContext context,
        Queue<BacklogItem> backlog)
    {
        var parent = new Sequence(context.NextNodeId());
        TryEnqueueBatch(context, backlog, parent);
        return parent;
    }

    private QueryPlanNode CreateSubscriptionRoot(
        QueryPlanContext context,
        Queue<BacklogItem> backlog)
    {
        var (root, step) = context.GetSubscribeRoot();

        var parent = new Sequence(context.NextNodeId());
        root.AddNode(parent);

        var selectionSet = Unsafe.As<SelectionSet>(ResolveSelectionSet(context, step));
        var compose = new Compose(context.NextNodeId(), selectionSet);
        parent.AddNode(compose);
        context.Complete(step);

        TryEnqueueBatch(context, backlog, parent);

        return root;
    }

    private static void Complete(
        QueryPlanContext context,
        Stack<QueryPlanNode> stack,
        QueryPlanNode node)
    {
        if (node.Nodes.Count == 0)
        {
            context.Complete(node);
            return;
        }

        stack.Push(node);

        while (stack.TryPop(out var current))
        {
            foreach (var next in current.Nodes)
            {
                stack.Push(next);
            }

            if (current is ResolverNodeBase resolver)
            {
                context.RegisterSelectionSet(resolver.SelectionSet);
            }

            context.Complete(current);
        }
    }

    private static void TryEnqueueBatch(
        QueryPlanContext context,
        Queue<BacklogItem> backlog,
        Sequence parent)
    {
        var batch = context.NextBatch();

        if (batch.Length == 0)
        {
            return;
        }

        backlog.Enqueue(new BacklogItem(batch, parent));
    }

    private ISelectionSet ResolveSelectionSet(
        QueryPlanContext context,
        ExecutionStep executionStep)
    {
        if (executionStep is SelectionExecutionStep selectionExecStep &&
            selectionExecStep.Resolver is null &&
            selectionExecStep.SelectionResolvers.Count == 0 &&
            selectionExecStep.ParentSelectionPath is not null)
        {
            return context.Operation.RootSelectionSet;
        }

        return executionStep.ParentSelection is null
            ? context.Operation.RootSelectionSet
            : context.Operation.GetSelectionSet(
                executionStep.ParentSelection,
                _schema.GetType<Types.ObjectType>(executionStep.SelectionSetTypeMetadata.Name));
    }

    private readonly record struct BacklogItem(NodeAndStep[] Batch, Sequence Parent);
}
