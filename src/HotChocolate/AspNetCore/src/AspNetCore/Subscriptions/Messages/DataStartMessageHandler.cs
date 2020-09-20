using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;
using static HotChocolate.AspNetCore.Utilities.ThrowHelper;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public sealed class DataStartMessageHandler
        : MessageHandler<DataStartMessage>
    {
        private readonly IRequestExecutor _requestExecutor;
        private readonly ISocketSessionInterceptor _socketSessionInterceptor;

        public DataStartMessageHandler(
            IRequestExecutor requestExecutor,
            ISocketSessionInterceptor socketSessionInterceptor)
        {
            _requestExecutor = requestExecutor ??
                throw new ArgumentNullException(nameof(requestExecutor));
            _socketSessionInterceptor = socketSessionInterceptor ??
                throw new ArgumentNullException(nameof(socketSessionInterceptor));
        }

        protected override async Task HandleAsync(
            ISocketConnection connection,
            DataStartMessage message,
            CancellationToken cancellationToken)
        {
            IQueryRequestBuilder requestBuilder =
                QueryRequestBuilder.From(message.Payload)
                    .SetServices(connection.RequestServices);

            await _socketSessionInterceptor.OnRequestAsync(
                connection, requestBuilder, cancellationToken);

            IExecutionResult result = await _requestExecutor.ExecuteAsync(
                requestBuilder.Create(), cancellationToken);

            switch (result)
            {
                case IResponseStream responseStream:
                    var subscription = new Subscription(connection, responseStream, message.Id);
                    connection.Subscriptions.Register(subscription);
                    break;

                case IQueryResult queryResult:
                    using (queryResult)
                        await HandleQueryResultAsync(
                            connection, message, queryResult, cancellationToken);
                    break;

                default:
                    throw DataStartMessageHandler_RequestTypeNotSupported();
            }
        }

        private static async Task HandleQueryResultAsync(
            ISocketConnection connection,
            DataStartMessage message,
            IQueryResult queryResult,
            CancellationToken cancellationToken)
        {
            await connection.SendAsync(
                new DataResultMessage(message.Id, queryResult).Serialize(),
                cancellationToken);

            await connection.SendAsync(
                new DataCompleteMessage(message.Id).Serialize(),
                cancellationToken);
        }
    }
}
