namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Represents the schema file initialization options.
/// </summary>
public struct SchemaFileInitializationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether a schema file shall be written
    /// to the file system every time the executor is initialized.
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    /// Gets or sets the name of the schema file.
    /// The default value is <c>"schema.graphqls"</c>.
    /// </summary>
    public string? FileName { get; set; }
}
