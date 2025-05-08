using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Validation.Properties;

namespace HotChocolate.Validation;

internal static class ErrorHelper
{
    public static IError VariableNotUsed(
        this IDocumentValidatorContext context,
        OperationDefinitionNode node)
    {
        return ErrorBuilder.New()
            .SetMessage(
                "The following variables were not used: " +
                $"{string.Join(", ", context.Unused)}.")
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SpecifiedBy("sec-All-Variables-Used")
            .Build();
    }

    public static IError VariableNotDeclared(
        this IDocumentValidatorContext context,
        OperationDefinitionNode node)
    {
        return ErrorBuilder.New()
            .SetMessage(
                "The following variables were not declared: " +
                $"{string.Join(", ", context.Used)}.")
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SpecifiedBy("sec-All-Variable-Uses-Defined")
            .Build();
    }

    public static IError VariableIsNotCompatible(
        this IDocumentValidatorContext context,
        VariableNode variable,
        VariableDefinitionNode variableDefinition)
    {
        var variableName = variableDefinition.Variable.Name.Value;

        return ErrorBuilder.New()
            .SetMessage(
                Resources.ErrorHelper_VariableIsNotCompatible,
                variableName)
            .AddLocation(variable)
            .SetPath(context.CreateErrorPath())
            .SetExtension("variable", variableName)
            .SetExtension("variableType", variableDefinition.Type.ToString())
            .SetExtension("locationType", context.Types.Peek().ToString())
            .SpecifiedBy("sec-All-Variable-Usages-are-Allowed")
            .Build();
    }

