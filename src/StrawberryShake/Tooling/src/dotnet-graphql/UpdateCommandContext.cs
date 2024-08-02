namespace StrawberryShake.Tools;

public class UpdateCommandContext
{
    public UpdateCommandContext(
        Uri? uri,
        string? path,
        string? token,
        string? scheme,
        Dictionary<string, IEnumerable<string>> customHeaders,
        int typeDepth)
    {
        Uri = uri;
        Path = path;
        Token = token;
        Scheme = scheme;
        CustomHeaders = customHeaders;
        TypeDepth = typeDepth;
    }

    public Uri? Uri { get; }
    public string? Path { get; }
    public string? Token { get; }
    public string? Scheme { get; }
    public Dictionary<string, IEnumerable<string>> CustomHeaders { get; }
    public int TypeDepth { get; }
}
