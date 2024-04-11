using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Transport.WebSockets.Messages;
using static StrawberryShake.ResultFields;
using static StrawberryShake.Transport.WebSockets.ResponseHelper;

namespace StrawberryShake.Transport.WebSockets;

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
    public IAsyncEnumerable<Response<JsonDocument>> ExecuteAsync(OperationRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return new ResponseStream(_sessionFactory, request);
    }

    private sealed class ResponseStream : IAsyncEnumerable<Response<JsonDocument>>
    {
        private readonly Func<CancellationToken, ValueTask<ISession>> _sessionFactory;
        private readonly OperationRequest _request;

        public ResponseStream(
            Func<CancellationToken, ValueTask<ISession>> sessionFactory,
            OperationRequest request)
        {
            _sessionFactory = sessionFactory;
            _request = request;
        }

        public async IAsyncEnumerator<Response<JsonDocument>> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            await using ISession session =
                await _sessionFactory(cancellationToken).ConfigureAwait(false);

            await using ISocketOperation operation =
                await session.StartOperationAsync(_request, cancellationToken)
                    .ConfigureAwait(false);

            await foreach (var message in
                operation.ReadAsync().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                switch (message.Type)
                {
                    case OperationMessageType.Data
                        when message is DataDocumentOperationMessage<JsonDocument> msg:

                        var payload = msg.Payload.RootElement;

                        var hasNext = false;
                        var isPatch = payload.TryGetProperty(ResultFields.Path, out _);

                        if (payload.TryGetProperty(HasNext, out var hasNextProp) &&
                            hasNextProp.GetBoolean())
                        {
                            hasNext = true;
                        }

                        yield return new(msg.Payload, null, isPatch, hasNext);
                        break;

                    case OperationMessageType.Error when message is ErrorOperationMessage msg:
                        var operationEx = new GraphQLClientException(msg.Payload);
                        yield return new(CreateBodyFromException(operationEx), operationEx);
                        yield break;

                    case OperationMessageType.Cancelled:
                        var canceledEx = new OperationCanceledException();
                        yield return new(CreateBodyFromException(canceledEx), canceledEx);
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
