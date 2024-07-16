using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Processing;

internal partial class OperationCompiler2
{
    //private readonly IReadOnlyList<IOperationCompilerOptimizer>? _optimizers = null;
    private readonly InputParser _inputParser;

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
        CompilerContext context,
        SelectionSetNode selectionSet,
        long includeCondition)
    {
        var selections = selectionSet.Selections;
        var selectionCount = selections.Count;

        for (var i = 0; i < selectionCount; i++)
        {
            ResolveFields(context, selections[i], includeCondition);
        }
    }

    private void ResolveFields(
        CompilerContext context,
        ISelectionNode selection,
        long includeCondition)
    {
        switch (selection.Kind)
        {
            case SyntaxKind.Field:
                ResolveField(
                    context,
                    (FieldNode)selection,
                    includeCondition);
                break;

            case SyntaxKind.InlineFragment:
                ResolveInlineFragment(
                    context,
                    (InlineFragmentNode)selection,
                    includeCondition);
                break;

            case SyntaxKind.FragmentSpread:
                ResolveFragmentSpread(
                    context,
                    (FragmentSpreadNode)selection,
                    includeCondition);
                break;
        }
    }

    private void ResolveField(
        CompilerContext context,
        FieldNode fieldSelection,
        long includeCondition)
    {
        includeCondition = GetSelectionIncludeCondition(fieldSelection, includeCondition);

        var fieldName = fieldSelection.Name.Value;
        var responseName = fieldSelection.Alias?.Value ?? fieldName;

        if (context.Type.Fields.TryGetField(fieldName, out var field))
        {
            var fieldType = context.EnableNullBubbling
                ? field.Type
                : field.Type.RewriteToNullableType();

            if (context.Fields.TryGetValue(responseName, out var selection))
            {
                selection.AddSelection(fieldSelection, includeCondition);

                if (fieldSelection.SelectionSet is not null)
                {
                    var selectionSetInfo = new SelectionSetInfo(
                        fieldSelection.SelectionSet!,
                        includeCondition);
                    context.Enqueue(selection, selectionSetInfo);
                }
            }
            else
            {
                // if this is the first time we find a selection to this field we have to
                // create a new prepared selection.
                selection = new Selection.Sealed(
                    context.GetNextSelectionId(),
                    context.Type,
                    field,
                    fieldType,
                    fieldSelection.SelectionSet is not null
                        ? fieldSelection.WithSelectionSet(
                            fieldSelection.SelectionSet.WithSelections(
                                fieldSelection.SelectionSet.Selections))
                        : fieldSelection,
                    responseName: responseName,
                    isParallelExecutable: field.IsParallelExecutable,
                    arguments: CoerceArgumentValues(field, fieldSelection, responseName),
                    includeConditions: includeCondition == 0
                        ? null
                        : [includeCondition,]);

                context.Fields.Add(responseName, selection);

                if (fieldSelection.SelectionSet is not null)
                {
                    var selectionSetInfo = new SelectionSetInfo(
                        fieldSelection.SelectionSet!,
                        includeCondition);
                    context.Enqueue(selection, selectionSetInfo);
                }
            }
        }
        else
        {
            throw ThrowHelper.FieldDoesNotExistOnType(fieldSelection, context.Type.Name);
        }
    }

    private void ResolveInlineFragment(
        CompilerContext context,
        InlineFragmentNode inlineFragment,
        long includeCondition)
    {
        ResolveFragment(
            context,
            inlineFragment,
            inlineFragment.TypeCondition,
            inlineFragment.SelectionSet,
            inlineFragment.Directives,
            includeCondition);
    }

    private void ResolveFragmentSpread(
        CompilerContext context,
        FragmentSpreadNode fragmentSpread,
        long includeCondition)
    {
        var fragmentDef = context.GetFragmentDefinition(fragmentSpread);

        ResolveFragment(
            context,
            fragmentSpread,
            fragmentDef.TypeCondition,
            fragmentDef.SelectionSet,
            fragmentSpread.Directives,
            includeCondition);
    }

    private void ResolveFragment(
        CompilerContext context,
        ISelectionNode selection,
        NamedTypeNode? typeCondition,
        SelectionSetNode selectionSet,
        IReadOnlyList<DirectiveNode> directives,
        long includeCondition)
    {
        if (typeCondition is null
            || (context.Schema.TryGetTypeFromAst(typeCondition, out IType typeCon)
                && DoesTypeApply(typeCon, context.Type)))
        {
            includeCondition = GetSelectionIncludeCondition(selection, includeCondition);

            if (directives.IsDeferrable())
            {
                throw new NotImplementedException();
            }
            else
            {
                CollectFields(context, selectionSet, includeCondition);
            }
        }
    }

    private static bool DoesTypeApply(IType typeCondition, IObjectType current)
        => typeCondition.Kind switch
        {
            TypeKind.Object => ReferenceEquals(typeCondition, current),
            TypeKind.Interface => current.IsImplementing((InterfaceType)typeCondition),
            TypeKind.Union => ((UnionType)typeCondition).Types.ContainsKey(current.Name),
            _ => false,
        };

    private long GetSelectionIncludeCondition(
        ISelectionNode selection,
        long parentIncludeCondition)
    {
        throw new NotImplementedException();
    }

    private long GetSelectionIncludeCondition(
        IncludeCondition condition,
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

        public int GetNextSelectionId() => default!;

        public int GetNextFragmentId() => default!;

        public void Enqueue(Selection selection, SelectionSetInfo selectionSet)
        {
        }

        public FragmentDefinitionNode GetFragmentDefinition(
            FragmentSpreadNode fragmentSpread)
        {
            throw new NotImplementedException();
        }
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
        context.Types.Push(schema.GetOperationType(operation.Operation)!);
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
