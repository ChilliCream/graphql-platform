using System.Collections.Generic;
using System.Collections.ObjectModel;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.ExpressionNodes;

public sealed class SealedExpressionNode
{
    public SealedExpressionNode? Parent { get; }
    public SealedScope? Instance { get; }
    public Identifier Id { get; }
    public IExpressionFactory ExpressionFactory { get; }
    public ReadOnlyStructuralDependencies Dependencies { get; }
    public IReadOnlyList<SealedExpressionNode> Children { get; }

    public SealedExpressionNode(
        SealedExpressionNode? parent,
        SealedScope? instance,
        Identifier id,
        IExpressionFactory expressionFactory,
        ReadOnlyStructuralDependencies dependencies,
        IReadOnlyList<SealedExpressionNode> children)
    {
        Parent = parent;
        Instance = instance;
        Id = id;
        ExpressionFactory = expressionFactory;
        Dependencies = dependencies;
        Children = children;
    }
}

public sealed class SealedScope
{
    public ExpressionNode Root { get; }
    public ExpressionNode Instance { get; }

    public SealedScope(ExpressionNode root, ExpressionNode instance)
    {
        Root = root;
        Instance = instance;
    }
}

public sealed class Scope
{
    // This indicates the root node that gets you the instance expression.
    public ExpressionNode? Root { get; set; }

    // This one can be wrapped an indicates
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

public class ExpressionNodeTree
{
    public ReadOnlyDictionary<Identifier, ExpressionNode> SelectionIdToOuterNode { get; }
    public ExpressionNode Root { get; }

    public ExpressionNodeTree(
        ReadOnlyDictionary<Identifier, ExpressionNode> selectionIdToOuterNode,
        ExpressionNode root)
    {
        SelectionIdToOuterNode = selectionIdToOuterNode;
        Root = root;
    }
}

public class SealedExpressionNodeTree
{

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

    public static SealedExpressionNodeTree SealExpressionNodeTree(
        ExpressionNodeTree tree)
    {
        return default!;
    }

    // Constructs the initial version of the expression tree meant for projections.
    // There are going to be no structural dependencies in the initial tree.
    public static ExpressionNodeTree CreateInitialTreeForProjections(
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
