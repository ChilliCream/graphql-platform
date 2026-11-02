namespace HotChocolate.Execution.Internal;

/// <summary>
/// Supplies the transport capabilities that <see cref="SchemaFileExporter"/> declares
/// in the settings file it creates for a schema.
/// </summary>
internal interface ITransportCapabilitiesProvider
{
    /// <summary>
    /// Gets the transport capabilities of the schema with the given name.
    /// </summary>
    /// <param name="schemaName">
    /// The name of the schema.
    /// </param>
    /// <returns>
    /// The transport capabilities to declare for the schema.
    /// </returns>
    TransportCapabilities GetCapabilities(string schemaName);
}
