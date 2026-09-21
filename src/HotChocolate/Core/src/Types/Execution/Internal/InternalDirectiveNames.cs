namespace HotChocolate.Execution.Internal;

/// <summary>
/// Provides the names of directives that HotChocolate appends to an operation as an
/// implementation detail of the execution pipeline. These directives are never part of the
/// public schema and are not meant to be forwarded to another server.
/// </summary>
internal static class InternalDirectiveNames
{
    /// <summary>
    /// The name of the marker directive appended to a normalized operation definition when it
    /// still carries incremental delivery parts (a defer or a stream) after static evaluation.
    /// </summary>
    public const string HasIncrementalParts = "hc__hasIncrementalParts";
}
