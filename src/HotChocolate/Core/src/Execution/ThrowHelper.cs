using System;
using System.Collections.Generic;
using HotChocolate.Language;
using static HotChocolate.Execution.Properties.Resources;

namespace HotChocolate.Execution
{
    internal static class ThrowHelper
    {
        public static GraphQLException VariableIsNotAnInputType(
            VariableDefinitionNode variableDefinition)
        {
            return new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(
                        ThrowHelper_VariableIsNotAnInputType_Message,
                        variableDefinition.Variable.Name.Value)
                    .SetCode(ErrorCodes.Execution.MustBeInputType)
                    .SetExtension("variable", variableDefinition.Variable.Name.Value)
                    .SetExtension("type", variableDefinition.Type.ToString()!)
                    .AddLocation(variableDefinition)
                    .Build());
        }

        public static GraphQLException NonNullVariableIsNull(
            VariableDefinitionNode variableDefinition)
        {
            return new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(
                        ThrowHelper_NonNullVariableIsNull_Message,
                        variableDefinition.Variable.Name.Value)
                    .SetCode(ErrorCodes.Execution.NonNullViolation)
                    .SetExtension("variable", variableDefinition.Variable.Name.Value)
                    .AddLocation(variableDefinition)
                    .Build());
        }

        public static GraphQLException VariableValueInvalidType(
            VariableDefinitionNode variableDefinition,
            Exception? exception = null)
        {
            IErrorBuilder errorBuilder = ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_VariableValueInvalidType_Message,
                    variableDefinition.Variable.Name.Value)
                .SetCode(ErrorCodes.Execution.InvalidType)
                .SetExtension("variable", variableDefinition.Variable.Name.Value)
                .AddLocation(variableDefinition);

            if (exception is not null)
            {
                errorBuilder.SetException(exception);
            }

            return new GraphQLException(errorBuilder.Build());
        }

        public static GraphQLException MissingIfArgument(
            DirectiveNode directive)
        {
            return new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(
                        ThrowHelper_MissingDirectiveIfArgument,
                        directive.Name.Value)
                    .AddLocation(directive)
                    .Build());
        }

        public static GraphQLException FieldDoesNotExistOnType(
            FieldNode selection, string typeName)
        {
            return new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_FieldDoesNotExistOnType,
                    selection.Name.Value,
                    typeName)
                .AddLocation(selection)
                .Build());
        }

        public static NotSupportedException QueryTypeNotSupported() =>
            new NotSupportedException(ThrowHelper_QueryTypeNotSupported_Message);

        public static GraphQLException VariableNotFound(
            NameString variableName) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_VariableNotFound_Message,
                    variableName)
                .Build());

        public static GraphQLException VariableNotFound(
            VariableNode variable) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_VariableNotFound_Message,
                    variable.Name.Value)
                .AddLocation(variable)
                .Build());

        public static GraphQLException VariableNotOfType(
            NameString variableName,
            Type type) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_VariableNotOfType_Message,
                    variableName,
                    type.FullName ?? string.Empty)
                .Build());

        public static GraphQLException RootTypeNotSupported(
            OperationType operationType) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(ThrowHelper_RootTypeNotSupported_Message, operationType)
                .Build());

        public static GraphQLException SubscriptionExecutor_ContextInvalidState() =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(ThrowHelper_SubscriptionExecutor_ContextInvalidState_Message)
                .Build());

        public static GraphQLException SubscriptionExecutor_SubscriptionsMustHaveOneField() =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(ThrowHelper_SubscriptionExecutor_SubscriptionsMustHaveOneField_Message)
                .Build());

        public static GraphQLException SubscriptionExecutor_NoSubscribeResolver() =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(ThrowHelper_SubscriptionExecutor_NoSubscribeResolver_Message)
                .Build());

        public static GraphQLException ResolverContext_LiteralsNotSupported(
            FieldNode field, Path path, NameString argumentName, Type requestedType) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(ThrowHelper_ResolverContext_LiteralsNotSupported_Message)
                .SetPath(path)
                .AddLocation(field)
                .SetExtension("fieldName", field.Name)
                .SetExtension("argumentName", argumentName)
                .SetExtension("requestedType", requestedType.FullName)
                .Build());

        public static GraphQLException ResolverContext_CannotConvertArgument(
            FieldNode field, Path path, NameString argumentName, Type requestedType) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_ResolverContext_CannotConvertArgument_Message,
                    argumentName,
                    requestedType.FullName ?? requestedType.Name)
                .SetPath(path)
                .AddLocation(field)
                .SetExtension("fieldName", field.Name)
                .SetExtension("argumentName", argumentName)
                .SetExtension("requestedType", requestedType.FullName)
                .Build());

        public static GraphQLException ResolverContext_LiteralNotCompatible(
            FieldNode field, Path path, NameString argumentName,
            Type requestedType, Type actualType) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_ResolverContext_LiteralNotCompatible_Message,
                    actualType.FullName ?? actualType.Name,
                    requestedType.FullName ?? actualType.Name)
                .SetPath(path)
                .AddLocation(field)
                .SetExtension("fieldName", field.Name)
                .SetExtension("argumentName", argumentName)
                .SetExtension("requestedType", requestedType.FullName)
                .SetExtension("actualType", actualType.FullName)
                .Build());

        public static GraphQLException ResolverContext_ArgumentDoesNotExist(
            FieldNode field, Path path, NameString argumentName) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_ResolverContext_ArgumentDoesNotExist_Message,
                    argumentName,
                    field.Name.Value)
                .SetPath(path)
                .AddLocation(field)
                .SetExtension("fieldName", field.Name)
                .SetExtension("argumentName", argumentName)
                .Build());

        public static InvalidOperationException RequestExecutorResolver_SchemaNameDoesNotMatch(
            NameString configurationSchemaName, NameString schemaName) =>
            new InvalidOperationException(
                "The schema name must allign with the schema name expected by the configuration.");

        public static GraphQLException OperationResolverHelper_NoOperationFound(
            DocumentNode documentNode) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(ThrowHelper_OperationResolverHelper_NoOperationFound_Message)
                .AddLocation(documentNode)
                .Build());

        public static GraphQLException OperationResolverHelper_MultipleOperation(
            OperationDefinitionNode firstOperation,
            OperationDefinitionNode secondOperation) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(ThrowHelper_OperationResolverHelper_MultipleOperation_Message)
                .AddLocation(firstOperation)
                .AddLocation(secondOperation)
                .Build());

        public static GraphQLException OperationResolverHelper_InvalidOperationName(
            DocumentNode documentNode, string operationName) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_OperationResolverHelper_InvalidOperationName_Message,
                    operationName)
                .AddLocation(documentNode)
                .SetExtension("operationName", operationName)
                .Build());

        public static GraphQLException BatchExecutor_CannotSerializeVariable(
            string variableName) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_BatchExecutor_CannotSerializeVariable_Message,
                    variableName)
                .SetCode(ErrorCodes.Execution.CannotSerialize)
                .Build());

        public static GraphQLException CollectVariablesVisitor_NoCompatibleType(
            ISyntaxNode node,
            IReadOnlyList<object> path) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(ThrowHelper_CollectVariablesVisitor_NoCompatibleType_Message)
                .SetCode(ErrorCodes.Execution.AutoMapVarError)
                .SetPath(path)
                .AddLocation(node)
                .Build());

        public static GraphQLException FieldVisibility_ValueNotSupported(IValueNode value) =>
            new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(ThrowHelper_FieldVisibility_ValueNotSupported_Message)
                    .AddLocation(value)
                    .Build());

        public static GraphQLException QueryCompiler_CompositeTypeSelectionSet(
            FieldNode selection) =>
            new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(ThrowHelper_QueryCompiler_CompositeTypeSelectionSet_Message)
                    .AddLocation(selection)
                    .Build());

        public static GraphQLException OperationExecutionMiddleware_NoBatchDispatcher() =>
            new GraphQLException(
                ThrowHelper_OperationExecutionMiddleware_NoBatchDispatcher_Message);

        public static GraphQLException OperationCompiler_FragmentNoSelections(
            ISyntaxNode syntaxNode) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage("Fragment selection set is empty.")
                .AddLocation(syntaxNode)
                .Build());

        public static GraphQLException OperationCompiler_NoCompositeSelections(
            FieldNode syntaxNode) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    "The composite field `{0}` has no selections.",
                    syntaxNode.Alias?.Value ?? syntaxNode.Name.Value)
                .AddLocation(syntaxNode)
                .Build());

        public static GraphQLException OperationCompiler_NoOperationSelections(
            OperationDefinitionNode syntaxNode) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage("The operation has no selections.")
                .AddLocation(syntaxNode)
                .Build());

        public static SchemaException Convention_UnableToCreateConvention(
            Type convention) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "Unable to create a convention instance from {0}.",
                        convention.FullName ?? convention.Name)
                    .Build());

        public static ObjectDisposedException Object_Returned_To_Pool() =>
            new ObjectDisposedException(
                "The specified object was returned to the pool and is no longer usable.");
    }
}
