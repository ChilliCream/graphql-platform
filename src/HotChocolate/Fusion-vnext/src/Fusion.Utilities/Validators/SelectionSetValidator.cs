using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.FusionUtilitiesResources;

namespace HotChocolate.Fusion.Validators;

public sealed class SelectionSetValidator(MutableSchemaDefinition schema)
    : SyntaxWalker<SelectionSetValidatorContext>
{
    public ImmutableArray<string> Validate(SelectionSetNode selectedSet, ITypeDefinition type)
    {
        var context = new SelectionSetValidatorContext(type);

        Visit(selectedSet, context);

        return [.. context.Errors];
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        SelectionSetValidatorContext context)
    {
        var type = context.TypeContext.Peek();

        if (type is MutableComplexTypeDefinition complexType)
        {
            if (complexType.Fields.TryGetField(node.Name.Value, out var field))
            {
                var fieldType = field.Type.NullableType();

                if (fieldType is MutableComplexTypeDefinition or MutableUnionTypeDefinition)
                {
                    if (node.SelectionSet?.Selections.Any() != true)
                    {
                        context.Errors.Add(
                            string.Format(
                                SelectionSetValidator_FieldMissingSubselections,
                                field.Name));

                        return Break;
                    }

                    if (fieldType is MutableUnionTypeDefinition
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

            if (ValidateConcreteType(concreteType, context))
            {
                context.TypeContext.Push(concreteType);
            }
            else
            {
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

    private static bool ValidateConcreteType(
        ITypeDefinition concreteType,
        SelectionSetValidatorContext context)
    {
        var type = context.TypeContext.Peek();

        switch (type)
        {
            case MutableInterfaceTypeDefinition interfaceType:
                if (concreteType is MutableComplexTypeDefinition complexType)
                {
                    if (complexType.Implements.Contains(interfaceType))
                    {
                        return true;
                    }

                    context.Errors.Add(
                        string.Format(
                            SelectionSetValidator_InvalidTypeCondition,
                            concreteType.Name,
                            interfaceType.Name));
                }
                else
                {
                    context.Errors.Add(
                        string.Format(
                            SelectionSetValidator_TypeMustBeObjectOrInterface,
                            concreteType.Name));
                }

                break;
            case MutableObjectTypeDefinition:
            case MutableUnionTypeDefinition:
                var possibleTypes = GetPossibleTypes(type);

                if (possibleTypes.Contains(concreteType))
                {
                    return true;
                }

                context.Errors.Add(
                    string.Format(
                        SelectionSetValidator_InvalidTypeCondition,
                        concreteType.Name,
                        type.Name));

                break;
        }

        return false;
    }

    private static ImmutableHashSet<MutableComplexTypeDefinition> GetPossibleTypes(
        ITypeDefinition type)
    {
        switch (type)
        {
            case MutableObjectTypeDefinition objectType:
                return [objectType];

            case MutableUnionTypeDefinition unionType:
                var builder = ImmutableHashSet.CreateBuilder<MutableComplexTypeDefinition>();

                foreach (var memberType in unionType.Types)
                {
                    builder.Add(memberType);

                    foreach (var memberInterface in memberType.Implements)
                    {
                        builder.Add(memberInterface);
                    }
                }

                return builder.ToImmutable();

            default:
                throw new InvalidOperationException();
        }
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
