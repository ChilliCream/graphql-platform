using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Utilities;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.Utilities.ThrowHelper;

namespace HotChocolate.Execution
{
    internal sealed partial class SubscriptionExecutor
    {
        private readonly ObjectPool<OperationContext> _operationContextPool;
        private readonly QueryExecutor _queryExecutor;
        private readonly IDiagnosticEvents _diagnosticEvents;

        public SubscriptionExecutor(
            ObjectPool<OperationContext> operationContextPool,
            QueryExecutor queryExecutor,
            IDiagnosticEvents diagnosticEvents)
        {
            _operationContextPool = operationContextPool;
            _queryExecutor = queryExecutor;
            _diagnosticEvents = diagnosticEvents;
        }

        public async Task<IExecutionResult> ExecuteAsync(
            IRequestContext requestContext)
        {
            if (requestContext is null)
            {
                throw new ArgumentNullException(nameof(requestContext));
            }

            if (requestContext.Operation is null || requestContext.Variables is null)
            {
                throw SubscriptionExecutor_ContextInvalidState();
            }

            IPreparedSelectionList rootSelections = requestContext.Operation.GetRootSelections();

            if (rootSelections.Count != 1)
            {
                throw SubscriptionExecutor_SubscriptionsMustHaveOneField();
            }

            if (rootSelections[0].Field.SubscribeResolver is null)
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
                    rootSelections,
                    _diagnosticEvents)
                    .ConfigureAwait(false);

                return new SubscriptionResult(
                    subscription.ExecuteAsync,
                    null,
                    session: subscription);
            }
            catch (GraphQLException ex)
            {
                if (subscription is { })
                {
                    await subscription.DisposeAsync().ConfigureAwait(false);
                }
                return new SubscriptionResult(null, ex.Errors);
            }
            catch (Exception ex)
            {
                requestContext.Exception = ex;
                IErrorBuilder errorBuilder = requestContext.ErrorHandler.CreateUnexpectedError(ex);
                IError error = requestContext.ErrorHandler.Handle(errorBuilder.Build());

                if (subscription is { })
                {
                    await subscription.DisposeAsync().ConfigureAwait(false);
                }

                return new SubscriptionResult(null, new[] { error });
            }
        }
    }
}
