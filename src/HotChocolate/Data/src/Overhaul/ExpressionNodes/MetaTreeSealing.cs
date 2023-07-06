using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HotChocolate.Data.ExpressionNodes;

public static class MetaTreeSealing
{
    private sealed class AssignIdsVisitor : PlanMetaTreeVisitor<AssignIdsVisitor.Context>
    {
        internal sealed record Context(SequentialIdentifierGenerator Generator);
        public static readonly AssignIdsVisitor Instance = new();
        public override void Visit(ExpressionNode node, Context context)
        {
            if (node.Id != default)
                return;
            base.Visit(node, context);
            node.Id = context.Generator.Next();
        }
    }

    private sealed class SealingVisitor : PlanMetaTreeVisitor<SealingVisitor.Context>
    {
        internal sealed record Context(
            SealedExpressionNode[] Nodes,
            // Need this since scopes don't have ids. Should they?
            Stack<SealedScope> Scopes)
        {
            public ref SealedExpressionNode NodeRef(Identifier id) => ref Nodes[id.AsIndex()];
        }

        public static readonly SealingVisitor Instance = new();
        public override void Visit(ExpressionNode node, Context context)
        {
            ref var sealedNode = ref context.NodeRef(node.Id);
            Debug.Assert(sealedNode.IsInitialized);

            // This adds a scope
            base.Visit(node, context);

            SealedScope? scope = null;
            if (node.Scope is not null)
                scope = context.Scopes.Pop();

            var childrenList = node.Children is { Count: > 0 }
                ? FindChildren()
                : Array.Empty<Identifier>();

            Identifier[] FindChildren()
            {
                var result = new Identifier[node.Children!.Count];
                var children = node.Children!;
                for (int i = 0; i < children.Count; i++)
                    result[i] = children[i].Id;
                return result;
            }

            Dependencies dependencies;
            {
                StructuralDependencies structuralDependencies;
                bool hasExpressionDependencies = false;

                if (GetDependencies().Any(c => c.Structural.Unspecified))
                {
                    structuralDependencies = StructuralDependencies.All;
                    hasExpressionDependencies = true;
                }
                else if (GetDependencies().All(c => c.Structural.VariableIds!.Count == 0))
                {
                    structuralDependencies = StructuralDependencies.None;
                }
                else
                {
                    var dependencyIds = new HashSet<Identifier>();
                    foreach (var c in GetDependencies())
                        dependencyIds.UnionWith(c.Structural.VariableIds!);
                    structuralDependencies = new() { VariableIds = dependencyIds };
                }

                if (!hasExpressionDependencies)
                    hasExpressionDependencies = GetDependencies().Any(c => c.HasExpressionDependencies);

                dependencies = new()
                {
                    Structural = structuralDependencies,
                    HasExpressionDependencies = hasExpressionDependencies,
                };
            }

            IEnumerable<Dependencies> GetDependencies()
            {
                yield return context.NodeRef(node.Id).Dependencies;

                if (node.Scope is { } s)
                    yield return context.NodeRef(s.Instance!.Id).Dependencies;

                foreach (var child in childrenList)
                    yield return context.NodeRef(child).Dependencies;
            }

            sealedNode = new SealedExpressionNode(
                scope,
                node.ExpressionFactory,
                dependencies,
                childrenList);
        }

        public override void VisitScope(Scope scope, Context context)
        {
            // The root has been initialized, which means the scope has too.
            ref var rootRef = ref context.NodeRef(scope.RootInstance!.Id);
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
                ref var parentRootRef = ref context.NodeRef(parentScopeMutable.RootInstance!.Id);
                Debug.Assert(parentRootRef.IsInitialized);
                parentScope = parentRootRef.Scope!.ParentScope;
            }

            base.VisitScope(scope, context);

            var sealedScope = new SealedScope(
                scope.RootInstance!.Id,
                scope.Instance!.Id,
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
        var assignIdsContext = new AssignIdsVisitor.Context(new());
        AssignIdsVisitor.Instance.Visit(tree, assignIdsContext);

        var nodes = new SealedExpressionNode[assignIdsContext.Generator.Count];
        var sealingContext = new SealingVisitor.Context(nodes, new());
        SealingVisitor.Instance.Visit(tree, sealingContext);

        foreach (var node in nodes)
            Debug.Assert(node.IsInitialized);

        ReturnToObjectPoolVisitor.Instance.Visit(tree, pools);

        var selectionIdToOuterNode = new Dictionary<Identifier, Identifier>(tree.SelectionIdToInnerNode.Count);
        foreach (var (id, node) in tree.SelectionIdToInnerNode)
            selectionIdToOuterNode.Add(id, node.OutermostNode.Id);

        return new SealedMetaTree(nodes, selectionIdToOuterNode);
    }
}
