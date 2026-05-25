namespace HotChocolate.Fusion.Options;

/// <summary>
/// Configuration options for parsing source schemas.
/// </summary>
public sealed class SourceSchemaParserOptions
{
    /// <summary>
    /// Enables schema validation when parsing source schemas.
    /// </summary>
    public bool EnableSchemaValidation { get; set; } = true;
}
