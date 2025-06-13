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
}
