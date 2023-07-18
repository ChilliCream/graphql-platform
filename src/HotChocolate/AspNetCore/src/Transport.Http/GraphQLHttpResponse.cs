using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Utilities;

namespace HotChocolate.Transport.Http;

public sealed class GraphQLHttpResponse : IDisposable
{
    private static readonly OperationResult _transportError = CreateTransportError();
    
#if NET6_0_OR_GREATER
    private static readonly Encoding _utf8 = Encoding.UTF8;
#endif
    private readonly HttpResponseMessage _message;

    public GraphQLHttpResponse(HttpResponseMessage message)
    {
        _message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public ValueTask<OperationResult> ReadAsResultAsync(CancellationToken cancellationToken = default)
    {
        var contentType = _message.Content.Headers.ContentType;

        // The server supports the newer graphql-response+json media type and users are free
        // to use status codes.
        if (contentType?.MediaType.EqualsOrdinal(ContentType.GraphQL) ?? false)
        {
            return ReadAsResultInternalAsync(contentType.CharSet, cancellationToken);
        }

        // The server supports the older application/json media type and the status code
        // is expected to be a 2xx for a valid GraphQL response.
        if (contentType?.MediaType.EqualsOrdinal(ContentType.Json) ?? false)
        {
            _message.EnsureSuccessStatusCode();
            return ReadAsResultInternalAsync(contentType.CharSet, cancellationToken);
        }

        // if the media type is anything else we will return a transport error.
        return new ValueTask<OperationResult>(_transportError);
    }
    
    private async ValueTask<OperationResult> ReadAsResultInternalAsync(string? charSet, CancellationToken ct)
    {
#if NET6_0_OR_GREATER
        await using var contentStream = await _message.Content.ReadAsStreamAsync(ct)
            .ConfigureAwait(false);
#else
        using var contentStream = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
        
        var stream = contentStream;

#if NET6_0_OR_GREATER
        var sourceEncoding = GetEncoding(charSet);
        if (sourceEncoding is not null && !Equals(sourceEncoding.EncodingName, _utf8.EncodingName))
        {
            stream = GetTranscodingStream(contentStream, sourceEncoding);
        }
#endif
        
        var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        try
        {
            return OperationResult.Parse(document);
        }
        catch
        {
            document.Dispose();
            throw;
        }
    }

    public IAsyncEnumerable<OperationResult> ReadAsResultStreamAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
    
#if NET6_0_OR_GREATER
    private static Encoding? GetEncoding(string? charset)
    {
        Encoding? encoding = null;

        if (charset != null)
        {
            try
            {
                // Remove at most a single set of quotes.
                if (charset.Length > 2 && charset[0] == '\"' && charset[^1] == '\"')
                {
                    encoding = Encoding.GetEncoding(charset.Substring(1, charset.Length - 2));
                }
                else
                {
                    encoding = Encoding.GetEncoding(charset);
                }
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException("Invalid Charset", e);
            }

            Debug.Assert(encoding != null);
        }

        return encoding;
    }
    
    private static Stream GetTranscodingStream(Stream contentStream, Encoding sourceEncoding)
        => Encoding.CreateTranscodingStream(
            contentStream,
            innerStreamEncoding: sourceEncoding,
            outerStreamEncoding: _utf8);
#endif
    
    private static OperationResult CreateTransportError()
        => new OperationResult(
            errors: JsonDocument.Parse(
                """
                [{"message": "Internal Execution Error"}]
                """).RootElement);

    public void Dispose() => _message.Dispose();
}