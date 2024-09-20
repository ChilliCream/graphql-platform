using StrawberryShake.Tools.Configuration;

namespace StrawberryShake.Tools;

public class InitCommandContext
{
    public InitCommandContext(
        string name,
        string path,
        string uri,
        string? token,
        string? scheme,
        Dictionary<string, IEnumerable<string>> customHeaders,
        int typeDepth)
    {
        SchemaName = "Schema";
        SchemaFileName = FileNames.SchemaFile;
        SchemaExtensionFileName = FileNames.SchemaExtensionFile;
        ClientName = name;
        Path = path;
        Uri = uri;
        Token = token;
        Scheme = scheme;
        CustomHeaders = customHeaders;
        TypeDepth = typeDepth;
    }

    public string SchemaName { get; }
    public string SchemaFileName { get; }
    public string SchemaExtensionFileName { get; }
    public string ConfigFileName => FileNames.GraphQLConfigFile;
    public string ClientName { get; }
    public string Path { get; }
    public string Uri { get; }
    public string? Token { get; }
    public string? Scheme { get; }
    public Dictionary<string, IEnumerable<string>> CustomHeaders { get; }
    public string? CustomNamespace { get; set; }
    public int TypeDepth { get; }
}
