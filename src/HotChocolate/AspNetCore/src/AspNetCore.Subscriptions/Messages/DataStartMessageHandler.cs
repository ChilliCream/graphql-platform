using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ISocketQueryRequestInterceptor[] _requestInterceptors;

        public DataStartMessageHandler(
            IQueryExecutor queryExecutor,
            IEnumerable<ISocketQueryRequestInterceptor> queryRequestInterceptors)
        {
            _queryExecutor = queryExecutor
                ?? throw new ArgumentNullException(nameof(queryExecutor));
            _requestInterceptors = queryRequestInterceptors?.ToArray();
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
                    .SetOperation(message.Payload.OperationName)
                    .SetVariableValues(message.Payload.Variables)
                    .SetProperties(message.Payload.Extensions)
                    .SetServices(connection.RequestServices);

            if (_requestInterceptors != null)
            {
                for (var i = 0; i < _requestInterceptors.Length; i++)
                {
                    await _requestInterceptors[i].OnCreateAsync(
                            connection,
                            requestBuilder,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
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
                        new Subscription(
                            connection,
                            responseStream,
                            message.Id));
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

        private static async Task HandleQueryResultAsync(
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
