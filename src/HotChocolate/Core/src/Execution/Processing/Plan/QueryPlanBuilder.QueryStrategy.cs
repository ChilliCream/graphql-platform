using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Execution.Processing.Plan
{
    internal static partial class QueryPlanBuilder
    {
        private static class QueryStrategy
        {
            public static OperationNode Build(QueryPlanContext context)
            {
                QueryPlanNode root = Build(context, context.Operation.GetRootSelectionSet());
                var operationNode = new OperationNode(root);

                if (context.Deferred.Count > 0)
                {
                    foreach (var deferred in BuildDeferred(context))
                    {
                        operationNode.Deferred.Add(deferred);
                    }
                }

                if (context.Streams.Count > 0)
                {
                    operationNode.Streams.AddRange(context.Streams.Values);
                }

                return operationNode;
            }

            public static QueryPlanNode Build(QueryPlanContext context, ISelectionSet selectionSet)
            {
                foreach (ISelection selection in selectionSet.Selections)
                {
                    Visit(selection, context);
                }

                CollectFragments(selectionSet, context);

                return Optimize(context.Root!);
            }

            public static IEnumerable<QueryPlanNode> BuildDeferred(QueryPlanContext context)
            {
                var processed = new HashSet<int>();

                while (context.Deferred.TryPop(out IFragment? fragment) &&
                    processed.Add(fragment.Id))
                {
                    yield return Build(context, fragment.SelectionSet);
                }
            }

            private static void Visit(ISelection selection, QueryPlanContext context)
            {
                if (selection.IsStreamable && selection.SelectionSet is not null)
                {
                    QueryPlanContext streamContext = context.Branch();

                    VisitChildren(selection, streamContext);

                    if (streamContext.Root is not null &&
                        !context.Streams.ContainsKey(selection.Id))
                    {
                        context.Streams.Add(selection.Id, new(selection.Id, streamContext.Root));
                    }

                    if (streamContext.Streams.Count > 0)
                    {
                        foreach (StreamPlanNode streamPlan in streamContext.Streams.Values)
                        {
                            if (!context.Streams.ContainsKey(selection.Id))
                            {
                                context.Streams.Add(selection.Id, streamPlan);
                            }
                        }
                    }
                }

                if (context.NodePath.Count == 0)
                {
                    context.Root = new ResolverNode(selection);
                    context.NodePath.Push(context.Root);
                }
                else
                {
                    QueryPlanNode parent = context.NodePath.Peek();

                    if (selection.Strategy == SelectionExecutionStrategy.Serial)
                    {
                        if (parent is ResolverNode { Strategy: ExecutionStrategy.Serial } p)
                        {
                            p.Selections.Add(selection);
                        }
                        else if (context.SelectionPath.Count > 0 &&
                            context.NodePath.TryPeek(2, out QueryPlanNode? grandParent) &&
                            grandParent is ResolverNode { Strategy: ExecutionStrategy.Serial } gp &&
                            gp.Selections.Contains(context.SelectionPath.PeekOrDefault()!))
                        {
                            gp.Selections.Add(selection);
                        }
                        else
                        {
                            var resolverPlanStep = new ResolverNode(
                                selection,
                                context.SelectionPath.PeekOrDefault());
                            parent.AddNode(resolverPlanStep);
                            context.NodePath.Push(resolverPlanStep);
                        }
                    }
                    else if (selection.Strategy == SelectionExecutionStrategy.Default ||
                        context.SelectionPath.Count == 0)
                    {
                        if (parent is ResolverNode { Strategy: ExecutionStrategy.Parallel } p)
                        {
                            p.Selections.Add(selection);
                        }
                        else if (context.SelectionPath.Count > 0 &&
                            context.NodePath.TryPeek(2, out QueryPlanNode? grandParent) &&
                            grandParent is ResolverNode { Strategy: ExecutionStrategy.Parallel } gp &&
                            gp.Selections.Contains(context.SelectionPath.PeekOrDefault()!))
                        {
                            gp.Selections.Add(selection);
                        }
                        else
                        {
                            var resolverPlanStep = new ResolverNode(
                                selection,
                                context.SelectionPath.PeekOrDefault());
                            parent.AddNode(resolverPlanStep);
                            context.NodePath.Push(resolverPlanStep);
                        }
                    }
                    else if (selection.Strategy == SelectionExecutionStrategy.Pure)
                    {
                        // if a selection is pure we just ignore it in the plan since
                        // it will be inlined.
                    }
                    else
                    {
                        throw new NotSupportedException(
                            "The specified selection strategy is not supported.");
                    }
                }

                VisitChildren(selection, context);
            }

            internal static void VisitChildren(ISelection selection, QueryPlanContext context)
            {
                if (selection.SelectionSet is { } selectionSetNode)
                {
                    var depth = context.NodePath.Count;
                    context.SelectionPath.Push(selection);

                    foreach (var objectType in context.Operation.GetPossibleTypes(selectionSetNode))
                    {
                        ISelectionSet selectionSet = context.Operation.GetSelectionSet(
                            selectionSetNode, objectType);

                        foreach (var child in selectionSet.Selections)
                        {
                            Visit(child, context);
                        }

                        CollectFragments(selectionSet, context);
                    }

                    if (context.NodePath.Count > depth)
                    {
                        context.NodePath.Pop();
                    }
                    context.SelectionPath.Pop();
                }
            }

            private static void CollectFragments(
                ISelectionSet selectionSet,
                QueryPlanContext context)
            {
                if (selectionSet.Fragments.Count > 0)
                {
                    foreach (IFragment fragment in selectionSet.Fragments)
                    {
                        context.Deferred.Add(fragment);
                    }
                }
            }

            internal static QueryPlanNode Optimize(QueryPlanNode node)
            {
                if (node.Nodes.Count == 0)
                {
                    return node;
                }

                if (node.Nodes.Count == 1)
                {
                    QueryPlanNode child = node.Nodes[0];
                    node.RemoveNode(child);
                    child = Optimize(child);

                    if (node is SequenceNode or ParallelNode)
                    {
                        return child;
                    }

                    return SequenceNode.Create(node, child);
                }

                var children = new List<QueryPlanNode>();

                SequenceNode? seq = null;
                ParallelNode? par = null;

                while (node.TryTakeNode(out var child))
                {
                    child = Optimize(child);

                    if (child is SequenceNode s)
                    {
                        if (par is not null)
                        {
                            par = null;
                            seq = s;
                            children.Add(seq);
                        }
                        else if (seq is not null)
                        {
                            foreach (var c in s.Nodes)
                            {
                                seq.AddNode(c);
                            }
                        }
                        else
                        {
                            seq = s;
                            children.Add(seq);
                        }
                    }
                    else if (child is ParallelNode p)
                    {
                        if (seq is not null)
                        {
                            seq = null;
                            par = p;
                            children.Add(par);
                        }
                        else if (par is not null)
                        {
                            foreach (var c in p.Nodes)
                            {
                                par.AddNode(c);
                            }
                        }
                        else
                        {
                            par = p;
                            children.Add(par);
                        }
                    }
                    else
                    {
                        seq = null;
                        par = null;
                        children.Add(child);
                    }
                }

                foreach (var child in children)
                {
                    node.AddNode(child);
                }

                return node;
            }
        }
    }
}
