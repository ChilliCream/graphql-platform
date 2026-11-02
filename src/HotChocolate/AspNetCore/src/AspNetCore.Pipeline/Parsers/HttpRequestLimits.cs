namespace HotChocolate.AspNetCore.Parsers;

/// <summary>
/// The limits the HTTP transport applies to a GraphQL request.
/// </summary>
internal sealed class HttpRequestLimits(int maxRequestSize)
{
    /// <summary>
    /// The maximum size, in bytes, of a GraphQL request: a JSON request body, or the
    /// <c>operations</c> field of a multipart request.
    /// </summary>
    public int MaxRequestSize { get; } =
        Math.Max(maxRequestSize, DefaultHttpRequestParser.MinRequestSize);
}
