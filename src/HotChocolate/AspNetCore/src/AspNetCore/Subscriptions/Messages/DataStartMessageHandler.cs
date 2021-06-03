using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Properties;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using static HotChocolate.AspNetCore.ThrowHelper;
using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public sealed class DataStartMessageHandler
        : MessageHandler<DataStartMessage>
    {
        private readonly IRequestExecutor _requestExecutor;
        private readonly ISocketSessionInterceptor _socketSessionInterceptor;
        private readonly IDiagnosticEvents _diagnosticEvents;

        public DataStartMessageHandler(
            IRequestExecutor requestExecutor,
            ISocketSessionInterceptor socketSessionInterceptor,
            IDiagnosticEvents diagnosticEvents)
        {
            _requestExecutor = requestExecutor ??
                throw new ArgumentNullException(nameof(requestExecutor));
            _socketSessionInterceptor = socketSessionInterceptor ??
                throw new ArgumentNullException(nameof(socketSessionInterceptor));
            _diagnosticEvents = diagnosticEvents ??
                throw new ArgumentNullException(nameof(diagnosticEvents));
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
                case ISubscriptionResult subscriptionResult:
                    // while a subscription result must be disposed we are not handling it here
                    // and leave this responsibility to the subscription session.
                    ISubscription subscription = GetSubscription(result);

                    var subscriptionSession = new SubscriptionSession(
                        connection,
                        subscriptionResult,
                        subscription,
                        _diagnosticEvents,
                        message.Id);

                    connection.Subscriptions.Register(subscriptionSession);
                    break;

                case IResponseStream streamResult:
                    // stream results represent deferred execution streams that use execution
                    // resources. We need to ensure that these are disposed when we are
                    // finished.
                    await using (streamResult)
                    {
                        await HandleStreamResultAsync(
                            connection,
                            message,
                            streamResult,
                            cancellationToken);
                    }
                    break;

                case IQueryResult queryResult:
                    // query results use pooled memory an need to be disposed after we have
                    // used them.
                    using (queryResult)
                    {
                        await HandleQueryResultAsync(
                            connection,
                            message,
                            queryResult,
                            cancellationToken);
                    }
                    break;

                default:
                    throw DataStartMessageHandler_RequestTypeNotSupported();
            }
        }

        private static async Task HandleStreamResultAsync(
            ISocketConnection connection,
            DataStartMessage message,
            IResponseStream responseStream,
            CancellationToken cancellationToken)
        {
            await foreach (IQueryResult queryResult in responseStream.ReadResultsAsync()
                .WithCancellation(cancellationToken))
            {
                await connection.SendAsync(
                    new DataResultMessage(message.Id, queryResult).Serialize(),
                    cancellationToken);
            }

            await connection.SendAsync(
                new DataCompleteMessage(message.Id).Serialize(),
                cancellationToken);
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

        private ISubscription GetSubscription(IExecutionResult result)
        {
            if (result.ContextData is not null &&
                result.ContextData.TryGetValue(WellKnownContextData.Subscription, out var value) &&
                value is ISubscription subscription)
            {
                return subscription;
            }

            throw new InvalidOperationException(DataStartMessageHandler_Not_A_SubscriptionResult);
        }
    }
}
