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
                context.OutputType = concreteType;
            }
            else
            {
                return Break;
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        PathSegmentNode node,
        FieldSelectionMapValidatorContext context)
    {
        if (context.OutputType is MutableComplexTypeDefinition complexType)
        {
            if (!complexType.Fields.TryGetField(node.FieldName.Value, out var field))
            {
                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_FieldDoesNotExistOnType,
                        node.FieldName.Value,
                        context.OutputType.Name));

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

                context.OutputType = fieldType.AsTypeDefinition();
            }
            else
            {
                if (node.PathSegment is null)
                {
                    var inputType = context.InputType.NullableType();

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
                    }
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
                context.OutputType = concreteType;
            }
            else
            {
                return Break;
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        SelectedObjectValueNode node,
        FieldSelectionMapValidatorContext context)
    {
        if (context.InputType is not MutableInputObjectTypeDefinition inputType)
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
            }
        }
        // Otherwise, we need to ensure that all required fields are selected.
        else if (!selectedFieldNames.IsSupersetOf(requiredFieldNames))
        {
            context.Errors.Add(
                string.Format(
                    FieldSelectionMapValidator_SelectionMissingRequiredFields,
                    inputType.Name));
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        SelectedObjectFieldNode node,
        FieldSelectionMapValidatorContext context)
    {
        if (context.InputType is MutableInputObjectTypeDefinition inputType)
        {
            if (inputType.Fields.TryGetField(node.Name.Value, out var inputField))
            {
                context.InputType = inputField.Type.AsTypeDefinition();
            }
            else
            {
                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_FieldDoesNotExistOnInputType,
                        node.Name,
                        inputType.Name));
            }
        }

        return Continue;
    }

    private static bool ValidateConcreteType(
        ITypeDefinition concreteType,
        FieldSelectionMapValidatorContext context)
    {
        switch (context.OutputType)
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
                var possibleTypes = GetPossibleTypes(context.OutputType);

                if (possibleTypes.Contains(concreteType))
                {
                    return true;
                }

                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_InvalidTypeCondition,
                        concreteType.Name,
                        context.OutputType.Name));

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

public sealed class FieldSelectionMapValidatorContext(
    ITypeDefinition inputType,
    ITypeDefinition outputType)
{
    public ITypeDefinition InputType { get; set; } = inputType;

    public ITypeDefinition OutputType { get; set; } = outputType;

    public List<string> Errors { get; } = [];
}
