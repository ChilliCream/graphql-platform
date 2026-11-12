using System.Collections.Immutable;
using HotChocolate.Fusion.Converters;
using HotChocolate.Fusion.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Types;
using static HotChocolate.Fusion.FusionUtilitiesResources;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Validators;

public sealed class FieldSelectionMapValidator(
    ISchemaDefinition schema,
    bool disallowNullableFieldsOnPathToNonNullInputType = false)
    : FieldSelectionMapSyntaxVisitor<FieldSelectionMapValidatorContext>(Continue)
{
    private static readonly IReadOnlyDictionary<string, ImmutableArray<ITypeDefinition>>
        s_noContextTypes = ImmutableDictionary<string, ImmutableArray<ITypeDefinition>>.Empty;

    public ImmutableArray<string> Validate(
        IValueSelectionNode choiceValueSelection,
        IType inputType,
        IType outputType)
        => Validate(choiceValueSelection, inputType, outputType, s_noContextTypes);

    /// <summary>
    /// Validates the field selection map against the input and output type.
    /// </summary>
    /// <param name="choiceValueSelection">The field selection map to validate.</param>
    /// <param name="inputType">The input type the selected value is assigned to.</param>
    /// <param name="outputType">The output type the paths of the selection start at.</param>
    /// <param name="contextTypes">
    /// The types that set each context, keyed by context name. A context that is missing or has
    /// no types is not set by any type.
    /// </param>
    /// <returns>The validation errors, or an empty array if the selection is valid.</returns>
    public ImmutableArray<string> Validate(
        IValueSelectionNode choiceValueSelection,
        IType inputType,
        IType outputType,
        IReadOnlyDictionary<string, ImmutableArray<ITypeDefinition>> contextTypes)
    {
        var context = new FieldSelectionMapValidatorContext(inputType, outputType, contextTypes);

        Visit(choiceValueSelection, context);

        return [.. context.Errors];
    }

    public ImmutableArray<string> Validate(
        IValueSelectionNode choiceValueSelection,
        IType inputType,
        IType outputType,
        out ImmutableHashSet<IOutputFieldDefinition> selectedFields)
        => Validate(
            choiceValueSelection,
            inputType,
            outputType,
            s_noContextTypes,
            out selectedFields);

    /// <summary>
    /// Validates the field selection map against the input and output type and returns the
    /// output fields it selects.
    /// </summary>
    /// <param name="choiceValueSelection">The field selection map to validate.</param>
    /// <param name="inputType">The input type the selected value is assigned to.</param>
    /// <param name="outputType">The output type the paths of the selection start at.</param>
    /// <param name="contextTypes">
    /// The types that set each context, keyed by context name. A context that is missing or has
    /// no types is not set by any type.
    /// </param>
    /// <param name="selectedFields">The output fields the selection selects.</param>
    /// <returns>The validation errors, or an empty array if the selection is valid.</returns>
    public ImmutableArray<string> Validate(
        IValueSelectionNode choiceValueSelection,
        IType inputType,
        IType outputType,
        IReadOnlyDictionary<string, ImmutableArray<ITypeDefinition>> contextTypes,
        out ImmutableHashSet<IOutputFieldDefinition> selectedFields)
    {
        var context = new FieldSelectionMapValidatorContext(inputType, outputType, contextTypes);

        Visit(choiceValueSelection, context);

        selectedFields = [.. context.SelectedFields];

        return [.. context.Errors];
    }

    protected override ISyntaxVisitorAction Enter(
        IFieldSelectionMapSyntaxNode node,
        FieldSelectionMapValidatorContext context)
    {
        if (TryGetContextPath(node, context) is { } contextPath
            && context.ContextTypes.TryGetValue(
                contextPath.ContextName!.Value,
                out var contextTypes)
            && !contextTypes.IsDefaultOrEmpty)
        {
            ValidateForEachContextType(node, contextPath, contextTypes, context);

            return Skip;
        }

        context.Nodes.Push(node);
        return base.Enter(node, context);
    }

    // A context path without a type condition is rooted at every type that sets the context. The
    // node that owns the path (the path itself, or the path with its object or list selection)
    // is validated as a whole once per context type.
    private static PathNode? TryGetContextPath(
        IFieldSelectionMapSyntaxNode node,
        FieldSelectionMapValidatorContext context)
    {
        var path = node switch
        {
            PathNode pathNode
                when !context.Nodes.TryPeek(out var parent)
                    || parent is not (PathObjectValueSelectionNode or PathListValueSelectionNode)
                => pathNode,
            PathObjectValueSelectionNode pathObject => pathObject.Path,
            PathListValueSelectionNode pathList => pathList.Path,
            _ => null
        };

        return path is { ContextName: not null, TypeName: null }
            && !ReferenceEquals(path, context.ResolvedContextPath)
            ? path
            : null;
    }

    private void ValidateForEachContextType(
        IFieldSelectionMapSyntaxNode node,
        PathNode contextPath,
        ImmutableArray<ITypeDefinition> contextTypes,
        FieldSelectionMapValidatorContext context)
    {
        foreach (var contextType in contextTypes)
        {
            var contextTypeContext =
                new FieldSelectionMapValidatorContext(context, contextType, contextPath);

            Visit(node, contextTypeContext);

            context.Errors.AddRange(contextTypeContext.Errors);
            context.SelectedFields.UnionWith(contextTypeContext.SelectedFields);
        }
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
        if (node.ContextName is { } contextName
            && !ReferenceEquals(node, context.ResolvedContextPath))
        {
            return EnterContextPath(node, contextName, context);
        }

        if (node.TypeName is { } typeName)
        {
            if (!schema.Types.TryGetType(typeName.Value, out var conditionType))
            {
                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_InvalidTypeConditionInPath,
                        node,
                        typeName));

                return Break;
            }

            var type = context.OutputTypes.Peek();

            if (HasPossibleTypeOverlap(type.AsTypeDefinition(), conditionType))
            {
                context.OutputTypes.Push(conditionType);
            }
            else
            {
                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_InvalidTypeCondition,
                        conditionType.Name,
                        type.AsTypeDefinition().Name));

                return Break;
            }
        }

        return Continue;
    }

    private ISyntaxVisitorAction EnterContextPath(
        PathNode node,
        NameNode contextName,
        FieldSelectionMapValidatorContext context)
    {
        if (!context.ContextTypes.TryGetValue(contextName.Value, out var contextTypes)
            || contextTypes.IsDefaultOrEmpty)
        {
            context.Errors.Add(
                string.Format(FieldSelectionMapValidator_ContextNotSet, contextName.Value));

            return Break;
        }

        // A context path without a type condition is validated per context type before it
        // reaches this method, so the path has a type condition here.
        var typeName = node.TypeName!;

        if (!schema.Types.TryGetType(typeName.Value, out var conditionType))
        {
            context.Errors.Add(
                string.Format(
                    FieldSelectionMapValidator_InvalidTypeConditionInPath,
                    node,
                    typeName));

            return Break;
        }

        if (conditionType is not IObjectTypeDefinition conditionObject)
        {
            context.Errors.Add(
                string.Format(
                    FieldSelectionMapValidator_ContextTypeConditionNotObjectType,
                    conditionType.Name,
                    contextName.Value));

            return Break;
        }

        if (!contextTypes.Any(t => SetsContext(t, conditionObject)))
        {
            context.Errors.Add(
                string.Format(
                    FieldSelectionMapValidator_TypeDoesNotSetContext,
                    conditionObject.Name,
                    contextName.Value));

            return Break;
        }

        context.OutputTypes.Push(conditionObject);

        return Continue;
    }

    private bool SetsContext(ITypeDefinition contextType, IObjectTypeDefinition objectType)
        => contextType.Name.Equals(objectType.Name, StringComparison.Ordinal)
            || (contextType is not IObjectTypeDefinition
                && schema.GetPossibleTypes(contextType)
                    .Any(t => t.Name.Equals(objectType.Name, StringComparison.Ordinal)));

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
        var outputType = context.OutputTypes.Peek();
        if (outputType.NullableType() is IComplexTypeDefinition complexType)
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

            if (node.Arguments.Length > 0)
            {
                ConstantArgumentValidator.Validate(
                    FieldSelectionMapValueNodeConverter.Convert(node.Arguments),
                    field,
                    context.Errors);
            }

            var fieldNullableType = field.Type.NullableType();

            if (fieldNullableType is IComplexTypeDefinition or IUnionTypeDefinition or ListType)
            {
                if (fieldNullableType is IUnionTypeDefinition && node.TypeName is null)
                {
                    context.Errors.Add(
                        string.Format(
                            FieldSelectionMapValidator_FieldMissingTypeCondition,
                            node.FieldName.Value));

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

                    if (disallowNullableFieldsOnPathToNonNullInputType
                        && inputType.IsNonNullType()
                        && context.LastNullableField is { } nullableFieldOnPath)
                    {
                        var printedInputType = inputType.ToTypeNode().Print(indented: false);

                        context.Errors.Add(
                            string.Format(
                                FieldSelectionMapValidator_NullableFieldOnPathToNonNullType,
                                printedInputType,
                                nullableFieldOnPath.Name));

                        return Break;
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

            if (field.Type.IsNullableType())
            {
                context.LastNullableField = field;
            }

            if (node.PathSegment is null)
            {
                context.TerminalTypes.Push(field.Type);
            }
        }
        else
        {
            context.Errors.Add(
                string.Format(
                    FieldSelectionMapValidator_InvalidFieldParentType,
                    node.FieldName.Value,
                    outputType));

            return Break;
        }

        if (node.TypeName is { } typeName)
        {
            if (!schema.Types.TryGetType(typeName.Value, out var conditionType))
            {
                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_InvalidTypeConditionInPath,
                        node,
                        typeName));

                return Break;
            }

            var type = context.OutputTypes.Peek();

            if (HasPossibleTypeOverlap(type.AsTypeDefinition(), conditionType))
            {
                context.OutputTypes.Pop();
                context.OutputTypes.Push(conditionType);
            }
            else
            {
                context.Errors.Add(
                    string.Format(
                        FieldSelectionMapValidator_InvalidTypeCondition,
                        conditionType.Name,
                        type.AsTypeDefinition().Name));

                return Break;
            }
        }

        return Continue;
    }

    private bool HasPossibleTypeOverlap(
        ITypeDefinition currentType,
        ITypeDefinition conditionType)
    {
        if (ReferenceEquals(currentType, conditionType)
            || currentType.Name.Equals(conditionType.Name, StringComparison.Ordinal))
        {
            return true;
        }

        if (conditionType is IObjectTypeDefinition conditionObject)
        {
            return schema.GetPossibleTypes(currentType).Contains(conditionObject);
        }

        if (currentType is IObjectTypeDefinition currentObject)
        {
            return schema.GetPossibleTypes(conditionType).Contains(currentObject);
        }

        var possibleConditionTypes = schema.GetPossibleTypes(conditionType).ToHashSet();

        foreach (var possibleCurrentType in schema.GetPossibleTypes(currentType))
        {
            if (possibleConditionTypes.Contains(possibleCurrentType))
            {
                return true;
            }
        }

        return false;
    }

    protected override ISyntaxVisitorAction Leave(
        PathSegmentNode node,
        FieldSelectionMapValidatorContext context)
    {
        context.OutputTypes.Pop();
        context.LastNullableField = null;

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

            if (field.Type.IsNullableType())
            {
                context.LastNullableField = field;
            }

            context.SelectedFields.Add(field);

            if (node.Arguments.Length > 0)
            {
                ConstantArgumentValidator.Validate(
                    FieldSelectionMapValueNodeConverter.Convert(node.Arguments),
                    field,
                    context.Errors);
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        ObjectFieldSelectionNode node,
        FieldSelectionMapValidatorContext context)
    {
        context.InputTypes.Pop();
        context.LastNullableField = null;

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
    public FieldSelectionMapValidatorContext(
        IType inputType,
        IType outputType,
        IReadOnlyDictionary<string, ImmutableArray<ITypeDefinition>> contextTypes)
    {
        InputTypes.Push(inputType);
        OutputTypes.Push(outputType);
        ContextTypes = contextTypes;
    }

    // Creates the context that validates a context path rooted at a single context type.
    internal FieldSelectionMapValidatorContext(
        FieldSelectionMapValidatorContext parent,
        ITypeDefinition contextType,
        PathNode resolvedContextPath)
    {
        foreach (var inputType in parent.InputTypes.Reverse())
        {
            InputTypes.Push(inputType);
        }

        OutputTypes.Push(contextType);
        ContextTypes = parent.ContextTypes;
        ResolvedContextPath = resolvedContextPath;
    }

    public IReadOnlyDictionary<string, ImmutableArray<ITypeDefinition>> ContextTypes { get; }

    /// <summary>
    /// Gets the context path whose root is already set to a context type, or <c>null</c>.
    /// </summary>
    public PathNode? ResolvedContextPath { get; }

    public Stack<IFieldSelectionMapSyntaxNode> Nodes { get; } = [];

    public Stack<IType> InputTypes { get; } = [];

    public Stack<IType> OutputTypes { get; } = [];

    public Stack<IType> TerminalTypes { get; } = [];

    public IOutputFieldDefinition? LastNullableField { get; set; }

    public HashSet<IOutputFieldDefinition> SelectedFields { get; } = [];

    public List<string> Errors { get; } = [];
}
