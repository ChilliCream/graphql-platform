namespace HotChocolate.AspNetCore.Instrumentation;

/// <summary>
/// Specifies the HTTP request kind that is being executed.
/// </summary>
public enum HttpRequestKind
{
    /// <summary>
    /// HTTP POST GraphQL Request.
    /// </summary>
    HttpPost,

    /// <summary>
    /// HTTP GET GraphQL Request.
    /// </summary>
    HttpGet,

    /// <summary>
    /// HTTP GET SDL request.
    /// </summary>
    HttpGetSchema,

    /// <summary>
    /// HTTP POST GraphQL MultiPart Request.
    /// </summary>
    HttpMultiPart,

    /// <summary>
    /// HTTP POST GraphQL-SSE
    /// </summary>
    HttpSse,
}
