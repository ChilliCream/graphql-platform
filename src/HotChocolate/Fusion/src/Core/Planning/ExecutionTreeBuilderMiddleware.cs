using System.Collections.Immutable;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Utilities;

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
            ProcessBacklog(context, backlog);
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





    private void ProcessBacklog(
        QueryPlanContext context,
        Queue<BacklogItem> backlog)
    {
        while (backlog.TryDequeue(out var next))
        {
            if (next.Batch.Length == 0)
            {
                continue;
            }

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
                var compose = new Compose(context.NextNodeId(), selectionSet);
                next.Parent.AddNode(compose);

                context.Complete(single.Step);

                // RegisterBranches(context, single.Node, next.Completed, backlog);
            }

            // If there are multiple side-effect-free nodes we can
            // group them into a parallel execution node.
            else
            {
                var parallel = new Parallel(context.NextNodeId());
                var selectionSets = new List<ISelectionSet>();

                foreach (var item in next.Batch)
                {
                    if (item.Node.Kind is not QueryPlanNodeKind.ResolveNode)
                    {
                        selectionSets.Add(ResolveSelectionSet(context, item.Step));
                    }

                    parallel.AddNode(item.Node);
                    context.Complete(item.Step);

                    // RegisterBranches(context, item.Node, next.Completed, backlog);
                }

                // we will chain a single composition step at the end.
                // The parent node always is serial.
                var compose = new Compose(context.NextNodeId(), selectionSets);

                next.Parent.AddNode(parallel);
                next.Parent.AddNode(compose);
            }

            var batch = context.NextBatch();

            if (batch.Length > 0)
            {
                backlog.Enqueue(new BacklogItem(batch, next.Parent));
            }
        }
    }

    private static QueryPlanNode CreateDefaultRoot(
        QueryPlanContext context,
        Queue<BacklogItem> backlog,
        HashSet<ExecutionStep> completed)
    {
        var parent = new Sequence(context.NextNodeId());

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
        var (step, root) = context.Nodes.First(t => t.Value.Kind is QueryPlanNodeKind.Subscribe);

        var parent = new Sequence(context.NextNodeId());
        root.AddNode(parent);


        var selectionSet = ResolveSelectionSet(context, step);
        var compose = new Compose(context.NextNodeId(), selectionSet);
        parent.AddNode(compose);
        context.Complete(step);

        var backlogItem = new BacklogItem(
            context.NextBatch(),
            parent,
            completed.ToImmutableHashSet());

        backlog.Enqueue(backlogItem);

        return root;
    }

    private static void RegisterChildren(
        QueryPlanContext context,
        Stack<QueryPlanNode> stack,
        QueryPlanNode node)
    {
        stack.Clear();
        stack.Push(node);

        while (stack.TryPop(out var current))
        {
            foreach (var next in current.Nodes)
            {
                stack.Push(next);
            }

            if (current is ResolverNodeBase resolver)
            {
                resolver.SelectionSet
            }
        }
    }

    private ISelectionSet ResolveSelectionSet(
        QueryPlanContext context,
        ExecutionStep executionStep)
        => executionStep.ParentSelection is null
            ? context.Operation.RootSelectionSet
            : context.Operation.GetSelectionSet(
                executionStep.ParentSelection,
                _schema.GetType<Types.ObjectType>(executionStep.SelectionSetTypeInfo.Name));

    private readonly record struct BacklogItem(NodeAndStep[] Batch, Sequence Parent);
}

static file class ExecutionTreeBuilderMiddlewareExtensions
{
    public static Sequence ExpectSerial(this QueryPlanNode node)
    {
        if (node is not Sequence serialNode)
        {
            throw new ArgumentException(
                "The node is expected to be a serial node.");
        }

        return serialNode;
    }
}

internal readonly record struct NodeAndStep(QueryPlanNode Node, ExecutionStep Step);
