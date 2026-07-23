namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// A single per-row result produced by <see cref="AliasResponseReader"/>, tagged with the index
/// of the inbound request it belongs to. The client glue adapts this to a
/// <see cref="SourceSchemaBatchResult"/> for the batch path or yields the <see cref="Result"/>
/// directly for the single request path.
/// </summary>
/// <param name="RequestIndex">The index of the inbound request this result belongs to.</param>
/// <param name="Result">The per-row source schema result with original field names restored.</param>
internal readonly record struct AliasRowResult(int RequestIndex, SourceSchemaResult Result);
