using System.Collections.Immutable;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal sealed partial class SubscriptionExecutor(
    ObjectPool<OperationContext> operationContextPool,
    QueryExecutor queryExecutor,
    IErrorHandler errorHandler,
    IExecutionDiagnosticEvents diagnosticEvents,
    ExecutionConcurrencyGate? concurrencyGate,
    IRequestTimeoutOptionsAccessor timeoutOptions)
{
    public async Task<IExecutionResult> ExecuteAsync(
        RequestContext requestContext,
        Func<object?> resolveQueryValue)
    {
        ArgumentNullException.ThrowIfNull(requestContext);

        if (requestContext.VariableValues.Length == 0)
        {
            throw SubscriptionExecutor_ContextInvalidState();
        }

        var operation = requestContext.GetOperation();
        var selectionSet = operation.RootSelectionSet;
        if (selectionSet.Selections.Length != 1)
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
                operationContextPool,
                queryExecutor,
                requestContext,
                operation.RootType,
                selectionSet,
                resolveQueryValue,
                diagnosticEvents,
                concurrencyGate,
                timeoutOptions.ExecutionTimeout)
                .ConfigureAwait(false);

            var response = new ResponseStream(() => subscription.ExecuteAsync());
            response.ContextData = response.ContextData.SetItem(WellKnownContextData.Subscription, subscription);
            response.RegisterForCleanup(subscription);
            return response;
        }
        catch (GraphQLException ex)
        {
            if (subscription is not null)
            {
                await subscription.DisposeAsync().ConfigureAwait(false);
            }

            return new OperationResult([.. ex.Errors]);
        }
        catch (Exception ex)
        {
            var error = errorHandler.Handle(ErrorBuilder.FromException(ex).Build());

            if (subscription is not null)
            {
                await subscription.DisposeAsync().ConfigureAwait(false);
            }

            return new OperationResult(Unwrap(error));
        }

        static ImmutableList<IError> Unwrap(IError error)
        {
            if (error is AggregateError aggregateError)
            {
                return [.. aggregateError.Errors];
            }

            return [error];
        }
    }
}
