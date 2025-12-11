namespace HotChocolate.Fusion.Options;

/// <summary>
/// Configuration options for composing source schemas.
/// </summary>
public sealed class SchemaComposerOptions
{
    /// <summary>
    /// Configuration options for each source schema.
    /// </summary>
    public Dictionary<string, SourceSchemaOptions> SourceSchemas { get; init; } = [];

    /// <summary>
    /// Configuration options for merging source schemas.
    /// </summary>
    public SourceSchemaMergerOptions Merger { get; init; } = new();

    /// <summary>
    /// Configuration options for validating the satisfiability of the composed schema.
    /// </summary>
    public SatisfiabilityOptions Satisfiability { get; init; } = new();
}
