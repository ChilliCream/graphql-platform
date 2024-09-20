using HotChocolate.Execution.Instrumentation;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal sealed partial class SubscriptionExecutor
{
    private readonly ObjectPool<OperationContext> _operationContextPool;
    private readonly QueryExecutor _queryExecutor;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;

    public SubscriptionExecutor(
        ObjectPool<OperationContext> operationContextPool,
        QueryExecutor queryExecutor,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        _operationContextPool = operationContextPool;
        _queryExecutor = queryExecutor;
        _diagnosticEvents = diagnosticEvents;
    }

    public async Task<IExecutionResult> ExecuteAsync(
        IRequestContext requestContext,
        Func<object?> resolveQueryValue)
    {
        if (requestContext is null)
        {
            throw new ArgumentNullException(nameof(requestContext));
        }

        if (requestContext.Operation is null || requestContext.Variables is null)
        {
            throw SubscriptionExecutor_ContextInvalidState();
        }

        var selectionSet = requestContext.Operation.RootSelectionSet;

        if (selectionSet.Selections.Count != 1)
        {
            throw SubscriptionExecutor_SubscriptionsMustHaveOneField();
        }

        if (selectionSet.Selections[0].Field.SubscribeResolver is null)
        {
            throw SubscriptionExecutor_NoSubscribeResolver();
        }

        Subscription? subscription = null;

        try
        {
            subscription = await Subscription.SubscribeAsync(
                _operationContextPool,
                _queryExecutor,
                requestContext,
                requestContext.Operation.RootType,
                selectionSet,
                resolveQueryValue,
                _diagnosticEvents)
                .ConfigureAwait(false);

            var response = new ResponseStream(
                () => subscription.ExecuteAsync(),
                contextData: new SingleValueExtensionData(
                    WellKnownContextData.Subscription,
                    subscription));
            response.RegisterForCleanup(subscription);
            return response;
        }
        catch (GraphQLException ex)
        {
            if (subscription is not null)
            {
                await subscription.DisposeAsync().ConfigureAwait(false);
            }

            return new OperationResult(null, ex.Errors);
        }
        catch (Exception ex)
        {
            requestContext.Exception = ex;
            var errorBuilder = requestContext.ErrorHandler.CreateUnexpectedError(ex);
            var error = requestContext.ErrorHandler.Handle(errorBuilder.Build());

            if (subscription is not null)
            {
                await subscription.DisposeAsync().ConfigureAwait(false);
            }

            return new OperationResult(null, Unwrap(error));
        }

        IReadOnlyList<IError> Unwrap(IError error)
        {
            if (error is AggregateError aggregateError)
            {
                return aggregateError.Errors;
            }

            return new[] { error, };
        }
    }
}
