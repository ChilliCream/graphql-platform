using System;
using System.Buffers;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using StrawberryShake;
using StrawberryShake.Transport.WebSockets;
using StrawberryShake.Transport.WebSockets.Messages;
using static HotChocolate.Language.Utf8GraphQLRequestParser;
using static HotChocolate.Stitching.WellKnownContextData;

namespace HotChocolate.Stitching.Execution;

internal sealed class SubscriptionRequestHandler : IRemoteRequestHandler
{
    private readonly ISocketClientFactory _clientFactory;
    private readonly NameString _targetSchema;

    public SubscriptionRequestHandler(
        ISocketClientFactory clientFactory,
        NameString targetSchema)
    {
        _clientFactory = clientFactory;
        _targetSchema = targetSchema;
    }

    public bool CanHandle(IQueryRequest request)
        => request.ContextData?.ContainsKey(IsSubscription) ?? false;

    public Task<IExecutionResult> ExecuteAsync(
        IQueryRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IExecutionResult>(
            new SubscriptionResult(
                () => new Subscription(
                    _clientFactory,
                    _targetSchema,
                    CreateRequest(request)),
                Array.Empty<IError>()));

    private static OperationRequest CreateRequest(IQueryRequest queryRequest)
    {
        var document = new RequestDocument(queryRequest.Query!);
        return new OperationRequest(
            queryRequest.OperationName!,
            document,
            new Dictionary<string, object?>());
    }

    private sealed class Subscription : IAsyncEnumerable<IQueryResult>
    {
        private readonly ISocketClientFactory _clientFactory;
        private readonly NameString _targetSchema;
        private readonly OperationRequest _request;

        public Subscription(
            ISocketClientFactory clientFactory,
            NameString targetSchema,
            OperationRequest request)
        {
            _clientFactory = clientFactory;
            _targetSchema = targetSchema;
            _request = request;
        }

        public async IAsyncEnumerator<IQueryResult> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            await using ISocketClient client = _clientFactory.CreateClient(_targetSchema);
            await using ISession session = new Session(client);

            await session.OpenSessionAsync(cancellationToken).ConfigureAwait(false);

            await using ISocketOperation operation =
                await session.StartOperationAsync(_request, cancellationToken)
                    .ConfigureAwait(false);

            await foreach (OperationMessage message in
                operation.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                switch (message.Type)
                {
                    case OperationMessageType.Data
                        when message is DataDocumentOperationMessage<JsonDocument> msg:

                        IReadOnlyDictionary<string, object?> result =
                            ParseResponse(msg.Payload.RootElement.GetRawText())!;

                        yield return HttpResponseDeserializer.Deserialize(result);
                        break;

                    case OperationMessageType.Error when message is ErrorOperationMessage msg:
                        var error = new Error(msg.Message);
                        yield return QueryResultBuilder.CreateError(error);
                        yield break;

                    case OperationMessageType.Cancelled:
                    case OperationMessageType.Complete:
                        yield break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    private sealed class RequestDocument : IDocument
    {
        private readonly IQuery _query;
        private DocumentHash? _hash;

        public RequestDocument(IQuery query)
        {
            _query = query;
        }

        public OperationKind Kind => OperationKind.Subscription;

        public ReadOnlySpan<byte> Body => _query.AsSpan();

        public DocumentHash Hash
        {
            get
            {
                if (!_hash.HasValue)
                {
                    var span = Body;
                    var buffer = ArrayPool<byte>.Shared.Rent(span.Length);
                    using var md5 = MD5.Create();
                    var hash = md5.ComputeHash(buffer, 0, span.Length);
                    ArrayPool<byte>.Shared.Return(buffer);
                    _hash = new DocumentHash("md5Hash", Encoding.UTF8.GetString(hash, 0, 1));
                }

                return _hash.Value;
            }
        }
    }
}