    public static IError DirectiveNotValidInLocation(
        this IDocumentValidatorContext context,
        DirectiveNode node)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_DirectiveNotValidInLocation)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SpecifiedBy("sec-Directives-Are-In-Valid-Locations")
            .Build();
    }

    public static IError DirectiveNotSupported(
        this IDocumentValidatorContext context,
        DirectiveNode node)
    {
        return ErrorBuilder.New()
            .SetMessage(
                Resources.ErrorHelper_DirectiveNotSupported,
                node.Name.Value)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SpecifiedBy("sec-Directives-Are-Defined")
            .Build();
    }

    public static IError DirectiveMustBeUniqueInLocation(
        this IDocumentValidatorContext context,
        DirectiveNode node) =>
        ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_DirectiveMustBeUniqueInLocation)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SpecifiedBy("sec-Directives-Are-Unique-Per-Location")
            .Build();

    public static IError TypeSystemDefinitionNotAllowed(
        this IDocumentValidatorContext context,
        IDefinitionNode node)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_TypeSystemDefinitionNotAllowed)
            .AddLocation(node)
            .SpecifiedBy("sec-Executable-Definitions")
            .Build();
    }

    public static IError UnionFieldError(
        this IDocumentValidatorContext context,
        SelectionSetNode node,
        IUnionTypeDefinition type)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_UnionFieldError)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SetExtension("type", type.Name)
            .SpecifiedBy("sec-Field-Selections-on-Objects-Interfaces-and-Unions-Types")
            .Build();
    }

    public static IError FieldDoesNotExist(
        this IDocumentValidatorContext context,
        FieldNode node,
        IComplexTypeDefinition outputType)
    {
        return ErrorBuilder.New()
            .SetMessage(
                Resources.ErrorHelper_FieldDoesNotExist,
                node.Name.Value, outputType.Name)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SetExtension("type", outputType.Name)
            .SetExtension("field", node.Name.Value)
            .SetExtension("responseName", (node.Alias ?? node.Name).Value)
            .SpecifiedBy("sec-Field-Selections-on-Objects-Interfaces-and-Unions-Types")
            .Build();
    }

    public static IError LeafFieldsCannotHaveSelections(
        this IDocumentValidatorContext context,
        FieldNode node,
        IComplexTypeDefinition declaringType,
        IType fieldType)
    {
        return ErrorBuilder.New()
            .SetMessage(
                Resources.ErrorHelper_LeafFieldsCannotHaveSelections,
                node.Name.Value,
                fieldType.ToString()!)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SetExtension("declaringType", declaringType.Name)
            .SetExtension("field", node.Name.Value)
            .SetExtension("type", fieldType.ToString())
            .SetExtension("responseName", (node.Alias ?? node.Name).Value)
            .SpecifiedBy("sec-Leaf-Field-Selections")
            .Build();
    }

    public static IError ArgumentValueIsNotCompatible(
        this IDocumentValidatorContext context,
        ArgumentNode node,
        IInputType locationType,
        IValueNode value)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_ArgumentValueIsNotCompatible)
            .AddLocation(value)
            .SetPath(context.CreateErrorPath())
            .SetExtension("argument", node.Name.Value)
            .SetExtension("argumentValue", value.ToString())
            .SetExtension("locationType", locationType.ToString())
            .SpecifiedBy("sec-Values-of-Correct-Type")
            .Build();
    }

    public static IError FieldValueIsNotCompatible(
        this IDocumentValidatorContext context,
        IInputValueDefinition field,
        IInputType locationType,
        IValueNode valueNode)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_FieldValueIsNotCompatible, field.Name)
            .AddLocation(valueNode)
            .SetExtension("fieldName", field.Name)
            .SetExtension("fieldType", field.Type.ToString())
            .SetExtension("locationType", locationType.ToString())
            .SetPath(context.CreateErrorPath())
            .SpecifiedBy("sec-Values-of-Correct-Type")
            .Build();
    }

    public static IError VariableDefaultValueIsNotCompatible(
        this IDocumentValidatorContext context,
        VariableDefinitionNode node,
        IInputType locationType,
        IValueNode valueNode)
    {
        return ErrorBuilder.New()
            .SetMessage(
                Resources.ErrorHelper_VariableDefaultValueIsNotCompatible,
                node.Variable.Name.Value)
            .AddLocation(valueNode)
            .SetPath(context.CreateErrorPath())
            .SetExtension("variable", node.Variable.Name.Value)
            .SetExtension("variableType", node.Type.ToString())
            .SetExtension("locationType", locationType.ToString())
            .SpecifiedBy("sec-Values-of-Correct-Type")
            .Build();
    }

    public static IError NoSelectionOnCompositeField(
        this IDocumentValidatorContext context,
        FieldNode node,
        IComplexTypeDefinition declaringType,
        IType fieldType)
    {
        return ErrorBuilder.New()
            .SetMessage(
                Resources.ErrorHelper_NoSelectionOnCompositeField,
                node.Name.Value,
                fieldType.ToTypeNode().ToString())
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SetExtension("declaringType", declaringType.Name)
            .SetExtension("field", node.Name.Value)
            .SetExtension("type", fieldType.ToString())
            .SetExtension("responseName", (node.Alias ?? node.Name).Value)
            .SpecifiedBy("sec-Field-Selections-on-Objects-Interfaces-and-Unions-Types")
            .Build();
    }

    public static IError NoSelectionOnRootType(
        this IDocumentValidatorContext context,
        OperationDefinitionNode node,
        IType fieldType)
    {
        return ErrorBuilder.New()
            .SetMessage(
                Resources.ErrorHelper_NoSelectionOnRootType,
                node.Name?.Value ?? "Unnamed")
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SetExtension("operation", node.Name?.Value ?? "Unnamed")
            .SetExtension("type", fieldType.ToString())
            .SpecifiedBy("sec-Field-Selections-on-Objects-Interfaces-and-Unions-Types")
            .Build();
    }

    public static IError FieldIsRequiredButNull(
        this IDocumentValidatorContext context,
        ISyntaxNode node,
        string fieldName)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_FieldIsRequiredButNull, fieldName)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SetExtension("field", fieldName)
            .SpecifiedBy("sec-Input-Object-Required-Fields")
             .Build();
    }

    public static IError FieldsAreNotMergeable(
        this IDocumentValidatorContext context,
        FieldInfo fieldA,
        FieldInfo fieldB)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_FieldsAreNotMergeable)
            .AddLocation(fieldA.SyntaxNode)
            .AddLocation(fieldB.SyntaxNode)
            .SetExtension("declaringTypeA", fieldA.DeclaringType.NamedType().Name)
            .SetExtension("declaringTypeB", fieldB.DeclaringType.NamedType().Name)
            .SetExtension("fieldA", fieldA.SyntaxNode.Name.Value)
            .SetExtension("fieldB", fieldB.SyntaxNode.Name.Value)
            .SetExtension("typeA", fieldA.Type.ToString())
            .SetExtension("typeB", fieldB.Type.ToString())
            .SetExtension("responseNameA", fieldA.ResponseName)
            .SetExtension("responseNameB", fieldB.ResponseName)
            .SpecifiedBy("sec-Field-Selection-Merging")
            .Build();
    }

    public static IError OperationNotSupported(
        this IDocumentValidatorContext context,
        OperationType operationType)
    {
        return ErrorBuilder.New()
            .SetMessage(
                Resources.ErrorHelper_OperationNotSupported,
                operationType)
            .Build();
    }

    public static IError FragmentNameNotUnique(
        this IDocumentValidatorContext context,
        FragmentDefinitionNode fragmentDefinition)
    {
        return ErrorBuilder.New()
            .SetMessage(
                Resources.ErrorHelper_FragmentNameNotUnique,
                fragmentDefinition.Name.Value)
            .AddLocation(fragmentDefinition)
            .SetExtension("fragment", fragmentDefinition.Name.Value)
            .SpecifiedBy("sec-Fragment-Name-Uniqueness")
            .Build();
    }

    public static IError FragmentNotUsed(
        this IDocumentValidatorContext context,
        FragmentDefinitionNode fragmentDefinition)
    {
        return ErrorBuilder.New()
            .SetMessage(
                Resources.ErrorHelper_FragmentNotUsed,
                fragmentDefinition.Name.Value)
            .AddLocation(fragmentDefinition)
            .SetPath(context.CreateErrorPath())
            .SetExtension("fragment", fragmentDefinition.Name.Value)
            .SpecifiedBy("sec-Fragments-Must-Be-Used")
            .Build();
    }

    public static IError FragmentCycleDetected(
        this IDocumentValidatorContext context,
        FragmentSpreadNode fragmentSpread)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_FragmentCycleDetected)
            .AddLocation(fragmentSpread)
            .SetPath(context.CreateErrorPath())
            .SetExtension("fragment", fragmentSpread.Name.Value)
            .SpecifiedBy("sec-Fragment-spreads-must-not-form-cycles")
            .Build();
    }

    public static IError FragmentDoesNotExist(
        this IDocumentValidatorContext context,
        FragmentSpreadNode fragmentSpread)
    {
        return ErrorBuilder.New()
            .SetMessage(
                Resources.ErrorHelper_FragmentDoesNotExist,
                fragmentSpread.Name.Value)
            .AddLocation(fragmentSpread)
            .SetPath(context.CreateErrorPath())
            .SetExtension("fragment", fragmentSpread.Name.Value)
            .SpecifiedBy("sec-Fragment-spread-target-defined")
            .Build();
    }

    public static IError FragmentNotPossible(
        this IDocumentValidatorContext context,
        ISyntaxNode node,
        ITypeDefinition typeCondition,
        ITypeDefinition parentType)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_FragmentNotPossible)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SetExtension("typeCondition", typeCondition.ToString())
            .SetExtension("selectionSetType", parentType.ToString())
            .SetFragmentName(node)
            .SpecifiedBy("sec-Fragment-spread-is-possible")
            .Build();
    }

    public static IError FragmentTypeConditionUnknown(
        this IDocumentValidatorContext context,
        ISyntaxNode node,
        NamedTypeNode typeCondition)
    {
        return ErrorBuilder.New()
            .SetMessage(
                Resources.ErrorHelper_FragmentTypeConditionUnknown,
                typeCondition.Name.Value)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SetExtension("typeCondition", typeCondition.Name.Value)
            .SetFragmentName(node)
            .SpecifiedBy("sec-Fragment-Spread-Type-Existence")
            .Build();
    }

    public static IError FragmentOnlyCompositeType(
        this IDocumentValidatorContext context,
        ISyntaxNode node,
        ITypeDefinition type)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_FragmentOnlyCompositeType)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SetExtension("typeCondition", type.ToString())
            .SetFragmentName(node)
            .SpecifiedBy("sec-Fragments-On-Composite-Types")
            .Build();
    }

    public static IError InputFieldAmbiguous(
        this IDocumentValidatorContext context,
        ObjectFieldNode field)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_InputFieldAmbiguous, field.Name.Value)
            .AddLocation(field)
            .SetPath(context.CreateErrorPath())
            .SetExtension("field", field.Name.Value)
            .SpecifiedBy("sec-Input-Object-Field-Uniqueness")
            .Build();
    }

    public static IError InputFieldDoesNotExist(
        this IDocumentValidatorContext context,
        ObjectFieldNode field)
    {
        return ErrorBuilder.New()
            .SetMessage(
                Resources.ErrorHelper_InputFieldDoesNotExist,
                field.Name.Value)
            .AddLocation(field)
            .SetPath(context.CreateErrorPath())
            .SetExtension("field", field.Name.Value)
            .SpecifiedBy("sec-Input-Object-Field-Names")
            .Build();
    }

    public static IError InputFieldRequired(
        this IDocumentValidatorContext context,
        ISyntaxNode node,
        string fieldName)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_InputFieldRequired, fieldName)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SetExtension("field", fieldName)
            .SpecifiedBy("sec-Input-Object-Required-Fields")
            .Build();
    }

    public static IError OperationNameNotUnique(
        this IDocumentValidatorContext context,
        OperationDefinitionNode operation,
        string operationName)
    {
        return ErrorBuilder.New()
            .SetMessage(
                Resources.ErrorHelper_OperationNameNotUnique,
                operationName)
            .AddLocation(operation)
            .SetExtension("operation", operationName)
            .SpecifiedBy("sec-Operation-Name-Uniqueness")
            .Build();
    }

    public static IError OperationAnonymousMoreThanOne(
        this IDocumentValidatorContext context,
        OperationDefinitionNode operation,
        int operations)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_OperationAnonymousMoreThanOne)
            .AddLocation(operation)
            .SetExtension("operations", operations)
            .SpecifiedBy("sec-Lone-Anonymous-Operation")
            .Build();
    }

    public static IError VariableNotInputType(
        this IDocumentValidatorContext context,
        VariableDefinitionNode node,
        string variableName)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_VariableNotInputType, variableName)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SetExtension("variable", variableName)
            .SetExtension("variableType", node.Type.ToString())
            .SpecifiedBy("sec-Variables-Are-Input-Types")
            .Build();
    }

    public static IError VariableNameNotUnique(
        this IDocumentValidatorContext context,
        VariableDefinitionNode node,
        string variableName)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_VariableNameNotUnique)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SetExtension("variable", variableName)
            .SetExtension("variableType", node.Type.ToString())
            .SpecifiedBy("sec-Variable-Uniqueness")
            .Build();
    }

    public static IError ArgumentNotUnique(
        this IDocumentValidatorContext context,
        ArgumentNode node,
        IOutputFieldDefinition? field = null,
        IDirectiveDefinition? directive = null)
    {
        var builder = ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_ArgumentNotUnique)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath());

        if (field is { })
        {
            builder
                .SetExtension("type", field.DeclaringType.Name)
                .SetExtension("field", field.Name);
        }

        if (directive is { })
        {
            builder.SetExtension("directive", directive.Name);
        }

        return builder
            .SetExtension("argument", node.Name.Value)
            .SpecifiedBy("sec-Argument-Uniqueness")
            .Build();
    }

    public static IError ArgumentRequired(
        this IDocumentValidatorContext context,
        ISyntaxNode node,
        string argumentName,
        IOutputFieldDefinition? field = null,
        IDirectiveDefinition? directive = null)
    {
        var builder = ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_ArgumentRequired, argumentName)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath());

        if (field is { })
        {
            builder
                .SetExtension("type", field.DeclaringType.Name)
                .SetExtension("field", field.Name);
        }

        if (directive is { })
        {
            builder.SetExtension("directive", directive.Name);
        }

        return builder
            .SetExtension("argument", argumentName)
            .SpecifiedBy("sec-Required-Arguments")
            .Build();
    }

    public static IError ArgumentDoesNotExist(
        this IDocumentValidatorContext context,
        ArgumentNode node,
        IOutputFieldDefinition? field = null,
        IDirectiveDefinition? directive = null)
    {
        var builder = ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_ArgumentDoesNotExist, node.Name.Value)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath());

        if (field is { })
        {
            builder
                .SetExtension("type", field.DeclaringType.Name)
                .SetExtension("field", field.Name);
        }

        if (directive is { })
        {
            builder.SetExtension("directive", directive.Name);
        }

        return builder
            .SetExtension("argument", node.Name.Value)
            .SpecifiedBy("sec-Required-Arguments")
            .Build();
    }

    public static IError SubscriptionSingleRootField(
        this IDocumentValidatorContext context,
        OperationDefinitionNode operation)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_SubscriptionSingleRootField)
            .AddLocation(operation)
            .SpecifiedBy("sec-Single-root-field")
            .Build();
    }

    public static IError SubscriptionNoTopLevelIntrospectionField(
        this IDocumentValidatorContext context,
        OperationDefinitionNode operation)
    {
        return ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_SubscriptionNoTopLevelIntrospectionField)
            .AddLocation(operation)
            .SpecifiedBy("sec-Single-root-field")
            .Build();
    }

    public static IError MaxExecutionDepth(
        this IDocumentValidatorContext context,
        OperationDefinitionNode operation,
        int allowedExecutionDepth,
        int detectedExecutionDepth)
    {
        return ErrorBuilder.New()
            .SetMessage(
                Resources.ErrorHelper_MaxExecutionDepth,
                detectedExecutionDepth, allowedExecutionDepth)
            .AddLocation(operation)
            .SetExtension("allowedExecutionDepth", allowedExecutionDepth)
            .SetExtension("detectedExecutionDepth", detectedExecutionDepth)
            .Build();
    }

    public static IError IntrospectionNotAllowed(
        this IDocumentValidatorContext context,
        FieldNode field)
    {
        var message = Resources.ErrorHelper_IntrospectionNotAllowed;

        if (context.ContextData.TryGetValue(ExecutionContextData.IntrospectionMessage, out var value))
        {
            if (value is Func<string> messageFactory)
            {
                message = messageFactory();
            }

            if (value is string messageString)
            {
                message = messageString;
            }
        }

        return ErrorBuilder.New()
            .SetMessage(message)
            .AddLocation(field)
            .SetExtension(nameof(field), field.Name)
            .SetCode(ErrorCodes.Validation.IntrospectionNotAllowed)
            .Build();
    }

    public static IError OneOfMustHaveExactlyOneField(
        this IDocumentValidatorContext context,
        ISyntaxNode node,
        IInputValueDefinition type)
        => ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_OneOfMustHaveExactlyOneField, type.Name)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SetExtension(nameof(type), type.Name)
            .SpecifiedBy("sec-OneOf-Input-Objects-Have-Exactly-One-Field", rfc: 825)
            .Build();

    public static IError OneOfVariablesMustBeNonNull(
        this IDocumentValidatorContext context,
        ISyntaxNode node,
        SchemaCoordinate fieldCoordinate,
        string variableName)
        => ErrorBuilder.New()
            .SetMessage(
                Resources.ErrorHelper_OneOfVariablesMustBeNonNull,
                variableName,
                fieldCoordinate.MemberName!,
                fieldCoordinate.Name)
            .AddLocation(node)
            .SetPath(context.CreateErrorPath())
            .SetFieldCoordinate(fieldCoordinate)
            .SpecifiedBy("sec-Oneof–Input-Objects-Have-Exactly-One-Field", rfc: 825)
            .Build();

    public static IError DeferAndStreamNotAllowedOnMutationOrSubscriptionRoot(
        ISelectionNode selection)
        => ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_DeferAndStreamNotAllowedOnMutationOrSubscriptionRoot)
            .AddLocation(selection)
            .SpecifiedBy("sec-Defer-And-Stream-Directives-Are-Used-On-Valid-Root-Field")
            .Build();

    public static IError DeferAndStreamDuplicateLabel(
        this IDocumentValidatorContext context,
        ISyntaxNode selection,
        string label)
        => ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_DeferAndStreamDuplicateLabel)
            .AddLocation(selection)
            .SpecifiedBy("sec-Defer-And-Stream-Directive-Labels-Are-Unique")
            .SetExtension(nameof(label), label)
            .SetPath(context.CreateErrorPath())
            .Build();

    public static IError DeferAndStreamLabelIsVariable(
        this IDocumentValidatorContext context,
        ISyntaxNode selection,
        string variable)
        => ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_DeferAndStreamLabelIsVariable)
            .AddLocation(selection)
            .SpecifiedBy("sec-Defer-And-Stream-Directive-Labels-Are-Unique")
            .SetExtension(nameof(variable),$"${variable}")
            .SetPath(context.CreateErrorPath())
            .Build();

    public static IError StreamOnNonListField(
        this IDocumentValidatorContext context,
        ISyntaxNode selection)
        => ErrorBuilder.New()
            .SetMessage("@stream directive is only valid on list fields.")
            .AddLocation(selection)
            .SpecifiedBy("sec-Stream-Directives-Are-Used-On-List-Fields")
            .SetPath(context.CreateErrorPath())
            .Build();

    public static void ReportMaxIntrospectionDepthOverflow(
        this IDocumentValidatorContext context,
        ISyntaxNode selection)
    {
        context.FatalErrorDetected = true;
        context.ReportError(
            ErrorBuilder.New()
                .SetMessage("Maximum allowed introspection depth exceeded.")
                .SetCode(ErrorCodes.Validation.MaxIntrospectionDepthOverflow)
                .AddLocation(selection)
                .SetPath(context.CreateErrorPath())
                .Build());
    }

    public static void ReportMaxCoordinateCycleDepthOverflow(
        this IDocumentValidatorContext context,
        ISyntaxNode selection)
    {
        context.FatalErrorDetected = true;

        context.ReportError(
            ErrorBuilder.New()
                .SetMessage("Maximum allowed coordinate cycle depth was exceeded.")
                .SetCode(ErrorCodes.Validation.MaxCoordinateCycleDepthOverflow)
                .AddLocation(selection)
                .SetPath(context.CreateErrorPath())
                .Build());
    }
}
