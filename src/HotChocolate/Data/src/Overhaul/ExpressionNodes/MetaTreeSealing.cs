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
            SealedExpressionNode?[] Nodes,
            // Need this since scopes don't have ids. Should they?
            Stack<SealedScope> Scopes)
        {
            public ref SealedExpressionNode? NodeRef(Identifier id) => ref Nodes[id.AsIndex()];
        }

        public static readonly SealingVisitor Instance = new();
        public override void Visit(ExpressionNode node, Context context)
        {
            ref var sealedNode = ref context.NodeRef(node.Id);
            Debug.Assert(sealedNode is null);

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

            ReadOnlyStructuralDependencies dependencies;
            {
                if (ScopeInstanceChildrenAndSelf().Any(c => c.Dependencies.Unspecified))
                {
                    dependencies = ReadOnlyStructuralDependencies.All;
                }
                else if (ScopeInstanceChildrenAndSelf().All(c => c.Dependencies.VariableIds!.Count == 0))
                {
                    dependencies = ReadOnlyStructuralDependencies.None;
                }
                else
                {
                    var dependencyIds = new HashSet<Identifier>();
                    foreach (var c in ScopeInstanceChildrenAndSelf())
                        dependencyIds.UnionWith(c.Dependencies.VariableIds!);
                    dependencies = new() { VariableIds = dependencyIds };
                }
            }

            IEnumerable<SealedExpressionNode> ScopeInstanceChildrenAndSelf()
            {
                yield return context.NodeRef(node.Id)!;

                if (node.Scope is { } s)
                    yield return context.NodeRef(s.Instance!.Id)!;

                foreach (var child in childrenList)
                    yield return context.NodeRef(child)!;
            }

            sealedNode = new SealedExpressionNode(
                scope,
                node.Id,
                node.ExpressionFactory,
                dependencies,
                childrenList);
        }

        public override void VisitScope(Scope scope, Context context)
        {
            // The root has been initialized, which means the scope has too.
            ref var rootRef = ref context.NodeRef(scope.Root!.Id);
            if (rootRef is not null)
            {
                context.Scopes.Push(rootRef.Scope!);
                return;
            }

            // Find the parent scope.
            // Note, that it's been initialized, because the scopes are visited prior to children.
            SealedScope? parentScope = null;
            if (scope.ParentScope is { } parentScopeMutable)
            {
                ref var parentRootRef = ref context.NodeRef(parentScopeMutable.Root!.Id);
                Debug.Assert(parentRootRef is not null);
                parentScope = parentRootRef.Scope!.ParentScope;
            }

            base.VisitScope(scope, context);

            var sealedScope = new SealedScope(
                rootRef!.Id,
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

        // Let's write them in a flat array for convenience.
        var nodes = new SealedExpressionNode[assignIdsContext.Generator.Count];
        var sealingContext = new SealingVisitor.Context(nodes, new());
        SealingVisitor.Instance.Visit(tree, sealingContext);

        foreach (var node in nodes)
            Debug.Assert(node is not null);

        ReturnToObjectPoolVisitor.Instance.Visit(tree, pools);

        var selectionIdToOuterNode = new Dictionary<Identifier, SealedExpressionNode>(tree.SelectionIdToOuterNode.Count);
        foreach (var (id, node) in tree.SelectionIdToOuterNode)
            selectionIdToOuterNode.Add(id, sealingContext.NodeRef(node.Id)!);

        return new SealedMetaTree(nodes, selectionIdToOuterNode);
    }

}
