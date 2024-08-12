using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.AspNetCore.Authorization;

public sealed class OriginalRequest
{
    public OriginalRequest(
        IHeaderDictionary headers,
        string host,
        string method,
        string path,
        IEnumerable<KeyValuePair<string, StringValues>>? query,
        string scheme)
    {
        Headers = headers ?? throw new ArgumentNullException(nameof(headers));
        Host = host ?? throw new ArgumentNullException(nameof(host));
        Method = method ?? throw new ArgumentNullException(nameof(method));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Query = query;
        Scheme = scheme ?? throw new ArgumentNullException(nameof(scheme));
    }

    public IHeaderDictionary Headers { get; }

    public string Host { get; }

    public string Method { get; }

    public string Path { get; }

    public IEnumerable<KeyValuePair<string, StringValues>>? Query { get; }

    public string Scheme { get; }
}
