using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using StrawberryShake.Transport.Http;
using StrawberryShake.Transport.Subscriptions;

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
            _operationManager = operationManager ?? throw new ArgumentNullException(nameof(operationManager));
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Response<JsonDocument>> ExecuteAsync(
            OperationRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await using SocketOperation operation =
                await _operationManager.StartOperationAsync(request, cancellationToken);

            await foreach (var result in operation.ReadAsync(cancellationToken))
            {
                // TODO : Exception? --------------------------------V
                yield return new Response<JsonDocument>(result, null);
            }
        }
    }
}
