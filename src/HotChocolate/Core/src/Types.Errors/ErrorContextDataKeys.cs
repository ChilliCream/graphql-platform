namespace HotChocolate.Types;

internal static class ErrorContextDataKeys
{
    /// <summary>
    /// Stores the definition of the errors on the context data
    /// </summary>
    public const string ErrorDefinitions = "HotChocolate.Types.Errors.ErrorDefinitions";

    /// <summary>
    /// Stores the errors on the Scoped context for the middleware
    /// </summary>
    public const string Errors = "HotChocolate.Types.Errors.Errors";

    /// <summary>
    /// Defines if a type is a error type
    /// </summary>
    public const string IsErrorType = "HotChocolate.Types.Errors.IsErrorType";

    /// <summary>
    /// Marks the common error type of the schema
    /// </summary>
    public const string ErrorType = "HotChocolate.Errors.ErrorType";

    /// <summary>
    /// Signals that error conventions are enabled.
    /// </summary>
    public const string ErrorConventionEnabled = "HotChocolate.Types.Errors.ErrorConventionEnabled";
}
