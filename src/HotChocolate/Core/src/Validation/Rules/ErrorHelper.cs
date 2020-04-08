using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation.Rules
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
            string variableName = variableDefinition.Variable.Name.Value;

            return ErrorBuilder.New()
                .SetMessage(
                    $"The variable `{variableName}` is not compatible " +
                    "with the type of the current location.")
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
                .SetMessage("The specified directive is not valid the current location.")
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
                    $"The specified directive `{node.Name.Value}` " +
                    "is not supported by the current schema.")
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SpecifiedBy("sec-Directives-Are-Defined")
                .Build();
        }

        public static IError TypeSystemDefinitionNotAllowed(
            this IDocumentValidatorContext context,
            IDefinitionNode node)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    "A document containing TypeSystemDefinition " +
                    "is invalid for execution.")
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
                .SetMessage(
                    "A union type cannot declare a field directly. " +
                    "Use inline fragments or fragments instead")
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
                    "The field `{0}` does not exist on the type `{1}`.",
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
                    "`{0}` returns {1} value. Selections on scalars " +
                    "or enums are never allowed, because they are the leaf " +
                    "nodes of any GraphQL query.",
                    node.Name.Value, fieldType.IsScalarType() ? "a scalar" : "en enum")
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SetExtension("declaringType", declaringType.Name)
                .SetExtension("field", node.Name.Value)
                .SetExtension("type", fieldType.Print())
                .SetExtension("responseName", (node.Alias ?? node.Name).Value)
                .SpecifiedBy("sec-Leaf-Field-Selections")
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
                    "`{0}` is an object, interface or union type " +
                    "field. Leaf selections on objects, interfaces, and " +
                    "unions without subfields are disallowed.",
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

        public static IError FieldsAreNotMergable(
            this IDocumentValidatorContext context,
            FieldInfo fieldA,
            FieldInfo fieldB)
        {
            return ErrorBuilder.New()
                .SetMessage("Encountered fields for the same object that cannot be merged.")
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
                    "There are multiple fragments with the name " +
                    $"`{fragmentDefinition.Name.Value}`.")
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
                    $"The specified fragment `{fragmentDefinition.Name.Value}` " +
                    "is not used within the current document.")
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
                .SetMessage(
                    "The graph of fragment spreads must not form any " +
                    "cycles including spreading itself. Otherwise an " +
                    "operation could infinitely spread or infinitely " +
                    "execute on cycles in the underlying data.")
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
                    "The specified fragment `{0}` does not exist.",
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
                .SetMessage("The parent type does not match the type condition on the fragment.")
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
                .SetMessage("Unknown type `{0}`.", typeCondition.Name.Value)
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SetExtension("typeCondition", typeCondition.Name.Value)
                .SetFragmentName(node)
                .SpecifiedBy("sec-Fragment-Spread-Type-Existence")
                .Build();
        }

        public static IError FragmentOnlyCompositType(
            this IDocumentValidatorContext context,
            ISyntaxNode node,
            INamedType type)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    "Fragments can only be declared on unions, interfaces, " +
                    "and objects.")
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
                .SetMessage("Field `{0}` is ambiguous.", field.Name.Value)
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
                    "The specified input object field " +
                    $"`{field.Name.Value}` does not exist.")
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
                .SetMessage("`{0}` is a required field and cannot be null.", fieldName)
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
                    "The operation name `{0}` is not unique.",
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
                .SetMessage(
                    "GraphQL allows a short‐hand form for defining query " +
                    "operations when only that one operation exists in the " +
                    "document.")
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
                .SetMessage("The type of variable `{0}` is not an input type.", variableName)
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
                .SetMessage(
                    "A document containing operations that " +
                    "define more than one variable with the same " +
                    "name is invalid for execution.")
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
                .SetMessage(
                    "More than one argument with the same name in an argument " +
                    "set is ambiguous and invalid.")
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
                .SetMessage("The argument `{0}` is required.", argumentName)
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
                .SetMessage("The argument `{0}` does not exist.", node.Name.Value)
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
                .SetMessage("Subscription operations must have exactly one root field.")
                .AddLocation(operation)
                .SpecifiedBy("sec-Single-root-field")
                .Build();
        }
    }
}
