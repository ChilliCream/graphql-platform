using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;
using static HotChocolate.AspNetCore.ThrowHelper;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

internal sealed class DataStartMessageHandler : MessageHandler<DataStartMessage>
{
    private readonly IRequestExecutor _requestExecutor;
    private readonly ISocketSessionInterceptor _socketSessionInterceptor;
    private readonly IErrorHandler _errorHandler;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;

    public DataStartMessageHandler(
        IRequestExecutor requestExecutor,
        ISocketSessionInterceptor socketSessionInterceptor,
        IErrorHandler errorHandler,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        _requestExecutor = requestExecutor ??
            throw new ArgumentNullException(nameof(requestExecutor));
        _socketSessionInterceptor = socketSessionInterceptor ??
            throw new ArgumentNullException(nameof(socketSessionInterceptor));
        _errorHandler = errorHandler ??
            throw new ArgumentNullException(nameof(errorHandler));
        _diagnosticEvents = diagnosticEvents ??
            throw new ArgumentNullException(nameof(diagnosticEvents));
    }

    protected override async Task HandleAsync(
        ISocketConnection connection,
        DataStartMessage message,
        CancellationToken cancellationToken)
    {
        var session = new CancellationTokenSource();
        var combined = CancellationTokenSource.CreateLinkedTokenSource(
            session.Token, cancellationToken);
        var sessionIsHandled = false;

        IExecutionResult result = await ExecuteAsync(combined.Token);

        try
        {
            switch (result)
            {
                case SubscriptionResult subscriptionResult:
                    // first we add the cts to the result so that they are disposed when the
                    // subscription is disposed.
                    subscriptionResult.RegisterDisposable(combined);

                    // while a subscription result must be disposed we are not handling it here
                    // and leave this responsibility to the subscription session.
                    ISubscription subscription = GetSubscription(subscriptionResult);

                    var subscriptionSession = new SubscriptionSession(
                        session,
                        _socketSessionInterceptor,
                        connection,
                        subscriptionResult,
                        subscription,
                        _diagnosticEvents,
                        message.Id);

                    connection.Subscriptions.Register(subscriptionSession);
                    sessionIsHandled = true;
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
                    if (result is IDisposable d)
                    {
                        d.Dispose();
                    }
                    throw DataStartMessageHandler_RequestTypeNotSupported();
            }
        }
        finally
        {
            if (!sessionIsHandled)
            {
                session.Dispose();
                combined.Dispose();
            }
        }

        async ValueTask<IExecutionResult> ExecuteAsync(CancellationToken abort)
        {
            try
            {
                IQueryRequestBuilder requestBuilder =
                    QueryRequestBuilder.From(message.Payload)
                        .SetServices(connection.RequestServices);

                await _socketSessionInterceptor.OnRequestAsync(
                    connection, requestBuilder, abort);

                return await _requestExecutor.ExecuteAsync(
                    requestBuilder.Create(), abort);
            }
            catch (Exception ex)
            {
                IErrorBuilder error = _errorHandler.CreateUnexpectedError(ex);
                return QueryResultBuilder.CreateError(error.Build());
            }
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

    private static ISubscription GetSubscription(SubscriptionResult subscriptionResult)
    {
        if (subscriptionResult.ContextData is not null &&
            subscriptionResult.ContextData.TryGetValue(Subscription, out var value) &&
            value is ISubscription subscription)
        {
            return subscription;
        }

        throw new InvalidOperationException(DataStartMessageHandler_Not_A_SubscriptionResult);
    }
}
