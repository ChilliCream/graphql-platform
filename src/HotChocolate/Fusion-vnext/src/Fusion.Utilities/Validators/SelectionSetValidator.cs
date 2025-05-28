using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using static HotChocolate.Fusion.FusionUtilitiesResources;

namespace HotChocolate.Fusion.Validators;

public sealed class SelectionSetValidator(ISchemaDefinition schema)
    : SyntaxWalker<SelectionSetValidatorContext>
{
    public ImmutableArray<string> Validate(SelectionSetNode selectionSet, ITypeDefinition type)
    {
        var context = new SelectionSetValidatorContext(type);

        Visit(selectionSet, context);

        return [.. context.Errors];
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        SelectionSetValidatorContext context)
    {
        var type = context.TypeContext.Peek();

        if (type is IComplexTypeDefinition complexType)
        {
            if (complexType.Fields.TryGetField(node.Name.Value, out var field))
            {
                var fieldType = field.Type.NullableType();

                if (fieldType is IComplexTypeDefinition or IUnionTypeDefinition)
                {
                    if (node.SelectionSet?.Selections.Any() != true)
                    {
                        context.Errors.Add(
                            string.Format(
                                SelectionSetValidator_FieldMissingSubselections,
                                field.Name));

                        return Break;
                    }

                    if (fieldType is IUnionTypeDefinition
                        && node.SelectionSet.Selections.Any(s => s is not InlineFragmentNode))
                    {
                        context.Errors.Add(
                            string.Format(
                                SelectionSetValidator_UnionFieldInvalidSelections,
                                field.Name));

                        return Break;
                    }

                    context.TypeContext.Push(fieldType.AsTypeDefinition());
                }
                else if (node.SelectionSet is not null)
                {
                    context.Errors.Add(
                        string.Format(
                            SelectionSetValidator_FieldInvalidSubselections,
                            node.Name.Value));

                    return Break;
                }
                else
                {
                    return Skip;
                }
            }
            else
            {
                context.Errors.Add(
                    string.Format(
                        SelectionSetValidator_FieldDoesNotExistOnType,
                        node.Name.Value,
                        complexType.Name));

                return Break;
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        SelectionSetValidatorContext context)
    {
        context.TypeContext.Pop();

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        InlineFragmentNode node,
        SelectionSetValidatorContext context)
    {
        if (node.TypeCondition is { } typeCondition)
        {
            if (!schema.Types.TryGetType(typeCondition.Name.Value, out var concreteType))
            {
                context.Errors.Add(
                    string.Format(
                        SelectionSetValidator_InvalidTypeConditionInSelectionSet,
                        typeCondition.Name.Value));

                return Break;
            }

            var type = context.TypeContext.Peek();

            if (schema.GetPossibleTypes(type).Contains(concreteType))
            {
                context.TypeContext.Push(concreteType);
            }
            else
            {
                context.Errors.Add(
                    string.Format(
                        SelectionSetValidator_InvalidTypeCondition,
                        concreteType.Name,
                        type.Name));

                return Break;
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        InlineFragmentNode node,
        SelectionSetValidatorContext context)
    {
        if (node.TypeCondition is not null)
        {
            context.TypeContext.Pop();
        }

        return Continue;
    }
}

public sealed class SelectionSetValidatorContext
{
    public SelectionSetValidatorContext(ITypeDefinition type)
    {
        TypeContext.Push(type);
    }

    public Stack<ITypeDefinition> TypeContext { get; } = [];

    public List<string> Errors { get; } = [];
}
