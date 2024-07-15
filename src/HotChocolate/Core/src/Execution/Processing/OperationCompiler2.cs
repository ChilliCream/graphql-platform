using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing;

internal class OperationCompiler2
{
    //private readonly IReadOnlyList<IOperationCompilerOptimizer>? _optimizers = null;
    //private readonly InputParser _inputParser;

    public IOperation Compile(
        string operationId,
        ObjectType operationType,
        OperationDefinitionNode operationDefinition,
        DocumentNode document,
        ISchema schema,
        bool enableNullBubbling = true)
    {
        var possibleTypeInspector = new PossibleTypeInspector();
        var selectionSetsWithPossibleTypes = possibleTypeInspector.Inspect(schema, document, operationDefinition);



        throw new NotImplementedException();
    }

    private void CollectFields(
        INamedOutputType compositeType,
        SelectionSetNode selectionSet)
    {
        throw new NotImplementedException();
    }

     private void ResolveField(
        CompilerContext context,
        FieldNode selection,
        long includeCondition)
    {
        includeCondition = GetSelectionIncludeCondition(selection, includeCondition);

        var fieldName = selection.Name.Value;
        var responseName = selection.Alias?.Value ?? fieldName;

        if (context.Type.Fields.TryGetField(fieldName, out var field))
        {
            var fieldType = context.EnableNullBubbling
                ? field.Type.RewriteNullability(selection.Required)
                : field.Type.RewriteToNullableType();

            if (context.Fields.TryGetValue(responseName, out var preparedSelection))
            {
                preparedSelection.AddSelection(selection, includeCondition);

                if (selection.SelectionSet is not null)
                {
                    var selectionSetInfo = new SelectionSetInfo(
                        selection.SelectionSet!,
                        includeCondition);
                    var selectionInfos = _selectionLookup[preparedSelection];
                    var next = selectionInfos.Length;
                    Array.Resize(ref selectionInfos, next + 1);
                    selectionInfos[next] = selectionSetInfo;
                    _selectionLookup[preparedSelection] = selectionInfos;
                }
            }
            else
            {
                // if this is the first time we find a selection to this field we have to
                // create a new prepared selection.
                preparedSelection = new Selection.Sealed(
                    GetNextSelectionId(),
                    context.Type,
                    field,
                    fieldType,
                    selection.SelectionSet is not null
                        ? selection.WithSelectionSet(
                            selection.SelectionSet.WithSelections(
                                selection.SelectionSet.Selections))
                        : selection,
                    responseName: responseName,
                    isParallelExecutable: field.IsParallelExecutable,
                    arguments: CoerceArgumentValues(field, selection, responseName),
                    includeConditions: includeCondition == 0
                        ? null
                        : [includeCondition,]);

                context.Fields.Add(responseName, preparedSelection);

                if (selection.SelectionSet is not null)
                {
                    var selectionSetInfo = new SelectionSetInfo(
                        selection.SelectionSet!,
                        includeCondition);
                    _selectionLookup.Add(preparedSelection, [selectionSetInfo,]);
                }
            }
        }
        else
        {
            throw ThrowHelper.FieldDoesNotExistOnType(selection, context.Type.Name);
        }
    }

    private long GetSelectionIncludeCondition(
        ISelectionNode selectionSyntax,
        long parentIncludeCondition)
    {
        throw new NotImplementedException();
    }

    internal sealed class CompilerContext
    {
        public ISchema Schema { get; } = default!;

        public DocumentNode Document { get; } = default!;

        public ObjectType Type { get; } = default!;

        public Dictionary<string, Selection> Fields { get; } = default!;

        public bool EnableNullBubbling { get; } = default!;
    }
}

public sealed class PossibleTypeInspector : SyntaxWalker<PossibleTypeInspector.Context>
{
    public HashSet<SelectionSetNode> Inspect(
        ISchema schema,
        DocumentNode document,
        OperationDefinitionNode operation)
    {
        var context = new Context(schema);
        context.Types.Push(schema.GetOperationType(operation.Operation));
        context.SelectionSets.Push(operation.SelectionSet);

        Visit(document, context);

        return context.SelectionSetsWithPossibleTypes;
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        Context context)
    {
        if (node.SelectionSet is not null)
        {
            context.SelectionSets.Push(node.SelectionSet);

            if (context.Types.TryPeek(out var type)
                && type.NamedType() is IComplexOutputType ct)
            {
                if (ct.Fields.TryGetField(node.Name.Value, out var of))
                {
                    if (of.Type.NamedType().IsLeafType())
                    {
                        return Skip;
                    }

                    context.Types.Push(of.Type.NamedType());
                    return Continue;
                }

                return Skip;
            }
        }

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        Context context)
    {
        if (node.SelectionSet is not null)
        {
            context.SelectionSets.Pop();
        }

        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        FragmentSpreadNode node,
        Context context)
    {
        if (VisitChildren(node, context).IsBreak())
        {
            return Break;
        }

        if (context.Fragments.TryGetValue(node.Name.Value, out var fragment))
        {
            if (Visit(fragment, node, context).IsBreak())
            {
                return Break;
            }
        }

        return DefaultAction;
    }

    protected override ISyntaxVisitorAction Enter(
        InlineFragmentNode node,
        Context context)
    {
        if (node.TypeCondition is not null
            && context.Types.TryPeek(out var type)
            && type.Kind != TypeKind.Object
            && !node.TypeCondition.Name.Value.EqualsOrdinal(type.Name))
        {
            context.SelectionSetsWithPossibleTypes.Add(node.SelectionSet);
            context.Types.Push(context.Schema.GetType<INamedOutputType>(node.TypeCondition.Name.Value));
        }

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(
        InlineFragmentNode node,
        Context context)
    {
        if (node.TypeCondition is not null
            && context.Types.TryPeek(out var type)
            && type.Kind != TypeKind.Object
            && !node.TypeCondition.Name.Value.EqualsOrdinal(type.Name))
        {
            context.Types.Pop();
        }

        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        FragmentDefinitionNode node,
        Context context)
    {
        if (context.Types.TryPeek(out var type)
            && type.Kind != TypeKind.Object
            && !node.TypeCondition.Name.Value.EqualsOrdinal(type.Name))
        {
            context.SelectionSetsWithPossibleTypes.Add(node.SelectionSet);
            context.Types.Push(context.Schema.GetType<INamedOutputType>(node.TypeCondition.Name.Value));
        }

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(
        FragmentDefinitionNode node,
        Context context)
    {
        if (context.Types.TryPeek(out var type)
            && type.Kind != TypeKind.Object
            && !node.TypeCondition.Name.Value.EqualsOrdinal(type.Name))
        {
            context.Types.Pop();
        }

        return base.Leave(node, context);
    }

    public class Context(ISchema schema)
    {
        public ISchema Schema { get; } = schema;

        public Dictionary<string, FragmentDefinitionNode> Fragments { get; } = new();

        public List<SelectionSetNode> SelectionSets { get; } = [];

        public HashSet<SelectionSetNode> SelectionSetsWithPossibleTypes { get; } = [];

        public List<INamedType> Types { get; } = [];
    }
}
