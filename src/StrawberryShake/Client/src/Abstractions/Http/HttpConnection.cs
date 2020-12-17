using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

namespace StrawberryShake.Http
{
    public class HttpConnection : IConnection<JsonDocument>
    {
        private readonly HttpClient _client;
        private readonly JsonOperationRequestSerializer _serializer;

        public HttpConnection(HttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public IAsyncEnumerable<JsonDocument> ExecuteAsync(
            OperationRequest request,
            CancellationToken cancellationToken = default)
        {


        }


        protected virtual HttpRequestMessage CreateRequestMessage()
        {
            var message = new HttpRequestMessage
            {
                Method = HttpMethod.Post, Content = new ByteArrayContent()
            };
        }

        private byte[] CreateRequestMessageBody(OperationRequest request)
        {
            var arrayWriter = new ArrayBufferWriter<byte>();
            _serializer.Serialize(request, arrayWriter);
        }
    }
}
