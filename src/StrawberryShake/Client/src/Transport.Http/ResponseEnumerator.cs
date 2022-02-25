using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using static System.Net.Http.HttpCompletionOption;
using static System.StringComparison;

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
            HttpClient client = _createClient();
            HttpRequestMessage request = _createRequest();
            HttpResponseMessage response =
                await client.SendAsync(request, ResponseHeadersRead, _abort).ConfigureAwait(false);

#if NET5_0_OR_GREATER
            Stream stream = await response.Content.ReadAsStreamAsync(_abort).ConfigureAwait(false);
#else
            Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif

            _context = new ConnectionContext(client, request, response, stream);

            if (response.Content.Headers.ContentType is { } contentType &&
                string.Equals(contentType.MediaType, "multipart/mixed"))
            {
                NameValueHeaderValue boundary =
                    contentType.Parameters.First(t => string.Equals(t.Name, "boundary", Ordinal));
                _reader = new MultipartReader(boundary.Value!.Trim('"'), stream);
            }
            else
            {
                Current = await stream.TryParseResponse(_abort).ConfigureAwait(false);
                _completed = true;
                return true;
            }
        }

        MultipartSection? multipartSection =
            await _reader.ReadNextSectionAsync(_abort).ConfigureAwait(false);

        if (multipartSection is null)
        {
            Current = default!;
            return false;
        }

#if NETCOREAPP3_1_OR_GREATER
        await using Stream body = multipartSection.Body;
#else
        using Stream body = multipartSection.Body;
#endif

        Current = await body.TryParseResponse(_abort).ConfigureAwait(false);

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
