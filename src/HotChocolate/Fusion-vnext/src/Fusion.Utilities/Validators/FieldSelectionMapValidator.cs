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
        IType inputType,
        IType outputType)
    {
        var context = new FieldSelectionMapValidatorContext(inputType, outputType);

        Visit(choiceValueSelection, context);

        return [.. context.Errors];
    }

    public ImmutableArray<string> Validate(
        IValueSelectionNode choiceValueSelection,
        IType inputType,
        IType outputType,
        out ImmutableHashSet<IOutputFieldDefinition> selectedFields)
    {
        var context = new FieldSelectionMapValidatorContext(inputType, outputType);

        Visit(choiceValueSelection, context);

        selectedFields = [.. context.SelectedFields];

        return [.. context.Errors];
    }

    protected override ISyntaxVisitorAction Enter(
        IFieldSelectionMapSyntaxNode node,
        FieldSelectionMapValidatorContext context)
    {
        context.Nodes.Push(node);
        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(
        IFieldSelectionMapSyntaxNode node,
        FieldSelectionMapValidatorContext context)
    {
        context.Nodes.Pop();
        return base.Leave(node, context);
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

            if (schema.GetPossibleTypes(type.AsTypeDefinition()).Contains(concreteType))
            {
                context.OutputTypes.Push(concreteType);
            }
            else
            {
                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_InvalidTypeCondition,
                        concreteType.Name,
                        type.AsTypeDefinition().Name));

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

        var terminalType = context.TerminalTypes.Pop();

        if (context.Nodes.TryPeek(out var contextNode)
            && contextNode is PathObjectValueSelectionNode or PathListValueSelectionNode)
        {
            context.OutputTypes.Push(terminalType);
        }
        else if (terminalType.IsComplexType())
        {
            context.Errors.Add(
                string.Format(
                    FieldSelectionMapValidator_FieldMissingSubselections,
                    node.ToString(indented: false)));
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        PathSegmentNode node,
        FieldSelectionMapValidatorContext context)
    {
        if (context.OutputTypes.Peek().NullableType() is IComplexTypeDefinition complexType)
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

            var fieldNullableType = field.Type.NullableType();

            if (fieldNullableType is IComplexTypeDefinition or IUnionTypeDefinition or ListType)
            {
                if (fieldNullableType is IUnionTypeDefinition && node.TypeName is null)
                {
                    context.Errors.Add(
                        string.Format(
                            FieldSelectionMapValidator_FieldMissingTypeCondition,
                            node.FieldName));

                    return Break;
                }

                context.OutputTypes.Push(field.Type);
            }
            else
            {
                if (node.PathSegment is null)
                {
                    var fieldType = field.Type;
                    var inputType = context.InputTypes.Peek();

                    // Fields of a OneOf input object are always non-nullable.
                    if (inputType.IsNullableType()
                        && context.InputTypes.ElementAtOrDefault(1) is IInputObjectTypeDefinition inputObjectType
                        && inputObjectType.Directives.ContainsName(OneOf))
                    {
                        inputType = new NonNullType(inputType);
                    }

                    if (!fieldType.IsCompatibleWith(inputType))
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

                    context.OutputTypes.Push(field.Type);
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

            if (node.PathSegment is null)
            {
                context.TerminalTypes.Push(field.Type);
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

            if (schema.GetPossibleTypes(type.AsTypeDefinition()).Contains(concreteType))
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
                        type.AsTypeDefinition().Name));

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
        if (context.InputTypes.Peek().NullableType() is not IInputObjectTypeDefinition inputType)
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

        if (currentInputType.NullableType() is IInputObjectTypeDefinition inputType)
        {
            if (inputType.Fields.TryGetField(node.Name.Value, out var inputField))
            {
                context.InputTypes.Push(inputField.Type);
            }
            else
            {
                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_FieldDoesNotExistOnInputType,
                        node.Name,
                        inputType.Name));

                return Skip;
            }
        }
        else
        {
            context.Errors.Add(
                string.Format(
                    FieldSelectionMapValidator_ExpectedInputObjectType,
                    currentInputType.ToTypeNode().ToString(indented: false)));

            return Break;
        }

        if (node.ValueSelection is null
            && context.OutputTypes.Peek().NullableType() is IComplexTypeDefinition complexType)
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
        ObjectFieldSelectionNode node,
        FieldSelectionMapValidatorContext context)
    {
        context.InputTypes.Pop();

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        PathObjectValueSelectionNode node,
        FieldSelectionMapValidatorContext context)
    {
        context.OutputTypes.Pop();

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        PathListValueSelectionNode node,
        FieldSelectionMapValidatorContext context)
    {
        context.OutputTypes.Pop();

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        ListValueSelectionNode selectionNode,
        FieldSelectionMapValidatorContext context)
    {
        context.InputTypes.Push(context.InputTypes.Peek().ElementType());
        context.OutputTypes.Push(context.OutputTypes.Peek().ElementType());

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        ListValueSelectionNode selectionNode,
        FieldSelectionMapValidatorContext context)
    {
        context.InputTypes.Pop();
        context.OutputTypes.Pop();

        return Continue;
    }
}

public sealed class FieldSelectionMapValidatorContext
{
    public FieldSelectionMapValidatorContext(IType inputType, IType outputType)
    {
        InputTypes.Push(inputType);
        OutputTypes.Push(outputType);
    }

    public Stack<IFieldSelectionMapSyntaxNode> Nodes { get; } = [];

    public Stack<IType> InputTypes { get; } = [];

    public Stack<IType> OutputTypes { get; } = [];

    public Stack<IType> TerminalTypes { get; } = [];

    public HashSet<IOutputFieldDefinition> SelectedFields { get; } = [];

    public List<string> Errors { get; } = [];
}
