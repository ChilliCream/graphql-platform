using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Validation.Properties;

namespace HotChocolate.Validation;

internal static class ErrorHelper
{
    public static IError SkipAndIncludeNotAllowedOnSubscriptionRootField(
        ISelectionNode selection)
        => ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_SkipAndIncludeNotAllowedOnSubscriptionRootField)
            .AddLocation(selection)
            .SpecifiedBy("sec-Single-Root-Field")
            .Build();

    public static IError DeferAndStreamNotAllowedOnMutationOrSubscriptionRoot(
        ISelectionNode selection)
        => ErrorBuilder.New()
            .SetMessage(Resources.ErrorHelper_DeferAndStreamNotAllowedOnMutationOrSubscriptionRoot)
            .AddLocation(selection)
            .SpecifiedBy("sec-Defer-And-Stream-Directives-Are-Used-On-Valid-Root-Field", rfc: 1110)
            .Build();

    extension(DocumentValidatorContext context)
    {
        public IError VariableNotUsed(
            OperationDefinitionNode node,
            IEnumerable<string> unusedVariables)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    "The following variables were not used: "
                    + $"{string.Join(", ", unusedVariables)}.")
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SpecifiedBy("sec-All-Variables-Used")
                .Build();
        }

        public IError VariableNotDeclared(
            OperationDefinitionNode node,
            IEnumerable<string> usedVariables)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    "The following variables were not declared: "
                    + $"{string.Join(", ", usedVariables)}.")
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SpecifiedBy("sec-All-Variable-Uses-Defined")
                .Build();
        }

        public IError OneOfVariableIsNotCompatible(
            VariableNode variable,
            VariableDefinitionNode variableDefinition)
        {
            var variableName = variableDefinition.Variable.Name.Value;

            return ErrorBuilder.New()
                .SetMessage(
                    Resources.ErrorHelper_OneOfVariableIsNotCompatible,
                    variableName)
                .AddLocation(variable)
                .SetPath(context.CreateErrorPath())
                .SetExtension("variable", variableName)
                .SpecifiedBy("sec-All-Variable-Usages-Are-Allowed")
                .Build();
        }

        public IError VariableIsNotCompatible(
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
                .SetExtension("locationType", context.Types.Peek().FullTypeName())
                .SpecifiedBy("sec-All-Variable-Usages-Are-Allowed")
                .Build();
        }

        public IError DirectiveNotValidInLocation(DirectiveNode node)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_DirectiveNotValidInLocation)
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SpecifiedBy("sec-Directives-Are-in-Valid-Locations")
                .Build();
        }

        public IError DirectiveNotSupported(DirectiveNode node)
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

        public IError DirectiveMustBeUniqueInLocation(DirectiveNode node) =>
            ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_DirectiveMustBeUniqueInLocation)
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SpecifiedBy("sec-Directives-Are-Unique-per-Location")
                .Build();

        public IError TypeSystemDefinitionNotAllowed(IDefinitionNode node)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_TypeSystemDefinitionNotAllowed)
                .AddLocation(node)
                .SpecifiedBy("sec-Executable-Definitions")
                .Build();
        }

        public IError UnionFieldError(SelectionSetNode node, IUnionTypeDefinition type)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_UnionFieldError)
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SetExtension("type", type.Name)
                .SpecifiedBy("sec-Field-Selections")
                .Build();
        }

        public IError FieldDoesNotExist(FieldNode node, IComplexTypeDefinition outputType)
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
                .SpecifiedBy("sec-Field-Selections")
                .Build();
        }

        public IError LeafFieldsCannotHaveSelections(
            FieldNode node,
            IComplexTypeDefinition declaringType,
            IType fieldType)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    Resources.ErrorHelper_LeafFieldsCannotHaveSelections,
                    node.Name.Value,
                    fieldType.FullTypeName())
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SetExtension("declaringType", declaringType.Name)
                .SetExtension("field", node.Name.Value)
                .SetExtension("type", fieldType.FullTypeName())
                .SetExtension("responseName", (node.Alias ?? node.Name).Value)
                .SpecifiedBy("sec-Leaf-Field-Selections")
                .Build();
        }

        public IError ArgumentValueIsNotCompatible(
            ArgumentNode node,
            IInputType locationType,
            IValueNode value)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_ArgumentValueIsNotCompatible)
                .AddLocation(value)
                .SetPath(context.CreateErrorPath())
                .SetExtension("argument", node.Name.Value)
                .SetExtension("argumentValue", value.ToString(indented: false))
                .SetExtension("locationType", locationType.FullTypeName())
                .SpecifiedBy("sec-Values-of-Correct-Type")
                .Build();
        }

        public IError FieldValueIsNotCompatible(
            IInputValueDefinition field,
            IInputType locationType,
            IValueNode valueNode)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_FieldValueIsNotCompatible, field.Name)
                .AddLocation(valueNode)
                .SetExtension("fieldName", field.Name)
                .SetExtension("fieldType", field.Type.FullTypeName())
                .SetExtension("locationType", locationType.FullTypeName())
                .SetPath(context.CreateErrorPath())
                .SpecifiedBy("sec-Values-of-Correct-Type")
                .Build();
        }

        public IError VariableDefaultValueIsNotCompatible(
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
                .SetExtension("locationType", locationType.FullTypeName())
                .SpecifiedBy("sec-Values-of-Correct-Type")
                .Build();
        }

        public IError NoSelectionOnCompositeField(
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
                .SetExtension("type", fieldType.FullTypeName())
                .SetExtension("responseName", (node.Alias ?? node.Name).Value)
                .SpecifiedBy("sec-Field-Selections")
                .Build();
        }

        public IError NoSelectionOnRootType(OperationDefinitionNode node, IType fieldType)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    Resources.ErrorHelper_NoSelectionOnRootType,
                    node.Name?.Value ?? "Unnamed")
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SetExtension("operation", node.Name?.Value ?? "Unnamed")
                .SetExtension("type", fieldType.FullTypeName())
                .SpecifiedBy("sec-Field-Selections")
                .Build();
        }

        public IError FieldIsRequiredButNull(ISyntaxNode node, string fieldName)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_FieldIsRequiredButNull, fieldName)
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SetExtension("field", fieldName)
                .SpecifiedBy("sec-Input-Object-Required-Fields")
                .Build();
        }

        public IError FieldsAreNotMergeable(FieldInfo fieldA, FieldInfo fieldB)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_FieldsAreNotMergeable)
                .AddLocation(fieldA.SyntaxNode)
                .AddLocation(fieldB.SyntaxNode)
                .SetExtension("declaringTypeA", fieldA.DeclaringType.NamedType().Name)
                .SetExtension("declaringTypeB", fieldB.DeclaringType.NamedType().Name)
                .SetExtension("fieldA", fieldA.SyntaxNode.Name.Value)
                .SetExtension("fieldB", fieldB.SyntaxNode.Name.Value)
                .SetExtension("typeA", fieldA.Type.FullTypeName())
                .SetExtension("typeB", fieldB.Type.FullTypeName())
                .SetExtension("responseNameA", fieldA.ResponseName)
                .SetExtension("responseNameB", fieldB.ResponseName)
                .SpecifiedBy("sec-Field-Selection-Merging")
                .Build();
        }

        public IError OperationNotSupported(OperationType operationType)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    Resources.ErrorHelper_OperationNotSupported,
                    operationType)
                .Build();
        }

        public IError FragmentNameNotUnique(FragmentDefinitionNode fragmentDefinition)
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

        public IError FragmentNotUsed(FragmentDefinitionNode fragmentDefinition)
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

        public IError FragmentCycleDetected(FragmentSpreadNode fragmentSpread)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_FragmentCycleDetected)
                .AddLocation(fragmentSpread)
                .SetPath(context.CreateErrorPath())
                .SetExtension("fragment", fragmentSpread.Name.Value)
                .SpecifiedBy("sec-Fragment-Spreads-Must-Not-Form-Cycles")
                .Build();
        }

        public IError FragmentDoesNotExist(FragmentSpreadNode fragmentSpread)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    Resources.ErrorHelper_FragmentDoesNotExist,
                    fragmentSpread.Name.Value)
                .AddLocation(fragmentSpread)
                .SetPath(context.CreateErrorPath())
                .SetExtension("fragment", fragmentSpread.Name.Value)
                .SpecifiedBy("sec-Fragment-Spread-Target-Defined")
                .Build();
        }

        public IError FragmentNotPossible(
            ISyntaxNode node,
            ITypeDefinition typeCondition,
            ITypeDefinition parentType)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_FragmentNotPossible)
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SetExtension("typeCondition", typeCondition.Name)
                .SetExtension("selectionSetType", parentType.Name)
                .SetFragmentName(node)
                .SpecifiedBy("sec-Fragment-Spread-Is-Possible")
                .Build();
        }

        public IError FragmentTypeConditionUnknown(ISyntaxNode node, NamedTypeNode typeCondition)
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

        public IError FragmentOnlyCompositeType(ISyntaxNode node, ITypeDefinition type)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_FragmentOnlyCompositeType)
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SetExtension("typeCondition", type.FullTypeName())
                .SetFragmentName(node)
                .SpecifiedBy("sec-Fragments-on-Object-Interface-or-Union-Types")
                .Build();
        }

        public IError InputFieldAmbiguous(ObjectFieldNode field)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_InputFieldAmbiguous, field.Name.Value)
                .AddLocation(field)
                .SetPath(context.CreateErrorPath())
                .SetExtension("field", field.Name.Value)
                .SpecifiedBy("sec-Input-Object-Field-Uniqueness")
                .Build();
        }

        public IError InputFieldDoesNotExist(ObjectFieldNode field)
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

        public IError InputFieldRequired(ISyntaxNode node, string fieldName)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_InputFieldRequired, fieldName)
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SetExtension("field", fieldName)
                .SpecifiedBy("sec-Input-Object-Required-Fields")
                .Build();
        }

        public IError OperationNameNotUnique(OperationDefinitionNode operation, string operationName)
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

        public IError OperationAnonymousMoreThanOne(OperationDefinitionNode operation, int operations)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_OperationAnonymousMoreThanOne)
                .AddLocation(operation)
                .SetExtension("operations", operations)
                .SpecifiedBy("sec-Lone-Anonymous-Operation")
                .Build();
        }

        public IError VariableNotInputType(VariableDefinitionNode node, string variableName)
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

        public IError VariableNameNotUnique(VariableDefinitionNode node, string variableName)
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

        public IError ArgumentNotUnique(
            ArgumentNode node,
            SchemaCoordinate? field = null,
            IDirectiveDefinition? directive = null)
        {
            var builder = ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_ArgumentNotUnique)
                .AddLocation(node)
                .SetPath(context.CreateErrorPath());

            if (field.HasValue)
            {
                builder
                    .SetExtension("type", field.Value.Name)
                    .SetExtension("field", field.Value.MemberName);
            }

            if (directive is not null)
            {
                builder.SetExtension("directive", directive.Name);
            }

            return builder
                .SetExtension("argument", node.Name.Value)
                .SpecifiedBy("sec-Argument-Uniqueness")
                .Build();
        }

        public IError ArgumentRequired(
            ISyntaxNode node,
            string argumentName,
            SchemaCoordinate? field = null,
            IDirectiveDefinition? directive = null)
        {
            var builder = ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_ArgumentRequired, argumentName)
                .AddLocation(node)
                .SetPath(context.CreateErrorPath());

            if (field.HasValue)
            {
                builder
                    .SetExtension("type", field.Value.Name)
                    .SetExtension("field", field.Value.MemberName);
            }

            if (directive is not null)
            {
                builder.SetExtension("directive", directive.Name);
            }

            return builder
                .SetExtension("argument", argumentName)
                .SpecifiedBy("sec-Required-Arguments")
                .Build();
        }

        public IError ArgumentDoesNotExist(
            ArgumentNode node,
            SchemaCoordinate? field = null,
            IDirectiveDefinition? directive = null)
        {
            var builder = ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_ArgumentDoesNotExist, node.Name.Value)
                .AddLocation(node)
                .SetPath(context.CreateErrorPath());

            if (field.HasValue)
            {
                builder
                    .SetExtension("type", field.Value.Name)
                    .SetExtension("field", field.Value.MemberName);
            }

            if (directive is not null)
            {
                builder.SetExtension("directive", directive.Name);
            }

            return builder
                .SetExtension("argument", node.Name.Value)
                .SpecifiedBy("sec-Required-Arguments")
                .Build();
        }

        public IError SubscriptionSingleRootField(OperationDefinitionNode operation)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_SubscriptionSingleRootField)
                .AddLocation(operation)
                .SpecifiedBy("sec-Single-Root-Field")
                .Build();
        }

        public IError SubscriptionNoTopLevelIntrospectionField(OperationDefinitionNode operation)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_SubscriptionNoTopLevelIntrospectionField)
                .AddLocation(operation)
                .SpecifiedBy("sec-Single-Root-Field")
                .Build();
        }

        public IError MaxExecutionDepth(
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

        public IError IntrospectionNotAllowed(FieldNode field, string? customErrorMessage)
        {
            var message = customErrorMessage ?? Resources.ErrorHelper_IntrospectionNotAllowed;

            return ErrorBuilder.New()
                .SetMessage(message)
                .AddLocation(field)
                .SetExtension(nameof(field), field.Name)
                .SetCode(ErrorCodes.Validation.IntrospectionNotAllowed)
                .Build();
        }

        public IError OneOfMustHaveExactlyOneField(ISyntaxNode node, IInputObjectTypeDefinition type)
            => ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_OneOfMustHaveExactlyOneField, type.Name)
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SetExtension(nameof(type), type.Name)
                .SpecifiedBy("sec-All-Variable-Usages-Are-Allowed")
                .Build();

        public IError OneOfVariablesMustBeNonNull(
            ISyntaxNode node,
            SchemaCoordinate fieldCoordinate,
            string variableName)
            => ErrorBuilder.New()
                .SetMessage(
                    Resources.ErrorHelper_OneOfVariablesMustBeNonNull,
                    variableName,
                    fieldCoordinate.MemberName,
                    fieldCoordinate.Name)
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SetCoordinate(fieldCoordinate)
                .SpecifiedBy("sec-All-Variable-Usages-Are-Allowed")
                .Build();

        public IError DeferAndStreamDuplicateLabel(ISyntaxNode selection, string label)
            => ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_DeferAndStreamDuplicateLabel)
                .AddLocation(selection)
                .SpecifiedBy("sec-Defer-And-Stream-Directive-Labels-Are-Unique", rfc: 1110)
                .SetExtension(nameof(label), label)
                .SetPath(context.CreateErrorPath())
                .Build();

        public IError DeferAndStreamLabelIsVariable(ISyntaxNode selection, string variable)
            => ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_DeferAndStreamLabelIsVariable)
                .AddLocation(selection)
                .SpecifiedBy("sec-Defer-And-Stream-Directive-Labels-Are-Unique", rfc: 1110)
                .SetExtension(nameof(variable), $"${variable}")
                .SetPath(context.CreateErrorPath())
                .Build();

        public IError StreamOnNonListField(ISyntaxNode selection)
            => ErrorBuilder.New()
                .SetMessage("@stream directive is only valid on list fields.")
                .AddLocation(selection)
                .SpecifiedBy("sec-Stream-Directives-Are-Used-On-List-Fields", rfc: 1110)
                .SetPath(context.CreateErrorPath())
                .Build();

        public void ReportMaxIntrospectionDepthOverflow(ISyntaxNode selection)
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

        public void ReportMaxCoordinateCycleDepthOverflow(ISyntaxNode selection)
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
}
