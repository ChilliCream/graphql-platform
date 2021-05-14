using System;
using System.Collections.Generic;
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
}
