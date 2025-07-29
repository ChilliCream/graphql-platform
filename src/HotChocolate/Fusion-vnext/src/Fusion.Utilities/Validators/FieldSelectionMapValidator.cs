using System.Collections.Immutable;
using HotChocolate.Fusion.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Types;
using static HotChocolate.Fusion.FusionUtilitiesResources;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Validators;

public sealed class FieldSelectionMapValidator(ISchemaDefinition schema)
    : FieldSelectionMapSyntaxVisitor<FieldSelectionMapValidatorContext>(Continue)
{
    public ImmutableArray<string> Validate(
        IValueSelectionNode choiceValueSelection,
        ITypeDefinition inputType,
        ITypeDefinition outputType)
    {
        var context = new FieldSelectionMapValidatorContext(inputType, outputType);

        Visit(choiceValueSelection, context);

        return [.. context.Errors];
    }

    public ImmutableArray<string> Validate(
        IValueSelectionNode choiceValueSelection,
        ITypeDefinition inputType,
        ITypeDefinition outputType,
        out ImmutableHashSet<IOutputFieldDefinition> selectedFields)
    {
        var context = new FieldSelectionMapValidatorContext(inputType, outputType);

        Visit(choiceValueSelection, context);

        selectedFields = [.. context.SelectedFields];

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

            var type = context.OutputTypes.Peek();

            if (schema.GetPossibleTypes(type).Contains(concreteType))
            {
                context.OutputTypes.Push(concreteType);
            }
            else
            {
                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_InvalidTypeCondition,
                        concreteType.Name,
                        type.Name));

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
        if (context.OutputTypes.Peek() is IComplexTypeDefinition complexType)
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

            context.SelectedFields.Add(field);

            var fieldType = field.Type.NullableType();

            if (fieldType is IComplexTypeDefinition or IUnionTypeDefinition)
            {
                if (node.PathSegment is null)
                {
                    context.Errors.Add(
                        string.Format(
                            FieldSelectionMapValidator_FieldMissingSubselections,
                            field.Name));

                    return Break;
                }

                if (fieldType is IUnionTypeDefinition && node.TypeName is null)
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

            var type = context.OutputTypes.Peek();

            if (schema.GetPossibleTypes(type).Contains(concreteType))
            {
                context.OutputTypes.Pop();
                context.OutputTypes.Push(concreteType);
            }
            else
            {
                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_InvalidTypeCondition,
                        concreteType.Name,
                        type.Name));

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
        ObjectValueSelectionNode selectionNode,
        FieldSelectionMapValidatorContext context)
    {
        if (context.InputTypes.Peek() is not IInputObjectTypeDefinition inputType)
        {
            return Continue;
        }

        var requiredFieldNames =
            inputType.Fields
                .AsEnumerable()
                .Where(f => f is { Type: NonNullType, DefaultValue: null })
                .Select(f => f.Name)
                .ToHashSet();

        var selectedFieldNames = selectionNode.Fields.Select(f => f.Name.Value).ToImmutableHashSet();

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
        ObjectFieldSelectionNode node,
        FieldSelectionMapValidatorContext context)
    {
        var currentInputType = context.InputTypes.Peek();

        if (currentInputType is IInputObjectTypeDefinition inputType)
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

        if (node.ValueSelection is null
            && context.OutputTypes.Peek() is IComplexTypeDefinition complexType)
        {
            if (!complexType.Fields.TryGetField(node.Name.Value, out var field))
            {
                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_FieldDoesNotExistOnType,
                        node.Name,
                        complexType.Name));

                return Skip;
            }

            context.SelectedFields.Add(field);
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        ObjectValueSelectionNode node,
        FieldSelectionMapValidatorContext context)
    {
        context.InputTypes.Pop();

        return Continue;
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

    public HashSet<IOutputFieldDefinition> SelectedFields { get; } = [];

    public List<string> Errors { get; } = [];
}
