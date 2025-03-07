using System.Collections.Immutable;
using HotChocolate.Fusion.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.FusionUtilitiesResources;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Validators;

public sealed class FieldSelectionMapValidator(MutableSchemaDefinition schema)
    : FieldSelectionMapSyntaxVisitor<FieldSelectionMapValidatorContext>(Continue)
{
    public ImmutableArray<string> Validate(
        SelectedValueNode selectedValue,
        ITypeDefinition inputType,
        ITypeDefinition outputType)
    {
        var context = new FieldSelectionMapValidatorContext(inputType, outputType);

        Visit(selectedValue, context);

        return [.. context.Errors];
    }

    protected override ISyntaxVisitorAction Enter(
        PathNode node,
        FieldSelectionMapValidatorContext context)
    {
        if (node.TypeName is { } typeName)
        {
            if (!schema.Types.TryGetType(typeName.Value, out var concreteType))
            {
                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_InvalidTypeConditionInPath,
                        node,
                        typeName));

                return Break;
            }

            if (ValidateConcreteType(concreteType, context))
            {
                context.OutputTypes.Push(concreteType);
            }
            else
            {
                return Break;
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        PathNode node,
        FieldSelectionMapValidatorContext context)
    {
        if (node.TypeName is not null)
        {
            context.OutputTypes.Pop();
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        PathSegmentNode node,
        FieldSelectionMapValidatorContext context)
    {
        if (context.OutputTypes.Peek() is MutableComplexTypeDefinition complexType)
        {
            if (!complexType.Fields.TryGetField(node.FieldName.Value, out var field))
            {
                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_FieldDoesNotExistOnType,
                        node.FieldName.Value,
                        complexType.Name));

                return Break;
            }

            var fieldType = field.Type.NullableType();

            if (fieldType is MutableComplexTypeDefinition or MutableUnionTypeDefinition)
            {
                if (node.PathSegment is null)
                {
                    context.Errors.Add(
                        string.Format(
                            FieldSelectionMapValidator_FieldMissingSubselections,
                            field.Name));

                    return Break;
                }

                if (fieldType is MutableUnionTypeDefinition && node.TypeName is null)
                {
                    context.Errors.Add(
                        string.Format(
                            FieldSelectionMapValidator_FieldMissingTypeCondition,
                            node.FieldName));

                    return Break;
                }

                context.OutputTypes.Push(fieldType.AsTypeDefinition());
            }
            else
            {
                if (node.PathSegment is null)
                {
                    var inputType = context.InputTypes.Peek().NullableType();

                    if (!fieldType.Equals(inputType, TypeComparison.Structural))
                    {
                        var printedFieldType = fieldType.ToTypeNode().Print(indented: false);
                        var printedInputType = inputType.ToTypeNode().Print(indented: false);

                        context.Errors.Add(
                            string.Format(
                                FieldSelectionMapValidator_FieldTypeMismatch,
                                node.FieldName,
                                printedFieldType,
                                printedInputType));

                        return Break;
                    }

                    context.OutputTypes.Push(fieldType.AsTypeDefinition());
                }
                else
                {
                    context.Errors.Add(
                        string.Format(
                            FieldSelectionMapValidator_FieldInvalidSubselections,
                            node.FieldName));

                    return Break;
                }
            }
        }

        if (node.TypeName is { } typeName)
        {
            if (!schema.Types.TryGetType(typeName.Value, out var concreteType))
            {
                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_InvalidTypeConditionInPath,
                        node,
                        typeName));

                return Break;
            }

            if (ValidateConcreteType(concreteType, context))
            {
                context.OutputTypes.Pop();
                context.OutputTypes.Push(concreteType);
            }
            else
            {
                return Break;
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        PathSegmentNode node,
        FieldSelectionMapValidatorContext context)
    {
        context.OutputTypes.Pop();

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        SelectedObjectValueNode node,
        FieldSelectionMapValidatorContext context)
    {
        if (context.InputTypes.Peek() is not MutableInputObjectTypeDefinition inputType)
        {
            return Continue;
        }

        var requiredFieldNames =
            inputType.Fields
                .AsEnumerable()
                .Where(f => f is { Type: NonNullType, DefaultValue: null })
                .Select(f => f.Name)
                .ToHashSet();

        var selectedFieldNames = node.Fields.Select(f => f.Name.Value).ToImmutableHashSet();

        // For an input type with the @oneOf directive, we need to ensure that exactly one of the
        // required fields is selected.
        if (inputType.Directives.ContainsName(OneOf))
        {
            if (selectedFieldNames.Count != 1)
            {
                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_InvalidOneOfFieldSelection,
                        inputType.Name));

                return Break;
            }
        }
        // Otherwise, we need to ensure that all required fields are selected.
        else if (!selectedFieldNames.IsSupersetOf(requiredFieldNames))
        {
            context.Errors.Add(
                string.Format(
                    FieldSelectionMapValidator_SelectionMissingRequiredFields,
                    inputType.Name));

            return Break;
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        SelectedObjectFieldNode node,
        FieldSelectionMapValidatorContext context)
    {
        var currentInputType = context.InputTypes.Peek();

        if (currentInputType is MutableInputObjectTypeDefinition inputType)
        {
            if (inputType.Fields.TryGetField(node.Name.Value, out var inputField))
            {
                context.InputTypes.Push(inputField.Type.AsTypeDefinition());
            }
            else
            {
                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_FieldDoesNotExistOnInputType,
                        node.Name,
                        inputType.Name));

                return Break;
            }
        }
        else
        {
            context.InputTypes.Push(currentInputType);
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        SelectedObjectFieldNode node,
        FieldSelectionMapValidatorContext context)
    {
        context.InputTypes.Pop();

        return Continue;
    }

    private static bool ValidateConcreteType(
        ITypeDefinition concreteType,
        FieldSelectionMapValidatorContext context)
    {
        var outputType = context.OutputTypes.Peek();

        switch (outputType)
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
                            FieldSelectionMapValidator_InvalidTypeCondition,
                            concreteType.Name,
                            interfaceType.Name));
                }
                else
                {
                    context.Errors.Add(
                        string.Format(
                            FieldSelectionMapValidator_TypeMustBeObjectOrInterface,
                            concreteType.Name));
                }

                break;
            case MutableObjectTypeDefinition:
            case MutableUnionTypeDefinition:
                var possibleTypes = GetPossibleTypes(outputType);

                if (possibleTypes.Contains(concreteType))
                {
                    return true;
                }

                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_InvalidTypeCondition,
                        concreteType.Name,
                        outputType.Name));

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

public sealed class FieldSelectionMapValidatorContext
{
    public FieldSelectionMapValidatorContext(ITypeDefinition inputType, ITypeDefinition outputType)
    {
        InputTypes.Push(inputType);
        OutputTypes.Push(outputType);
    }

    public Stack<ITypeDefinition> InputTypes { get; } = [];

    public Stack<ITypeDefinition> OutputTypes { get; } = [];

    public List<string> Errors { get; } = [];
}
