namespace HotChocolate.Fusion.Options;

public sealed class SourceSchemaOptions
{
    /// <summary>
    /// The source schema version.
    /// </summary>
    public Version Version { get; set; } = WellKnownVersions.LatestSourceSchemaVersion;

    /// <summary>
    /// Configuration options for parsing source schemas.
    /// </summary>
    public SourceSchemaParserOptions Parser { get; set; } = new();

    /// <summary>
    /// Configuration options for preprocessing source schemas.
    /// </summary>
    public SourceSchemaPreprocessorOptions Preprocessor { get; set; } = new();
}
