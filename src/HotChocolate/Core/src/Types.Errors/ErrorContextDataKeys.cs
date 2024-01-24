namespace HotChocolate.Types;

internal static class ErrorContextDataKeys
{
    /// <summary>
    /// Stores the definition of the errors on the context data
    /// </summary>
    public static readonly string ErrorDefinitions = "HotChocolate.Types.Errors.ErrorDefinitions";

    /// <summary>
    /// Stores the errors on the Scoped context for the middleware
    /// </summary>
    public static readonly string Errors = "HotChocolate.Types.Errors.Errors";

    /// <summary>
    /// Defines if a type is a error type
    /// </summary>
    public static readonly string IsErrorType = "HotChocolate.Types.Errors.IsErrorType";

    /// <summary>
    /// Marks the common error type of the schema
    /// </summary>
    public static readonly string ErrorType = "HotChocolate.Errors.ErrorType";
}
