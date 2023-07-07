using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
// ReSharper disable PossibleMultipleEnumeration

namespace HotChocolate.Data.ExpressionNodes;

public static class MetaTreeSealing
{
    private sealed class AssignIndexVisitor : PlanMetaTreeVisitor<AssignIndexVisitor.Context>
    {
        internal sealed class Context
        {
            public int NodeCounter;
        }
        public static readonly AssignIndexVisitor Instance = new();
        public override void Visit(ExpressionNode node, Context context)
        {
            if (node.Index != -1)
                return;
            base.Visit(node, context);
            node.Index = context.NodeCounter++;
        }
    }

    private sealed class SealingVisitor : PlanMetaTreeVisitor<SealingVisitor.Context>
    {
        internal sealed record Context(
            SealedExpressionNode[] Nodes,
            // Need this since scopes don't have ids. Should they?
            Stack<SealedScope> Scopes)
        {
            public ref SealedExpressionNode NodeRef(int index) => ref Nodes[index];
        }

        public static readonly SealingVisitor Instance = new();
        public override void Visit(ExpressionNode node, Context context)
        {
            ref var sealedNode = ref context.NodeRef(node.Index);
            Debug.Assert(!sealedNode.IsInitialized);

            // This adds a scope
            base.Visit(node, context);

            SealedScope? scope = null;
            if (node.Scope is not null)
                scope = context.Scopes.Pop();

            var childrenList = node.Children is { Count: > 0 }
                ? FindChildren()
                : Array.Empty<int>();

            int[] FindChildren()
            {
                var result = new int[node.Children.Count];
                var children = node.Children;
                for (int i = 0; i < children.Count; i++)
                    result[i] = children[i].Index;
                return result;
            }

            Dependencies dependencies;
            {
                StructuralDependencies structuralDependencies;
                bool hasExpressionDependencies = false;

                var dependencyObjects = GetDependencies();

                if (dependencyObjects.Any(c => c.Structural.Unspecified))
                {
                    structuralDependencies = StructuralDependencies.All;
                    hasExpressionDependencies = true;
                }
                else if (dependencyObjects.All(c => c.Structural.VariableIds!.Count == 0))
                {
                    structuralDependencies = StructuralDependencies.None;
                }
                else
                {
                    var dependencyIds = new HashSet<Identifier>();
                    foreach (var c in dependencyObjects)
                        dependencyIds.UnionWith(c.Structural.VariableIds!);
                    structuralDependencies = new() { VariableIds = dependencyIds };
                }

                if (!hasExpressionDependencies)
                    hasExpressionDependencies = dependencyObjects.Any(c => c.HasExpressionDependencies);

                dependencies = new()
                {
                    Structural = structuralDependencies,
                    HasExpressionDependencies = hasExpressionDependencies,
                };
            }

            IEnumerable<Dependencies> GetDependencies()
            {
                yield return node.OwnDependencies;

                if (node.Scope is { } s)
                    yield return context.NodeRef(s.Instance.Index).AllDependencies;

                foreach (var child in childrenList)
                    yield return context.NodeRef(child).AllDependencies;
            }

            sealedNode = new SealedExpressionNode(
                scope,
                node.ExpressionFactory,
                dependencies,
                node.OwnDependencies,
                childrenList,
                node.ExpectedType ?? typeof(object));
        }

        public override void VisitScope(Scope scope, Context context)
        {
            // The root has been initialized, which means the scope has too.
            ref var rootRef = ref context.NodeRef(scope.Instance.Index);
            if (rootRef.IsInitialized)
            {
                context.Scopes.Push(rootRef.Scope!);
                return;
            }

            // Find the parent scope.
            // Note, that it's been initialized, because the scopes are visited prior to children.
            SealedScope? parentScope = null;
            if (scope.ParentScope is { } parentScopeMutable)
            {
                ref var parentRootRef = ref context.NodeRef(parentScopeMutable.Instance.Index);
                Debug.Assert(parentRootRef.IsInitialized);
                parentScope = parentRootRef.Scope!.ParentScope;
            }

            base.VisitScope(scope, context);

            var sealedScope = new SealedScope(
                scope.Instance.Index,
                scope.InnerInstance!.Index,
                parentScope);
            context.Scopes.Push(sealedScope);
        }
    }

    public sealed class ReturnToObjectPoolVisitor : PlanMetaTreeVisitor<ExpressionPools>
    {
        public static readonly ReturnToObjectPoolVisitor Instance = new();
        public override void Visit(ExpressionNode node, ExpressionPools context)
        {
            base.Visit(node, context);
            context.ExpressionNodePool.Return(node);
        }

        public override void VisitScope(Scope scope, ExpressionPools context)
        {
            base.VisitScope(scope, context);
            context.ScopePool.Return(scope);
        }
    }

    // NOTE: Destroys the tree that was passed in.
    public static SealedMetaTree Seal(
        this PlanMetaTree tree,
        ExpressionPools pools)
    {
        var assignIndexContext = new AssignIndexVisitor.Context();
        AssignIndexVisitor.Instance.Visit(tree, assignIndexContext);

        var nodes = new SealedExpressionNode[assignIndexContext.NodeCounter];
        var sealingContext = new SealingVisitor.Context(nodes, new());
        SealingVisitor.Instance.Visit(tree, sealingContext);

        foreach (var node in nodes)
            Debug.Assert(node.IsInitialized);

        ReturnToObjectPoolVisitor.Instance.Visit(tree, pools);

        var selectionIdToOuterNode = new Dictionary<Identifier, int>(tree.SelectionIdToInnerNode.Count);
        foreach (var (id, node) in tree.SelectionIdToInnerNode)
            selectionIdToOuterNode.Add(id, node.OutermostNode.Index);

        return new SealedMetaTree(nodes, selectionIdToOuterNode);
    }
}
