using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Resolvers;

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
            Activity activity = DiagnosticEvents.BeginResolveField(
                resolverTask.ResolverContext);

            object result = await ExecuteMiddlewareAsync(
                resolverTask,
                errorHandler)
                    .ConfigureAwait(false);

            if (result is IError || result is IEnumerable<IError>)
            {
                activity?.AddTag("error", "true");
            }

            DiagnosticEvents.EndResolveField(
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
                result = await ExecuteFieldMiddlewareAsync(resolverTask)
                    .ConfigureAwait(false);

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
                DiagnosticEvents.ResolverError(resolverTask.ResolverContext,
                    ex);

                return errorHandler.Handle(ex, error => error
                    .WithPath(resolverTask.Path)
                    .WithSyntaxNodes(resolverTask.FieldSelection.Selection));
            }
        }

        private static async Task<object> ExecuteFieldMiddlewareAsync(
            ResolverTask resolverTask)
        {
            var middlewareContext = new MiddlewareContext
            (
                resolverTask.ResolverContext,
                () => resolverTask.FieldSelection.Field
                    .Resolver?.Invoke(resolverTask.ResolverContext)
                        ?? Task.FromResult<object>(null),
                result => resolverTask.CompleteResolverResult(result)
            );

            await resolverTask.FieldDelegate.Invoke(middlewareContext)
                .ConfigureAwait(false);

            return middlewareContext.Result;
        }

        protected static void CompleteValue(
            FieldValueCompletionContext completionContext)
        {
            _fieldValueCompleter.CompleteValue(completionContext);
        }
    }
}
