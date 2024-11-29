using System.Diagnostics;
using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using ThrowHelper = HotChocolate.Fusion.Utilities.ThrowHelper;

namespace HotChocolate.Fusion.Planning;

internal sealed class NodeRequestDocumentFormatter(
    FusionGraphConfiguration configuration,
    ISchema schema)
    : RequestDocumentFormatter(configuration)
{
    internal RequestDocument CreateRequestDocument(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        string entityTypeName,
        OperationType operationType = OperationType.Query)
    {
        var rootSelectionSetNode =
            CreateRootSelectionSetNode(
                context,
                executionStep,
                entityTypeName);

        IReadOnlyList<string> path = Array.Empty<string>();

        var operationDefinitionNode = new OperationDefinitionNode(
            null,
            context.CreateRemoteOperationName(),
            operationType,
            context.Exports.CreateVariableDefinitions(
                context.ForwardedVariables,
                executionStep.Variables.Values,
                executionStep.ArgumentTypes),
            Array.Empty<DirectiveNode>(),
            rootSelectionSetNode);

        return new RequestDocument(
            new DocumentNode(new[] { operationDefinitionNode, }),
            path);
    }

    private SelectionSetNode CreateRootSelectionSetNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        string entityTypeName)
    {
        var selectionNodes = new List<ISelectionNode>();
        var selectionSet = context.Operation.GetSelectionSet(executionStep);
        var selectionSetType = executionStep.SelectionSetTypeMetadata;
        var nodeSelection = executionStep.RootSelections[0];
        Debug.Assert(selectionSet is not null);
        Debug.Assert(executionStep.RootSelections.Count == 1);

        // create
        if (nodeSelection.Resolver is null)
        {
            throw new InvalidOperationException(
                "Node fields will always have a resolver.");
        }

        if (nodeSelection.Selection.SelectionSet is null)
        {
            throw new InvalidOperationException(
                "Node fields will always have a selection-set.");
        }

        var selectionSetNode =
            CreateSelectionSetNode(
                context,
                executionStep,
                nodeSelection.Selection,
                entityTypeName);

        ResolveRequirements(
            context,
            nodeSelection.Selection,
            selectionSetType,
            executionStep.ParentSelection,
            nodeSelection.Resolver,
            executionStep.Variables);

        var (selectionNode, _) = nodeSelection.Resolver.CreateSelection(
            context.VariableValues,
            selectionSetNode,
            nodeSelection.Selection.ResponseName,
            null,
            nodeSelection.Selection.SyntaxNode.Directives);

        if (selectionNode is FieldNode fieldNode &&
            !nodeSelection.Selection.ResponseName.EqualsOrdinal(fieldNode.Name.Value))
        {
            selectionNode = fieldNode.WithAlias(new(nodeSelection.Selection.ResponseName));
        }

        selectionNodes.Add(selectionNode);

        return new SelectionSetNode(selectionNodes);
    }

    private SelectionSetNode CreateSelectionSetNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        ISelection parentSelection,
        string entityTypeName)
    {
        var selectionNodes = new List<ISelectionNode>();
        var typeSelectionNodes = new List<ISelectionNode>();
        var entityType = schema.GetType<ObjectType>(entityTypeName);
        var selectionSet = (SelectionSet)context.Operation.GetSelectionSet(parentSelection, entityType);

        CreateSelectionNodes(
            context,
            executionStep,
            entityType,
            selectionSet,
            typeSelectionNodes);

        AddInlineFragment(entityType);

        return new SelectionSetNode(selectionNodes);

        void AddInlineFragment(IObjectType possibleType)
        {
            var needsTypeNameField = true;

            for (var i = typeSelectionNodes.Count - 1; i >= 0; i--)
            {
                var selection = typeSelectionNodes[i];

                if (selection is FieldNode field &&
                    field.Name.Value.EqualsOrdinal(IntrospectionFields.TypeName))
                {
                    needsTypeNameField = false;
                    break;
                }
            }

            if (needsTypeNameField)
            {
                typeSelectionNodes.Add(TypeNameField);
            }

            var inlineFragment = new InlineFragmentNode(
                null,
                new NamedTypeNode(
                    null,
                    new NameNode(
                        Configuration.GetSubgraphTypeName(
                            executionStep.SubgraphName,
                            possibleType.Name))),
                Array.Empty<DirectiveNode>(),
                new SelectionSetNode(typeSelectionNodes));
            selectionNodes.Add(inlineFragment);
            typeSelectionNodes = [];
        }
    }

    protected override bool CreateSelectionNodes(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        IObjectType possibleType,
        SelectionSet selectionSet,
        List<ISelectionNode> selectionNodes)
    {
        var onlyIntrospection = true;
        var typeContext = Configuration.GetType<ObjectTypeMetadata>(possibleType.Name);

        ref var selection = ref selectionSet.GetSelectionsReference();
        ref var end = ref Unsafe.Add(ref selection, selectionSet.Selections.Count);

        while(Unsafe.IsAddressLessThan(ref selection, ref end))
        {
            if (!executionStep.AllSelections.Contains(selection) &&
                !selection.Field.Name.EqualsOrdinal(IntrospectionFields.TypeName))
            {
                goto NEXT;
            }

            if (onlyIntrospection && !selection.Field.IsIntrospectionField)
            {
                onlyIntrospection = false;
            }

            AddSelectionNode(
                context,
                executionStep,
                selection,
                typeContext.Fields[selection.Field.Name],
                selectionNodes);

            if (!selection.Arguments.IsFullyCoercedNoErrors)
            {
                foreach (var argument in selection.Arguments)
                {
                    if (!argument.IsFullyCoerced)
                    {
                        TryForwardVariable(
                            context,
                            executionStep.SubgraphName,
                            null,
                            argument,
                            argument.Name);
                    }
                }
            }

            NEXT:
            selection = ref Unsafe.Add(ref selection, 1)!;
        }

        if (selectionSet.Selections.Count == 0 && selectionNodes.Count == 0)
        {
            // Since each entity type has its unique subgraph query we need to substitute
            // subgraph queries where the consumer did not specify any fields explicitly.
            selectionNodes.Add(TypeNameField);
        }

        // append exports that were required by other execution steps.
        foreach (var exportSelection in
            context.Exports.GetExportSelections(executionStep, selectionSet))
        {
            selectionNodes.Add(exportSelection);
        }

        if (selectionSet.Selections.Count > 0 && selectionNodes.Count == 0)
        {
            throw ThrowHelper.RequestFormatter_SelectionSetEmpty();
        }

        return onlyIntrospection;
    }
}
