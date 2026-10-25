namespace HotChocolate.Execution.Internal;

/// <summary>
/// Provides names for executor-specific directives that are absent from the public schema.
/// These directives must not be forwarded to another server.
/// </summary>
internal static class InternalDirectiveNames
{
    /// <summary>
    /// The directive name indicating that a normalized operation has deferred or streamed parts.
    /// </summary>
    public const string HasIncrementalParts = "hc__hasIncrementalParts";
}
