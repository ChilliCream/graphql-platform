using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Pipeline;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal sealed partial class SubscriptionExecutor
{
    private readonly ObjectPool<OperationContext> _operationContextPool;
    private readonly QueryExecutor _queryExecutor;
    private readonly IErrorHandler _errorHandler;
    private readonly IExecutionDiagnosticEvents _diagnosticEvents;

    public SubscriptionExecutor(
        ObjectPool<OperationContext> operationContextPool,
        QueryExecutor queryExecutor,
        IErrorHandler errorHandler,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        _operationContextPool = operationContextPool;
        _queryExecutor = queryExecutor;
        _errorHandler = errorHandler;
        _diagnosticEvents = diagnosticEvents;
    }

    public async Task<IExecutionResult> ExecuteAsync(
        RequestContext requestContext,
        Func<object?> resolveQueryValue)
    {
        ArgumentNullException.ThrowIfNull(requestContext);

        var operationInfo = requestContext.GetOperationInfo();
        if (operationInfo.Operation is null || requestContext.VariableValues.Length == 0)
        {
            throw SubscriptionExecutor_ContextInvalidState();
        }

        var selectionSet = operationInfo.Operation.RootSelectionSet;
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
                operationInfo.Operation.RootType,
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
            var error = _errorHandler.Handle(ErrorBuilder.FromException(ex).Build());

            if (subscription is not null)
            {
                await subscription.DisposeAsync().ConfigureAwait(false);
            }

            return new OperationResult(null, Unwrap(error));
        }

        static IReadOnlyList<IError> Unwrap(IError error)
        {
            if (error is AggregateError aggregateError)
            {
                return aggregateError.Errors;
            }

            return [error];
        }
    }
}
