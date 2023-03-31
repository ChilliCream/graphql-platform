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

        next(context);
    }


    private void ProcessBacklog(
        QueryPlanContext context,
        Queue<BacklogItem> backlog)
    {
        var stack = new Stack<QueryPlanNode>();

        while (backlog.TryDequeue(out var next))
        {
            // If the batch contains only one node we can add it to the parent node which will be
            // a serial node. Same applies if the current batch steps deal with the
            // mutation selection set as we need to adhere to the GraphQL spec and ensure
            // that mutation fields are executed serially.
            if (next.Batch.Length == 1 ||
                (_schema.MutationType?.Name.EqualsOrdinal(
                        next.Batch[0].Step.SelectionSetTypeInfo.Name) ??
                    false))
            {
                var single = next.Batch[0];
                next.Parent.AddNode(single.Node);

                var selectionSet = ResolveSelectionSet(context, single.Step);
                var compose = new Compose(context.NextNodeId(), selectionSet);
                next.Parent.AddNode(compose);

                Complete(context, stack, single.Node);
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
                    Complete(context, stack, item.Node);
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

        var selectionSet = ResolveSelectionSet(context, step);
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
