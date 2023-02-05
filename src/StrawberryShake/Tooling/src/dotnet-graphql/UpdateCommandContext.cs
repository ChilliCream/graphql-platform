using System;
using System.Collections.Generic;

namespace StrawberryShake.Tools;

public class UpdateCommandContext
{
    public UpdateCommandContext(
        Uri? uri,
        string? path,
        string? token,
        string? scheme,
        Dictionary<string, IEnumerable<string>> customHeaders)
    {
        Uri = uri;
        Path = path;
        Token = token;
        Scheme = scheme;
        CustomHeaders = customHeaders;
    }

    public Uri? Uri { get; }
    public string? Path { get; }
    public string? Token { get; }
    public string? Scheme { get; }
    public Dictionary<string, IEnumerable<string>> CustomHeaders { get; }
}