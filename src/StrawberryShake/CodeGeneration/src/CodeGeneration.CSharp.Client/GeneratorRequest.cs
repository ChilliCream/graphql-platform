namespace StrawberryShake.CodeGeneration.CSharp;

public sealed class GeneratorRequest
{
    public GeneratorRequest(
        string configFileName,
        IReadOnlyList<string> documentFileNames,
        string? rootDirectory = null,
        string? defaultNamespace = null,
        string? persistedQueryDirectory = null,
        RequestOptions option = RequestOptions.Default)
    {
        ConfigFileName = configFileName ??
            throw new ArgumentNullException(nameof(configFileName));
        DocumentFileNames = documentFileNames ??
            throw new ArgumentNullException(nameof(documentFileNames));
        RootDirectory = rootDirectory ?? Path.GetDirectoryName(configFileName)!;
        DefaultNamespace = defaultNamespace;
        PersistedQueryDirectory = persistedQueryDirectory;
        Option = option;
    }

    public string ConfigFileName { get; }

    public string RootDirectory { get; }

    public IReadOnlyList<string> DocumentFileNames { get; }

    public string? DefaultNamespace { get; }

    public string? PersistedQueryDirectory { get; }

    public RequestOptions Option { get; }
}
