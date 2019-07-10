using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public sealed class DataStartMessageHandler
        : MessageHandler<DataStartMessage>
    {
        private readonly IQueryExecutor _queryExecutor;
        private readonly ISocketQueryRequestInterceptor _requestInterceptor;


        public DataStartMessageHandler(
            IQueryExecutor queryExecutor,
            ISocketQueryRequestInterceptor queryRequestInterceptor)
        {
            _queryExecutor = queryExecutor
                ?? throw new ArgumentNullException(nameof(queryExecutor));
            _requestInterceptor = queryRequestInterceptor;
        }

        protected override async Task HandleAsync(
            ISocketConnection connection,
            DataStartMessage message,
            CancellationToken cancellationToken)
        {
            IQueryRequestBuilder requestBuilder =
                QueryRequestBuilder.New()
                    .SetQuery(message.Payload.Query)
                    .SetQueryName(message.Payload.QueryName)
                    .SetQueryHash(message.Payload.QueryName)
                    .SetOperation(message.Payload.OperationName)
                    .SetVariableValues(message.Payload.Variables)
                    .SetProperties(message.Payload.Extensions)
                    .SetServices(connection.RequestServices);

            if (_requestInterceptor == null)
            {
                await _requestInterceptor.OnCreateAsync(
                    connection,
                    requestBuilder,
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            IExecutionResult result =
                await _queryExecutor.ExecuteAsync(
                    requestBuilder.Create(),
                    cancellationToken)
                    .ConfigureAwait(false);

            switch (result)
            {
                case IResponseStream responseStream:
                    connection.Subscriptions.Register(
                        message.Id,
                        responseStream);
                    break;

                case IReadOnlyQueryResult queryResult:
                    await HandleQueryResultAsync(
                        connection,
                        message,
                        queryResult,
                        cancellationToken)
                        .ConfigureAwait(false);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        private async Task HandleQueryResultAsync(
            ISocketConnection connection,
            DataStartMessage message,
            IReadOnlyQueryResult queryResult,
            CancellationToken cancellationToken)
        {
            await connection.SendAsync(
                new DataResultMessage(message.Id, queryResult).Serialize(),
                cancellationToken)
                .ConfigureAwait(false);

            await connection.SendAsync(
                new DataCompleteMessage(message.Id).Serialize(),
                cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
