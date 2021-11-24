namespace StrawberryShake.CodeGeneration.CSharp;

/// <summary>
/// The csharp generator server settings.
/// </summary>
public class CSharpGeneratorServerSettings : CSharpGeneratorSettings
{
    /// <summary>
    /// The GraphQL File Glob Filter.
    /// </summary>
    public string Documents { get; set; } = "**/*.graphql";

    /// <summary>
    /// The directory where the code generation shall copy the files to.
    /// </summary>
    public string? PersistedQueryDirectory { get; set; }
}
