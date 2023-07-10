using System;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Handlers;

public static class SelectionSetHelper
{
    public static bool IsFieldAlreadyInSelection(
        this SelectionSetOptimizerContext context,
        string fieldName,
        string alias)
    {
        Selection? field;
        if (IsFieldAlreadyInSelection(context, fieldName))
        {
            return true;
        }

        // if the field is already added as an alias we do not need to add it
        if (context.Selections.TryGetValue(alias, out field) &&
            field.Field.Name == fieldName)
        {
            return true;
        }

        return false;
    }

    public static bool IsFieldAlreadyInSelection(
        this SelectionSetOptimizerContext context, string fieldName)
    {
        // if the field is already in the selection set we do not need to project it
        if (context.Selections.TryGetValue(fieldName, out var field) &&
            field.Field.Name == fieldName)
        {
            return true;
        }

        return false;
    }

    public static void AddNewFieldToSelection(
        this SelectionSetOptimizerContext context,
        Selection selection,
        ObjectType type,
        string fieldName,
        string alias)
    {
        IObjectField nodesField = type.Fields[fieldName];
        var nodesFieldNode = new FieldNode(
            null,
            new NameNode(fieldName),
            new NameNode(alias),
            null,
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
            null);

        var nodesPipeline = context.CompileResolverPipeline(nodesField, nodesFieldNode);

        var compiledSelection = new Selection.Sealed(
            context.GetNextSelectionId(),
            context.Type,
            nodesField,
            nodesField.Type,
            nodesFieldNode,
            alias,
            resolverPipeline: nodesPipeline,
            arguments: selection.Arguments,
            isInternal: true);

        context.AddSelection(compiledSelection);
    }
}
