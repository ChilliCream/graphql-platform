using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing.Plan
{
    internal static class QueryPlanBuilder
    {
        public static QueryPlan Build(IPreparedOperation operation)
        {
            return new(BuildNode(operation).CreateStep());
        }

        public static QueryPlanNode BuildNode(IPreparedOperation operation)
        {
            return QueryStrategy.Build(operation);
        }

        private static class QueryStrategy
        {
            public static QueryPlanNode Build(IPreparedOperation operation)
            {
                var context = new QueryPlanContext(operation);

                foreach (ISelection selection in operation.GetRootSelectionSet().Selections)
                {
                    Visit(selection, context);
                }

                QueryPlanNode root = Optimize(context.Root!);

                if (context.PureSelections.Count > 0)
                {
                    if (root is ResolverQueryPlanNode
                        {
                            Strategy: ExecutionStrategy.Parallel,
                            Nodes: { Count: 0 }
                        } rootResolver)
                    {
                        for (var i = 0; i < context.PureSelections.Count; i++)
                        {
                            rootResolver.Selections.Add(context.PureSelections[i]);
                        }

                        return rootResolver;
                    }

                    var pureRoot = new ResolverQueryPlanNode(context.PureSelections[0]);

                    for (var i = 1; i < context.PureSelections.Count; i++)
                    {
                        pureRoot.Selections.Add(context.PureSelections[i]);
                    }

                    root = ParallelQueryPlanNode.Create(root, pureRoot);
                }

                return root;
            }

            private static void Visit(ISelection selection, QueryPlanContext context)
            {
                if (context.NodePath.Count == 0)
                {
                    context.Root = new ResolverQueryPlanNode(selection);
                    context.NodePath.Push(context.Root);
                }
                else
                {
                    QueryPlanNode parent = context.NodePath.Peek();

                    if (selection.Strategy == SelectionExecutionStrategy.Serial)
                    {
                        if (parent is ResolverQueryPlanNode { Strategy: ExecutionStrategy.Serial } p)
                        {
                            p.Selections.Add(selection);
                        }
                        else if(context.SelectionPath.Count > 0 &&
                            context.NodePath.TryPeek(2, out QueryPlanNode? grandParent) &&
                            grandParent is ResolverQueryPlanNode { Strategy: ExecutionStrategy.Serial } gp &&
                            gp.Selections.Contains(context.SelectionPath.PeekOrDefault()))
                        {
                            gp.Selections.Add(selection);
                        }
                        else
                        {
                            var resolverPlanStep = new ResolverQueryPlanNode(
                                selection,
                                context.SelectionPath.PeekOrDefault());
                            parent.AddNode(resolverPlanStep);
                            context.NodePath.Push(resolverPlanStep);
                        }
                    }
                    else if (selection.Strategy == SelectionExecutionStrategy.Default ||
                        context.SelectionPath.Count == 0)
                    {
                        if (parent is ResolverQueryPlanNode { Strategy: ExecutionStrategy.Parallel } p)
                        {
                            p.Selections.Add(selection);
                        }
                        else if(context.SelectionPath.Count > 0 &&
                            context.NodePath.TryPeek(2, out QueryPlanNode? grandParent) &&
                            grandParent is ResolverQueryPlanNode { Strategy: ExecutionStrategy.Parallel } gp &&
                            gp.Selections.Contains(context.SelectionPath.PeekOrDefault()))
                        {
                            gp.Selections.Add(selection);
                        }
                        else
                        {
                            var resolverPlanStep = new ResolverQueryPlanNode(
                                selection,
                                context.SelectionPath.PeekOrDefault());
                            parent.AddNode(resolverPlanStep);
                            context.NodePath.Push(resolverPlanStep);
                        }
                    }
                    else if (selection.Strategy == SelectionExecutionStrategy.Pure)
                    {
                        context.PureSelections.Add(selection);
                    }
                    else if (selection.Strategy == SelectionExecutionStrategy.Inline)
                    {
                        // if a selection is inlined we just ignore it in the plan.
                    }
                    else
                    {
                        throw new NotSupportedException(
                            "The specified selection strategy is not supported.");
                    }
                }

                if (selection.SelectionSet is { } selectionSetNode)
                {
                    var depth = context.NodePath.Count;
                    context.SelectionPath.Push(selection);

                    foreach (var objectType in context.Operation.GetPossibleTypes(selectionSetNode))
                    {
                        ISelectionSet selectionSet = context.Operation.GetSelectionSet(selectionSetNode, objectType);

                        foreach (var child in selectionSet.Selections)
                        {
                            Visit(child, context);
                        }
                    }

                    if (context.NodePath.Count > depth)
                    {
                        context.NodePath.Pop();
                    }
                    context.SelectionPath.Pop();
                }
            }

            private static QueryPlanNode Optimize(QueryPlanNode node)
            {
                if (node.Nodes.Count == 0)
                {
                    return node;
                }

                if (node.Nodes.Count == 1)
                {
                    Optimize(node.Nodes[0]);
                    QueryPlanNode child = node.Nodes[0];
                    node.RemoveNode(child);
                    return SequenceQueryPlanNode.Create(node, child);
                }

                foreach (QueryPlanNode child in node.Nodes)
                {
                    Optimize(child);
                }

                ResolverQueryPlanNode? parallel = null;
                ResolverQueryPlanNode? serial = null;
                var nodes = new List<QueryPlanNode>();

                while(node.TryTakeNode(out var child))
                {
                    switch (child)
                    {
                        case ResolverQueryPlanNode { Strategy: ExecutionStrategy.Parallel } p
                            when parallel is null:
                            parallel = p;
                            nodes.Add(p);
                            break;
                        case ResolverQueryPlanNode { Strategy: ExecutionStrategy.Parallel } p:
                        {
                            foreach (ISelection selection in p.Selections)
                            {
                                parallel.Selections.Add(selection);
                            }

                            break;
                        }
                        case ResolverQueryPlanNode { Strategy: ExecutionStrategy.Serial } s
                            when serial is null:
                            serial = s;
                            nodes.Add(s);
                            break;
                        case ResolverQueryPlanNode { Strategy: ExecutionStrategy.Serial } s:
                        {
                            foreach (ISelection selection in s.Selections)
                            {
                                serial.Selections.Add(selection);
                            }

                            break;
                        }
                        default:
                            nodes.Add(child);
                            break;
                    }
                }

                var sequence = new SequenceQueryPlanNode();

                foreach (QueryPlanNode child in nodes)
                {
                    sequence.AddNode(child);
                }

                node.AddNode(sequence);

                return node;
            }
        }

        private class QueryPlanContext
        {
            public QueryPlanContext(IPreparedOperation operation)
            {
                Operation = operation;
            }

            public IPreparedOperation Operation { get; }

            public QueryPlanNode? Root { get; set; }

            public List<ISelection> PureSelections { get; } = new();

            public List<QueryPlanNode> NodePath { get; } = new();

            public List<ISelection> SelectionPath { get; } = new();
        }

        internal static ExecutionStrategy GetStrategyFromSelection(ISelection selection) =>
            selection.Strategy == SelectionExecutionStrategy.Serial
                ? ExecutionStrategy.Serial
                : ExecutionStrategy.Parallel;
    }

    internal sealed class ResolverQueryPlanNode : QueryPlanNode
    {
        public ResolverQueryPlanNode(ISelection first, ISelection? firstParent = null)
            : base(QueryPlanBuilder.GetStrategyFromSelection(first))
        {
            First = first;
            FirstParent = firstParent;
            Selections.Add(first);
        }

        public ISelection? FirstParent { get; }

        public ISelection First { get; }

        public List<ISelection> Selections { get; } = new();

        public override QueryPlanStep CreateStep()
        {
            var selectionStep = new ResolverQueryPlanStep(Strategy, Selections);

            if (Nodes.Count == 0)
            {
                return selectionStep;
            }

            if (Nodes.Count == 1)
            {
                return new SequenceQueryPlanStep(
                    new[]
                    {
                        selectionStep,
                        Nodes[0].CreateStep()
                    });
            }

            return new SequenceQueryPlanStep(
                new QueryPlanStep[]
                {
                    selectionStep,
                    new SequenceQueryPlanStep(Nodes.Select(t => t.CreateStep()).ToArray())
                });
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("type", "Resolver");
            writer.WriteString("strategy", Strategy.ToString());

            writer.WritePropertyName("selections");
            writer.WriteStartArray();
            foreach (var selection in Selections)
            {
                writer.WriteStartObject();
                writer.WriteNumber("id", selection.Id);
                writer.WriteString("field", $"{selection.DeclaringType.Name}.{selection.Field.Name}");
                writer.WriteString("responseName", selection.ResponseName);

                if (selection.Strategy == SelectionExecutionStrategy.Pure)
                {
                    writer.WriteBoolean("pure", true);
                }

                if (selection.Strategy == SelectionExecutionStrategy.Inline)
                {
                    writer.WriteBoolean("inline", true);
                }

                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            if (Nodes.Count > 0)
            {
                writer.WritePropertyName("nodes");
                writer.WriteStartArray();
                foreach (var node in Nodes)
                {
                    node.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }

    internal sealed class SequenceQueryPlanNode : QueryPlanNode
    {
        public SequenceQueryPlanNode() : base(ExecutionStrategy.Serial)
        {
        }

        public override QueryPlanStep CreateStep()
        {
            return new SequenceQueryPlanStep(Nodes.Select(t => t.CreateStep()).ToArray());
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("type", "Sequence");

            if (Nodes.Count > 0)
            {
                writer.WritePropertyName("nodes");
                writer.WriteStartArray();
                foreach (var node in Nodes)
                {
                    node.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        public static SequenceQueryPlanNode Create(params QueryPlanNode[] nodes)
        {
            var sequence = new SequenceQueryPlanNode();
            foreach (QueryPlanNode node in nodes)
            {
                sequence.AddNode(node);
            }
            return sequence;
        }
    }

    internal sealed class ParallelQueryPlanNode : QueryPlanNode
    {
        public ParallelQueryPlanNode() : base(ExecutionStrategy.Parallel)
        {
        }

        public override QueryPlanStep CreateStep()
        {
            return new SequenceQueryPlanStep(Nodes.Select(t => t.CreateStep()).ToArray());
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("type", "Parallel");

            if (Nodes.Count > 0)
            {
                writer.WritePropertyName("nodes");
                writer.WriteStartArray();
                foreach (var node in Nodes)
                {
                    node.Serialize(writer);
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        public static ParallelQueryPlanNode Create(params QueryPlanNode[] nodes)
        {
            var parallel = new ParallelQueryPlanNode();
            foreach (QueryPlanNode node in nodes)
            {
                parallel.AddNode(node);
            }
            return parallel;
        }
    }

    internal abstract class QueryPlanNode
    {
        private readonly List<QueryPlanNode> _nodes = new();

        protected QueryPlanNode(ExecutionStrategy strategy)
        {
            Strategy = strategy;
        }

        public ExecutionStrategy Strategy { get; }

        public QueryPlanNode? Parent { get; private set; }

        public IReadOnlyList<QueryPlanNode> Nodes => _nodes;

        public void AddNode(QueryPlanNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            node.Parent = this;
            _nodes.Add(node);
        }

        public void RemoveNode(QueryPlanNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            node.Parent = null;
            _nodes.Remove(node);
        }

        public bool TryTakeNode([MaybeNullWhen(false)] out QueryPlanNode node)
        {
            if (_nodes.Count > 0)
            {
                node = _nodes[0];
                node.Parent = null;
                _nodes.RemoveAt(0);
                return true;
            }

            node = null;
            return false;
        }

        public abstract QueryPlanStep CreateStep();

        public abstract void Serialize(Utf8JsonWriter writer);
    }
}
