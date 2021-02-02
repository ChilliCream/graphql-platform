using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using StrawberryShake.Transport.WebSockets;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    /// <summary>
    /// A WebSocket connection to a GraphQL server and allows to execute requests against it.
    /// </summary>
    public class WebSocketConnection : IConnection<JsonDocument>
    {
        private readonly ISocketOperationManager _operationManager;

        /// <summary>
        /// Creates a new instance of a <see cref="WebSocketConnection"/>
        /// </summary>
        /// <param name="operationManager"></param>
        public WebSocketConnection(ISocketOperationManager operationManager)
        {
            _operationManager = operationManager ??
                throw new ArgumentNullException(nameof(operationManager));
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Response<JsonDocument>> ExecuteAsync(
            OperationRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await using ISocketOperation operation =
                await _operationManager.StartOperationAsync(request, cancellationToken);

            await foreach (var message in operation.ReadAsync(cancellationToken))
            {
                switch (message.Type)
                {
                    case OperationMessageType.Data
                        when message is DataDocumentOperationMessage<JsonDocument> msg:
                        yield return new Response<JsonDocument>(msg.Payload, null);
                        break;
                    case OperationMessageType.Error when message is ErrorOperationMessage msg:
                        yield return new Response<JsonDocument>(
                            null,
                            new SocketOperationException(msg.Message));
                        yield break;
                    case OperationMessageType.Cancelled:
                        yield return new Response<JsonDocument>(
                            null,
                            new OperationCanceledException());
                        yield break;
                    case OperationMessageType.Complete:
                        yield break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
