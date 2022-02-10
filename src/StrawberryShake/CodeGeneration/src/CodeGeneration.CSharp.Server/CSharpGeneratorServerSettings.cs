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

    /// <summary>
    /// The root directory where the GraphQL client is located.
    /// </summary>
    public string RootDirectoryName { get; set; } = "./";

    /// <summary>
    /// The relative output directory for generated code.
    /// </summary>
    public string OutputDirectoryName { get; set; } = "Generated";
}
