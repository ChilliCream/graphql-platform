using System.Collections.Immutable;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Planning.QueryPlanNodeKind;

namespace HotChocolate.Fusion.Planning;

internal sealed class ExecutionTreeBuilderMiddleware : IQueryPlanMiddleware
{
    private readonly ISchema _schema;

    public ExecutionTreeBuilderMiddleware(ISchema schema)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    public void Invoke(QueryPlanContext context, QueryPlanDelegate next)
    {
        var backlog = new Queue<BacklogItem>();
        var completed = new HashSet<ExecutionStep>();

        // Depending on the operation type we will create the root nodes of the query plan.
        var rootNode = context.Operation.Type is OperationType.Subscription
            ? CreateSubscriptionRoot(context, backlog, completed)
            : CreateDefaultRoot(context, backlog, completed);

        if (backlog.Count > 0)
        {
            // If there are nodes that are dependant on the root node and are
            // not yet integrated into the execution tree we will start
            // processing the backlog to do so.
            ProcessBacklog(context, backlog, completed);
        }

        // Last we will create the query plan.
        context.Plan = new QueryPlan(
            context.Operation,
            rootNode,
            context.Exports.All
                .GroupBy(t => t.SelectionSet)
                .ToDictionary(t => t.Key, t => t.Select(x => x.StateKey).ToArray()),
            context.HasNodes);

        next(context);
    }

    private readonly record struct BacklogItem(
        NodeAndStep[] Batch,
        SerialNode Parent,
        ImmutableHashSet<ExecutionStep> Completed);

    private readonly record struct NodeAndStep(QueryPlanNode Node, ExecutionStep Step);

    private void ProcessBacklog(
        QueryPlanContext context,
        Queue<BacklogItem> backlog,
        HashSet<ExecutionStep> completed)
    {
        while (backlog.TryDequeue(out var next))
        {
            if (next.Batch.Length == 0)
            {
                continue;
            }

            var pathCompleted = next.Completed;

            // If the batch contains only one node we can add it to the parent node which will be
            // a serial node. Same applies if the current batch steps deal with the
            // mutation selection set as we need to adhere to the GraphQL spec and ensure
            // that mutation fields are executed serially.
            if (next.Batch.Length == 1 ||
                (_schema.MutationType?.Name.EqualsOrdinal(
                        next.Batch[0].Step.SelectionSetTypeInfo.Name) ==
                    true))
            {
                var single = next.Batch[0];
                next.Parent.AddNode(single.Node);

                var selectionSet = ResolveSelectionSet(context, single.Step);
                var compose = new CompositionNode(context.CreateNodeId(), selectionSet);
                next.Parent.AddNode(compose);

                context.Nodes.Remove(single.Step);
                completed.Add(single.Step);
                pathCompleted = pathCompleted.Add(single.Step);

                RegisterBranches(context, single.Node, next.Completed, backlog);
            }

            // If there are multiple side-effect-free nodes we can
            // group them into a parallel execution node.
            else
            {
                var parallel = new ParallelNode(context.CreateNodeId());
                var selectionSets = new List<ISelectionSet>();

                foreach (var item in next.Batch)
                {
                    selectionSets.Add(ResolveSelectionSet(context, item.Step));
                    parallel.AddNode(item.Node);
                    context.Nodes.Remove(item.Step);
                    completed.Add(item.Step);
                    pathCompleted = pathCompleted.Add(item.Step);

                    RegisterBranches(context, item.Node, next.Completed, backlog);
                }

                // we will chain a single composition step at the end.
                // The parent node always is serial.
                var compose = new CompositionNode(context.CreateNodeId(), selectionSets);

                next.Parent.AddNode(parallel);
                next.Parent.AddNode(compose);
            }

            var batch = CreateBatch(context, pathCompleted);

            if (batch.Length > 0)
            {
                backlog.Enqueue(new BacklogItem(batch, next.Parent, pathCompleted));
            }
        }
    }

