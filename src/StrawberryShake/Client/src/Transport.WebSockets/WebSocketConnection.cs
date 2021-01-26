using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using StrawberryShake.Transport.Http;
using StrawberryShake.Transport.Subscriptions;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    public class WebSocketConnection : IConnection<JsonDocument>
    {
        private readonly ISocketOperationManager _client;
        private readonly JsonOperationRequestSerializer _serializer = new();

        public WebSocketConnection(ISocketOperationManager client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async IAsyncEnumerable<Response<JsonDocument>> ExecuteAsync(
            OperationRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await using SocketOperation operation =
                await _client.StartOperationAsync(request, cancellationToken);

            await foreach (var result in operation.ReadAsync(cancellationToken))
            {
                if (result is OperationMessage<JsonDocument> jsonResult)
                {
                    // TODO : Exception? --------------------------------V
                    yield return new Response<JsonDocument>(jsonResult.Payload, null);
                }
            }
        }
    }
}
