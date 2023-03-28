using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
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
        var rootNode = BuildQueryTree(context);

        context.Plan = new QueryPlan(
            context.Operation,
            rootNode,
            context.Exports.All
                .GroupBy(t => t.SelectionSet)
                .ToDictionary(t => t.Key, t => t.Select(x => x.StateKey).ToArray()),
            context.HasNodes);

        next(context);
    }

    private QueryPlanNode BuildQueryTree(QueryPlanContext context)
    {
        var completed = new HashSet<ExecutionStep>();
        KeyValuePair<ExecutionStep, QueryPlanNode>[] current;
        QueryPlanNode parent;

        if (context.Operation.Type is not OperationType.Subscription)
        {
            current = context.Nodes.Where(t => t.Key.DependsOn.Count is 0).ToArray();
            parent = new SerialNode(context.CreateNodeId());
        }
        else
        {
            var root = context.Nodes.First(t => t.Value.Kind is QueryPlanNodeKind.Subscription);
            parent = root.Value;

            var selectionSet = ResolveSelectionSet(context, root.Key);
            var compose = new CompositionNode(context.CreateNodeId(), selectionSet);
            parent.AddNode(compose);
            completed.Add(root.Key);
            context.Nodes.Remove(root.Key);

            current = context.Nodes.Where(t => completed.IsSupersetOf(t.Key.DependsOn)).ToArray();
        }

        while (current.Length > 0)
        {
            if (current.Length is 1 ||
                (_schema.MutationType?.Name.EqualsOrdinal(
                    current[0].Key.SelectionSetTypeInfo.Name) == true))
            {
                var node = current[0];
                var selectionSet = ResolveSelectionSet(context, node.Key);
                var compose = new CompositionNode(context.CreateNodeId(), selectionSet);
                parent.AddNode(node.Value);
                parent.AddNode(compose);
                context.Nodes.Remove(node.Key);
                completed.Add(node.Key);
            }
            else
            {
                var parallel = new ParallelNode(context.CreateNodeId());
                var selectionSets = new List<ISelectionSet>();

                foreach (var node in current)
                {
                    selectionSets.Add(ResolveSelectionSet(context, node.Key));
                    parallel.AddNode(node.Value);
                    context.Nodes.Remove(node.Key);
                    completed.Add(node.Key);
                }

                var compose = new CompositionNode(context.CreateNodeId(), selectionSets);

                parent.AddNode(parallel);
                parent.AddNode(compose);
            }

            current = context.Nodes.Where(t => completed.IsSupersetOf(t.Key.DependsOn)).ToArray();
        }

        return parent;
    }

    private ISelectionSet ResolveSelectionSet(
        QueryPlanContext context,
        ExecutionStep executionStep)
        => executionStep.ParentSelection is null
            ? context.Operation.RootSelectionSet
            : context.Operation.GetSelectionSet(
                executionStep.ParentSelection,
                _schema.GetType<Types.ObjectType>(executionStep.SelectionSetTypeInfo.Name));
}
