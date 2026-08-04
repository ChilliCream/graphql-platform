using System.Net;
using System.Net.Http.Headers;

#if FUSION
namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

/// <summary>
/// Holds a buffered snapshot of an HTTP response so that multiple callers
/// can each receive their own independent <see cref="HttpResponseMessage"/>.
/// </summary>
internal sealed class BufferedHttpResponse
{
    private readonly HttpStatusCode _statusCode;
    private readonly byte[] _body;
    private readonly MediaTypeHeaderValue? _contentType;

    private BufferedHttpResponse(
        HttpStatusCode statusCode,
        byte[] body,
        MediaTypeHeaderValue? contentType)
    {
        _statusCode = statusCode;
        _body = body;
        _contentType = contentType;
    }

    /// <summary>
    /// Captures the response into a buffered snapshot.
    /// The response content is fully read and buffered.
    /// </summary>
    public static async Task<BufferedHttpResponse> CaptureAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content
            .ReadAsByteArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var contentType = response.Content.Headers.ContentType;

        return new BufferedHttpResponse(response.StatusCode, body, contentType);
    }

    /// <summary>
    /// Creates a new <see cref="HttpResponseMessage"/> from the buffered snapshot.
    /// The returned response shares the same underlying byte array (read-only).
    /// </summary>
    public HttpResponseMessage CreateResponse()
    {
        var response = new HttpResponseMessage(_statusCode);
        var content = new ByteArrayContent(_body);

        if (_contentType is not null)
        {
            content.Headers.ContentType = _contentType;
        }

        response.Content = content;
        return response;
    }
}
