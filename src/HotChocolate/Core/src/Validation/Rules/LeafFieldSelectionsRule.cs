using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation.Rules;

internal sealed class LeafFieldSelectionsRule : IDocumentValidatorRule
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
                if (operationDef.SelectionSet.Selections.Count == 0)
                {
                    context.ReportError(
                        context.NoSelectionOnRootType(
                            operationDef,
                            rootType));

                    continue;
                }

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
                if (inlineFrag.TypeCondition is null)
                {
                    ValidateSelectionSet(context, inlineFrag.SelectionSet, type);
                }
                else if (context.Schema.Types.TryGetType(inlineFrag.TypeCondition.Name.Value, out var typeCondition))
                {
                    ValidateSelectionSet(context, inlineFrag.SelectionSet, typeCondition);
                }
            }
            else if (selection is FragmentSpreadNode spread && context.Fragments.TryEnter(spread, out var frag))
            {
                if (context.Schema.Types.TryGetType(frag.TypeCondition.Name.Value, out var typeCondition))
                {
                    ValidateSelectionSet(context, frag.SelectionSet, typeCondition);
                }

                context.Fragments.Leave(spread);
            }
        }
    }

    private void ValidateField(DocumentValidatorContext context, FieldNode field, IType parentType)
    {
        if (parentType is not IComplexTypeDefinition complex
            || !complex.Fields.TryGetField(field.Name.Value, out var fieldDef))
        {
            // handled by other rules like KnownFieldNames
            return;
        }

        var namedType = fieldDef.Type.NamedType();

        if (namedType.IsLeafType())
        {
            if (field.SelectionSet is not null)
            {
                context.ReportError(
                    context.LeafFieldsCannotHaveSelections(field, complex, fieldDef.Type));
            }
        }
        else
        {
            if (field.SelectionSet is null or { Selections.Count: 0 })
            {
                context.ReportError(
                    context.NoSelectionOnCompositeField(field, complex, fieldDef.Type));
            }
            else
            {
                ValidateSelectionSet(context, field.SelectionSet, fieldDef.Type);
            }
        }
    }
}
