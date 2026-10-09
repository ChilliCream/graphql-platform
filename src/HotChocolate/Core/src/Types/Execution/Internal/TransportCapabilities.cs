namespace HotChocolate.Execution.Internal;

/// <summary>
/// The transport capabilities that <see cref="SchemaFileExporter"/> declares
/// in the settings file it creates for a schema.
/// </summary>
/// <param name="VariableBatching">
/// Whether the server accepts variable batching requests.
/// </param>
/// <param name="RequestBatching">
/// Whether the server accepts request batching requests.
/// </param>
internal readonly record struct TransportCapabilities(
    bool VariableBatching,
    bool RequestBatching);
