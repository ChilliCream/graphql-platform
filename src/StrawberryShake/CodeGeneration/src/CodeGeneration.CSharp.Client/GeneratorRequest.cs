namespace StrawberryShake.CodeGeneration.CSharp;

public sealed class GeneratorRequest
{
    public GeneratorRequest(
        string configFileName,
        IReadOnlyList<string> documentFileNames,
        string? rootDirectory = null,
        string? defaultNamespace = null,
        string? persistedQueryDirectory = null)
    {
        ConfigFileName = configFileName ??
            throw new ArgumentNullException(nameof(configFileName));
        DocumentFileNames = documentFileNames ??
            throw new ArgumentNullException(nameof(documentFileNames));
        RootDirectory = rootDirectory ?? Path.GetDirectoryName(configFileName)!;
        DefaultNamespace = defaultNamespace;
        PersistedQueryDirectory = persistedQueryDirectory;
    }

    public string ConfigFileName { get; }

    public string RootDirectory { get; }

    public IReadOnlyList<string> DocumentFileNames { get; }

    public string? DefaultNamespace { get; }

    public string? PersistedQueryDirectory { get; }
}
