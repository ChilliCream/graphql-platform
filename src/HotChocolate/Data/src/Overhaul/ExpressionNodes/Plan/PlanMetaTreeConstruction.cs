using System;
using System.Collections.Generic;
using System.Diagnostics;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.ExpressionNodes;

public static class MetaTreeConstruction
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

    // Constructs the initial version of the expression tree meant for projections.
    // There are going to be no structural dependencies in the initial tree.
    public static PlanMetaTree CreateInitialForProjections(
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

        // We can wrap the innermost node (x.Property) e.g. for filtering before projections

        // Also, should allow an abstraction when selecting members and types,
        // in order to map projections from dtos.
        // I don't grasp how this is going to work yet though.
        throw new NotImplementedException();
    }
}
