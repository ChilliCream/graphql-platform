using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;

namespace HotChocolate.Execution
{
    internal abstract partial class ExecutionStrategyBase
    {
        private static readonly FieldValueCompleter _fieldValueCompleter =
            new FieldValueCompleter();

        protected static async Task<object> ExecuteResolverAsync(
           ResolverTask resolverTask,
           IErrorHandler errorHandler,
           CancellationToken cancellationToken)
        {
            Activity activity = ResolverDiagnosticEvents.BeginResolveField(
                resolverTask.ResolverContext);

            object result = await ExecuteMiddlewareAsync(
                resolverTask, errorHandler).ConfigureAwait(false);

            if (result is IError || result is IEnumerable<IError>)
            {
                activity?.AddTag("error", "true");
            }

            ResolverDiagnosticEvents.EndResolveField(
                activity,
                resolverTask.ResolverContext,
                result);

            return result;
        }

        private static async Task<object> ExecuteMiddlewareAsync(
            ResolverTask resolverTask,
            IErrorHandler errorHandler)
        {
            object result = null;

            try
            {
                if (!resolverTask.FieldSelection.Field.IsIntrospectionField
                    && resolverTask.HasMiddleware)
                {
                    result = await ExecuteDirectiveMiddlewareAsync(
                        resolverTask).ConfigureAwait(false);
                }
                else
                {
                    result = await ExecuteFieldMiddlewareAsync(
                        resolverTask).ConfigureAwait(false);
                }

                if (result is IError error)
                {
                    return errorHandler.Handle(error);
                }
                else if (result is IEnumerable<IError> errors)
                {
                    return errorHandler.Handle(errors);
                }
                else
                {
                    return result;
                }
            }
            catch (QueryException ex)
            {
                return errorHandler.Handle(ex.Errors);
            }
            catch (Exception ex)
            {
                ResolverDiagnosticEvents.ResolverError(
                    resolverTask.ResolverContext,
                    ex);

                return errorHandler.Handle(ex, error => error
                    .WithPath(resolverTask.Path)
                    .WithSyntaxNodes(resolverTask.FieldSelection.Selection));
            }
        }

        private static async Task<object> ExecuteFieldMiddlewareAsync(
            ResolverTask resolverTask)
        {
            if (resolverTask.FieldSelection.Field.Resolver == null)
            {
                return null;
            }

            object result = await resolverTask.FieldSelection.Field.Resolver(
                resolverTask.ResolverContext).ConfigureAwait(false);

            return resolverTask.CompleteResolverResult(result);
        }

        private static async Task<object> ExecuteDirectiveMiddlewareAsync(
            ResolverTask resolverTask)
        {
            return await resolverTask.ExecuteMiddleware.Invoke(
                resolverTask.ResolverContext, ExecuteResolver)
                    .ConfigureAwait(false);

            Task<object> ExecuteResolver()
            {
                return ExecuteFieldMiddlewareAsync(
                    resolverTask);
            }
        }

        protected static void CompleteValue(
            FieldValueCompletionContext completionContext)
        {
            _fieldValueCompleter.CompleteValue(completionContext);
        }
    }
}
