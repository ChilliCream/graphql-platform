using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using static System.Net.Http.HttpCompletionOption;
using static System.StringComparison;
using static StrawberryShake.Properties.Resources;
using static StrawberryShake.Transport.Http.ResponseHelper;

namespace StrawberryShake.Transport.Http;

internal sealed class ResponseEnumerator : IAsyncEnumerator<Response<JsonDocument>>
{
    private readonly Func<HttpClient> _createClient;
    private readonly Func<HttpRequestMessage> _createRequest;
    private readonly CancellationToken _abort;
    private ConnectionContext? _context;
    private MultipartReader? _reader;
    private bool _completed;
    private bool _disposed;

    public ResponseEnumerator(
        Func<HttpClient> createClient,
        Func<HttpRequestMessage> createRequest,
        CancellationToken abort)
    {
        _createClient = createClient;
        _createRequest = createRequest;
        _abort = abort;
    }

    public Response<JsonDocument> Current { get; private set; } = default!;

    public async ValueTask<bool> MoveNextAsync()
    {
        if (_completed || _disposed)
        {
            return false;
        }

        if (_context is null || _reader is null)
        {
            var client = _createClient();
            var request = _createRequest();
            var response =
                await client.SendAsync(request, ResponseHeadersRead, _abort).ConfigureAwait(false);

#if NET5_0_OR_GREATER
            var stream = await response.Content.ReadAsStreamAsync(_abort).ConfigureAwait(false);
#else
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            _context = new ConnectionContext(client, request, response, stream);

            if (response.Content.Headers.ContentType is { } contentType &&
                string.Equals(contentType.MediaType, "multipart/mixed"))
            {
                var boundary = contentType.Parameters.First(
                    t => string.Equals(t.Name, "boundary", Ordinal));
                _reader = new MultipartReader(boundary.Value!.Trim('"'), stream);
            }
            else
            {
                try
                {
                    Exception? transportError = null;

                    // If we detect that the response has a non-success status code we will
                    // create a transport error that will be added to the response.
                    if (!response.IsSuccessStatusCode)
                    {
#if NET5_0_OR_GREATER
                        transportError =
                            new HttpRequestException(
                                string.Format(
                                    ResponseEnumerator_HttpNoSuccessStatusCode,
                                    (int)response.StatusCode,
                                    response.ReasonPhrase),
                                null,
                                response.StatusCode);
#else
                        transportError =
                            new HttpRequestException(
                                string.Format(
                                    ResponseEnumerator_HttpNoSuccessStatusCode,
                                    (int)response.StatusCode,
                                    response.ReasonPhrase),
                                null);
#endif
                    }

                    // We now try to parse the possible GraphQL response, this step could fail
                    // as the response might not be a GraphQL response. It could in some cases
                    // be a HTML error page.
                    Current = await stream.TryParseResponse(transportError, _abort)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Current = new Response<JsonDocument>(CreateBodyFromException(ex), ex);
                }
                _completed = true;
                return true;
            }
        }

        var multipartSection = await _reader.ReadNextSectionAsync(_abort).ConfigureAwait(false);

        if (multipartSection is null)
        {
            Current = default!;
            return false;
        }

#if NETCOREAPP3_1_OR_GREATER
        await using var body = multipartSection.Body;
#else
        using var body = multipartSection.Body;
#endif

        Current = await body.TryParseResponse(null, _abort).ConfigureAwait(false);

        if (Current.Exception is not null)
        {
            _completed = true;
        }

        return true;
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            Current = default!;
            _reader = null;
            _context?.Dispose();
            _disposed = true;
        }

        return default;
    }

    private sealed class ConnectionContext : IDisposable
    {
        public ConnectionContext(
            HttpClient client,
            HttpRequestMessage requestMessage,
            HttpResponseMessage responseMessage,
            Stream stream)
        {
            Client = client;
            RequestMessage = requestMessage;
            ResponseMessage = responseMessage;
            Stream = stream;
        }

        public HttpClient Client { get; }

        public HttpRequestMessage RequestMessage { get; }

        public HttpResponseMessage ResponseMessage { get; }

        public Stream Stream { get; }

        public void Dispose()
        {
            Client.Dispose();
            RequestMessage.Dispose();
            ResponseMessage.Dispose();
            Stream.Dispose();
        }
    }
}
