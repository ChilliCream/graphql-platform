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
            if(operation is not OperationDefinitionNode operationDef)
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
                    // TODO: TryGet?
                    : context.Schema.Types[inlineFrag.TypeCondition.Name.Value];
                ValidateSelectionSet(context, inlineFrag.SelectionSet, typeCondition);
            }
            else if (selection is FragmentSpreadNode spread
                && context.Fragments.TryEnter(spread, out var frag))
            {
                // TODO: TryGet?
                var typeCondition = context.Schema.Types[frag.TypeCondition.Name.Value];
                ValidateSelectionSet(context, frag.SelectionSet, typeCondition);
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
                    ErrorBuilder.New()
                        .SetMessage($"Field \"{field.Name.Value}\" must not have a selection since type \"{namedType.Name}\" has no subfields.")
                        .AddLocation(field)
                        .Build());
            }
        }
        else
        {
            if (field.SelectionSet is null)
            {
                context.ReportError(
                    ErrorBuilder.New()
                        .SetMessage($"Field \"{field.Name.Value}\" of type \"{namedType.Name}\" must have a selection of subfields. Did you mean \"{field.Name.Value} {{ ... }}\"?")
                        .AddLocation(field)
                        .Build());
            }
            else
            {
                ValidateSelectionSet(context, field.SelectionSet, fieldDef.Type);
            }
        }
    }
}
