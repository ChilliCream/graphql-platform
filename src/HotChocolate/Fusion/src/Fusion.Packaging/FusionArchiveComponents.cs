namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Identifies the archive components that can be stripped from a Fusion Archive when they are not
/// required for a specific deployment phase, such as configuring the gateway.
/// </summary>
[Flags]
public enum FusionArchiveComponents
{
    /// <summary>
    /// No components.
    /// </summary>
    None = 0,

    /// <summary>
    /// The <c>source-schemas/</c> directory holding the individual source schemas and their settings.
    /// </summary>
    SourceSchemas = 1,

    /// <summary>
    /// The <c>composition-settings.json</c> file at the archive root.
    /// </summary>
    CompositionSettings = 2
}
