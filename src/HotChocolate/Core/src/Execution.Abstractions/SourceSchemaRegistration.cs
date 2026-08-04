namespace HotChocolate;

/// <summary>
/// A marker that indicates a schema has been configured with source schema defaults
/// for the Composite Schema Specification.
/// </summary>
/// <param name="schemaName">
/// The name of the schema that was configured as a source schema.
/// </param>
public sealed class SourceSchemaRegistration(string schemaName)
{
    /// <summary>
    /// Gets the name of the schema.
    /// </summary>
    public string SchemaName { get; } = schemaName;
}
