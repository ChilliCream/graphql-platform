using HotChocolate.Fusion.Logging;

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

    /// <summary>
    /// The severity used when a field is deprecated but the implemented interface field is not.
    /// </summary>
    public LogSeverity InvalidFieldDeprecationSeverity { get; set; } = LogSeverity.Warning;

    internal bool IsApolloFederationV1 { get; set; }
}
