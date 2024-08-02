using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Execution.Properties.Resources;

namespace HotChocolate.Execution;

internal static class ThrowHelper
{
    public static GraphQLException VariableIsNotAnInputType(
        VariableDefinitionNode variableDefinition)
    {
        return new(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_VariableIsNotAnInputType_Message,
                    variableDefinition.Variable.Name.Value)
                .SetCode(ErrorCodes.Execution.MustBeInputType)
                .SetExtension("variable", variableDefinition.Variable.Name.Value)
                .SetExtension("type", variableDefinition.Type.ToString())
                .SetLocations([variableDefinition])
                .Build());
    }

    public static GraphQLException NonNullVariableIsNull(
        VariableDefinitionNode variableDefinition)
    {
        return new(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_NonNullVariableIsNull_Message,
                    variableDefinition.Variable.Name.Value)
                .SetCode(ErrorCodes.Execution.NonNullViolation)
                .SetExtension("variable", variableDefinition.Variable.Name.Value)
                .SetLocations([variableDefinition])
                .Build());
    }

    public static GraphQLException VariableValueInvalidType(
        VariableDefinitionNode variableDefinition,
        Exception? exception = null)
    {
        var underlyingError = exception is SerializationException serializationException
            ? serializationException.Message
            : null;

        var errorBuilder = ErrorBuilder.New()
            .SetMessage(
                ThrowHelper_VariableValueInvalidType_Message,
                variableDefinition.Variable.Name.Value)
            .SetCode(ErrorCodes.Execution.InvalidType)
            .SetExtension("variable", variableDefinition.Variable.Name.Value)
            .SetLocations([variableDefinition]);

        if (exception is not null)
        {
            errorBuilder.SetException(exception);
        }

        if (underlyingError is not null)
        {
            errorBuilder.SetExtension(nameof(underlyingError), underlyingError);
        }

        return new GraphQLException(errorBuilder.Build());
    }

    public static GraphQLException FieldDoesNotExistOnType(
        FieldNode selection, string typeName)
    {
        return new(
            ErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_FieldDoesNotExistOnType,
                    selection.Name.Value,
                    typeName)
                .SetLocations([selection])
                .Build());
    }

    public static NotSupportedException QueryTypeNotSupported() =>
        new(ThrowHelper_QueryTypeNotSupported_Message);

    public static GraphQLException VariableNotFound(
        string variableName) =>
        new(ErrorBuilder.New()
            .SetMessage(
                ThrowHelper_VariableNotFound_Message,
                variableName)
            .Build());

    public static GraphQLException VariableNotOfType(
        string variableName,
        Type type) =>
        new(ErrorBuilder.New()
            .SetMessage(
                ThrowHelper_VariableNotOfType_Message,
                variableName,
                type.FullName ?? string.Empty)
            .Build());

    public static GraphQLException RootTypeNotSupported(
        OperationType operationType) =>
        new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_RootTypeNotSupported_Message, operationType)
            .Build());

    public static GraphQLException SubscriptionExecutor_ContextInvalidState() =>
        new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_SubscriptionExecutor_ContextInvalidState_Message)
            .Build());

    public static GraphQLException SubscriptionExecutor_SubscriptionsMustHaveOneField() =>
        new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_SubscriptionExecutor_SubscriptionsMustHaveOneField_Message)
            .Build());

    public static GraphQLException SubscriptionExecutor_NoSubscribeResolver() =>
        new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_SubscriptionExecutor_NoSubscribeResolver_Message)
            .Build());

    public static GraphQLException ResolverContext_LiteralsNotSupported(
        FieldNode field, Path path, string argumentName, Type requestedType) =>
        new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_ResolverContext_LiteralsNotSupported_Message)
            .SetPath(path)
            .SetLocations([field])
            .SetExtension("fieldName", field.Name)
            .SetExtension("argumentName", argumentName)
            .SetExtension("requestedType", requestedType.FullName)
            .Build());

    public static GraphQLException ResolverContext_CannotConvertArgument(
        FieldNode field, Path path, string argumentName, Type requestedType) =>
        new(ErrorBuilder.New()
            .SetMessage(
                ThrowHelper_ResolverContext_CannotConvertArgument_Message,
                argumentName,
                requestedType.FullName ?? requestedType.Name)
            .SetPath(path)
            .SetLocations([field])
            .SetExtension("fieldName", field.Name)
            .SetExtension("argumentName", argumentName)
            .SetExtension("requestedType", requestedType.FullName)
            .Build());

    public static GraphQLException ResolverContext_LiteralNotCompatible(
        FieldNode field, Path path, string argumentName,
        Type requestedType, Type actualType) =>
        new(ErrorBuilder.New()
            .SetMessage(
                ThrowHelper_ResolverContext_LiteralNotCompatible_Message,
                actualType.FullName ?? actualType.Name,
                requestedType.FullName ?? actualType.Name)
            .SetPath(path)
            .SetLocations([field])
            .SetExtension("fieldName", field.Name)
            .SetExtension("argumentName", argumentName)
            .SetExtension("requestedType", requestedType.FullName)
            .SetExtension("actualType", actualType.FullName)
            .Build());

    public static GraphQLException ResolverContext_ArgumentDoesNotExist(
        FieldNode field, Path path, string argumentName) =>
        new(ErrorBuilder.New()
            .SetMessage(
                ThrowHelper_ResolverContext_ArgumentDoesNotExist_Message,
                argumentName,
                field.Name.Value)
            .SetPath(path)
            .SetLocations([field])
            .SetExtension("fieldName", field.Name)
            .SetExtension("argumentName", argumentName)
            .Build());

    public static InvalidOperationException RequestExecutorResolver_SchemaNameDoesNotMatch(
        string configurationSchemaName, string schemaName) =>
        new("The schema name must align with the schema name expected by the configuration.");

    public static GraphQLException OperationResolverHelper_NoOperationFound(
        DocumentNode documentNode) =>
        new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_OperationResolverHelper_NoOperationFound_Message)
            .SetLocations([documentNode])
            .Build());

    public static GraphQLException OperationResolverHelper_MultipleOperation(
        OperationDefinitionNode firstOperation,
        OperationDefinitionNode secondOperation) =>
        new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_OperationResolverHelper_MultipleOperation_Message)
            .SetLocations([firstOperation, secondOperation])
            .Build());

    public static GraphQLException OperationResolverHelper_InvalidOperationName(
        DocumentNode documentNode, string operationName) =>
        new(ErrorBuilder.New()
            .SetMessage(
                ThrowHelper_OperationResolverHelper_InvalidOperationName_Message,
                operationName)
            .SetLocations([documentNode])
            .SetExtension("operationName", operationName)
            .Build());

    public static GraphQLException BatchExecutor_CannotSerializeVariable(
        string variableName) =>
        new(ErrorBuilder.New()
            .SetMessage(
                ThrowHelper_BatchExecutor_CannotSerializeVariable_Message,
                variableName)
            .SetCode(ErrorCodes.Execution.CannotSerialize)
            .Build());

    public static GraphQLException CollectVariablesVisitor_NoCompatibleType(
        ISyntaxNode node,
        IReadOnlyList<object> path) =>
        new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_CollectVariablesVisitor_NoCompatibleType_Message)
            .SetCode(ErrorCodes.Execution.AutoMapVarError)
            .SetPath(path)
            .SetLocations([node])
            .Build());

    public static GraphQLException FieldVisibility_ValueNotSupported(IValueNode value) =>
        new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_FieldVisibility_ValueNotSupported_Message)
            .SetLocations([value])
            .Build());

    public static GraphQLException QueryCompiler_CompositeTypeSelectionSet(
        FieldNode selection) =>
        new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_QueryCompiler_CompositeTypeSelectionSet_Message)
            .SetLocations([selection])
            .Build());

    public static GraphQLException OperationExecutionMiddleware_NoBatchDispatcher() =>
        new(ThrowHelper_OperationExecutionMiddleware_NoBatchDispatcher_Message);

    public static SchemaException Convention_UnableToCreateConvention(
        Type convention) =>
        new(SchemaErrorBuilder.New()
            .SetMessage(
                "Unable to create a convention instance from {0}.",
                convention.FullName ?? convention.Name)
            .Build());

    public static ObjectDisposedException Object_Not_Initialized() =>
        new("The specified object was not initialized and is no longer usable.");

    public static GraphQLException ResolverContext_CannotCastParent(
        SchemaCoordinate fieldCoordinate,
        Path path,
        Type requestType,
        Type parentType)
        => new(ErrorBuilder.New()
            .SetMessage(
                ThrowHelper_ResolverContext_CannotCastParent,
                fieldCoordinate,
                parentType,
                requestType)
            .SetPath(path)
            .SetFieldCoordinate(fieldCoordinate)
            .SetCode(ErrorCodes.Execution.CannotCastParent)
            .Build());

    public static GraphQLException OneOfFieldMustBeNonNull(
        SchemaCoordinate field)
        => new(ErrorBuilder.New()
            .SetMessage(string.Format(ThrowHelper_OneOfFieldMustBeNonNull, field.MemberName))
            .SetCode(ErrorCodes.Execution.OneOfFieldMustBeNonNull)
            .SetExtension(nameof(field), field.ToString())
            .Build());

    public static ArgumentException SelectionSet_TypeContextInvalid(IObjectType typeContext)
        => new(string.Format(SelectionVariants_TypeContextInvalid, typeContext.Name));

    public static InvalidOperationException SelectionSet_TypeAlreadyAdded(IObjectType typeContext)
        => new(string.Format(ThrowHelper_SelectionSet_TypeAlreadyAdded, typeContext.Name));

    public static ArgumentException Operation_NoSelectionSet()
        => new(ThrowHelper_Operation_NoSelectionSet);

    public static NotSupportedException JsonFormatter_ResultNotSupported(string typeName)
        => new(string.Format(ThrowHelper_JsonFormatter_ResultNotSupported, typeName));

    public static NotSupportedException MultiPartFormatter_ResultNotSupported(string typeName)
        => new(string.Format(ThrowHelper_JsonFormatter_ResultNotSupported, typeName));
}
