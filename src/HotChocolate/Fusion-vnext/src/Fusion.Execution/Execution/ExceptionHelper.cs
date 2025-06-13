namespace HotChocolate.Fusion.Execution;

internal static class ExceptionHelper
{
    public static GraphQLException VariableNotFound(
        string variableName) =>
        new(ErrorBuilder.New()
            .SetMessage(
                "The variable with the name `{0}` does not exist.",
                variableName)
            .Build());

    public static GraphQLException VariableNotOfType(
        string variableName,
        Type type) =>
        new(ErrorBuilder.New()
            .SetMessage(
                "The variable with the name `{0}` is not of the requested type `{1}`.",
                variableName,
                type.FullName ?? string.Empty)
            .Build());

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
            .AddLocation(variableDefinition);

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
}
