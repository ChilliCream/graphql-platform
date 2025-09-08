using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.AspNetCore.Authorization;

/// <summary>
/// The class representing the information about the original GraphQl request.
/// </summary>
public sealed class OriginalRequest(
    IHeaderDictionary headers,
    string host,
    string method,
    string path,
    IEnumerable<KeyValuePair<string, StringValues>>? query,
    string scheme)
{
    /// <summary>
    /// Original request headers.
    /// </summary>
    public IHeaderDictionary Headers { get; } = headers ?? throw new ArgumentNullException(nameof(headers));

    /// <summary>
    /// Information about the host sent request.
    /// </summary>
    public string Host { get; } = host ?? throw new ArgumentNullException(nameof(host));

    /// <summary>
    /// The HTTP request method.
    /// </summary>
    public string Method { get; } = method ?? throw new ArgumentNullException(nameof(method));

    /// <summary>
    /// Path of the request.
    /// </summary>
    public string Path { get; } = path ?? throw new ArgumentNullException(nameof(path));

    /// <summary>
    /// The query of the request.
    /// </summary>
    public IEnumerable<KeyValuePair<string, StringValues>>? Query { get; } = query;

    /// <summary>
    /// GraphQl schema of the request.
    /// </summary>
    public string Scheme { get; } = scheme ?? throw new ArgumentNullException(nameof(scheme));
}
