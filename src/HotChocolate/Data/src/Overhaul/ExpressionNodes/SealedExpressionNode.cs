using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class SealedExpressionNode
{
    public SealedExpressionNode? Parent { get; internal set; }
    public SealedScope? Scope { get; }
    public Identifier Id { get; }
    public IExpressionFactory ExpressionFactory { get; }
    public ReadOnlyStructuralDependencies Dependencies { get; }
    public IReadOnlyList<SealedExpressionNode> Children { get; }

    public SealedExpressionNode(
        SealedScope? scope,
        Identifier id,
        IExpressionFactory expressionFactory,
        ReadOnlyStructuralDependencies dependencies,
        IReadOnlyList<SealedExpressionNode> children)
    {
        Scope = scope;
        Id = id;
        ExpressionFactory = expressionFactory;
        Dependencies = dependencies;
        Children = children;
    }
}

public sealed class SealedScope
{
    public SealedScope? ParentScope { get; }
    public SealedExpressionNode Root { get; }
    public SealedExpressionNode Instance { get; }

    public SealedScope(
        SealedExpressionNode root,
        SealedExpressionNode instance,
        SealedScope? parentScope)
    {
        Root = root;
        Instance = instance;
        ParentScope = parentScope;
    }
}

public sealed class Scope
{
    public Scope? ParentScope { get; set; }

    // This indicates the root node that gets you the instance expression.
    public ExpressionNode? Root { get; set; }

    // This one can be wrapped
    public ExpressionNode? Instance { get; set; }
}

public sealed class ExpressionNode
{
    public ExpressionNode? Parent { get; set; }
    // This exists in order to be able to wrap the instance used in this scope
    // without changing all dependencies each time.
    public Scope? Scope { get; set; }
    public Identifier Id { get; set; }
    public required IExpressionFactory ExpressionFactory { get; set; }
    public StructuralDependencies? OwnDependencies { get; set; }
    public List<ExpressionNode>? Children { get; set; } = new();
    public ExpressionNode? InnermostInitialNode { get; set; }

    public ExpressionNode GetInnermostInitialNode() => InnermostInitialNode ?? this;
}

public class MetaTree<T>
{
    public ReadOnlyDictionary<Identifier, T> SelectionIdToOuterNode { get; }
    public T Root { get; }

    public MetaTree(
        ReadOnlyDictionary<Identifier, T> selectionIdToOuterNode,
        T root)
    {
        SelectionIdToOuterNode = selectionIdToOuterNode;
        Root = root;
    }
}

public interface IExpressionNodePool
{
    // TODO:
    ExpressionNode Get(IExpressionFactory factory);
}

public interface IObjectPool<T>
{
    T Get();
    void Return(T item);
}

public static class Helper
{
    public static ExpressionNode GetExpressionNodeOfSelection(
        // This has to return the node that's current the immediate child of the initial parent.
        // That is, the wrapped node (if it's been wrapped) rather than the initial node.
        Dictionary<Identifier, ExpressionNode> selectionIdToOuterNode,
        Identifier selectionId,
        bool innermost = true)
    {
        var node = selectionIdToOuterNode[selectionId];
        if (innermost)
            return node.GetInnermostInitialNode();
        return node;
    }

    public static void WrapExpressionNode(
        ExpressionNode wrapperNode,
        ExpressionNode nodeToWrap)
    {
        var initialNode = nodeToWrap.GetInnermostInitialNode();
        wrapperNode.InnermostInitialNode = initialNode;
        (wrapperNode.Children ??= new()).Insert(0, nodeToWrap);

        var parent = nodeToWrap.Parent;
        wrapperNode.Parent = parent;
        nodeToWrap.Parent = wrapperNode;

        if (parent is not null)
        {
            var children = parent.Children!;
            var indexOfWrapped = children.IndexOf(nodeToWrap);
            children[indexOfWrapped] = wrapperNode;
        }

        if (wrapperNode.Scope is { } scope)
        {
            Debug.Assert(ReferenceEquals(scope, nodeToWrap.Scope));
        }
        else
        {
            wrapperNode.Scope = nodeToWrap.Scope;
        }
    }

    public enum ResultKind
    {
        All,
        Particular,
    }

    public static ResultKind RetrieveStructuralDependenciesOfBranch(
        ExpressionNode? node,
        HashSet<Identifier> outResult)
    {
        if (node is null)
            return ResultKind.Particular;

        if (node.OwnDependencies is { } deps)
        {
            if (deps.Unspecified)
                return ResultKind.All;
            foreach (var id in deps.VariableIds!)
                outResult.Add(id);
        }

        var instanceNode = node.Scope?.Instance;
        var instanceResult = RetrieveStructuralDependenciesOfBranch(instanceNode, outResult);
        if (instanceResult == ResultKind.All)
            return ResultKind.All;

        var children = node.Children;
        if (children is not null)
        {
            foreach (var child in children)
            {
                var childResult = RetrieveStructuralDependenciesOfBranch(child, outResult);
                if (childResult == ResultKind.All)
                    return ResultKind.All;
            }
        }

        return ResultKind.Particular;
    }

    private sealed record AssignIdsContext(SequentialIdentifierGenerator Generator);

    private sealed class AssignIdsVisitor : MetaTreeVisitor<AssignIdsContext>
    {
        public static readonly AssignIdsVisitor Instance = new();
        public override void Visit(ExpressionNode node, AssignIdsContext context)
        {
            if (node.Id != default)
                return;
            node.Id = context.Generator.Next();
            base.Visit(node, context);
        }
    }

