namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Error codes for errors that are specific to the Fusion gateway execution pipeline.
/// </summary>
internal static class FusionExecutionErrorCodes
{
    /// <summary>
    /// The operation defines a variable whose name starts with the reserved
    /// <c>__fusion</c> prefix.
    /// </summary>
    public const string ReservedVariablePrefix = "FUSION_RESERVED_VARIABLE_PREFIX";

    /// <summary>
    /// A field or type was not accessible to the current user because an authorization
    /// policy denied access, or because policy evaluation failed.
    /// </summary>
    public const string UnauthorizedFieldOrType = "UNAUTHORIZED_FIELD_OR_TYPE";
}
