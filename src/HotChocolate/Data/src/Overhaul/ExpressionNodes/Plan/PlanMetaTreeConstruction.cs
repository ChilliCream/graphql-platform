using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Data.ExpressionNodes;

public static class MetaTreeConstruction
{
    public static ExpressionNode GetExpressionNodeOfSelection(
        Dictionary<Identifier, ExpressionNode> selectionIdToInnerNode,
        Identifier selectionId,
        bool innermost = true)
    {
        var node = selectionIdToInnerNode[selectionId];
        if (innermost)
            return node;
        return node.OutermostNode;
    }

    public static void WrapExpressionNode(
        ExpressionNode wrapperNode,
        ExpressionNode nodeToWrap)
    {
        Debug.Assert(!wrapperNode.IsInnermost);

        var initialNode = nodeToWrap.InnermostInitialNode;
        var outerNode = nodeToWrap.OutermostNode;
        if (nodeToWrap == outerNode)
            initialNode.OutermostNode = wrapperNode;

        // The wrapper node can never be the innermost node.
        wrapperNode.InnermostInitialNode = initialNode;
        (wrapperNode.ChildrenInternal ??= new()).Insert(0, nodeToWrap);

        var parent = nodeToWrap.Parent;
        wrapperNode.Parent = parent;
        nodeToWrap.Parent = wrapperNode;

        if (parent is not null)
        {
            var children = parent.Children;
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

        {
            var deps = node.OwnDependencies;
            if (deps.Structural.Unspecified)
                return ResultKind.All;
            foreach (var id in deps.Structural.VariableIds!)
                outResult.Add(id);
        }

        var instanceNode = node.Scope?.Instance;
        var instanceResult = RetrieveStructuralDependenciesOfBranch(instanceNode, outResult);
        if (instanceResult == ResultKind.All)
            return ResultKind.All;

        var children = node.ChildrenInternal;
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
        ExpressionPools pools)
    {
        var selectionIdToInnerNode = new Dictionary<Identifier, ExpressionNode>();
        var rootLambdaNode = pools.ExpressionNodePool.Create(Lambda.Instance);
        rootLambdaNode.Scope = pools.CreateScopeWithInstance(rootLambdaNode);

        // Go through the selection tree

        // Move logic from the optimizer here?:
        // Add always projected nodes
        // Add nodes that can be projected (has to be a property, or define a projection expression)
        // Include related nodes

        // Convert the nodes to an initial tree by just mapping
        // each source node to an expression factory node.

        // scalar property access --> MemberAccess
        ExpressionNode CreateMemberAccess(
            PropertyInfo property,
            Scope scope)
        {
            var expressionFactory = new MemberAccess(property);
            var result = pools.ExpressionNodePool.CreateInnermost(expressionFactory);
            result.Scope = scope;
            return result;
        }

        // object property access --> ObjectCreationAsObjectArray
        ExpressionNode HandleObjectNode(Scope scope)
        {
            var result = pools.ExpressionNodePool.CreateInnermost(ObjectCreationAsObjectArray.Instance);
            result.Scope = scope;

            // Could pool the lists as well for each of the pooled nodes.
            // var children = new List<ExpressionNode>();
            // foreach (var property in propertiesToProject)
            // {
            //     var child = CreateMemberAccess(property, scope);
            //     children.Add(child);
            //     child.Parent = result;
            // }
            // result.Children = children;

            return result;
        }

        // array property access -->
        /*
         Note that the member access node has to be a child rather than an instance,
         because by definition the instance refers to the outer variable,
         which may be further wrapped (per scope).

         If you think of .Select as a static method call instead of as an extension method call,
         it makes more sense why it should wrap the member access.

         // .Select
         Select(
            children: [
                // x.Property
                MemberAccess(instance: x, Property),
                Lambda(
                    instance (root): x1,
                    children: [
                        // Here we have the regular object projection with the new scope.
                        ObjectArray(
                            children: [
                                // x2 is initially x1, but can be wrapped
                                MemberAccess(instance: x2, Property1),
                                MemberAccess(instance: x2, Property2)
                            ]
                        )
                    ]
                ]
            )
        */
        // We can wrap everything into null checks if needed at a later stage.
        // It might be worth it to scan the respective branch of the tree
        // to see whether it's been added by other components.

        // We can wrap the innermost node (x.Property) e.g. for filtering before projections

        // Returns the node that you can safely call AssumeArray on.
        ExpressionNode HandleArrayNode(
            PropertyInfo property,
            Scope scope)
        {
            // x.Property
            var memberAccess = CreateMemberAccess(property, scope);

            // x => { }
            var lambda = pools.ExpressionNodePool.CreateInnermost(Lambda.Instance);
            // the x parameter
            lambda.Scope = pools.CreateScopeWithInstance(lambda);

            // ().Select(...)
            var select = pools.ExpressionNodePool.Create(Select.Instance);
            select.AssumeArray().ArrangeChildren(memberAccess, lambda);
            select.Scope = scope;

            return select;
        }

        var rootNode = HandleObjectNode(rootLambdaNode.Scope);
        rootLambdaNode.Children.Add(rootNode);

        selectionIdToInnerNode.Add(new(operation.RootSelectionSet.Id), rootNode);
        // TODO:
        var fieldsToProject = Array.Empty<FieldNode>();
        var children = new List<ExpressionNode>();
        foreach (var field in fieldsToProject)
        {
            var propertyInfo = default(PropertyInfo)!; // field.PropertyInfo
            bool hasProjections = field.SelectionSet is not null;
            bool isArray = propertyInfo.PropertyType.IsArray;

            ExpressionNode node;
            if (hasProjections && isArray)
                node = HandleArrayNode(propertyInfo, rootNode.Scope!); // recurse here for the child selections
            else if (hasProjections)
                node = HandleObjectNode(rootNode.Scope!); // recurse here for the child selections
            else
                node = CreateMemberAccess(propertyInfo, rootLambdaNode.Scope);

            node.Parent = rootNode;
            children.Add(node);

            var id = default(Identifier); // how do I get the selection id here?
            selectionIdToInnerNode.Add(id, node.InnermostInitialNode);
        }
        rootNode.ChildrenInternal = children;

        return new PlanMetaTree(selectionIdToInnerNode, rootNode);
        // Also, should allow an abstraction when selecting members and types,
        // in order to map projections from dtos.
        // I don't grasp how this is going to work yet though.
        // May also want to allow some sort of tagging, but that's for later.
    }
}