    private sealed record SealContext(
        SealedExpressionNode?[] Nodes,
        // Need this since scopes don't have ids. Should they?
        Stack<SealedScope> Scopes)
    {
        public ref SealedExpressionNode? NodeRef(Identifier id) => ref Nodes[id.Value - 1];
    }

    private sealed class SealVisitor : MetaTreeVisitor<SealContext>
    {
        public static readonly SealVisitor Instance = new();
        public override void Visit(ExpressionNode node, SealContext context)
        {
            ref var sealedNode = ref context.NodeRef(node.Id);
            Debug.Assert(sealedNode is null);

            // This adds a scope
            base.Visit(node, context);

            SealedExpressionNode[] FindChildren()
            {
                var result = new SealedExpressionNode[node.Children!.Count];
                var children = node.Children!;
                for (int i = 0; i < children.Count; i++)
                    result[i] = context.NodeRef(children[i].Id)!;
                return result;
            }

            var scope = context.Scopes.Pop();
            var childrenList = node.Children is { Count: > 0 }
                ? FindChildren()
                : Array.Empty<SealedExpressionNode>();

            ReadOnlyStructuralDependencies dependencies;
            {
                if (childrenList.Any(c => c.Dependencies.Unspecified)
                    || scope.Instance.Dependencies.Unspecified
                    || node.OwnDependencies?.Unspecified == true)
                {
                    dependencies = ReadOnlyStructuralDependencies.All;
                }
                else if (childrenList.All(c => c.Dependencies.VariableIds!.Count == 0)
                    && scope.Instance.Dependencies.VariableIds!.Count == 0
                    && node.OwnDependencies?.VariableIds?.Count == 0)
                {
                    dependencies = ReadOnlyStructuralDependencies.None;
                }
                else
                {
                    var dependencyIds = new HashSet<Identifier>();

                    foreach (var child in childrenList)
                    {
                        foreach (var id in child.Dependencies.VariableIds!)
                            dependencyIds.Add(id);
                    }

                    foreach (var id in scope.Instance.Dependencies.VariableIds!)
                        dependencyIds.Add(id);

                    if (node.OwnDependencies is { } ownDependencies)
                    {
                        foreach (var id in ownDependencies.VariableIds!)
                            dependencyIds.Add(id);
                    }

                    dependencies = new() { VariableIds = dependencyIds };
                }
            }

            sealedNode = new SealedExpressionNode(
                scope,
                node.Id,
                node.ExpressionFactory,
                dependencies,
                childrenList);

            foreach (var c in childrenList)
                c.Parent = sealedNode;
        }

        public override void VisitScope(Scope scope, SealContext context)
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
                rootRef!,
                context.NodeRef(scope.Instance!.Id)!,
                parentScope);
            context.Scopes.Push(sealedScope);
        }
    }

    public static MetaTree<SealedExpressionNode> SealExpressionNodeTree(
        MetaTree<ExpressionNode> tree)
    {
        var assignIdsContext = new AssignIdsContext(new());
        AssignIdsVisitor.Instance.Visit(tree, assignIdsContext);

        // Let's write them in a flat array for convenience.
        var array = new SealedExpressionNode?[assignIdsContext.Generator.Count];
        var sealContext = new SealContext(array, new());
        SealVisitor.Instance.Visit(tree, sealContext);

        return default!;
    }

    // Constructs the initial version of the expression tree meant for projections.
    // There are going to be no structural dependencies in the initial tree.
    public static MetaTree<ExpressionNode> CreateInitialTreeForProjections(
        IOperation operation,
        IExpressionNodePool pool,
        IObjectPool<Scope> scopePool)
    {
        var rootInstance = pool.Get(InstanceExpressionFactory.Instance);
        var rootScope = scopePool.Get();
        rootScope.Instance = rootInstance;

        // Go through the selection tree

        // Move logic from the optimizer here?:
        // Add always projected nodes
        // Add nodes that can be projected (has to be a property, or define a projection expression)
        // Include related nodes

        // Convert the nodes to an initial tree by just mapping
        // each source node to an expression factory node.
        // scalar property access --> MemberAccess
        // object property access --> ObjectCreationAsObjectArray
        // array property access -->
        /*
         Note that the member access node has to be a child rather than an instance,
         because by definition the instance refers to the outer variable,
         which may be further wrapped (per scope).

         // .Select
         Select(
            children: [
                // x.Property
                MemberAccess(instance: x, Property),
                Lambda(
                    instance (root): x1,
                    children: [
                        // x2 is initially x1, but can be wrapped
                        MemberAccess(instance: x2, Property1),
                        MemberAccess(instance: x2, Property2)
                    ]
                ]
            )
        */
        // We can wrap everything into null checks if needed at a later stage.
        // It might be worth it to scan the respective branch of the tree
        // to see whether it's been added by other components.
        return default!;
    }
}

public abstract class MetaTreeVisitor<TContext>
{
    public virtual void Visit(MetaTree<ExpressionNode> tree, TContext context)
    {
        Visit(tree.Root, context);
    }

    public virtual void Visit(ExpressionNode node, TContext context)
    {
        Debug.Assert(node.Scope is not null);
        VisitScope(node.Scope!, context);

        var children = node.Children;
        if (children is not null)
            VisitChildren(children, context);
    }

    public virtual void VisitChildren(List<ExpressionNode> children, TContext context)
    {
        foreach (var child in children)
            VisitChild(child, context);
    }

    public virtual void VisitChild(ExpressionNode child, TContext context)
    {
        Visit(child, context);
    }

    public virtual void VisitScope(Scope scope, TContext context)
    {
        Debug.Assert(scope is not null);
        // if (scope.ParentScope is { } parentScope)
        //     VisitScope(parentScope, context);

        Visit(scope.Instance!, context);
    }
}
