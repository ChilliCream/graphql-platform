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
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Creates a new instance of a <see cref="WebSocketConnection"/>
        /// </summary>
        /// <param name="sessionManager"></param>
        public WebSocketConnection(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager ??
                throw new ArgumentNullException(nameof(sessionManager));
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
                await _sessionManager.StartOperationAsync(request, cancellationToken);

            await foreach (var message in operation.ReadAsync(cancellationToken))
            {
                switch (message.Type)
                {
                    case OperationMessageType.Data
                        when message is DataDocumentOperationMessage<JsonDocument> msg:
                        yield return new Response<JsonDocument>(msg.Payload, null);
                        break;
                    case OperationMessageType.Error when message is ErrorOperationMessage msg:
                        var operationEx = new SocketOperationException(msg.Message);
                        yield return new Response<JsonDocument>(null, operationEx);
                        yield break;
                    case OperationMessageType.Cancelled:
                        var canceledException = new OperationCanceledException();
                        yield return new Response<JsonDocument>(null, canceledException);
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
