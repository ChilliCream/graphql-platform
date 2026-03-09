namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Represents the configuration for fetching data from a source schema.
/// </summary>
public interface ISourceSchemaClientConfiguration
{
    /// <summary>
    /// Gets the name of the source schema.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the supported operations.
    /// </summary>
    SupportedOperationType SupportedOperations { get; }
}
