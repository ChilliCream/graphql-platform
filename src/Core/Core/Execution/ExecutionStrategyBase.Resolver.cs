using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

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
                resolverTask, errorHandler);

            // TODO : ErrorFilter
            if (result is IError error)
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
                        resolverTask);
                }
                else
                {
                    result = await ExecuteFieldMiddlewareAsync(
                        resolverTask);
                }
            }
            // TODO : ErrorFilter
            catch (QueryException ex)
            {
                result = ex.Errors;
            }
            catch (Exception ex)
            {
                ResolverDiagnosticEvents.ResolverError(
                    resolverTask.ResolverContext,
                    ex);

                result = errorHandler.Handle(ex);
            }

            return result;
        }

        private static async Task<object> ExecuteFieldMiddlewareAsync(
            ResolverTask resolverTask)
        {
            if (resolverTask.FieldSelection.Field.Resolver == null)
            {
                return null;
            }

            object result = await resolverTask.FieldSelection.Field.Resolver(
                resolverTask.ResolverContext);

            return resolverTask.CompleteResolverResult(result);
        }

        private static async Task<object> ExecuteDirectiveMiddlewareAsync(
            ResolverTask resolverTask)
        {
            return await resolverTask.ExecuteMiddleware.Invoke(
                resolverTask.ResolverContext, ExecuteResolver);

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
