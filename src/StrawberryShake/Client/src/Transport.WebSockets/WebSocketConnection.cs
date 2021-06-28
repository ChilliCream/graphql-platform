using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Transport.WebSockets.Messages;

namespace StrawberryShake.Transport.WebSockets
{
    /// <summary>
    /// A WebSocket connection to a GraphQL server and allows to execute requests against it.
    /// </summary>
    public class WebSocketConnection : IWebSocketConnection
    {
        private readonly Func<CancellationToken, ValueTask<ISession>> _sessionFactory;

        /// <summary>
        /// Creates a new instance of a <see cref="WebSocketConnection"/>
        /// </summary>
        /// <param name="sessionFactory"></param>
        public WebSocketConnection(Func<CancellationToken, ValueTask<ISession>> sessionFactory)
        {
            _sessionFactory = sessionFactory ??
                throw new ArgumentNullException(nameof(sessionFactory));
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

            await using ISession session = await _sessionFactory(cancellationToken);

            await using ISocketOperation operation =
                await session.StartOperationAsync(request, cancellationToken);

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
