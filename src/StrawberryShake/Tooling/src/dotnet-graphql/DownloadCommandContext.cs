namespace StrawberryShake.Tools;

public class DownloadCommandContext
{
    public DownloadCommandContext(
        Uri uri,
        string fileName,
        string? token,
        string? scheme,
        Dictionary<string, IEnumerable<string>> customHeaders,
        int typeDepth)
    {
        Uri = uri;
        FileName = fileName;
        Token = token;
        Scheme = scheme;
        CustomHeaders = customHeaders;
        TypeDepth = typeDepth;
    }

    public Uri Uri { get; }
    public string FileName { get; }
    public string? Token { get; }
    public string? Scheme { get; }
    public Dictionary<string, IEnumerable<string>> CustomHeaders { get; }
    public int TypeDepth { get; }
}