    private static QueryPlanNode CreateDefaultRoot(
        QueryPlanContext context,
        Queue<BacklogItem> backlog,
        HashSet<ExecutionStep> completed)
    {
        var parent = new SerialNode(context.CreateNodeId());

        var batch = context.Nodes
            .Where(t => t.Key.DependsOn.Count == 0)
            .Select(t => new NodeAndStep(t.Value, t.Key))
            .ToArray();

        var backlogItem = new BacklogItem(
            batch,
            parent.ExpectSerial(),
            completed.ToImmutableHashSet());

        backlog.Enqueue(backlogItem);

        return parent;
    }

    private QueryPlanNode CreateSubscriptionRoot(
        QueryPlanContext context,
        Queue<BacklogItem> backlog,
        HashSet<ExecutionStep> completed)
    {
        var (step, root) = context.Nodes.First(t => t.Value.Kind is Subscription);

        var parent = new SerialNode(context.CreateNodeId());
        root.AddNode(parent);

        var selectionSet = ResolveSelectionSet(context, step);
        var compose = new CompositionNode(context.CreateNodeId(), selectionSet);
        parent.AddNode(compose);

        completed.Add(step);
        context.Nodes.Remove(step);

        var batch = context.Nodes
            .Where(t => completed.IsSupersetOf(t.Key.DependsOn))
            .Select(t => new NodeAndStep(t.Value, t.Key))
            .ToArray();

        var backlogItem = new BacklogItem(
            batch,
            parent,
            completed.ToImmutableHashSet());

        backlog.Enqueue(backlogItem);

        return root;
    }

    private static void RegisterBranches(
        QueryPlanContext context,
        QueryPlanNode node,
        ImmutableHashSet<ExecutionStep> completed,
        Queue<BacklogItem> backlog)
    {
        if (node.Kind == NodeResolver)
        {
            foreach (var branch in node.Nodes)
            {
                if (branch is not SerialNode serialNode)
                {
                    // todo : error helper
                    throw new InvalidOperationException(
                        "A node branch must be  serial node.");
                }

                if (serialNode.Nodes.Count != 1)
                {
                    // todo : error helper
                    throw new InvalidOperationException(
                        "A node branch must contain exactly one node.");
                }

                var resolverNode = serialNode.Nodes[0];
                var result = context.Nodes.FirstOrDefault(t => t.Value.Equals(resolverNode));

                if (result.Value != resolverNode)
                {
                    continue;
                }

                context.Nodes.Remove(result.Key);

                var branchCompleted = completed.Add(result.Key);
                var branchBatch = CreateBatch(context, branchCompleted);

                if (branchBatch.Length > 0)
                {
                    var branchBacklog = new BacklogItem(
                        branchBatch,
                        serialNode,
                        branchCompleted);
                    backlog.Enqueue(branchBacklog);
                }
            }
        }
    }

    private static NodeAndStep[] CreateBatch(
        QueryPlanContext context,
        ImmutableHashSet<ExecutionStep> completed)
        => context.Nodes
            .Where(t => completed.IsSupersetOf(t.Key.DependsOn))
            .Select(t => new NodeAndStep(t.Value, t.Key))
            .ToArray();

    private ISelectionSet ResolveSelectionSet(
        QueryPlanContext context,
        ExecutionStep executionStep)
        => executionStep.ParentSelection is null
            ? context.Operation.RootSelectionSet
            : context.Operation.GetSelectionSet(
                executionStep.ParentSelection,
                _schema.GetType<Types.ObjectType>(executionStep.SelectionSetTypeInfo.Name));
}

static file class ExecutionTreeBuilderMiddlewareExtensions
{
    public static SerialNode ExpectSerial(this QueryPlanNode node)
    {
        if (node is not SerialNode serialNode)
        {
            throw new ArgumentException(
                "The node is expected to be a serial node.");
        }

        return serialNode;
    }
}
