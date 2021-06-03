using System;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Validation.Properties;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Validation
{
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
                .SetExtension("locationType", context.Types.Peek().Visualize())
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
            UnionType type)
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
            IComplexOutputType outputType)
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
            IComplexOutputType declaringType,
            IType fieldType)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    Resources.ErrorHelper_LeafFieldsCannotHaveSelections,
                    node.Name.Value, fieldType.IsScalarType() ? "a scalar" : "an enum")
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SetExtension("declaringType", declaringType.Name)
                .SetExtension("field", node.Name.Value)
                .SetExtension("type", fieldType.Print())
                .SetExtension("responseName", (node.Alias ?? node.Name).Value)
                .SpecifiedBy("sec-Leaf-Field-Selections")
                .Build();
        }

        public static IError ArgumentValueIsNotCompatible(
            this IDocumentValidatorContext context,
            ArgumentNode node,
            IInputType locationType,
            IValueNode valueNode)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_ArgumentValueIsNotCompatible)
                .AddLocation(valueNode)
                .SetPath(context.CreateErrorPath())
                .SetExtension("argument", node.Name.Value)
                .SetExtension("argumentValue", valueNode.ToString())
                .SetExtension("locationType", locationType.Print())
                .SpecifiedBy("sec-Values-of-Correct-Type")
                .Build();
        }

        public static IError FieldValueIsNotCompatible(
            this IDocumentValidatorContext context,
            IInputField field,
            IInputType locationType,
            IValueNode valueNode)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    Resources.ErrorHelper_FieldValueIsNotCompatible,
                    field.Name.Value)
                .AddLocation(valueNode)
                .SetExtension("fieldName", field.Name.Value)
                .SetExtension("fieldType", field.Type.Print())
                .SetExtension("locationType", locationType.Print())
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
                .SetExtension("locationType", locationType.Print())
                .SpecifiedBy("sec-Values-of-Correct-Type")
                .Build();
        }

        public static IError NoSelectionOnCompositeField(
            this IDocumentValidatorContext context,
            FieldNode node,
            IComplexOutputType declaringType,
            IType fieldType)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    Resources.ErrorHelper_NoSelectionOnCompositeField,
                    node.Name.Value)
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SetExtension("declaringType", declaringType.Name)
                .SetExtension("field", node.Name.Value)
                .SetExtension("type", fieldType.Print())
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
                .SetExtension("type", fieldType.Print())
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

        public static IError FieldsAreNotMergable(
            this IDocumentValidatorContext context,
            FieldInfo fieldA,
            FieldInfo fieldB)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_FieldsAreNotMergable)
                .AddLocation(fieldA.Field)
                .AddLocation(fieldB.Field)
                .SetExtension("declaringTypeA", fieldA.DeclaringType.NamedType().Name)
                .SetExtension("declaringTypeB", fieldB.DeclaringType.NamedType().Name)
                .SetExtension("fieldA", fieldA.Field.Name.Value)
                .SetExtension("fieldB", fieldB.Field.Name.Value)
                .SetExtension("typeA", fieldA.Type.Print())
                .SetExtension("typeB", fieldB.Type.Print())
                .SetExtension("responseNameA", fieldA.ResponseName)
                .SetExtension("responseNameB", fieldB.ResponseName)
                .SpecifiedBy("sec-Field-Selection-Merging")
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
            INamedType typeCondition,
            INamedType parentType)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_FragmentNotPossible)
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SetExtension("typeCondition", typeCondition.Visualize())
                .SetExtension("selectionSetType", parentType.Visualize())
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
            INamedType type)
        {
            return ErrorBuilder.New()
                .SetMessage(Resources.ErrorHelper_FragmentOnlyCompositeType)
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SetExtension("typeCondition", type.Visualize())
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
            IOutputField? field = null,
            DirectiveType? directive = null)
        {
            IErrorBuilder builder = ErrorBuilder.New()
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
            IOutputField? field = null,
            DirectiveType? directive = null)
        {
            IErrorBuilder builder = ErrorBuilder.New()
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
            IOutputField? field = null,
            DirectiveType? directive = null)
        {
            IErrorBuilder builder = ErrorBuilder.New()
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

        public static IError MaxOperationComplexity(
            this IDocumentValidatorContext context,
            OperationDefinitionNode operation,
            int allowedComplexity,
            int detectedComplexity)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    Resources.ErrorHelper_MaxOperationComplexity,
                    detectedComplexity, allowedComplexity)
                .AddLocation(operation)
                .SetExtension("allowedComplexity", allowedComplexity)
                .SetExtension("detectedComplexity", detectedComplexity)
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
            string message = Resources.ErrorHelper_IntrospectionNotAllowed;

            if (context.ContextData.TryGetValue(IntrospectionMessage, out object? value))
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
    }
}
