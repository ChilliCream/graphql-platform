using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation.Rules;

internal sealed class FieldSelectionsRule : IDocumentValidatorRule
{
    public ushort Priority => ushort.MaxValue;

    public bool IsCacheable => true;

    public void Validate(DocumentValidatorContext context, DocumentNode document)
    {
        foreach (var operation in document.Definitions)
        {
            if (operation is not OperationDefinitionNode operationDef)
            {
                continue;
            }

            if (context.Schema.TryGetOperationType(operationDef.Operation, out var rootType))
            {
                ValidateSelectionSet(context, operationDef.SelectionSet, rootType);
            }
        }
    }

    private void ValidateSelectionSet(
        DocumentValidatorContext context,
        SelectionSetNode selectionSet,
        IType type)
    {
        if (type.NamedType() is IUnionTypeDefinition unionType
            && HasFields(selectionSet))
        {
            context.ReportError(context.UnionFieldError(selectionSet, unionType));
            return;
        }

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode field)
            {
                ValidateField(context, field, type);
            }
            else if (selection is InlineFragmentNode inlineFrag)
            {
                if (inlineFrag.TypeCondition is null)
                {
                    ValidateSelectionSet(context, inlineFrag.SelectionSet, type);
                }
                else if (context.Schema.Types.TryGetType(inlineFrag.TypeCondition.Name.Value, out var typeCondition))
                {
                    ValidateSelectionSet(context, inlineFrag.SelectionSet, typeCondition);
                }
            }
            else if (selection is FragmentSpreadNode spread)
            {
                var parentType = type.NamedType();

                if (!context.VisitedFragments.Add((parentType.Name, spread.Name.Value)))
                {
                    continue;
                }

                if (context.Fragments.TryGet(spread, out var fragment)
                    && context.Schema.Types.TryGetType(fragment.TypeCondition.Name.Value, out var fragType))
                {
                    ValidateSelectionSet(context, fragment.SelectionSet, fragType);
                }
            }
        }
    }

    private void ValidateField(
        DocumentValidatorContext context,
        FieldNode field,
        IType parentType)
    {
        if (IsTypeNameField(field.Name.Value))
        {
            if (field.IsStreamable())
            {
                context.ReportError(context.StreamOnNonListField(field));
            }

            return;
        }

        var unwrappedParentType = parentType.NamedType();

        if (unwrappedParentType is not IComplexTypeDefinition complex)
        {
            return;
        }

        if (!complex.Fields.TryGetField(field.Name.Value, out var fieldDef))
        {
            context.ReportError(context.FieldDoesNotExist(field, complex));
            return;
        }

        if (field.IsStreamable() && !fieldDef.Type.IsListType())
        {
            context.ReportError(context.StreamOnNonListField(field));
        }

        if (field.SelectionSet is { } fieldSelectionSet)
        {
            ValidateSelectionSet(context, fieldSelectionSet, fieldDef.Type);
        }
    }

    private static bool HasFields(SelectionSetNode selectionSet)
    {
        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            var selection = selectionSet.Selections[i];

            if (selection.Kind is SyntaxKind.Field)
            {
                if (!IsTypeNameField(((FieldNode)selection).Name.Value))
                {
                    return true;
                }
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsTypeNameField(string fieldName)
        => fieldName.Equals(IntrospectionFieldNames.TypeName, StringComparison.Ordinal);
}
