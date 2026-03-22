namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Represents a single result from a batch stream, tagged with the index of the
/// request it belongs to.
/// </summary>
public readonly record struct BatchStreamResult(int RequestIndex, SourceSchemaResult Result);
