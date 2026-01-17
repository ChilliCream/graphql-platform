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

            if (context.Schema.GetOperationType(operationDef.Operation) is { } rootType)
            {
                ValidateSelectionSet(context, operationDef.SelectionSet, rootType);
            }
        }
    }

    private void ValidateSelectionSet(DocumentValidatorContext context, SelectionSetNode selectionSet, IType type)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode field)
            {
                ValidateField(context, field, type);
            }
            else if (selection is InlineFragmentNode inlineFrag)
            {
                var typeCondition = inlineFrag.TypeCondition is null
                    ? type
                    : context.Schema.Types[inlineFrag.TypeCondition.Name.Value];
                ValidateSelectionSet(context, inlineFrag.SelectionSet, typeCondition);
            }
            else if (selection is FragmentSpreadNode spread && context.Fragments.TryEnter(spread, out var frag))
            {
                var typeCondition = context.Schema.Types[frag.TypeCondition.Name.Value];
                ValidateSelectionSet(context, frag.SelectionSet, typeCondition);
                context.Fragments.Leave(spread);
            }
        }
    }

    private void ValidateField(DocumentValidatorContext context, FieldNode field, IType parentType)
    {
        var unwrappedParentType = parentType.NamedType();

        if (unwrappedParentType is IUnionTypeDefinition unionType
            && field.SelectionSet is {} unionSelectionSet
            && HasFields(unionSelectionSet))
        {
            context.ReportError(context.UnionFieldError(unionSelectionSet, unionType));
            return;
        }

        if (IsTypeNameField(field.Name.Value))
        {
            if (field.IsStreamable())
            {
                context.ReportError(context.StreamOnNonListField(field));
            }

            return;
        }

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

        if (field.SelectionSet is not null)
        {
            ValidateSelectionSet(context, field.SelectionSet, fieldDef.Type);
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
