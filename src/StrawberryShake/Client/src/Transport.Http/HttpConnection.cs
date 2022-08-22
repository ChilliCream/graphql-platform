using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using StrawberryShake.Internal;
using StrawberryShake.Json;

namespace StrawberryShake.Transport.Http
{
    public class HttpConnection : IHttpConnection
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
            var operation = CreateRequestMessageBody(request);

            var content = request.Files.Count == 0
                ? CreateRequestContent(operation)
                : CreateMultipartContent(request, operation);

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

        private static HttpContent CreateRequestContent(byte[] operation)
        {
            var content = new ByteArrayContent(operation);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }

        private static HttpContent CreateMultipartContent(OperationRequest request, byte[] operation)
        {
            var fileMap = new Dictionary<string, string[]>();
            var form = new MultipartFormDataContent
            {
                { new ByteArrayContent(operation), "operations" },
                { JsonContent.Create(fileMap), "map" }
            };

            foreach (var file in request.Files)
            {
                if (file.Value is { } fileContent)
                {
                    var identifier = (fileMap.Count + 1).ToString();
                    fileMap.Add(identifier, new[] { file.Key });
                    form.Add(new StreamContent(fileContent.Content), identifier, fileContent.FileName);
                }
            }

            return form;
        }
    }
}
