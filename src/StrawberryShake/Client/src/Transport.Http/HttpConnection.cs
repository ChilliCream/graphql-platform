using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using StrawberryShake.Internal;

namespace StrawberryShake.Transport.Http
{
    public class HttpConnection : IConnection<JsonDocument>
    {
        private readonly Func<HttpClient> _createClient;
        private readonly JsonOperationRequestSerializer _serializer = new();

        public HttpConnection(Func<HttpClient> createClient)
        {
            _createClient = createClient ?? throw new ArgumentNullException(nameof(createClient));
        }

        public async IAsyncEnumerable<Response<JsonDocument>> ExecuteAsync(
            OperationRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using HttpClient client = _createClient();

            using HttpRequestMessage requestMessage =
                CreateRequestMessage(request, client.BaseAddress!);

            using HttpResponseMessage responseMessage =
                await client
                    .SendAsync(requestMessage, cancellationToken)
                    .ConfigureAwait(false);

            JsonDocument? body = null;
            Exception? exception = null;

            try
            {
                #if NETSTANDARD2_0
                using Stream stream =
                    await responseMessage.Content
                        .ReadAsStreamAsync()
                        .ConfigureAwait(false);
                #else
                await using Stream stream =
                    await responseMessage.Content
                        .ReadAsStreamAsync()
                        .ConfigureAwait(false);
                #endif

                body = await JsonDocument
                    .ParseAsync(stream, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // ignore any error.
            }

            try
            {
                responseMessage.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                exception = ex;
            }

            yield return new Response<JsonDocument>(body, exception);
        }

        protected virtual HttpRequestMessage CreateRequestMessage(
            OperationRequest request,
            Uri baseAddress)
        {
            var content = new ByteArrayContent(CreateRequestMessageBody(request));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return new()
            {
                RequestUri = baseAddress,
                Method = HttpMethod.Post,
                Content = content
            };
        }

        private byte[] CreateRequestMessageBody(
            OperationRequest request)
        {
            using var arrayWriter = new ArrayWriter();
            _serializer.Serialize(request, arrayWriter);
            var buffer = new byte[arrayWriter.Length];
            arrayWriter.Body.Span.CopyTo(buffer);
            return buffer;
        }
    }
}
